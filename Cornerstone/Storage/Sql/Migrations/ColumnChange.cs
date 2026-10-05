#region References

using Cornerstone.Storage.Sql.Data;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// One column add, drop, or modify. Column is the desired (Add/Modify) or removed (Drop) shape.
/// Previous is set on Modify.
/// </summary>
public class ColumnChange
{
	#region Constructors

	public ColumnChange(ColumnAction action, SqlTableColumn column)
		: this(action, column, null)
	{
	}

	public ColumnChange(ColumnAction action, SqlTableColumn column, SqlTableColumn previous)
	{
		Action = action;
		Column = column;
		Previous = previous;
	}

	#endregion

	#region Properties

	public ColumnAction Action { get; }

	public SqlTableColumn Column { get; }

	public string Name => Column != null ? Column.Name : Previous?.Name;

	public SqlTableColumn Previous { get; }

	#endregion
}