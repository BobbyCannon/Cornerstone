#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Selection;

public interface ITreeDataGridColumnSelectionModel : ISelectionModel
{
	#region Properties

	new IColumn SelectedItem { get; set; }
	new IReadOnlyList<IColumn> SelectedItems { get; }

	#endregion
}