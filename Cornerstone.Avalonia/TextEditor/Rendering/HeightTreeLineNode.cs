#region References

using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

internal struct HeightTreeLineNode
{
	#region Fields

	internal List<CollapsedLineSection> CollapsedSections;

	internal double Height;

	#endregion

	#region Constructors

	internal HeightTreeLineNode(double height)
	{
		CollapsedSections = null;
		Height = height;
	}

	#endregion

	#region Properties

	internal bool IsDirectlyCollapsed => CollapsedSections != null;

	/// <summary>
	/// Returns 0 if the line is directly collapsed, otherwise, returns <see cref="Height" />.
	/// </summary>
	internal double TotalHeight => IsDirectlyCollapsed ? 0 : Height;

	#endregion

	#region Methods

	internal void AddDirectlyCollapsed(CollapsedLineSection section)
	{
		if (CollapsedSections == null)
		{
			CollapsedSections = [];
		}
		CollapsedSections.Add(section);
	}

	internal void RemoveDirectlyCollapsed(CollapsedLineSection section)
	{
		Debug.Assert(CollapsedSections.Contains(section));
		CollapsedSections.Remove(section);
		if (CollapsedSections.Count == 0)
		{
			CollapsedSections = null;
		}
	}

	#endregion
}