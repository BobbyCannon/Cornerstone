#region References

using System;
using Cornerstone.Avalonia.TreeDataGrid.Cells;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

public class TreeDataGridRowEventArgs
{
	#region Constructors

	public TreeDataGridRowEventArgs(TreeDataGridRow row, int rowIndex)
	{
		Row = row;
		RowIndex = rowIndex;
	}

	internal TreeDataGridRowEventArgs()
	{
		Row = null!;
	}

	#endregion

	#region Properties

	public TreeDataGridRow Row { get; private set; }
	public int RowIndex { get; private set; }

	#endregion

	#region Methods

	internal void Update(TreeDataGridRow row, int rowIndex)
	{
		if (row is not null && Row is not null)
		{
			throw new NotSupportedException("Nested TreeDataGrid row prepared/clearing detected.");
		}

		Row = row!;
		RowIndex = rowIndex;
	}

	#endregion
}