#region References

using System;
using System.Collections;

#endregion

namespace Cornerstone.Presentation.Controls.BookmarkBar;

/// <summary>
/// Hit testing for the bookmark bar. Horizontal chips use width; folder rows use height.
/// </summary>
public static class BookmarkBarDrop
{
	#region Fields

	/// <summary>
	/// Dwell before opening a folder submenu while dragging, matching menu hover delay.
	/// </summary>
	public static readonly TimeSpan FolderHoverDelay;

	#endregion

	#region Constructors

	static BookmarkBarDrop()
	{
		FolderHoverDelay = TimeSpan.FromMilliseconds(400);
	}

	#endregion

	#region Methods

	public static bool FolderHoverShouldOpen(bool targetIsParent, bool sameHoverItem, TimeSpan hoveredFor)
	{
		return targetIsParent && sameHoverItem && (hoveredFor >= FolderHoverDelay);
	}

	public static void Classify(
		double along,
		double extent,
		bool targetIsParent,
		out bool insertBefore,
		out bool intoFolder)
	{
		if (extent <= 0)
		{
			insertBefore = true;
			intoFolder = targetIsParent;
			return;
		}

		var edge = extent / 4;
		var isEdge = (along < edge) || (along > (extent - edge));
		intoFolder = !isEdge && targetIsParent;
		insertBefore = isEdge ? along < edge : along < (extent / 2);
	}

	public static IBookmarkBarItem FindItem(IEnumerable items, Guid syncId)
	{
		if ((items == null) || (syncId == Guid.Empty))
		{
			return null;
		}

		foreach (var entry in items)
		{
			if (entry is not IBookmarkBarItem item)
			{
				continue;
			}

			if (item.SyncId == syncId)
			{
				return item;
			}

			var nested = FindItem(item.Children, syncId);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	public static bool CannotDropOn(IBookmarkBarItem source, IBookmarkBarItem target)
	{
		if ((source == null) || (target == null) || (source.SyncId == target.SyncId))
		{
			return true;
		}

		return IsDescendant(source, target);
	}

	public static bool IsDescendant(IBookmarkBarItem folder, IBookmarkBarItem node)
	{
		if ((folder == null) || (node == null) || !folder.IsParent || (folder.Children == null))
		{
			return false;
		}

		foreach (var entry in folder.Children)
		{
			if (entry is not IBookmarkBarItem child)
			{
				continue;
			}

			if ((child.SyncId == node.SyncId) || IsDescendant(child, node))
			{
				return true;
			}
		}

		return false;
	}

	#endregion
}
