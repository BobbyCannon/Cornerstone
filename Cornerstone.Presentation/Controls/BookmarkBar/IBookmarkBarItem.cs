#region References

using System;
using System.Collections;

#endregion

namespace Cornerstone.Presentation.Controls.BookmarkBar;

/// <summary>
/// One node on a bookmark bar. Folders expose child nodes through <see cref="Children" />.
/// </summary>
public interface IBookmarkBarItem
{
	#region Properties

	/// <summary>
	/// Child nodes when <see cref="IsParent" /> is true.
	/// </summary>
	IEnumerable Children { get; }

	/// <summary>
	/// Favicon bytes for a link. Empty when the node is a folder or has no icon.
	/// </summary>
	byte[] Favicon { get; }

	/// <summary>
	/// True when this node is a folder.
	/// </summary>
	bool IsParent { get; }

	/// <summary>
	/// Label shown on the chip.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// True when a link chip shows its icon without the label.
	/// </summary>
	bool ShowIconOnly { get; set; }

	/// <summary>
	/// Stable id used to find the node while dragging.
	/// </summary>
	Guid SyncId { get; }

	#endregion
}
