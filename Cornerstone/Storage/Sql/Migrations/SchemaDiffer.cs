#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Storage.Sql.Data;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// Diffs two <see cref="SchemaSnapshot" /> instances (previous model vs current model).
/// Does not read the live catalog.
/// </summary>
public static class SchemaDiffer
{
	#region Methods

	public static SchemaDiff Diff(SchemaSnapshot previous, SchemaSnapshot current)
	{
		ArgumentNullException.ThrowIfNull(previous);
		ArgumentNullException.ThrowIfNull(current);

		var response = new SchemaDiff();
		var previousTables = ToTableMap(previous.Tables);
		var currentTables = ToTableMap(current.Tables);

		foreach (var name in currentTables.Keys.Union(previousTables.Keys).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			previousTables.TryGetValue(name, out var fromTable);
			currentTables.TryGetValue(name, out var toTable);

			if (fromTable == null)
			{
				response.Tables.Add(new TableDiff(TableAction.Create, toTable));
				continue;
			}

			if (toTable == null)
			{
				response.Tables.Add(new TableDiff(TableAction.Drop, fromTable));
				continue;
			}

			var columnChanges = DiffColumns(fromTable, toTable);
			var indexChanges = DiffIndexes(fromTable, toTable);
			var foreignKeysChanged = !ForeignKeysMatch(fromTable, toTable);
			if ((columnChanges.Count > 0) || (indexChanges.Count > 0) || foreignKeysChanged)
			{
				var diff = new TableDiff(TableAction.Alter, toTable, columnChanges);
				diff.IndexChanges.AddRange(indexChanges);
				diff.ForeignKeysChanged = foreignKeysChanged;
				response.Tables.Add(diff);
			}
		}

		return response;
	}

	private static bool ColumnsEqual(SqlTableColumn left, SqlTableColumn right)
	{
		return string.Equals(left.ClrTypeName, right.ClrTypeName, StringComparison.OrdinalIgnoreCase)
			&& (left.IsNullable == right.IsNullable)
			&& (left.IsPrimaryKey == right.IsPrimaryKey)
			&& (left.IsAutoIncrement == right.IsAutoIncrement)
			&& (left.IsUnique == right.IsUnique)
			&& (left.MaxLength == right.MaxLength)
			&& string.Equals(left.DefaultValue ?? string.Empty, right.DefaultValue ?? string.Empty, StringComparison.Ordinal);
	}

	private static List<ColumnChange> DiffColumns(SqlTable fromTable, SqlTable toTable)
	{
		var changes = new List<ColumnChange>();
		var fromColumns = ToColumnMap(fromTable.Columns);
		var toColumns = ToColumnMap(toTable.Columns);

		foreach (var name in toColumns.Keys.Union(fromColumns.Keys).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			fromColumns.TryGetValue(name, out var fromColumn);
			toColumns.TryGetValue(name, out var toColumn);

			if (fromColumn == null)
			{
				changes.Add(new ColumnChange(ColumnAction.Add, toColumn));
				continue;
			}

			if (toColumn == null)
			{
				changes.Add(new ColumnChange(ColumnAction.Drop, fromColumn));
				continue;
			}

			if (!ColumnsEqual(fromColumn, toColumn))
			{
				changes.Add(new ColumnChange(ColumnAction.Modify, toColumn, fromColumn));
			}
		}

		return changes;
	}

	private static List<IndexChange> DiffIndexes(SqlTable fromTable, SqlTable toTable)
	{
		var changes = new List<IndexChange>();
		var fromMap = fromTable.Indexes.ToDictionary(IndexKey, StringComparer.OrdinalIgnoreCase);
		var toMap = toTable.Indexes.ToDictionary(IndexKey, StringComparer.OrdinalIgnoreCase);
		foreach (var key in toMap.Keys.Union(fromMap.Keys).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			fromMap.TryGetValue(key, out var fromIndex);
			toMap.TryGetValue(key, out var toIndex);
			if (fromIndex == null)
			{
				changes.Add(new IndexChange(ColumnAction.Add, toIndex));
			}
			else if (toIndex == null)
			{
				changes.Add(new IndexChange(ColumnAction.Drop, fromIndex));
			}
		}

		return changes;
	}

	private static bool ForeignKeysEqual(SqlForeignKey left, SqlForeignKey right)
	{
		if (!string.Equals(left.PrincipalTable, right.PrincipalTable, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(left.PrincipalColumn, right.PrincipalColumn, StringComparison.OrdinalIgnoreCase)
			|| (left.Columns.Count != right.Columns.Count))
		{
			return false;
		}

		for (var i = 0; i < left.Columns.Count; i++)
		{
			if (!string.Equals(left.Columns[i].ColumnName, right.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return true;
	}

	private static bool ForeignKeysMatch(SqlTable fromTable, SqlTable toTable)
	{
		if (fromTable.ForeignKeys.Count != toTable.ForeignKeys.Count)
		{
			return false;
		}

		foreach (var fk in toTable.ForeignKeys)
		{
			if (!fromTable.ForeignKeys.Any(left => ForeignKeysEqual(left, fk)))
			{
				return false;
			}
		}

		return true;
	}

	private static string IndexKey(SqlIndex index)
	{
		var columns = string.Join(",", index.Columns.Select(x => x.ColumnName.ToUpperInvariant()));
		return (index.IsUnique ? "U:" : "I:") + columns;
	}

	private static Dictionary<string, SqlTableColumn> ToColumnMap(List<SqlTableColumn> columns)
	{
		return columns.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
	}

	private static Dictionary<string, SqlTable> ToTableMap(List<SqlTable> tables)
	{
		return tables.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
	}

	#endregion
}