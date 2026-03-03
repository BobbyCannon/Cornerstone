#region References

using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

internal interface ITreeDataGridCell
{
	#region Properties

	int ColumnIndex { get; }

	#endregion

	#region Methods

	void Realize(
		TreeDataGridElementFactory factory,
		ITreeDataGridSelectionInteraction selection,
		ICell model,
		int columnIndex,
		int rowIndex);

	void Unrealize();

	#endregion
}