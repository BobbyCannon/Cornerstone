#region References

using System;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Navigation;

public class BreadcrumbClickedEventArgs : EventArgs
{
	#region Constructors

	public BreadcrumbClickedEventArgs(BreadcrumbSegment segment, int index)
	{
		Segment = segment;
		Index = index;
	}

	#endregion

	#region Properties

	public int Index { get; }

	public BreadcrumbSegment Segment { get; }

	#endregion
}