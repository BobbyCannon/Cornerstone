#region References

#endregion

using Cornerstone.Text.Document;

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// A text section with syntax highlighting information.
/// </summary>
public class HighlightedSection : ISegment
{
	#region Properties

	/// <summary>
	/// Gets the highlighting color associated with the highlighted section.
	/// </summary>
	public HighlightingColor Color { get; set; }

	/// <summary>
	/// The end offset.
	/// </summary>
	public int EndOffset => Offset + Length;

	/// <summary>
	/// Gets/sets the length of the section.
	/// </summary>
	public int Length { get; set; }

	/// <summary>
	/// Gets/sets the document offset of the section.
	/// </summary>
	public int Offset { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[HighlightedSection ({Offset}-{Offset + Length})={Color}]";
	}

	#endregion
}