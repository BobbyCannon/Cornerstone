#region References

using System.Collections.Generic;
using Avalonia.Controls.Selection;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

public interface ITreeDataGridColumnSelectionModel : ISelectionModel
{
	#region Properties

	new IColumn SelectedItem { get; set; }
	new IReadOnlyList<IColumn> SelectedItems { get; }

	#endregion
}