#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Text.CodeGenerators;

#endregion

namespace Cornerstone.Storage.Sql;

public static class SqlGenerator
{
	#region Methods

	public static string GetCreateTableScript(SourceTypeInfo sourceTypeInfo, SqlProvider provider)
	{
		using var rented = CodeBuilderPool.Rent();
		var builder = rented.Value;
		var tableName = GetTableName(sourceTypeInfo);
		var columns = GetColumnProperties(sourceTypeInfo);
		var sql = provider == SqlProvider.Sqlite
			? GetCreateTableForSqlite(builder, tableName, columns)
			: GetCreateTableForSqlServer(builder, tableName, columns);
		return sql;
	}

	public static (string Sql, IDictionary<string, object> Parameters) GetInsertQuery<T>(T entity, SqlProvider provider)
		where T : Entity
	{
		var sourceTypeInfo = SourceReflector.GetRequiredSourceType<T>();
		var columns = GetColumnProperties(sourceTypeInfo);
		var values = GetColumnValues(entity, columns);
		var tableName = GetTableName(sourceTypeInfo);

		return provider == SqlProvider.Sqlite
			? GetInsertEntityForSqlite(tableName, columns, values)
			: GetInsertEntityForSqlServer(tableName, columns, values);
	}

	private static string GetColumnName(SourcePropertyInfo prop)
	{
		var attr = prop.GetAttribute<ColumnAttribute>();
		var s = attr?.Name;
		return !string.IsNullOrWhiteSpace(s) ? s : prop.Name;
	}

	private static List<SourcePropertyInfo> GetColumnProperties(SourceTypeInfo type)
	{
		return type
			.GetProperties()
			.Where(p => !p.IsStatic
				&& !p.IsIndexer
				&& p.CanRead
				&& p.CanWrite)
			.OrderBy(x => x.Name)
			.ToList();
	}

	private static IDictionary<string, object> GetColumnValues(object entity, List<SourcePropertyInfo> properties)
	{
		return properties.ToDictionary(x => x.Name, x => x.PropertyInfo.GetValue(entity));
	}

	private static string GetCreateTableForSqlite(CodeBuilder builder, string tableName, List<SourcePropertyInfo> columns)
	{
		builder.WriteLine($"CREATE TABLE IF NOT EXISTS \"{tableName}\"");
		builder.Write("(");
		builder.IncreaseIndent();

		for (var index = 0; index < columns.Count; index++)
		{
			builder.WriteLine(index > 0 ? "," : string.Empty);

			var propertyInfo = columns[index];
			var columnInfo = propertyInfo.GetAttribute<ColumnAttribute>();
			var columnName = GetColumnName(propertyInfo);
			var sqlType = GetSqliteType(propertyInfo);
			builder.IndentWrite($"\"{columnName}\" {sqlType}");

			if ((!propertyInfo.PropertyInfo.PropertyType.IsNullable()
					&& !propertyInfo.PropertyInfo.PropertyType.IsClass)
				|| (columnInfo?.IsNullable == false))
			{
				builder.Write(" NOT NULL");
			}

			if (columnInfo?.IsPrimaryKey == true)
			{
				builder.Write(columnInfo.IsAutoIncrement
					? " PRIMARY KEY AUTOINCREMENT"
					: " PRIMARY KEY"
				);
			}
		}

		builder.WriteLine();
		builder.DecreaseIndent();
		builder.Write(");");

		return builder.ToString();
	}

