#region References

using System;
using System.Collections.Generic;
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

public class SqlQuery<T> : SqlQuery
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

		var (open, close) = SqlGenerator.GetIdentifierBrackets(query.Provider);

		var ps = query._sourceType.GetProperties();
		var first = true;
		for (var index = 0; index < ps.Length; index++)
		{
			if (!first)
			{
				builder.Append(", ");
			}
			var p = ps[index];
			builder.Append(open);
			builder.Append(p.Name);
			builder.Append(close);
			first = false;
		}

		builder.Append($" FROM {open}{SqlGenerator.GetTableName(query._sourceType)}{close}");

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
			return targetType.IsValueType ? SourceReflector.CreateInstance(targetType) : null;
		}

		if (targetType.IsInstanceOfType(dbValue))
		{
			return dbValue;
		}

		var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

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
				return System.Convert.ToDateTime(dbValue);
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

		while (reader.Read())
		{
			var item = new T();
			for (var i = 0; i < fieldCount; i++)
			{
				var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
				propertyMap[i].SetValue(item, ConvertTo(value, typeMap[i]));
			}
			results.Add(item);
		}

		return results;
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