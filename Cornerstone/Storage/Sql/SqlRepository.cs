#region References

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Cornerstone.Reflection;
using Cornerstone.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

#endregion

namespace Cornerstone.Storage.Sql;

public enum SqlProvider
{
	SqlServer,
	Sqlite
}

public class SqlDatabase : IDisposable
{
	#region Fields

	private readonly DbConnection _connection;

	#endregion

	#region Constructors

	public SqlDatabase(string connectionString, SqlProvider provider)
	{
		ConnectionString = connectionString;
		Provider = provider;

		if ((provider == SqlProvider.Sqlite)
			&& ConnectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
		{
			// This connection keeps the :memory: DB alive until this database is disposed.
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public SqlRepository<T> GetRepository<T>() where T : Entity, new()
	{
		return new SqlRepository<T>(ConnectionString, Provider);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		_connection?.Close();
		_connection?.Dispose();
	}

	#endregion
}

public class SqlRepository<T>
	: IRepository<T>
	where T : Entity, new()
{
	#region Fields

	private readonly SourceTypeInfo _sourceTypeInfo;

	#endregion

	#region Constructors

	public SqlRepository(string connectionString, SqlProvider provider)
	{
		_sourceTypeInfo = SourceReflector.GetRequiredSourceType<T>();

		ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		Provider = provider;
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	public void Add(T entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var (sql, values) = SqlGenerator.GetInsertQuery(entity, Provider);
		using var connection = CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;

		if (values != null)
		{
			foreach (var kv in values)
			{
				var param = command.CreateParameter();
				param.ParameterName = kv.Key;
				param.Value = kv.Value ?? DBNull.Value;
				param.DbType = GetParameterType(param.Value);

				command.Parameters.Add(param);
			}
		}

		var rows = command.ExecuteNonQuery();
		if (rows != 1)
		{
			throw new InvalidOperationException("Add failed");
		}
	}

	/// <summary>
	/// Creates the table if it does not already exist
	/// </summary>
	public void EnsureTableCreated()
	{
		var sql = SqlGenerator.GetCreateTableScript(_sourceTypeInfo, Provider);
		using var connection = CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	public SqlQuery<T> Where(Func<T, bool> predicate)
	{
		return new SqlQuery<T>(ConnectionString, Provider);
	}

	private DbConnection CreateConnection()
	{
		return Provider == SqlProvider.SqlServer
			? new SqlConnection(ConnectionString)
			: new SqliteConnection(ConnectionString);
	}

	private DbType GetParameterType(object value)
	{
		return value switch
		{
			string => DbType.String,
			int => DbType.Int32,
			uint => DbType.UInt32,
			long => DbType.Int64,
			ulong => DbType.UInt64,
			DateTime => DbType.DateTime2,
			Guid => DbType.Guid,
			bool => DbType.Boolean,
			_ => throw new NotSupportedException($"Parameter type {value.GetType().FullName} value not supported.")
		};
	}

	#endregion
}

public class SqlQuery<T>
	where T : class, new()
{
	#region Fields

	private readonly string _connectionString;
	private readonly List<(LambdaExpression KeySelector, bool Descending)> _orderings;
	private readonly SourceTypeInfo _sourceType;
	private readonly List<LambdaExpression> _wherePredicates;

	#endregion

	#region Constructors

	public SqlQuery(string connectionString, SqlProvider provider)
	{
		Provider = provider;

		_connectionString = connectionString;
		_orderings = [];
		_wherePredicates = [];
		_sourceType = SourceReflector.GetRequiredSourceType<T>();
	}

	#endregion

	#region Properties

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	public SqlQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		_orderings.Add((keySelector, false));
		return this;
	}

	public SqlQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		_orderings.Add((keySelector, true));
		return this;
	}

	public IEnumerable<T> Query()
	{
		return Provider == SqlProvider.SqlServer
			? QuerySqlServer()
			: QuerySqlite();
	}

	public SqlQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		if (_orderings.Count == 0)
		{
			throw new InvalidOperationException("ThenBy can only be used after OrderBy");
		}
		_orderings.Add((keySelector, false));
		return this;
	}

	public SqlQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		if (_orderings.Count == 0)
		{
			throw new InvalidOperationException("ThenBy can only be used after OrderBy");
		}
		_orderings.Add((keySelector, true));
		return this;
	}

	public static (string Sql, object[] Parameters) ToSqlQuery(SqlQuery<T> query)
	{
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		var parameters = new List<object>();

		builder.Append("SELECT ");

		var ps = query._sourceType.GetProperties();
		var first = true;
		for (var index = 0; index < ps.Length; index++)
		{
			if (!first)
			{
				builder.Append(", ");
			}
			var p = ps[index];
			builder.Append('[');
			builder.Append(p.Name);
			builder.Append(']');
			first = false;
		}

		builder.Append($" FROM [{query._sourceType.Name}s]");

		if (query._wherePredicates.Count > 0)
		{
			builder.Append(" WHERE ");
			var parameterIndex = 0;

			for (var i = 0; i < query._wherePredicates.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(" AND ");
				}

				var visitor = new PredicateToSqlVisitor(parameterIndex);
				var (whereSql, whereParams) = visitor.Translate(query._wherePredicates[i]);
				parameterIndex += whereParams.Length;

				builder.Append('(');
				builder.Append(whereSql);
				builder.Append(')');
				parameters.AddRange(whereParams);
			}
		}

		if (query._orderings.Count > 0)
		{
			builder.Append(" ORDER BY ");

			for (var i = 0; i < query._orderings.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				var (selector, desc) = query._orderings[i];
				var orderVisitor = new OrderByExpressionVisitor();
				var columnSql = orderVisitor.Translate(selector);
				builder.Append(columnSql);

				if (desc)
				{
					builder.Append(" DESC");
				}
			}
		}

		return (builder.ToString().Trim(), parameters.ToArray());
	}

