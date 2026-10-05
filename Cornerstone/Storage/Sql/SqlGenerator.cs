#region References

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql.Data;
using Cornerstone.Text;
using Cornerstone.Text.CodeGenerators;

#endregion

namespace Cornerstone.Storage.Sql;

[SourceReflection]
public static class SqlGenerator
{
	#region Constants

	public const string SqlTableAttributeTypeFullName = "Cornerstone.Storage.Sql.SqlTableAttribute";
	public const string SqlTableColumnAttributeTypeFullName = "Cornerstone.Storage.Sql.SqlTableColumnAttribute";

	#endregion

	#region Fields

	private static readonly Dictionary<(Type, SqlProvider), string> _deleteScripts;
	private static readonly (string Open, string Close) _identifierBracketsSqlServer = ("[", "]");
	private static readonly (string Open, string Close) _identifierBracketsSqlite = ("\"", "\"");
	private static readonly Dictionary<(Type, SqlProvider), Func<object, (object, Type)>> _primaryKeyExtractors;
	private static readonly Dictionary<(Type, SqlProvider), Func<object, IDictionary<string, (object, Type)>>> _syncUpsertParameterExtractors;
	private static readonly Dictionary<(Type, SqlProvider), string> _syncUpsertScripts;
	private static readonly Dictionary<(Type, SqlProvider), string> _tableScripts;
	private static readonly Dictionary<(Type, SqlProvider), Func<object, IDictionary<string, (object, Type)>>> _upsertParameterExtractors;
	private static readonly Dictionary<(Type, SqlProvider), string> _upsertScripts;

	#endregion

	#region Constructors

	static SqlGenerator()
	{
		_tableScripts = [];
		_upsertScripts = [];
		_upsertParameterExtractors = [];
		_syncUpsertScripts = [];
		_syncUpsertParameterExtractors = [];
		_deleteScripts = [];
		_primaryKeyExtractors = [];
	}

	#endregion

	#region Methods

	/// <summary>
	/// Maps a CLR column name to the SQL column name from [SqlTableColumn(Name = ...)].
	/// </summary>
	public static string GetColumnName(SourceTypeInfo sourceType, string memberName)
	{
		if ((sourceType == null) || string.IsNullOrEmpty(memberName))
		{
			return memberName;
		}

		var prop = sourceType.GetProperty(memberName);
		if (prop == null)
		{
			return memberName;
		}

		var attr = prop.Attributes.FirstOrDefault(a => a.Name == nameof(SqlTableColumnAttribute));
		if ((attr != null)
			&& attr.NamedArguments.TryGetValue(nameof(SqlTableColumnAttribute.Name), out var value)
			&& (value != null))
		{
			var columnName = value.ToString();
			if (!string.IsNullOrEmpty(columnName))
			{
				return columnName;
			}
		}

		return memberName;
	}

	/// <summary>
	/// Builds a SELECT COUNT(*) ... WHERE from an expression predicate.
	/// </summary>
	public static (string Sql, object[] Parameters) GetCountWhereQuery<T>(
		Expression<Func<T, bool>> predicate, SqlProvider provider)
	{
		var sourceType = SourceReflector.GetRequiredSourceType<T>();
		var tableName = GetTableName(sourceType);

		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;

		var (open, close) = GetIdentifierBrackets(provider);
		builder.Append("SELECT COUNT(*) FROM ");
		builder.Append(open);
		builder.Append(tableName);
		builder.Append(close);
		builder.Append(" WHERE ");

		var visitor = new PredicateToSqlVisitor(provider);
		var (whereSql, parameters) = visitor.Translate(predicate);
		builder.Append(whereSql);

		return (builder.ToString(), parameters);
	}

	public static string GetCreateDatabaseScript(string databaseName, SqlProvider provider)
	{
		using var rented = CodeBuilderPool.Rent();
		var builder = rented.Value;
		var sql = provider == SqlProvider.SqlServer
			? GetCreateDatabaseForSqlServer(builder, databaseName)
			: string.Empty;
		return sql;
	}

	public static string GetCreateTableScript(SourceTypeInfo sourceTypeInfo, SqlProvider provider)
	{
		return GetCreateTableScript(sourceTypeInfo, provider, true);
	}

	/// <summary>
	/// Generated CREATE TABLE. ifNotExists wraps for EnsureTableCreated. Migrations pass false
	/// so an existing table is an error instead of a silent no-op.
	/// </summary>
	public static string GetCreateTableScript(SourceTypeInfo sourceTypeInfo, SqlProvider provider, bool ifNotExists)
	{
		if ((sourceTypeInfo.Type == null)
			|| !_tableScripts.TryGetValue((sourceTypeInfo.Type, provider), out var script))
		{
			throw new InvalidOperationException(
				$"No generated CREATE TABLE script registered for type '{sourceTypeInfo.Name}' and provider '{provider}'. "
				+ "Ensure the type has the [SourceReflection] and [SqlTable] attributes so the source generator can emit the script.");
		}

		return ifNotExists
			? WrapCreateTableIfNotExists(script, GetTableName(sourceTypeInfo), provider)
			: script;
	}

