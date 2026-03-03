#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

internal class SelectedCellIndexes : IReadOnlyList<CellIndex>
{
	#region Fields

	private readonly ITreeDataGridColumnSelectionModel _selectedColumns;
	private readonly ITreeDataGridRowSelectionModel _selectedRows;

	#endregion

	#region Constructors

	public SelectedCellIndexes(
		ITreeDataGridColumnSelectionModel selectedColumns,
		ITreeDataGridRowSelectionModel selectedRows)
	{
		_selectedColumns = selectedColumns;
		_selectedRows = selectedRows;
	}

	#endregion

	#region Properties

	public int Count => _selectedColumns.Count * _selectedRows.Count;

	public CellIndex this[int index]
	{
		get
		{
			if ((index < 0) || (index >= Count))
			{
				throw new IndexOutOfRangeException("The index was out of range.");
			}
			var column = _selectedColumns.SelectedIndexes[index % _selectedColumns.Count];
			var row = _selectedRows.SelectedIndexes[index / _selectedColumns.Count];
			return new(column, row);
		}
	}

	#endregion

	#region Methods

	public IEnumerator<CellIndex> GetEnumerator()
	{
		for (var i = 0; i < Count; ++i)
		{
			yield return this[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		for (var i = 0; i < Count; ++i)
		{
			yield return this[i];
		}
	}

	#endregion
}