	private static string GetCreateTableForSqlServer(CodeBuilder builder, string tableName, List<SourcePropertyInfo> columns)
	{
		builder.WriteLine($"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')");
		builder.WriteLine("BEGIN");
		builder.IncreaseIndent();
		builder.IndentWriteLine($"CREATE TABLE [{tableName}]");
		builder.IndentWrite("(");
		builder.IncreaseIndent();

		var primaryKeys = new List<SourcePropertyInfo>();
		for (var index = 0; index < columns.Count; index++)
		{
			builder.WriteLine(index > 0 ? "," : string.Empty);

			var propertyInfo = columns[index];
			var columnInfo = propertyInfo.GetAttribute<ColumnAttribute>();
			var columnName = GetColumnName(propertyInfo);
			var sqlType = GetSqlServerType(propertyInfo);
			builder.IndentWrite($"\"{columnName}\" {sqlType}");

			if ((!propertyInfo.PropertyInfo.PropertyType.IsNullable()
					&& !propertyInfo.PropertyInfo.PropertyType.IsClass)
				|| (columnInfo?.IsNullable == false))
			{
				builder.Write(" NOT NULL");
			}

			if (columnInfo?.IsPrimaryKey == true)
			{
				if (columnInfo.IsAutoIncrement)
				{
					builder.Write(" IDENTITY(1,1)");
				}

				primaryKeys.Add(propertyInfo);
			}
		}

		if (primaryKeys.Count > 0)
		{
			builder.WriteLine(",");
			builder.IndentWrite($"CONSTRAINT PK_{tableName} PRIMARY KEY CLUSTERED (");
			foreach (var key in primaryKeys)
			{
				builder.Write("[");
				builder.Write(key.Name);
				builder.Write("]");
			}
			builder.Write(")");
		}

		builder.WriteLine();
		builder.DecreaseIndent();
		builder.IndentWriteLine(")");
		builder.DecreaseIndent();
		builder.Write("END");

		return builder.ToString();
	}

	private static (string Sql, IDictionary<string, object> Values) GetInsertEntityForSqlite(
		string tableName, List<SourcePropertyInfo> columns, IDictionary<string, object> values)
	{
		if ((columns == null) || (columns.Count == 0))
		{
			throw new ArgumentException("Columns list cannot be empty.", nameof(columns));
		}

		// todo: find primary key based on attribute
		var pkColumn = columns.FirstOrDefault(x => x.Name == "Id") ?? columns[0];
		var pkName = GetColumnName(pkColumn);
		var columnNames = columns
			.Where(x => x.Name != pkColumn.Name)
			.Select(GetColumnName)
			.ToArray();

		var index = 0;
		var paramNames = columnNames.ToDictionary(x => x, _ => $"@p{index++}");
		using var rented = CodeBuilderPool.Rent();
		var builder = rented.Value;

		builder.Write($"INSERT INTO \"{tableName}\" (\"");
		builder.Write(string.Join("\", \"", columnNames));
		builder.WriteLine("\")");
		builder.IncreaseIndent();
		builder.IndentWrite("VALUES (");
		builder.Write(string.Join(", ", paramNames.Values));
		builder.WriteLine(")");
		builder.DecreaseIndent();

		builder.Write("ON CONFLICT(\"");
		builder.Write(pkName);
		builder.Write("\") DO UPDATE SET");
		builder.IncreaseIndent();

		// Update only the non-PK columns
		var updateAssignments = columnNames
			.Zip(paramNames)
			.Select(pair => $"\"{pair.First}\" = {pair.Second}")
			.ToArray();

		for (var i = 0; i < updateAssignments.Length; i++)
		{
			builder.WriteLine(i > 0 ? "," : string.Empty);

			var assignment = updateAssignments[i];
			builder.IndentWrite(assignment);
		}

		builder.WriteLine();
		builder.DecreaseIndent();
		builder.Write("RETURNING \"");
		builder.Write(pkName);
		builder.Write("\";");

		return (builder.ToString(), paramNames.ToDictionary(x => x.Value, x => values[x.Key]));
	}

