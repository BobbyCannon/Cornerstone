#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql.Data;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// Collects provider-specific SQL for one migration Up or Down.
/// </summary>
public class MigrationBuilder
{
	#region Constructors

	public MigrationBuilder(SqlProvider provider)
	{
		CreateTableTypes = [];
		RebuildTableTypes = [];
		Provider = provider;
		Statements = [];
	}

	#endregion

	#region Properties

	public List<Type> CreateTableTypes { get; }

	public SqlProvider Provider { get; }

	public List<Type> RebuildTableTypes { get; }

	public List<string> Statements { get; }

	#endregion

	#region Methods

	public void AddColumn(string tableName, SqlTableColumn column)
	{
		ArgumentNullException.ThrowIfNull(column);

		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		var sqlType = GetAddColumnSqlType(column);
		var addKeyword = Provider == SqlProvider.Sqlite ? "ADD COLUMN" : "ADD";

		var sql = $"ALTER TABLE {open}{tableName}{close} {addKeyword} {open}{column.Name}{close} {sqlType}";
		if (!column.IsNullable)
		{
			sql += " NOT NULL";
			sql += " DEFAULT " + (string.IsNullOrEmpty(column.DefaultValue)
				? InferDefaultLiteral(sqlType)
				: column.DefaultValue);
		}
		else if (!string.IsNullOrEmpty(column.DefaultValue))
		{
			sql += " DEFAULT " + column.DefaultValue;
		}

		Statements.Add(sql);
	}

	public void AddForeignKey(string tableName, SqlForeignKey foreignKey)
	{
		ArgumentNullException.ThrowIfNull(foreignKey);
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		var local = string.Join(", ", foreignKey.Columns.Select(x => open + x.ColumnName + close));
		var name = string.IsNullOrWhiteSpace(foreignKey.Name)
			? "FK_" + tableName + "_" + foreignKey.PrincipalTable
			: foreignKey.Name;
		if (Provider == SqlProvider.Sqlite)
		{
			Statements.Add(
				$"-- SQLite foreign keys are created with CREATE TABLE / RebuildTable ({name})");
			return;
		}

		Statements.Add(
			$"ALTER TABLE {open}{tableName}{close} ADD CONSTRAINT {open}{name}{close} FOREIGN KEY ({local}) REFERENCES {open}{foreignKey.PrincipalTable}{close} ({open}{foreignKey.PrincipalColumn}{close})");
	}

	public void AlterColumn(string tableName, SqlTableColumn column)
	{
		ArgumentNullException.ThrowIfNull(column);
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		var sqlType = GetAddColumnSqlType(column);
		var nullability = column.IsNullable ? "NULL" : "NOT NULL";
		Statements.Add($"ALTER TABLE {open}{tableName}{close} ALTER COLUMN {open}{column.Name}{close} {sqlType} {nullability}");
	}

	public void CreateIndex(string tableName, SqlIndex index)
	{
		ArgumentNullException.ThrowIfNull(index);
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		var unique = index.IsUnique ? "UNIQUE " : string.Empty;
		var columns = string.Join(", ", index.Columns.Select(x => open + x.ColumnName + close));
		var name = string.IsNullOrWhiteSpace(index.Name) ? "IX_" + tableName : index.Name;
		if (Provider == SqlProvider.Sqlite)
		{
			Statements.Add($"CREATE {unique}INDEX IF NOT EXISTS {open}{name}{close} ON {open}{tableName}{close} ({columns})");
			return;
		}

		Statements.Add(
			$"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'{tableName}')) CREATE {unique}NONCLUSTERED INDEX {open}{name}{close} ON {open}{tableName}{close} ({columns})");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Entity types are supplied by generated table registration and host migrations.")]
	public void CreateTable(Type entityType)
	{
		var sourceType = SourceReflector.GetRequiredSourceType(entityType);
		CreateTableTypes.Add(entityType);
		Statements.Add(SqlGenerator.GetCreateTableScript(sourceType, Provider, false));
	}

	public void CreateTable<T>()
	{
		CreateTable(typeof(T));
	}

	public void DropColumn(string tableName, string columnName)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		Statements.Add($"ALTER TABLE {open}{tableName}{close} DROP COLUMN {open}{columnName}{close}");
	}