	/// <summary>
	/// Builds DELETE FROM table WHERE pk IN (@p0, ...).
	/// </summary>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetDeleteBatchQuery<T>(
		IReadOnlyList<T> entities, SqlProvider provider)
		where T : Entity
	{
		ArgumentNullException.ThrowIfNull(entities);
		if (entities.Count == 0)
		{
			throw new ArgumentException("At least one entity is required.", nameof(entities));
		}

		var key = (typeof(T), provider);
		if (!_primaryKeyExtractors.TryGetValue(key, out var extractor))
		{
			throw new InvalidOperationException(
				$"No generated DELETE script registered for type '{typeof(T).Name}' and provider '{provider}'.");
		}

		var table = GetExpectedTableInfo(provider, typeof(T));
		var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey)
			?? throw new InvalidOperationException($"No primary key mapped for '{typeof(T).Name}'.");
		var (open, close) = GetIdentifierBrackets(provider);
		var parameters = new Dictionary<string, (object, Type)>();
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		builder.Append("DELETE FROM ");
		builder.Append(open);
		builder.Append(table.Name);
		builder.Append(close);
		builder.Append(" WHERE ");
		builder.Append(open);
		builder.Append(pk.Name);
		builder.Append(close);
		builder.Append(" IN (");
		for (var i = 0; i < entities.Count; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			var paramName = $"@p{i}";
			parameters[paramName] = extractor(entities[i]);
			builder.Append(paramName);
		}

		builder.Append(')');
		return (builder.ToString(), parameters);
	}

	/// <summary>
	/// Gets a parameterized DELETE by primary key for a single entity.
	/// </summary>
	public static (string Sql, (object, Type) PkValue) GetDeleteQuery<T>(T entity, SqlProvider provider)
		where T : Entity
	{
		var key = (typeof(T), provider);

		if (_deleteScripts.TryGetValue(key, out var template)
			&& _primaryKeyExtractors.TryGetValue(key, out var extractor))
		{
			return (template, extractor(entity));
		}

		throw new InvalidOperationException(
			$"No generated DELETE script registered for type '{typeof(T).Name}' and provider '{provider}'. "
			+ "Ensure the type has the [SourceReflection] and [SqlTable] attributes.");
	}

	/// <summary>
	/// Builds a DELETE ... WHERE from an expression predicate.
	/// </summary>
	public static (string Sql, object[] Parameters) GetDeleteWhereQuery<T>(
		Expression<Func<T, bool>> predicate, SqlProvider provider)
	{
		var sourceType = SourceReflector.GetRequiredSourceType<T>();
		var tableName = GetTableName(sourceType);

		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;

		var (open, close) = GetIdentifierBrackets(provider);
		builder.Append("DELETE FROM ");
		builder.Append(open);
		builder.Append(tableName);
		builder.Append(close);
		builder.Append(" WHERE ");

		var visitor = new PredicateToSqlVisitor(provider);
		var (whereSql, parameters) = visitor.Translate(predicate);
		builder.Append(whereSql);

		return (builder.ToString(), parameters);
	}

	/// <summary>
	/// Builds SELECT CASE WHEN EXISTS (SELECT 1 FROM ... WHERE ...) THEN 1 ELSE 0 END.
	/// </summary>
	public static (string Sql, object[] Parameters) GetExistsWhereQuery<T>(
		Expression<Func<T, bool>> predicate, SqlProvider provider)
	{
		var sourceType = SourceReflector.GetRequiredSourceType<T>();
		var tableName = GetTableName(sourceType);

		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;

		var (open, close) = GetIdentifierBrackets(provider);
		builder.Append("SELECT CASE WHEN EXISTS (SELECT 1 FROM ");
		builder.Append(open);
		builder.Append(tableName);
		builder.Append(close);
		builder.Append(" WHERE ");

		var visitor = new PredicateToSqlVisitor(provider);
		var (whereSql, parameters) = visitor.Translate(predicate);
		builder.Append(whereSql);
		builder.Append(") THEN 1 ELSE 0 END");

		return (builder.ToString(), parameters);
	}

	/// <summary>
	/// Builds the expected SqlTable schema for a given entity type.
	/// </summary>
	public static SqlTable GetExpectedTableInfo<T>() where T : Entity
	{
		return GetExpectedTableInfo(typeof(T));
	}

	/// <summary>
	/// Builds the expected SqlTable schema for a given entity type.
	/// </summary>
	public static SqlTable GetExpectedTableInfo<T>(SqlProvider provider) where T : Entity
	{
		return GetExpectedTableInfo(provider, typeof(T));
	}

	/// <summary>
	/// Builds the expected SqlTable schema for a given entity type (CLR snapshot, no provider SQL types).
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Entity Type is supplied by generated table info; SourceReflection cannot carry DynamicallyAccessedMembers.")]
	public static SqlTable GetExpectedTableInfo(Type type)
	{
		return GetExpectedTableInfo(null, type);
	}

	/// <summary>
	/// Builds the expected SqlTable schema for a given entity type.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Entity Type is supplied by generated table info; SourceReflection cannot carry DynamicallyAccessedMembers.")]
	public static SqlTable GetExpectedTableInfo(SqlProvider? provider, Type type)
	{
		var sourceType = SourceReflector.GetRequiredSourceType(type);
		var tableName = GetTableName(sourceType);
		var table = new SqlTable(tableName, string.Empty);

		var properties = sourceType.GetProperties();
		for (var i = 0; i < properties.Length; i++)
		{
			var prop = properties[i];
			if ((prop.PropertyInfo == null) || !prop.PropertyInfo.CanWrite)
			{
				continue;
			}

			var attr = prop.Attributes.FirstOrDefault(a => a.Name == nameof(SqlTableColumnAttribute));
			var columnName = (attr != null) && attr.NamedArguments.TryGetValue("Name", out var value)
				? value?.ToString() ?? prop.Name
				: prop.Name;
			var order = (attr != null) && attr.NamedArguments.TryGetValue("Order", out value) && value is int namedOrder
				? namedOrder
				: i;
			var isPrimaryKey = (attr != null) && attr.NamedArguments.TryGetValue("IsPrimaryKey", out value) && value is true;
			var isAutoIncrement = (attr != null) && attr.NamedArguments.TryGetValue("IsAutoIncrement", out value) && value is true;
			var isUnique = (attr != null) && attr.NamedArguments.TryGetValue("IsUnique", out value) && value is true;
			var maxLength = (attr != null) && attr.NamedArguments.TryGetValue("MaxLength", out value) && value is int namedMax
				? namedMax
				: (int?) null;
			var defaultValue = (attr != null) && attr.NamedArguments.TryGetValue(nameof(SqlTableColumnAttribute.DefaultValue), out value)
				? value?.ToString()
				: null;

			var propertyType = prop.PropertyInfo.PropertyType;
			var inferredNullable = !propertyType.IsValueType || propertyType.IsNullableType();
			var isNullable = attr == null
				? inferredNullable
				: attr.NamedArguments.TryGetValue("IsNullable", out value) && value is true;
			var clrType = propertyType;
			if (clrType.IsNullableType())
			{
				clrType = clrType.FromNullableType();
			}

			var sqlType = clrType.IsEnum
				? Enum.GetUnderlyingType(clrType)
				: clrType;

			var column = new SqlTableColumn
			{
				Name = columnName,
				Order = order,
				ClrTypeName = clrType.FullName,
				ColumnType = provider is { } sqlProvider ? GetSqlColumnType(sqlType, sqlProvider) : string.Empty,
				DefaultValue = defaultValue,
				IsNullable = isNullable,
				IsPrimaryKey = isPrimaryKey,
				IsAutoIncrement = isAutoIncrement,
				IsUnique = isUnique,
				MaxLength = maxLength ?? -1
			};

			table.Columns.Add(column);
			AddExpectedIndex(table, prop, column);
			AddExpectedForeignKey(table, prop, column);
		}

		return table;
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	public static string GetForeignKeyQueryScript(SqlProvider provider)
	{
		return provider == SqlProvider.Sqlite
			? """
			SELECT
				m.name AS TableName,
				f."table" AS PrincipalTable,
				f."from" AS ColumnName,
				f."to" AS PrincipalColumn,
				f.seq AS Ordinal
			FROM sqlite_schema AS m
			JOIN pragma_foreign_key_list(m.name) AS f
			WHERE m.type = 'table'
				AND m.name NOT LIKE 'sqlite_%'
			ORDER BY m.name, f.id, f.seq
			"""
			: """
			SELECT
				t.name AS TableName,
				fk.name AS ForeignKeyName,
				rt.name AS PrincipalTable,
				pc.name AS PrincipalColumn,
				c.name AS ColumnName,
				fkc.constraint_column_id AS Ordinal
			FROM sys.foreign_keys fk
			INNER JOIN sys.tables t ON t.object_id = fk.parent_object_id
			INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
			INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
			INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
			INNER JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id
			WHERE t.is_ms_shipped = 0
			ORDER BY t.name, fk.name, fkc.constraint_column_id
			""";
	}

	/// <summary>
	/// Returns the open and close identifier-quoting characters for the given provider.
	/// </summary>
	public static (string Open, string Close) GetIdentifierBrackets(SqlProvider provider)
	{
		return provider == SqlProvider.SqlServer
			? _identifierBracketsSqlServer
			: _identifierBracketsSqlite;
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	public static string GetIndexQueryScript(SqlProvider provider)
	{
		return provider == SqlProvider.Sqlite
			? """
			SELECT
				m.name AS TableName,
				il.name AS IndexName,
				il."unique" AS IsUnique,
				ii.seqno AS Ordinal,
				ii.name AS ColumnName
			FROM sqlite_schema AS m
			JOIN pragma_index_list(m.name) AS il
			JOIN pragma_index_info(il.name) AS ii
			WHERE m.type = 'table'
				AND m.name NOT LIKE 'sqlite_%'
				AND il.origin != 'pk'
			ORDER BY m.name, il.name, ii.seqno
			"""
			: """
			SELECT
				t.name AS TableName,
				i.name AS IndexName,
				i.is_unique AS IsUnique,
				ic.key_ordinal AS Ordinal,
				c.name AS ColumnName
			FROM sys.tables t
			INNER JOIN sys.indexes i ON i.object_id = t.object_id
			INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
			INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
			WHERE t.is_ms_shipped = 0
				AND i.is_primary_key = 0
				AND i.is_hypothetical = 0
				AND i.name IS NOT NULL
			ORDER BY t.name, i.name, ic.key_ordinal
			""";
	}

	/// <summary>
	/// Multi-row insert (non-PK columns). SQLite RETURNING and SQL Server OUTPUT return Id
	/// (and SyncId when mapped) so callers can assign identity in insert order or by SyncId.
	/// </summary>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetInsertBatchQuery<T>(
		IReadOnlyList<T> entities, SqlProvider provider)
		where T : Entity
	{
		ArgumentNullException.ThrowIfNull(entities);
		if (entities.Count == 0)
		{
			throw new ArgumentException("At least one entity is required.", nameof(entities));
		}

		if (entities.Count == 1)
		{
			return GetInsertQuery(entities[0], provider);
		}

		var key = (typeof(T), provider);
		if (!_upsertParameterExtractors.TryGetValue(key, out var extractor))
		{
			throw new InvalidOperationException(
				$"No generated INSERT/UPSERT script registered for type '{typeof(T).Name}' and provider '{provider}'.");
		}

		var table = GetExpectedTableInfo(provider, typeof(T));
		var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey)
			?? throw new InvalidOperationException($"No primary key mapped for '{typeof(T).Name}'.");
		var insertColumns = table.Columns
			.Where(c => !c.IsPrimaryKey)
			.OrderBy(c => c.Name, StringComparer.Ordinal)
			.ToList();
		var syncColumn = insertColumns.FirstOrDefault(c => string.Equals(c.Name, "SyncId", StringComparison.Ordinal));
		var width = insertColumns.Count;
		var parameters = new Dictionary<string, (object, Type)>();
		for (var row = 0; row < entities.Count; row++)
		{
			var extracted = extractor(entities[row]);
			for (var col = 0; col < width; col++)
			{
				parameters[$"@p{(row * width) + col}"] = extracted[$"@p{col}"];
			}
		}

		var (open, close) = GetIdentifierBrackets(provider);
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		builder.Append("INSERT INTO ");
		builder.Append(open);
		builder.Append(table.Name);
		builder.Append(close);
		builder.Append(" (");
		for (var i = 0; i < insertColumns.Count; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			builder.Append(open);
			builder.Append(insertColumns[i].Name);
			builder.Append(close);
		}

		builder.Append(") VALUES ");
		AppendBatchValueRows(builder, entities.Count, width);
		if (provider == SqlProvider.Sqlite)
		{
			builder.Append(" RETURNING ");
			builder.Append(open);
			builder.Append(pk.Name);
			builder.Append(close);
			if (syncColumn != null)
			{
				builder.Append(", ");
				builder.Append(open);
				builder.Append(syncColumn.Name);
				builder.Append(close);
			}
		}
		else
		{
			builder.Append(" OUTPUT inserted.");
			builder.Append(open);
			builder.Append(pk.Name);
			builder.Append(close);
			if (syncColumn != null)
			{
				builder.Append(", inserted.");
				builder.Append(open);
				builder.Append(syncColumn.Name);
				builder.Append(close);
			}

			builder.Append(';');
		}

		return (builder.ToString(), parameters);
	}

	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetInsertQuery<T>(T entity, SqlProvider provider)
		where T : Entity
	{
		var key = (typeof(T), provider);

		if (_upsertScripts.TryGetValue(key, out var template)
			&& _upsertParameterExtractors.TryGetValue(key, out var extractor))
		{
			return (template, extractor(entity));
		}

		throw new InvalidOperationException(
			$"No generated INSERT/UPSERT script registered for type '{typeof(T).Name}' and provider '{provider}'. "
			+ "Ensure the type has the [SourceReflection] and [SqlTable] attributes so the source generator can emit the script.");
	}

	/// <summary>
	/// Rows that fit in one command. SQL Server caps at 2100 parameters. SQLite
	/// allows 32766 but prepare/bind that large is slower than more, smaller
	/// commands, so SQLite uses 2048.
	/// </summary>
	public static int GetMaxBatchRows(SqlProvider provider, int parameterWidth)
	{
		if (parameterWidth < 1)
		{
			return 1;
		}

		var limit = provider == SqlProvider.SqlServer ? 2100 : 2048;
		return Math.Max(1, limit / parameterWidth);
	}

	public static DbType GetParameterDbType(Type type)
	{
		if (type == null)
		{
			return DbType.String;
		}

		if (type.IsNullableType())
		{
			type = type.FromNullableType();
		}

		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}

		return type switch
		{
			not null when ReferenceEquals(type, typeof(string)) => DbType.String,
			not null when ReferenceEquals(type, typeof(byte)) => DbType.Byte,
			not null when ReferenceEquals(type, typeof(sbyte)) => DbType.SByte,
			not null when ReferenceEquals(type, typeof(short)) => DbType.Int16,
			not null when ReferenceEquals(type, typeof(ushort)) => DbType.UInt16,
			not null when ReferenceEquals(type, typeof(int)) => DbType.Int32,
			not null when ReferenceEquals(type, typeof(uint)) => DbType.UInt32,
			not null when ReferenceEquals(type, typeof(long)) => DbType.Int64,
			not null when ReferenceEquals(type, typeof(ulong)) => DbType.UInt64,
			not null when ReferenceEquals(type, typeof(DateTime)) => DbType.DateTime2,
			not null when ReferenceEquals(type, typeof(DateTimeOffset)) => DbType.DateTimeOffset,
			not null when ReferenceEquals(type, typeof(TimeSpan)) => DbType.Int64,
			not null when ReferenceEquals(type, typeof(Guid)) => DbType.Guid,
			not null when ReferenceEquals(type, typeof(bool)) => DbType.Boolean,
			not null when ReferenceEquals(type, typeof(decimal)) => DbType.Decimal,
			not null when ReferenceEquals(type, typeof(double)) => DbType.Double,
			not null when ReferenceEquals(type, typeof(float)) => DbType.Double,
			not null when ReferenceEquals(type, typeof(byte[])) => DbType.Binary,
			_ => throw new NotSupportedException(
				$"Parameter type {type.FullName} is not supported.")
		};
	}

	public static SourcePropertyInfo GetPrimaryKeyProperty(SourceTypeInfo sourceType)
	{
		if (sourceType == null)
		{
			return null;
		}

		var properties = sourceType.GetProperties();
		for (var i = 0; i < properties.Length; i++)
		{
			var prop = properties[i];
			var attr = prop.Attributes.FirstOrDefault(a => a.Name == nameof(SqlTableColumnAttribute));
			if ((attr != null)
				&& attr.NamedArguments.TryGetValue(nameof(SqlTableColumnAttribute.IsPrimaryKey), out var value)
				&& value is true)
			{
				return prop;
			}
		}

		return sourceType.GetProperty("Id");
	}

	public static string GetSqlColumnType(Type type, SqlProvider provider)
	{
		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}

		if (type.IsNullableType())
		{
			type = type.FromNullableType();
		}

		return provider switch
		{
			SqlProvider.SqlServer => type switch
			{
				not null when type == typeof(int) => "INT",
				not null when type == typeof(long) => "BIGINT",
				not null when type == typeof(short) => "SMALLINT",
				not null when type == typeof(byte) => "TINYINT",
				not null when type == typeof(string) => "NVARCHAR(MAX)",
				not null when type == typeof(bool) => "BIT",
				not null when type == typeof(DateTime) => "DATETIME2",
				not null when type == typeof(Guid) => "UNIQUEIDENTIFIER",
				not null when type == typeof(byte[]) => "VARBINARY(MAX)",
				not null when type == typeof(decimal) => "DECIMAL(18,6)",
				not null when type == typeof(double) => "FLOAT",
				not null when type == typeof(float) => "FLOAT",
				_ => "NVARCHAR"
			},
			SqlProvider.Sqlite => type switch
			{
				not null when type == typeof(int) => "INTEGER",
				not null when type == typeof(long) => "INTEGER",
				not null when type == typeof(short) => "INTEGER",
				not null when type == typeof(byte) => "INTEGER",
				not null when type == typeof(string) => "TEXT",
				not null when type == typeof(bool) => "INTEGER",
				not null when type == typeof(DateTime) => "DATE",
				not null when type == typeof(Guid) => "TEXT",
				not null when type == typeof(byte[]) => "BLOB",
				not null when type == typeof(decimal) => "REAL",
				not null when type == typeof(double) => "REAL",
				not null when type == typeof(float) => "REAL",
				_ => "TEXT"
			},
			_ => "TEXT"
		};
	}

	/// <summary>
	/// Multi-row SyncId upsert. SQLite uses excluded.*; SQL Server MERGE uses a VALUES source.
	/// Older rows (incoming ModifiedOn not greater) are omitted from RETURNING / OUTPUT.
	/// </summary>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetSyncUpsertBatchQuery<T>(
		IReadOnlyList<T> entities, SqlProvider provider)
		where T : Entity
	{
		return GetSyncUpsertBatchQuery(entities, provider, true);
	}

	/// <summary>
	/// Multi-row SyncId upsert. When requireNewerModifiedOn is false, an existing row is
	/// updated even if incoming ModifiedOn is not newer (applied tombstone).
	/// </summary>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetSyncUpsertBatchQuery<T>(
		IReadOnlyList<T> entities, SqlProvider provider, bool requireNewerModifiedOn)
		where T : Entity
	{
		ArgumentNullException.ThrowIfNull(entities);
		if (entities.Count == 0)
		{
			throw new ArgumentException("At least one entity is required.", nameof(entities));
		}

		if (entities.Count == 1)
		{
			return GetSyncUpsertQuery(entities[0], provider, requireNewerModifiedOn);
		}

		var key = (typeof(T), provider);
		if (!_syncUpsertParameterExtractors.TryGetValue(key, out var extractor))
		{
			throw new InvalidOperationException(
				$"No generated SyncId upsert script registered for type '{typeof(T).Name}' and provider '{provider}'.");
		}

		var table = GetExpectedTableInfo(provider, typeof(T));
		var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey)
			?? throw new InvalidOperationException($"No primary key mapped for '{typeof(T).Name}'.");
		var insertColumns = table.Columns
			.Where(c => !c.IsPrimaryKey)
			.OrderBy(c => c.Name, StringComparer.Ordinal)
			.ToList();
		var syncColumn = insertColumns.FirstOrDefault(c => string.Equals(c.Name, "SyncId", StringComparison.Ordinal))
			?? throw new InvalidOperationException($"No SyncId column mapped for '{typeof(T).Name}'.");
		var modifiedColumn = insertColumns.FirstOrDefault(c => string.Equals(c.Name, "ModifiedOn", StringComparison.Ordinal))
			?? throw new InvalidOperationException($"No ModifiedOn column mapped for '{typeof(T).Name}'.");
		var updateColumns = insertColumns
			.Where(c => !string.Equals(c.Name, "SyncId", StringComparison.Ordinal))
			.ToList();

		var width = insertColumns.Count;
		var parameters = new Dictionary<string, (object, Type)>();
		for (var row = 0; row < entities.Count; row++)
		{
			var extracted = extractor(entities[row]);
			for (var col = 0; col < width; col++)
			{
				parameters[$"@p{(row * width) + col}"] = extracted[$"@p{col}"];
			}
		}

		var (open, close) = GetIdentifierBrackets(provider);
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		if (provider == SqlProvider.Sqlite)
		{
			builder.Append("INSERT INTO ");
			builder.Append(open);
			builder.Append(table.Name);
			builder.Append(close);
			builder.Append(" (");
			for (var i = 0; i < insertColumns.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(open);
				builder.Append(insertColumns[i].Name);
				builder.Append(close);
			}

			builder.Append(") VALUES ");
			AppendBatchValueRows(builder, entities.Count, width);
			builder.Append(" ON CONFLICT(");
			builder.Append(open);
			builder.Append(syncColumn.Name);
			builder.Append(close);
			builder.Append(") DO UPDATE SET ");
			for (var i = 0; i < updateColumns.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(open);
				builder.Append(updateColumns[i].Name);
				builder.Append(close);
				builder.Append(" = excluded.");
				builder.Append(open);
				builder.Append(updateColumns[i].Name);
				builder.Append(close);
			}

			if (requireNewerModifiedOn)
			{
				builder.Append(" WHERE excluded.");
				builder.Append(open);
				builder.Append(modifiedColumn.Name);
				builder.Append(close);
				builder.Append(" > ");
				builder.Append(open);
				builder.Append(table.Name);
				builder.Append(close);
				builder.Append('.');
				builder.Append(open);
				builder.Append(modifiedColumn.Name);
				builder.Append(close);
			}

			builder.Append(" RETURNING ");
			builder.Append(open);
			builder.Append(pk.Name);
			builder.Append(close);
			builder.Append(", ");
			builder.Append(open);
			builder.Append(syncColumn.Name);
			builder.Append(close);
		}
		else
		{
			builder.Append("MERGE INTO ");
			builder.Append(open);
			builder.Append(table.Name);
			builder.Append(close);
			builder.Append(" AS x USING (VALUES ");
			AppendBatchValueRows(builder, entities.Count, width);
			builder.Append(") AS y (");
			for (var i = 0; i < insertColumns.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(open);
				builder.Append(insertColumns[i].Name);
				builder.Append(close);
			}

			builder.Append(") ON x.");
			builder.Append(open);
			builder.Append(syncColumn.Name);
			builder.Append(close);
			builder.Append(" = y.");
			builder.Append(open);
			builder.Append(syncColumn.Name);
			builder.Append(close);
			builder.Append(" WHEN MATCHED");
			if (requireNewerModifiedOn)
			{
				builder.Append(" AND y.");
				builder.Append(open);
				builder.Append(modifiedColumn.Name);
				builder.Append(close);
				builder.Append(" > x.");
				builder.Append(open);
				builder.Append(modifiedColumn.Name);
				builder.Append(close);
			}

			builder.Append(" THEN UPDATE SET ");
			for (var i = 0; i < updateColumns.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(open);
				builder.Append(updateColumns[i].Name);
				builder.Append(close);
				builder.Append(" = y.");
				builder.Append(open);
				builder.Append(updateColumns[i].Name);
				builder.Append(close);
			}

			builder.Append(" WHEN NOT MATCHED THEN INSERT (");
			for (var i = 0; i < insertColumns.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(open);
				builder.Append(insertColumns[i].Name);
				builder.Append(close);
			}

			builder.Append(") VALUES (");
			for (var i = 0; i < insertColumns.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append("y.");
				builder.Append(open);
				builder.Append(insertColumns[i].Name);
				builder.Append(close);
			}

			builder.Append(") OUTPUT inserted.");
			builder.Append(open);
			builder.Append(pk.Name);
			builder.Append(close);
			builder.Append(", inserted.");
			builder.Append(open);
			builder.Append(syncColumn.Name);
			builder.Append(close);
			builder.Append(';');
		}

		return (builder.ToString(), parameters);
	}

	/// <summary>
	/// Gets a parameterized upsert keyed on SyncId. The update is applied only when the incoming
	/// ModifiedOn is newer than the stored row, unless requireNewerModifiedOn is false (tombstone flush).
	/// </summary>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetSyncUpsertQuery<T>(T entity, SqlProvider provider)
		where T : Entity
	{
		return GetSyncUpsertQuery(entity, provider, true);
	}

	/// <summary>
	/// Gets a parameterized upsert keyed on SyncId.
	/// </summary>
	/// <param name="requireNewerModifiedOn"> False skips the ModifiedOn guard so an applied tombstone still writes. </param>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetSyncUpsertQuery<T>(
		T entity,
		SqlProvider provider,
		bool requireNewerModifiedOn
	)
		where T : Entity
	{
		var key = (typeof(T), provider);

		if (_syncUpsertScripts.TryGetValue(key, out var template)
			&& _syncUpsertParameterExtractors.TryGetValue(key, out var extractor))
		{
			var sql = requireNewerModifiedOn ? template : RemoveModifiedOnGuard(template, provider);
			return (sql, extractor(entity));
		}

		throw new InvalidOperationException(
			$"No generated SyncId upsert script registered for type '{typeof(T).Name}' and provider '{provider}'. "
			+ "Ensure the type has [SourceReflection], [SqlTable], SyncId, and ModifiedOn.");
	}

	public static string GetTableName(SourceTypeInfo type)
	{
		// Check for explicit [SqlTable] attribute first
		var tableName = type.GetAttributeNamedArgument<string>(SqlTableAttributeTypeFullName, nameof(SqlTableAttribute.TableName))
			?? type.GetAttributeConstructorArgument<string>(SqlTableAttributeTypeFullName, 0);

		// todo: add a bit better pluralization technique.
		return !string.IsNullOrWhiteSpace(tableName)
			? tableName
			: string.Concat(type.Name, "s");
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	public static string GetTableQueryScript(SqlProvider provider)
	{
		return provider switch
		{
			SqlProvider.Sqlite =>
				"""
				SELECT
					m.name AS TableName,
					'' AS SchemaName,
					p.cid AS Ordinal,
					p.name AS ColumnName,
					p."type" AS TypeName,
					p."notnull" AS IsNullable,
					p.dflt_value AS "Default",
					p.pk AS IsPrimaryKey,
					CASE 
						WHEN p.pk = 1 AND p."type" = 'INTEGER' 
								AND EXISTS (
									SELECT 1 
									FROM sqlite_schema 
									WHERE type = 'table' 
									AND name = m.name 
									AND sql LIKE '%AUTOINCREMENT%'
								) 
						THEN 1 
						ELSE 0 
					END AS IsAutoIncrement,
					0 AS IsUnique,
					CASE 
						WHEN UPPER(p."type") LIKE '%CHAR%' 
								OR UPPER(p."type") LIKE '%TEXT%' 
								OR UPPER(p."type") LIKE '%BLOB%' 
						THEN -1
						ELSE 0 
					END AS MaxLength
				FROM sqlite_schema AS m
				CROSS JOIN pragma_table_info(m.name) AS p
				WHERE m.type = 'table' 
					AND m.name NOT LIKE 'sqlite_%'
				ORDER BY m.name, p.cid;
				""",
			_ => """
				SELECT
				    t.name AS TableName,
				    SCHEMA_NAME(t.schema_id) AS SchemaName,
				    c.column_id AS Ordinal,
				    c.name AS ColumnName,
				    ty.name AS TypeName,
				    CASE WHEN c.is_nullable = 1 THEN 0 ELSE 1 END AS IsNullable,
				    dc.definition AS "Default",
				    CASE WHEN EXISTS (
				        SELECT 1
				        FROM sys.index_columns ic
				        INNER JOIN sys.indexes pk ON pk.object_id = ic.object_id AND pk.index_id = ic.index_id
				        WHERE ic.object_id = c.object_id
				          AND ic.column_id = c.column_id
				          AND pk.is_primary_key = 1
				    ) THEN 1 ELSE 0 END AS IsPrimaryKey,
				    c.is_identity AS IsAutoIncrement,
				    CASE WHEN EXISTS (
				        SELECT 1
				        FROM sys.index_columns ic
				        INNER JOIN sys.indexes uq ON uq.object_id = ic.object_id AND uq.index_id = ic.index_id
				        WHERE ic.object_id = c.object_id
				          AND ic.column_id = c.column_id
				          AND uq.is_unique = 1
				          AND uq.is_hypothetical = 0
				    ) THEN 1 ELSE 0 END AS IsUnique,
				    c.max_length AS MaxLength
				FROM sys.tables t
				INNER JOIN sys.columns c ON t.object_id = c.object_id
				INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
				LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
				WHERE t.is_ms_shipped = 0
				ORDER BY t.name, c.column_id;
				"""
		};
	}

	/// <summary>
	/// UPDATE by primary key. SQLite INSERT ON CONFLICT(Id) cannot update because
	/// identity Id is omitted from INSERT.
	/// </summary>
	public static (string Sql, IDictionary<string, (object, Type)> Parameters) GetUpdateByIdQuery<T>(
		T entity, SqlProvider provider)
		where T : Entity
	{
		ArgumentNullException.ThrowIfNull(entity);
		var key = (typeof(T), provider);
		if (!_upsertParameterExtractors.TryGetValue(key, out var extractor)
			|| !_primaryKeyExtractors.TryGetValue(key, out var pkExtractor))
		{
			throw new InvalidOperationException(
				$"No generated UPDATE script registered for type '{typeof(T).Name}' and provider '{provider}'.");
		}

		var table = GetExpectedTableInfo(provider, typeof(T));
		var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey)
			?? throw new InvalidOperationException($"No primary key mapped for '{typeof(T).Name}'.");
		var updateColumns = table.Columns
			.Where(c => !c.IsPrimaryKey)
			.OrderBy(c => c.Name, StringComparer.Ordinal)
			.ToList();
		var extracted = extractor(entity);
		var parameters = new Dictionary<string, (object, Type)>();
		for (var i = 0; i < updateColumns.Count; i++)
		{
			parameters[$"@p{i}"] = extracted[$"@p{i}"];
		}

		parameters[$"@p{updateColumns.Count}"] = pkExtractor(entity);
		var (open, close) = GetIdentifierBrackets(provider);
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		builder.Append("UPDATE ");
		builder.Append(open);
		builder.Append(table.Name);
		builder.Append(close);
		builder.Append(" SET ");
		for (var i = 0; i < updateColumns.Count; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			builder.Append(open);
			builder.Append(updateColumns[i].Name);
			builder.Append(close);
			builder.Append(" = @p");
			builder.Append(i);
		}

		builder.Append(" WHERE ");
		builder.Append(open);
		builder.Append(pk.Name);
		builder.Append(close);
		builder.Append(" = @p");
		builder.Append(updateColumns.Count);
		return (builder.ToString(), parameters);
	}

	/// <summary>
	/// Maps declared SQL types for live-vs-expected compare (SQLite INT vs INTEGER, etc.).
	/// DATE stays distinct from TEXT.
	/// </summary>
	public static string NormalizeDeclaredType(string sqlType)
	{
		if (string.IsNullOrWhiteSpace(sqlType))
		{
			return string.Empty;
		}

		var normalized = sqlType.Trim().ToUpperInvariant();
		var paren = normalized.IndexOf('(');
		if (paren > 0)
		{
			normalized = normalized.Substring(0, paren);
		}

		return normalized switch
		{
			"INT" or "INTEGER" or "BIGINT" or "SMALLINT" or "TINYINT" or "BIT" => "INTEGER",
			"TEXT" or "CHAR" or "CLOB" or "VARCHAR" or "NVARCHAR" => "TEXT",
			"DATE" or "DATETIME" or "DATETIME2" => "DATE",
			"BLOB" or "VARBINARY" => "BLOB",
			"REAL" or "FLOAT" or "DOUBLE" or "DECIMAL" or "NUMERIC" => "REAL",
			_ => normalized
		};
	}

	/// <summary>
	/// Registers a pre-computed CREATE TABLE script for a given type and provider.
	/// Called from the source-generated module initializer.
	/// </summary>
	public static void RegisterCreateTableScript(Type type, SqlProvider provider, string script)
	{
		_tableScripts[(type, provider)] = script;
	}

	/// <summary>
	/// Registers a pre-computed DELETE script and PK extractor for a given type and provider.
	/// Called from the source-generated module initializer.
	/// </summary>
	public static void RegisterDeleteQuery(Type type, SqlProvider provider, string sqlTemplate, Func<object, (object, Type)> primaryKeyExtractor)
	{
		_deleteScripts[(type, provider)] = sqlTemplate;
		_primaryKeyExtractors[(type, provider)] = primaryKeyExtractor;
	}

	/// <summary>
	/// Registers a pre-computed INSERT/UPSERT template and parameter extractor for a given type and provider.
	/// Called from the source-generated module initializer.
	/// </summary>
	public static void RegisterInsertQuery(Type type, SqlProvider provider, string sqlTemplate, Func<object, IDictionary<string, (object, Type)>> parameterExtractor)
	{
		_upsertScripts[(type, provider)] = sqlTemplate;
		_upsertParameterExtractors[(type, provider)] = parameterExtractor;
	}

	/// <summary>
	/// Registers a pre-computed SyncId upsert template and parameter extractor.
	/// Called from the source-generated module initializer.
	/// </summary>
	public static void RegisterSyncUpsertQuery(Type type, SqlProvider provider, string sqlTemplate, Func<object, IDictionary<string, (object, Type)>> parameterExtractor)
	{
		_syncUpsertScripts[(type, provider)] = sqlTemplate;
		_syncUpsertParameterExtractors[(type, provider)] = parameterExtractor;
	}

	private static void AddExpectedForeignKey(SqlTable table, SourcePropertyInfo prop, SqlTableColumn column)
	{
		var attr = prop.Attributes.FirstOrDefault(a => a.Name == nameof(SqlForeignKeyAttribute));
		if (attr == null)
		{
			return;
		}

		var principalTable = attr.NamedArguments.TryGetValue(nameof(SqlForeignKeyAttribute.PrincipalTable), out var named)
			? named?.ToString()
			: null;
		if (string.IsNullOrWhiteSpace(principalTable)
			&& (attr.ConstructorArguments != null)
			&& (attr.ConstructorArguments.Length > 0))
		{
			principalTable = attr.ConstructorArguments[0]?.ToString();
		}

		if (string.IsNullOrWhiteSpace(principalTable))
		{
			return;
		}

		var principalColumn = attr.NamedArguments.TryGetValue(nameof(SqlForeignKeyAttribute.PrincipalColumn), out var pc)
			? pc?.ToString()
			: "Id";
		if (string.IsNullOrWhiteSpace(principalColumn))
		{
			principalColumn = "Id";
		}

		var fk = new SqlForeignKey();
		fk.Name = "FK_" + table.Name + "_" + principalTable + "_" + column.Name;
		fk.PrincipalTable = principalTable;
		fk.PrincipalColumn = principalColumn;
		var fkColumn = new SqlForeignKeyColumn();
		fkColumn.ColumnName = column.Name;
		fkColumn.Ordinal = 0;
		fk.Columns.Add(fkColumn);
		table.ForeignKeys.Add(fk);
	}

	private static void AddExpectedIndex(SqlTable table, SourcePropertyInfo prop, SqlTableColumn column)
	{
		var indexAttr = prop.Attributes.FirstOrDefault(a => a.Name == nameof(SqlIndexAttribute));
		if (!column.IsUnique && (indexAttr == null))
		{
			return;
		}

		if (column.IsPrimaryKey)
		{
			return;
		}

		var index = new SqlIndex();
		index.IsUnique = column.IsUnique
			|| ((indexAttr != null)
				&& indexAttr.NamedArguments.TryGetValue(nameof(SqlIndexAttribute.IsUnique), out var uniqueObj)
				&& uniqueObj is true);
		index.Name = (indexAttr != null)
			&& indexAttr.NamedArguments.TryGetValue(nameof(SqlIndexAttribute.Name), out var nameObj)
			&& !string.IsNullOrWhiteSpace(nameObj?.ToString())
				? nameObj.ToString()
				: "IX_" + table.Name + "_" + column.Name;
		var indexColumn = new SqlIndexColumn();
		indexColumn.ColumnName = column.Name;
		indexColumn.Ordinal = 0;
		index.Columns.Add(indexColumn);
		table.Indexes.Add(index);
	}

	private static void AppendBatchValueRows(StringBuilder builder, int rows, int width)
	{
		for (var row = 0; row < rows; row++)
		{
			if (row > 0)
			{
				builder.Append(", ");
			}

			builder.Append('(');
			for (var col = 0; col < width; col++)
			{
				if (col > 0)
				{
					builder.Append(", ");
				}

				builder.Append("@p");
				builder.Append((row * width) + col);
			}

			builder.Append(')');
		}
	}

	private static string GetCreateDatabaseForSqlServer(CodeBuilder builder, string databaseName)
	{
		builder.Append("IF NOT EXISTS (SELECT * FROM [sys].[databases] WHERE [name] = N'");
		builder.Append(databaseName);
		builder.AppendLine("')");
		builder.AppendLine("BEGIN");
		builder.Append("\tCREATE DATABASE [");
		builder.Append(databaseName);
		builder.AppendLine("]");
		builder.AppendLine("END");
		return builder.ToString();
	}

	private static string RemoveModifiedOnGuard(string sql, SqlProvider provider)
	{
		if (string.IsNullOrEmpty(sql))
		{
			return sql;
		}

		if (provider == SqlProvider.Sqlite)
		{
			var where = sql.IndexOf("\nWHERE ", StringComparison.Ordinal);
			if (where < 0)
			{
				where = sql.IndexOf("\r\nWHERE ", StringComparison.Ordinal);
			}

			if (where < 0)
			{
				return sql;
			}

			var returning = sql.IndexOf("\nRETURNING ", where, StringComparison.Ordinal);
			if (returning < 0)
			{
				returning = sql.IndexOf("\r\nRETURNING ", where, StringComparison.Ordinal);
			}

			if (returning < 0)
			{
				return sql;
			}

			return sql.Remove(where, returning - where);
		}

		return sql.Replace(
			"WHEN MATCHED AND y.[ModifiedOn] > x.[ModifiedOn] THEN",
			"WHEN MATCHED THEN",
			StringComparison.Ordinal);
	}

	private static string WrapCreateTableIfNotExists(string script, string tableName, SqlProvider provider)
	{
		if (provider == SqlProvider.Sqlite)
		{
			const string prefix = "CREATE TABLE ";
			return script.StartsWith(prefix, StringComparison.Ordinal)
				? "CREATE TABLE IF NOT EXISTS " + script.Substring(prefix.Length)
				: script;
		}

		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		builder.Append("IF NOT EXISTS (SELECT * FROM [sys].[tables] WHERE [name] = '");
		builder.Append(tableName);
		builder.AppendLine("')");
		builder.AppendLine("BEGIN");
		var lines = script.Replace("\r\n", "\n").Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			builder.Append('\t');
			builder.Append(lines[i]);
			if (i < (lines.Length - 1))
			{
				builder.AppendLine();
			}
		}

		builder.AppendLine();
		builder.Append("END");
		return builder.ToString();
	}

	#endregion
}