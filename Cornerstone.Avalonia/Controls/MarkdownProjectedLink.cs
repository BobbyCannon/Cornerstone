namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// A link projected into a <see cref="MarkdownBlockGroup" /> display buffer (markers stripped).
/// Offsets are relative to the group's content / <see cref="Text.TextRenderer" /> document.
/// </summary>
public readonly struct MarkdownProjectedLink
{
	#region Constructors

	public MarkdownProjectedLink(int startOffset, int endOffset, string href, string text)
	{
		StartOffset = startOffset;
		EndOffset = endOffset;
		Href = href ?? string.Empty;
		Text = text ?? string.Empty;
	}

	#endregion

	#region Properties

	public int EndOffset { get; }

	public string Href { get; }

	public int StartOffset { get; }

	public string Text { get; }

	#endregion

	#region Methods

	public bool Contains(int offset)
	{
		return (offset >= StartOffset) && (offset < EndOffset);
	}

	#endregion
}