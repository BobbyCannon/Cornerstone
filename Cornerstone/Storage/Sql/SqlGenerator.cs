#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Cornerstone.Reflection;
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
	private static readonly (string Open, string Close) _identifierBracketsSqlite = ("\"", "\"");
	private static readonly (string Open, string Close) _identifierBracketsSqlServer = ("[", "]");
	private static readonly Dictionary<(Type, SqlProvider), Func<object, (object, Type)>> _primaryKeyExtractors;
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
		_deleteScripts = [];
		_primaryKeyExtractors = [];
	}

	#endregion

	#region Methods

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

		var visitor = new PredicateToSqlVisitor();
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
		if ((sourceTypeInfo.Type != null)
			&& _tableScripts.TryGetValue((sourceTypeInfo.Type, provider), out var script))
		{
			return script;
		}

		throw new InvalidOperationException(
			$"No generated CREATE TABLE script registered for type '{sourceTypeInfo.Name}' and provider '{provider}'. "
			+ "Ensure the type has the [SourceReflection] and [SqlTable] attributes so the source generator can emit the script.");
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

		var visitor = new PredicateToSqlVisitor();
		var (whereSql, parameters) = visitor.Translate(predicate);
		builder.Append(whereSql);

		return (builder.ToString(), parameters);
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
				    CASE WHEN ic.index_id IS NOT NULL AND pk.is_primary_key = 1
				         THEN 1 ELSE 0 END AS IsPrimaryKey,
				    c.is_identity AS IsAutoIncrement,
				    CASE WHEN uq.is_unique = 1 OR pk.is_primary_key = 1 
				         THEN 1 ELSE 0 END AS IsUnique,
				    c.max_length AS MaxLength
				FROM sys.tables t
				INNER JOIN sys.columns c ON t.object_id = c.object_id
				INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
				LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
				LEFT JOIN sys.index_columns ic ON ic.object_id = c.object_id 
				                              AND ic.column_id = c.column_id
				LEFT JOIN sys.indexes pk ON pk.object_id = ic.object_id 
				                        AND pk.index_id = ic.index_id 
				                        AND pk.is_primary_key = 1
				LEFT JOIN sys.indexes uq ON uq.object_id = t.object_id 
				                        AND uq.is_unique = 1
				                        AND EXISTS (
				                            SELECT 1 
				                            FROM sys.index_columns ic2 
				                            WHERE ic2.object_id = uq.object_id 
				                              AND ic2.index_id = uq.index_id 
				                              AND ic2.column_id = c.column_id
				                        )
				WHERE t.is_ms_shipped = 0
				ORDER BY t.name, c.column_id;
				"""
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

	private static string GetCreateDatabaseForSqlServer(CodeBuilder builder, string databaseName)
	{
		builder.Write("IF NOT EXISTS (SELECT * FROM [sys].[databases] WHERE [name] = N'");
		builder.Write(databaseName);
		builder.WriteLine("')");
		builder.WriteLine("BEGIN");
		builder.Write("\tCREATE DATABASE [");
		builder.Write(databaseName);
		builder.WriteLine("]");
		builder.WriteLine("END");
		return builder.ToString();
	}

	#endregion
}