	private static (string Sql, IDictionary<string, object> Values) GetInsertEntityForSqlServer(
		string tableName, List<SourcePropertyInfo> columns, IDictionary<string, object> values)
	{
		if ((columns == null) || (columns.Count == 0))
		{
			throw new ArgumentException("Columns list cannot be empty.", nameof(columns));
		}

		// todo: find primary key based on attribute (same logic as SQLite version)
		var pkColumn = columns.FirstOrDefault(x => x.Name == "Id") ?? columns[0];
		var pkName = GetColumnName(pkColumn);

		var allColumnNames = columns.Select(GetColumnName).ToList();
		var nonPkColumnNames = allColumnNames.Where(name => name != pkName).ToList();
		var index = 0;
		var paramNames = nonPkColumnNames.ToDictionary(x => x, _ => $"@p{index++}");
		using var rented = CodeBuilderPool.Rent();
		var builder = rented.Value;

		builder.WriteLine($"MERGE INTO [{tableName}] AS x");
		builder.Write("USING (VALUES (");
		builder.Write(string.Join(", ", paramNames));
		builder.WriteLine("))");
		builder.IncreaseIndent();
		builder.IndentWrite("AS y ([");
		builder.Write(string.Join("], [", allColumnNames));
		builder.WriteLine("])");
		builder.DecreaseIndent();

		builder.Write("ON x.[");
		builder.Write(pkName);
		builder.Write("] = y.[");
		builder.Write(pkName);
		builder.WriteLine("]");

		builder.WriteLine("WHEN MATCHED THEN");
		builder.IncreaseIndent();
		builder.IndentWrite("UPDATE SET");
		builder.IncreaseIndent();

		for (var i = 0; i < nonPkColumnNames.Count; i++)
		{
			builder.WriteLine(i > 0 ? "," : string.Empty);

			var name = nonPkColumnNames[i];
			builder.IndentWrite($"[{name}] = y.[{name}]");
		}

		builder.DecreaseIndent();
		builder.WriteLine();
		builder.WriteLine("WHEN NOT MATCHED THEN");
		builder.IndentWrite("INSERT ([");
		builder.Write(string.Join("], [", allColumnNames));
		builder.WriteLine("])");

		builder.IndentWrite("VALUES (");
		builder.Write(string.Join(", ", allColumnNames.Select(name => $"y.[{name}]")));
		builder.WriteLine(")");

		builder.Write("OUTPUT inserted.[");
		builder.Write(pkName);
		builder.Write("];");

		return (builder.ToString(), paramNames.ToDictionary(x => x.Value, x => values[x.Key]));
	}

	private static string GetSqliteType(SourcePropertyInfo info)
	{
		var type = Nullable.GetUnderlyingType(info.PropertyInfo.PropertyType)
			?? info.PropertyInfo.PropertyType;

		if (type == typeof(string))
		{
			return "TEXT";
		}
		if (type == typeof(int))
		{
			return "INTEGER";
		}
		if (type == typeof(long))
		{
			return "INTEGER";
		}
		if (type == typeof(bool))
		{
			return "INTEGER";
		}
		if (type == typeof(DateTime))
		{
			return "DATE";
		}
		if (type == typeof(decimal))
		{
			return "REAL";
		}
		if (type == typeof(double))
		{
			return "REAL";
		}
		if (type == typeof(Guid))
		{
			return "TEXT";
		}
		if (type == typeof(byte[]))
		{
			return "BLOB";
		}

		throw new NotSupportedException($"Type not supported for SQLite mapping: {info.PropertyInfo.PropertyType.Name}");
	}

	private static string GetSqlServerType(SourcePropertyInfo info)
	{
		var type = Nullable.GetUnderlyingType(info.PropertyInfo.PropertyType) ?? info.PropertyInfo.PropertyType;

		if (type == typeof(string))
		{
			var attr = info.Attributes.FirstOrDefault(x => x.Type == typeof(ColumnAttribute));
			var maxLength = (attr != null) && attr.NamedArguments.TryGetValue(nameof(ColumnAttribute.MaxLength), out var value) ? value : 0;
			return maxLength is > 0 and <= 4000
				? $"NVARCHAR({maxLength})"
				: "NVARCHAR(MAX)";
		}
		if (type == typeof(int))
		{
			return "INT";
		}
		if (type == typeof(long))
		{
			return "BIGINT";
		}
		if (type == typeof(bool))
		{
			return "BIT";
		}
		if (type == typeof(DateTime))
		{
			return "DATETIME2";
		}
		if (type == typeof(decimal))
		{
			return "DECIMAL(18,6)";
		}
		if (type == typeof(double))
		{
			return "FLOAT";
		}
		if (type == typeof(Guid))
		{
			return "UNIQUEIDENTIFIER";
		}
		if (type == typeof(byte[]))
		{
			return "VARBINARY(MAX)";
		}

		throw new NotSupportedException($"Type not supported for SQL Server mapping: {info.PropertyInfo.PropertyType.Name}");
	}

	private static string GetTableName(SourceTypeInfo type)
	{
		// You can later support [Table("CustomName")] attribute
		return $"{type.Name}s"; // same convention as in SqlQuery
	}

	#endregion
}