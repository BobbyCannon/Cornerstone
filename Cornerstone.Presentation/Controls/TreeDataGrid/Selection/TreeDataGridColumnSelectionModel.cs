#region References

using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Selection;

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