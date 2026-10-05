#region References

using Cornerstone.Storage.Sql.Data;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

public class IndexChange
{
	#region Constructors

	public IndexChange(ColumnAction action, SqlIndex index)
	{
		Action = action;
		Index = index;
	}

	#endregion

	#region Properties

	public ColumnAction Action { get; }

	public SqlIndex Index { get; }

	#endregion
}