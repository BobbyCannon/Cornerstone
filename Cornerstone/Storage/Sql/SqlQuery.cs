#region References

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Cornerstone.Reflection;
using Cornerstone.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

#endregion

namespace Cornerstone.Storage.Sql;

public class SqlQuery<T> : SqlQuery
	where T : Entity, new()
{
	#region Fields

	private readonly string _connectionString;
	private readonly SqlDatabase _database;
	private readonly List<(LambdaExpression KeySelector, bool Descending)> _orderings;
	private int _skip;
	private readonly SourceTypeInfo _sourceType;
	private int? _take;
	private readonly List<LambdaExpression> _wherePredicates;

	#endregion

	#region Constructors

	public SqlQuery(SqlDatabase database)
		: this(database.ConnectionString, database.Provider)
	{
		_database = database;
	}

	public SqlQuery(string connectionString, SqlProvider provider)
	{
		Provider = provider;

		_connectionString = connectionString;
		_database = null;
		_orderings = [];
		_skip = 0;
		_wherePredicates = [];
		_sourceType = SourceReflector.GetRequiredSourceType<T>();
		_take = null;
	}

	#endregion

	#region Properties

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	public bool Any()
	{
		var (sql, parameters) = ToExistsSqlQuery(this);
		return System.Convert.ToInt32(ExecuteScalar(sql, parameters)) != 0;
	}

	public int Count()
	{
		var (sql, parameters) = ToCountSqlQuery(this);
		return System.Convert.ToInt32(ExecuteScalar(sql, parameters));
	}

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
		var (sql, parameters) = ToSqlQuery(this);
		if (_database != null)
		{
			return _database.ExecuteOnCommand(sql, command => AddParameters(command, parameters), ReadResults);
		}

		return Provider == SqlProvider.SqlServer
			? QuerySqlServer(sql, parameters)
			: QuerySqlite(sql, parameters);
	}

	public SqlQuery<T> Skip(int count)
	{
		_skip = count;
		return this;
	}

	public SqlQuery<T> Take(int count)
	{
		_take = count;
		return this;
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

		var (open, close) = SqlGenerator.GetIdentifierBrackets(query.Provider);
		var columns = SqlGenerator.GetExpectedTableInfo(typeof(T)).Columns;
		if (columns.Count == 0)
		{
			throw new InvalidOperationException(
				$"No mapped SQL columns for '{typeof(T).Name}'. Add [SqlTable] / [SqlTableColumn] so SELECT cannot emit non-column properties.");
		}

		for (var index = 0; index < columns.Count; index++)
		{
			if (index > 0)
			{
				builder.Append(", ");
			}

			builder.Append(open);
			builder.Append(columns[index].Name);
			builder.Append(close);
		}

		AppendFromAndWhere(builder, query, parameters);

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
				var orderVisitor = new OrderByExpressionVisitor(query.Provider);
				var columnSql = orderVisitor.Translate(selector);
				builder.Append(columnSql);

				if (desc)
				{
					builder.Append(" DESC");
				}
			}
		}

		AppendSkipTake(builder, query);

		return (builder.ToString().Trim(), parameters.ToArray());
	}

	public SqlQuery<T> Where(Expression<Func<T, bool>> predicate)
	{
		_wherePredicates.Add(predicate);
		return this;
	}

	private static void AddParameters(DbCommand command, object[] parameters)
	{
		for (var i = 0; i < parameters.Length; i++)
		{
			var param = command.CreateParameter();
			param.ParameterName = $"@p{i}";
			param.Value = parameters[i] ?? DBNull.Value;
			if (parameters[i] != null)
			{
				param.DbType = SqlGenerator.GetParameterDbType(parameters[i].GetType());
			}
			command.Parameters.Add(param);
		}
	}

	private static void AppendFromAndWhere(StringBuilder builder, SqlQuery<T> query, List<object> parameters)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(query.Provider);
		builder.Append($" FROM {open}{SqlGenerator.GetTableName(query._sourceType)}{close}");

		if (query._wherePredicates.Count <= 0)
		{
			return;
		}

		builder.Append(" WHERE ");
		var parameterIndex = 0;

		for (var i = 0; i < query._wherePredicates.Count; i++)
		{
			if (i > 0)
			{
				builder.Append(" AND ");
			}

			var visitor = new PredicateToSqlVisitor(query.Provider, parameterIndex);
			var (whereSql, whereParams) = visitor.Translate(query._wherePredicates[i]);
			parameterIndex += whereParams.Length;

			builder.Append('(');
			builder.Append(whereSql);
			builder.Append(')');
			parameters.AddRange(whereParams);
		}
	}

	private static void AppendSkipTake(StringBuilder builder, SqlQuery<T> query)
	{
		if ((query._skip <= 0) && (query._take == null))
		{
			return;
		}

		if (query.Provider == SqlProvider.SqlServer)
		{
			if (query._orderings.Count == 0)
			{
				throw new InvalidOperationException("SQL Server OFFSET/FETCH requires ORDER BY.");
			}

			builder.Append(" OFFSET ");
			builder.Append(query._skip);
			builder.Append(" ROWS");
			if (query._take != null)
			{
				builder.Append(" FETCH NEXT ");
				builder.Append(query._take.Value);
				builder.Append(" ROWS ONLY");
			}

			return;
		}

		builder.Append(" LIMIT ");
		builder.Append(query._take ?? -1);
		if (query._skip > 0)
		{
			builder.Append(" OFFSET ");
			builder.Append(query._skip);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Column CLR types come from generated source reflection property maps.")]
	private object ConvertTo(object dbValue, Type targetType)
	{
		if ((dbValue == null)
			|| (dbValue == DBNull.Value))
		{
			if (Nullable.GetUnderlyingType(targetType) != null)
			{
				return null;
			}

			return targetType.IsValueType ? SourceReflector.CreateInstance(targetType) : null;
		}

		var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

		// Most columns can be returned as-is when the reader already produced the
		// target type. DateTime cannot: the database does not store Kind, so the
		// value is Unspecified. The DateTime branch below sets Kind to UTC.
		if (targetType.IsInstanceOfType(dbValue) && (underlyingType != typeof(DateTime)))
		{
			return dbValue;
		}

		if (Converters.TryGetValue(underlyingType, out var converter))
		{
			return converter(dbValue);
		}

		// fall through to DateTime, Guid, enum, etc. handling

		try
		{
			if (underlyingType == typeof(string))
			{
				return dbValue.ToString();
			}
			if (underlyingType == typeof(bool))
			{
				return System.Convert.ToBoolean(dbValue);
			}
			if (underlyingType == typeof(int))
			{
				return System.Convert.ToInt32(dbValue);
			}
			if (underlyingType == typeof(long))
			{
				return System.Convert.ToInt64(dbValue);
			}
			if (underlyingType == typeof(short))
			{
				return System.Convert.ToInt16(dbValue);
			}
			if (underlyingType == typeof(byte))
			{
				return System.Convert.ToByte(dbValue);
			}
			if (underlyingType == typeof(uint))
			{
				return System.Convert.ToUInt32(dbValue);
			}
			if (underlyingType == typeof(ulong))
			{
				return System.Convert.ToUInt64(dbValue);
			}
			if (underlyingType == typeof(ushort))
			{
				return System.Convert.ToUInt16(dbValue);
			}
			if (underlyingType == typeof(sbyte))
			{
				return System.Convert.ToSByte(dbValue);
			}
			if (underlyingType == typeof(double))
			{
				return System.Convert.ToDouble(dbValue);
			}
			if (underlyingType == typeof(float))
			{
				return System.Convert.ToSingle(dbValue);
			}
			if (underlyingType == typeof(decimal))
			{
				return System.Convert.ToDecimal(dbValue);
			}
			if (underlyingType == typeof(DateTime))
			{
				if (dbValue is DateTime dateTime)
				{
					return ToUtcDateTime(dateTime);
				}

				// SQLite usually returns TEXT or REAL (Unix time)
				if (dbValue is string str)
				{
					if (DateTime.TryParse(str, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
					{
						return ToUtcDateTime(dt);
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
				return ToUtcDateTime(System.Convert.ToDateTime(dbValue));
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
				return System.Convert.ToDateTime(dbValue);
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
				return TimeSpan.FromTicks(System.Convert.ToInt64(dbValue));
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
					return System.Convert.FromBase64String(s);
				}
				throw new InvalidCastException("Expected byte[] or base64 string");
			}
			if (underlyingType.IsEnum)
			{
				return ConvertToEnum(underlyingType, dbValue);
			}

			return System.Convert.ChangeType(dbValue, underlyingType, CultureInfo.InvariantCulture);
		}
		catch (Exception ex)
			when (ex is InvalidCastException
					or FormatException
					or OverflowException)
		{
			throw new InvalidCastException($"Cannot convert database value '{dbValue}' (type: {dbValue.GetType().Name}) to property type {targetType.Name}", ex);
		}
	}

	private static object ConvertToEnum(Type enumType, object dbValue)
	{
		if ((dbValue == null)
			|| (dbValue == DBNull.Value))
		{
			return null;
		}

		var underlyingType = Enum.GetUnderlyingType(enumType);

		// 1. If the DB returned a string (e.g. you stored enum names as TEXT/VARCHAR)
		if (dbValue is string strValue)
		{
			return Enum.Parse(enumType, strValue, true);
		}

		// 2. Numeric value from database, convert to the exact underlying type first
		try
		{
			// Convert to the enum's actual underlying type (byte, int, long, ulong, etc.)
			var converted = System.Convert.ChangeType(dbValue, underlyingType);

			// For safety: check if the value is defined (skip for Flags enums if you allow any bit combination)
			if (!enumType.IsDefined(typeof(FlagsAttribute), false))
			{
				if (!Enum.IsDefined(enumType, converted))
				{
					throw new InvalidCastException($"Value '{converted}' is not defined in enum {enumType.Name}");
				}
			}

			return Enum.ToObject(enumType, converted);
		}
		catch (Exception ex)
		{
			throw new InvalidCastException($"Cannot convert value '{dbValue}' (type {dbValue.GetType().Name}) to enum {enumType.Name}", ex);
		}
	}

	private object ExecuteScalar(string sql, object[] parameters)
	{
		if (_database != null)
		{
			return _database.ExecuteScalar(sql, command => AddParameters(command, parameters));
		}

		if (Provider == SqlProvider.SqlServer)
		{
			using var connection = new SqlConnection(_connectionString);
			connection.Open();
			using var command = new SqlCommand(sql, connection);
			AddParameters(command, parameters);
			return command.ExecuteScalar();
		}

		using var sqliteConnection = new SqliteConnection(_connectionString);
		sqliteConnection.Open();
		using var sqliteCommand = new SqliteCommand(sql, sqliteConnection);
		AddParameters(sqliteCommand, parameters);
		return sqliteCommand.ExecuteScalar();
	}

	private IEnumerable<T> QuerySqlServer(string sql, object[] parameters)
	{
		using var connection = new SqlConnection(_connectionString);
		connection.Open();
		using var command = new SqlCommand(sql, connection);
		AddParameters(command, parameters);
		return ReadResults(command);
	}

	private IEnumerable<T> QuerySqlite(string sql, object[] parameters)
	{
		using var connection = new SqliteConnection(_connectionString);
		connection.Open();
		using var command = new SqliteCommand(sql, connection);
		AddParameters(command, parameters);
		return ReadResults(command);
	}

	private object ReadColumn(DbDataReader reader, int ordinal, Type targetType, Type fieldType)
	{
		if (reader.IsDBNull(ordinal))
		{
			return ConvertTo(null, targetType);
		}

		var destinationType = Nullable.GetUnderlyingType(targetType) ?? targetType;
		var expectedFieldType = destinationType.IsEnum
			? Enum.GetUnderlyingType(destinationType)
			: destinationType;

		if (fieldType == expectedFieldType)
		{
			var raw = ReadTyped(reader, ordinal, fieldType);
			if (destinationType.IsEnum)
			{
				return Enum.ToObject(destinationType, raw);
			}

			return raw;
		}

		if (fieldType == typeof(string))
		{
			return ConvertTo(reader.GetString(ordinal), targetType);
		}

		return ConvertTo(reader.GetValue(ordinal), targetType);
	}

	private IEnumerable<T> ReadResults(DbCommand command)
	{
		var results = new List<T>();
		using var reader = command.ExecuteReader();

		// Build column-to-property map once
		var fieldCount = reader.FieldCount;
		var propertyMap = new SourcePropertyInfo[fieldCount];
		var typeMap = new Type[fieldCount];
		for (var i = 0; i < fieldCount; i++)
		{
			var prop = _sourceType.GetProperty(reader.GetName(i));
			propertyMap[i] = prop;
			typeMap[i] = prop.PropertyInfo.PropertyType;
		}

		var fieldTypes = new Type[fieldCount];
		for (var i = 0; i < fieldCount; i++)
		{
			fieldTypes[i] = reader.GetFieldType(i);
		}

		while (reader.Read())
		{
			var item = new T();
			for (var i = 0; i < fieldCount; i++)
			{
				propertyMap[i].SetValue(item, ReadColumn(reader, i, typeMap[i], fieldTypes[i]));
			}

			results.Add(item);
		}

		return results;
	}

	private static object ReadTyped(DbDataReader reader, int ordinal, Type fieldType)
	{
		if (fieldType == typeof(string))
		{
			return reader.GetString(ordinal);
		}

		if (fieldType == typeof(bool))
		{
			return reader.GetBoolean(ordinal);
		}

		if (fieldType == typeof(int))
		{
			return reader.GetInt32(ordinal);
		}

		if (fieldType == typeof(long))
		{
			return reader.GetInt64(ordinal);
		}

		if (fieldType == typeof(short))
		{
			return reader.GetInt16(ordinal);
		}

		if (fieldType == typeof(byte))
		{
			return reader.GetByte(ordinal);
		}

		if (fieldType == typeof(uint))
		{
			return reader.GetFieldValue<uint>(ordinal);
		}

		if (fieldType == typeof(ulong))
		{
			return reader.GetFieldValue<ulong>(ordinal);
		}

		if (fieldType == typeof(ushort))
		{
			return reader.GetFieldValue<ushort>(ordinal);
		}

		if (fieldType == typeof(sbyte))
		{
			return reader.GetFieldValue<sbyte>(ordinal);
		}

		if (fieldType == typeof(double))
		{
			return reader.GetDouble(ordinal);
		}

		if (fieldType == typeof(float))
		{
			return reader.GetFloat(ordinal);
		}

		if (fieldType == typeof(decimal))
		{
			return reader.GetDecimal(ordinal);
		}

		if (fieldType == typeof(DateTime))
		{
			return ToUtcDateTime(reader.GetDateTime(ordinal));
		}

		if (fieldType == typeof(DateTimeOffset))
		{
			return reader.GetFieldValue<DateTimeOffset>(ordinal);
		}

		if (fieldType == typeof(Guid))
		{
			return reader.GetGuid(ordinal);
		}

		if (fieldType == typeof(byte[]))
		{
			return (byte[]) reader.GetValue(ordinal);
		}

		return reader.GetValue(ordinal);
	}

	private static (string Sql, object[] Parameters) ToCountSqlQuery(SqlQuery<T> query)
	{
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		var parameters = new List<object>();
		builder.Append("SELECT COUNT(*)");
		AppendFromAndWhere(builder, query, parameters);
		return (builder.ToString().Trim(), parameters.ToArray());
	}

	private static (string Sql, object[] Parameters) ToExistsSqlQuery(SqlQuery<T> query)
	{
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		var parameters = new List<object>();
		builder.Append("SELECT CASE WHEN EXISTS (SELECT 1");
		AppendFromAndWhere(builder, query, parameters);
		builder.Append(") THEN 1 ELSE 0 END");
		return (builder.ToString().Trim(), parameters.ToArray());
	}

	private static DateTime ToUtcDateTime(DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			_ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
		};
	}

	#endregion
}

public class SqlQuery
{
	#region Fields

	protected static readonly Dictionary<Type, Func<object, object>> Converters;

	#endregion

	#region Constructors

	static SqlQuery()
	{
		Converters = new()
		{
			[typeof(string)] = v => v.ToString(),
			[typeof(bool)] = v => System.Convert.ToBoolean(v),
			[typeof(int)] = v => System.Convert.ToInt32(v),
			[typeof(long)] = v => System.Convert.ToInt64(v),
			[typeof(short)] = v => System.Convert.ToInt16(v),
			[typeof(byte)] = v => System.Convert.ToByte(v),
			[typeof(uint)] = v => System.Convert.ToUInt32(v),
			[typeof(ulong)] = v => System.Convert.ToUInt64(v),
			[typeof(ushort)] = v => System.Convert.ToUInt16(v),
			[typeof(sbyte)] = v => System.Convert.ToSByte(v),
			[typeof(double)] = v => System.Convert.ToDouble(v),
			[typeof(float)] = v => System.Convert.ToSingle(v),
			[typeof(decimal)] = v => System.Convert.ToDecimal(v)
		};
	}

	#endregion
}