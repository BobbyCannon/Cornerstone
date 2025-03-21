#region References

using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Highlights ranges of bytes within a document of a hex view.
/// </summary>
public class RangesHighlighter : ByteHighlighter
{
	#region Properties

	/// <summary>
	/// Gets the bit ranges that should be highlighted in the document.
	/// </summary>
	public BitRangeUnion Ranges { get; } = new();

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override bool IsHighlighted(HexView hexView, VisualBytesLine line, BitLocation location)
	{
		return Ranges.Contains(location);
	}

	#endregion
}