#region References

using System;
using System.Collections.Generic;
using Cornerstone.Storage.Sql.Data;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// Last committed SQL mapper schema (tables and columns). Used as the previous
/// model when adding a migration, the same role as an EF model snapshot.
/// </summary>
public class SchemaSnapshot
{
	#region Constructors

	public SchemaSnapshot()
	{
		Tables = [];
	}

	#endregion

	#region Properties

	public List<SqlTable> Tables { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds a table to the snapshot and returns it for column setup.
	/// </summary>
	public SqlTable AddTable(string name, string schema)
	{
		var table = new SqlTable(name, schema);
		Tables.Add(table);
		return table;
	}

	/// <summary>
	/// Captures the current mapped entity types into a snapshot (CLR column types and flags).
	/// </summary>
	public static SchemaSnapshot Capture(IEnumerable<Type> entityTypes)
	{
		ArgumentNullException.ThrowIfNull(entityTypes);

		var snapshot = new SchemaSnapshot();
		foreach (var type in entityTypes)
		{
			snapshot.Tables.Add(SqlGenerator.GetExpectedTableInfo(type));
		}

		snapshot.Tables.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
		foreach (var table in snapshot.Tables)
		{
			table.Columns.Sort((left, right) =>
			{
				var order = left.Order.CompareTo(right.Order);
				return order != 0
					? order
					: string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
			});
			table.Indexes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
		}

		return snapshot;
	}

	/// <summary>
	/// Builds a column for generated snapshot C#.
	/// </summary>
	public static SqlTableColumn CreateColumn(
		string name,
		string clrTypeName,
		int order,
		bool isNullable,
		bool isPrimaryKey,
		bool isAutoIncrement,
		bool isUnique,
		int maxLength,
		string defaultValue)
	{
		var column = new SqlTableColumn();
		column.Name = name;
		column.ClrTypeName = clrTypeName;
		column.Order = order;
		column.IsNullable = isNullable;
		column.IsPrimaryKey = isPrimaryKey;
		column.IsAutoIncrement = isAutoIncrement;
		column.IsUnique = isUnique;
		column.MaxLength = maxLength;
		column.DefaultValue = defaultValue;
		return column;
	}

	public static SqlForeignKey CreateForeignKey(string name, string principalTable, string principalColumn, string columnName)
	{
		var fk = new SqlForeignKey();
		fk.Name = name;
		fk.PrincipalTable = principalTable;
		fk.PrincipalColumn = principalColumn;
		var fkColumn = new SqlForeignKeyColumn();
		fkColumn.ColumnName = columnName;
		fkColumn.Ordinal = 0;
		fk.Columns.Add(fkColumn);
		return fk;
	}

	public static SqlIndex CreateIndex(string name, bool isUnique, string columnName)
	{
		var index = new SqlIndex();
		index.Name = name;
		index.IsUnique = isUnique;
		var indexColumn = new SqlIndexColumn();
		indexColumn.ColumnName = columnName;
		indexColumn.Ordinal = 0;
		index.Columns.Add(indexColumn);
		return index;
	}

	/// <summary>
	/// Diffs this snapshot (previous) against current.
	/// </summary>
	public SchemaDiff Diff(SchemaSnapshot current)
	{
		return SchemaDiffer.Diff(this, current);
	}

	/// <summary>
	/// Emits C# for a partial snapshot class that rebuilds this instance in its constructor.
	/// </summary>
	public string ToCSharp(string namespaceName, string className)
	{
		return SchemaSnapshotWriter.Write(this, namespaceName, className);
	}

	#endregion
}