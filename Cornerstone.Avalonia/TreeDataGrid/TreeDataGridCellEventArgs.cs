#region References

using System;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

public class TreeDataGridCellEventArgs
{
	#region Constructors

	public TreeDataGridCellEventArgs(Control cell, int columnIndex, int rowIndex)
	{
		Cell = cell;
		ColumnIndex = columnIndex;
		RowIndex = rowIndex;
	}

	internal TreeDataGridCellEventArgs()
	{
		Cell = null!;
	}

	#endregion

	#region Properties

	public Control Cell { get; private set; }
	public int ColumnIndex { get; private set; }
	public int RowIndex { get; private set; }

	#endregion

	#region Methods

	internal void Update(Control cell, int columnIndex, int rowIndex)
	{
		if (cell is not null && Cell is not null)
		{
			throw new NotSupportedException("Nested TreeDataGrid cell prepared/clearing detected.");
		}

		Cell = cell!;
		ColumnIndex = columnIndex;
		RowIndex = rowIndex;
	}

	#endregion
}