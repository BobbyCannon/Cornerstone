#region References

using System.Collections.Generic;
using Cornerstone.Storage.Sql.Data;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// One table create, drop, or alter. Table is the desired shape on Create and the previous shape on Drop.
/// Columns is populated on Alter.
/// </summary>
public class TableDiff
{
	#region Constructors

	public TableDiff(TableAction action, SqlTable table)
		: this(action, table, [])
	{
	}

	public TableDiff(TableAction action, SqlTable table, List<ColumnChange> columns)
	{
		Action = action;
		Table = table;
		Columns = columns ?? [];
		IndexChanges = [];
		ForeignKeysChanged = false;
	}

	#endregion

	#region Properties

	public TableAction Action { get; }

	public List<ColumnChange> Columns { get; }

	public bool ForeignKeysChanged { get; set; }

	public List<IndexChange> IndexChanges { get; }

	public SqlTable Table { get; }

	public string TableName => Table != null ? Table.Name : string.Empty;

	#endregion
}