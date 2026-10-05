namespace Cornerstone.Presentation.Controls.BookmarkBar;

/// <summary>
/// Drag-drop payload for reordering a bookmark relative to another node.
/// </summary>
public sealed class BookmarkBarMove
{
	#region Properties

	public bool InsertBefore { get; set; }

	public bool IntoFolder { get; set; }

	public IBookmarkBarItem Source { get; set; }

	public IBookmarkBarItem Target { get; set; }

	#endregion
}
