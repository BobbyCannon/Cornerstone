#region References

using System.Collections;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

public interface ITreeDataGridSelection
{
	#region Properties

	IEnumerable Source { get; set; }

	#endregion
}