	public SqlQuery<T> Where(Expression<Func<T, bool>> predicate)
	{
		_wherePredicates.Add(predicate);
		return this;
	}

	private object ConvertTo(object dbValue, Type targetType)
	{
		if ((dbValue == null)
			|| (dbValue == DBNull.Value))
		{
			return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
		}

		if (targetType.IsInstanceOfType(dbValue))
		{
			return dbValue;
		}

		var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

		try
		{
			if (underlyingType == typeof(string))
			{
				return dbValue.ToString();
			}
			if (underlyingType == typeof(bool))
			{
				return Convert.ToBoolean(dbValue);
			}
			if (underlyingType == typeof(int))
			{
				return Convert.ToInt32(dbValue);
			}
			if (underlyingType == typeof(long))
			{
				return Convert.ToInt64(dbValue);
			}
			if (underlyingType == typeof(short))
			{
				return Convert.ToInt16(dbValue);
			}
			if (underlyingType == typeof(byte))
			{
				return Convert.ToByte(dbValue);
			}
			if (underlyingType == typeof(uint))
			{
				return Convert.ToUInt32(dbValue);
			}
			if (underlyingType == typeof(ulong))
			{
				return Convert.ToUInt64(dbValue);
			}
			if (underlyingType == typeof(ushort))
			{
				return Convert.ToUInt16(dbValue);
			}
			if (underlyingType == typeof(sbyte))
			{
				return Convert.ToSByte(dbValue);
			}
			if (underlyingType == typeof(double))
			{
				return Convert.ToDouble(dbValue);
			}
			if (underlyingType == typeof(float))
			{
				return Convert.ToSingle(dbValue);
			}
			if (underlyingType == typeof(decimal))
			{
				return Convert.ToDecimal(dbValue);
			}
			if (underlyingType == typeof(DateTime))
			{
				// SQLite usually returns TEXT or REAL (Unix time)
				if (dbValue is string str)
				{
					if (DateTime.TryParse(str, out var dt))
					{
						return dt;
					}
					if (long.TryParse(str, out var unix))
					{
						return DateTime.UnixEpoch.AddSeconds(unix);
					}
				}

				// julian day or unix timestamp as real
				if (dbValue is double dbl)
				{
					return DateTime.UnixEpoch.AddSeconds(dbl);
				}
				return Convert.ToDateTime(dbValue);
			}

			if (underlyingType == typeof(DateTimeOffset))
			{
				if (dbValue is string s
					&& DateTimeOffset.TryParse(s, out var dto))
				{
					return dto;
				}
				if (dbValue is DateTime dt)
				{
					return new DateTimeOffset(dt);
				}
				return Convert.ToDateTime(dbValue);
			}

			if (underlyingType == typeof(TimeSpan))
			{
				if (dbValue is long ticks)
				{
					return new TimeSpan(ticks);
				}
				if (dbValue is string s && TimeSpan.TryParse(s, out var ts))
				{
					return ts;
				}
				return TimeSpan.FromTicks(Convert.ToInt64(dbValue));
			}
			if (underlyingType == typeof(Guid))
			{
				if (dbValue is string s)
				{
					return Guid.Parse(s);
				}
				if (dbValue is byte[] bytes)
				{
					return new Guid(bytes);
				}
				throw new InvalidCastException($"Cannot convert {dbValue.GetType()} to Guid");
			}
			if (underlyingType == typeof(byte[]))
			{
				if (dbValue is byte[] arr)
				{
					return arr;
				}
				if (dbValue is string s)
				{
					return Convert.FromBase64String(s);
				}
				throw new InvalidCastException("Expected byte[] or base64 string");
			}
			if (underlyingType.IsEnum)
			{
				if (dbValue is string str)
				{
					return Enum.Parse(underlyingType, str, true);
				}

				var numeric = Convert.ToInt64(dbValue);
				if (Enum.IsDefined(underlyingType, numeric))
				{
					return Enum.ToObject(underlyingType, numeric);
				}

				throw new InvalidCastException($"Value {numeric} is not defined in enum {underlyingType.Name}");
			}

			return Convert.ChangeType(dbValue, underlyingType, CultureInfo.InvariantCulture);
		}
		catch (Exception ex)
			when (ex is InvalidCastException
					or FormatException
					or OverflowException)
		{
			throw new InvalidCastException($"Cannot convert database value '{dbValue}' (type: {dbValue.GetType().Name}) to property type {targetType.Name}", ex);
		}
	}

	private IEnumerable<T> QuerySqlite()
	{
		using var connection = new SqliteConnection(_connectionString);
		connection.Open();

		var (query, parameters) = ToSqlQuery(this);
		using var command = new SqliteCommand(query, connection);

		for (var i = 0; i < parameters.Length; i++)
		{
			command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
		}

		Debug.WriteLine(query);

		return ReadResults(command);
	}

	private IEnumerable<T> QuerySqlServer()
	{
		using var connection = new SqlConnection(_connectionString);
		connection.Open();

		var (query, parameters) = ToSqlQuery(this);
		using var command = new SqlCommand(query, connection);

		for (var i = 0; i < parameters.Length; i++)
		{
			command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
		}

		return ReadResults(command);
	}

	private IEnumerable<T> ReadResults(DbCommand command)
	{
		var results = new List<T>();
		using var reader = command.ExecuteReader();

		while (reader.Read())
		{
			var item = new T();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var name = reader.GetName(i);
				var prop = _sourceType.GetProperty(name);
				var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
				prop.SetValue(item, ConvertTo(value, prop.PropertyInfo.PropertyType));
			}
			results.Add(item);
		}

		return results.ToArray();
	}

	#endregion
}