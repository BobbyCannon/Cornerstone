#region References

using Cornerstone.Presentation.Controls.TreeDataGrid.Models;
using Cornerstone.Presentation.Controls.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Cells;

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