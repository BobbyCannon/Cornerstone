#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

public interface ITreeDataGridRowSelectionModel<T> : ITreeDataGridRowSelectionModel
{
	#region Properties

	new T SelectedItem { get; }

	new IReadOnlyList<T> SelectedItems { get; }

	#endregion

	#region Events

	new event EventHandler<TreeSelectionModelSelectionChangedEventArgs<T>> SelectionChanged;

	#endregion
}

public interface ITreeDataGridRowSelectionModel : ITreeSelectionModel
{
}