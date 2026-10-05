#region References

using System.Collections;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Selection;

public interface ITreeDataGridSelection
{
	#region Properties

	IEnumerable Source { get; set; }

	#endregion
}