	public void DropIndex(string tableName, string indexName)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		Statements.Add(Provider == SqlProvider.Sqlite
			? $"DROP INDEX IF EXISTS {open}{indexName}{close}"
			: $"DROP INDEX {open}{indexName}{close} ON {open}{tableName}{close}");
	}

	public void DropTable(string tableName)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		Statements.Add(Provider == SqlProvider.Sqlite
			? $"DROP TABLE IF EXISTS {open}{tableName}{close}"
			: $"IF OBJECT_ID(N'{open}{tableName}{close}', N'U') IS NOT NULL DROP TABLE {open}{tableName}{close}");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "RebuildTable entity types come from host migrations.")]
	public void RebuildTable(Type entityType)
	{
		RebuildTableTypes.Add(entityType);
		Statements.Add("-- RebuildTable " + entityType.FullName);
	}

	public void RebuildTable<T>()
	{
		RebuildTable(typeof(T));
	}

	public void Sql(string sql)
	{
		if (!string.IsNullOrWhiteSpace(sql))
		{
			Statements.Add(sql);
		}
	}

	internal static string GetCreateHistoryTableScript(SqlProvider provider)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(provider);
		var table = open + SqlMigration.HistoryTableName + close;
		if (provider == SqlProvider.Sqlite)
		{
			return $"""
					CREATE TABLE IF NOT EXISTS {table}
					(
						{open}MigrationId{close} TEXT NOT NULL PRIMARY KEY,
						{open}ProductVersion{close} TEXT NOT NULL
					)
					""";
		}

		return $"""
				IF OBJECT_ID(N'{table}', N'U') IS NULL
				BEGIN
					CREATE TABLE {table}
					(
						{open}MigrationId{close} NVARCHAR(150) NOT NULL PRIMARY KEY,
						{open}ProductVersion{close} NVARCHAR(32) NOT NULL
					)
				END
				""";
	}

	internal static string InferDefaultLiteral(string sqlType)
	{
		var normalized = (sqlType ?? string.Empty).ToUpperInvariant();
		if (normalized.Contains("INT")
			|| normalized.Contains("REAL")
			|| normalized.Contains("FLOAT")
			|| normalized.Contains("DOUBLE")
			|| normalized.Contains("DECIMAL")
			|| normalized.Contains("NUMERIC")
			|| normalized.Contains("BIT")
			|| normalized.Contains("MONEY"))
		{
			return "0";
		}

		return "''";
	}

	private string GetAddColumnSqlType(SqlTableColumn column)
	{
		if (!string.IsNullOrEmpty(column.ColumnType))
		{
			return column.ColumnType;
		}

		var type = ResolveClrType(column.ClrTypeName);
		if ((Provider == SqlProvider.SqlServer) && (type == typeof(string)))
		{
			return (column.MaxLength > 0) && (column.MaxLength <= 4000)
				? $"NVARCHAR({column.MaxLength})"
				: "NVARCHAR(MAX)";
		}

		return SqlGenerator.GetSqlColumnType(type, Provider);
	}

	private static Type ResolveClrType(string clrTypeName)
	{
		if (string.IsNullOrWhiteSpace(clrTypeName))
		{
			throw new InvalidOperationException("AddColumn requires ClrTypeName or ColumnType.");
		}

		var type = Type.GetType(clrTypeName);
		if (type != null)
		{
			return type;
		}

		throw new InvalidOperationException(
			$"Unable to resolve CLR type '{clrTypeName}' for AddColumn. Set ColumnType or use an assembly-qualified name.");
	}

	#endregion
}