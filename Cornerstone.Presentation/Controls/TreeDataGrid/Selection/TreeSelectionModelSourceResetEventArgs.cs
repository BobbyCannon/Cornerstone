#region References

using System;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Selection;

public class TreeSelectionModelSourceResetEventArgs : EventArgs
{
	#region Constructors

	public TreeSelectionModelSourceResetEventArgs(IndexPath parentIndex)
	{
		ParentIndex = parentIndex;
	}

	#endregion

	#region Properties

	public IndexPath ParentIndex { get; }

	#endregion
}