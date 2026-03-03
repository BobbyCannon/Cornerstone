#region References

using Avalonia.Controls.Selection;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

public class TreeDataGridColumnSelectionModel : SelectionModel<IColumn>,
	ITreeDataGridColumnSelectionModel
{
	#region Constructors

	public TreeDataGridColumnSelectionModel(IColumns columns)
		: base(columns)
	{
	}

	#endregion
}