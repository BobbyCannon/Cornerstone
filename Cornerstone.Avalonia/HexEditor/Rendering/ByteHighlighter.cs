#region References

using Avalonia.Media;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Provides a base for a byte-level highlighter in a hex view.
/// </summary>
public abstract class ByteHighlighter : ILineTransformer
{
	#region Properties

	/// <summary>
	/// Gets or sets the brush used for rendering the background of the highlighted bytes.
	/// </summary>
	public IBrush Background { get; set; }

	/// <summary>
	/// Gets or sets the brush used for rendering the foreground of the highlighted bytes.
	/// </summary>
	public IBrush Foreground { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Transform(HexView hexView, VisualBytesLine line)
	{
		for (var i = 0; i < line.Segments.Count; i++)
		{
			ColorizeSegment(hexView, line, ref i);
		}
	}

	/// <summary>
	/// Determines whether the provided location is highlighted or not.
	/// </summary>
	/// <param name="hexView"> </param>
	/// <param name="line"> </param>
	/// <param name="location"> </param>
	/// <returns> </returns>
	protected abstract bool IsHighlighted(HexView hexView, VisualBytesLine line, BitLocation location);

	private void ColorizeSegment(HexView hexView, VisualBytesLine line, ref int index)
	{
		var originalSegment = line.Segments[index];

		var currentSegment = originalSegment;

		var isInModifiedRange = false;
		for (ulong j = 0; j < originalSegment.Range.ByteLength; j++)
		{
			var currentLocation = new BitLocation(originalSegment.Range.Start.ByteIndex + j);

			var shouldSplit = IsHighlighted(hexView, line, currentLocation) ? !isInModifiedRange : isInModifiedRange;
			if (!shouldSplit)
			{
				continue;
			}

			isInModifiedRange = !isInModifiedRange;

			// Split the segment.
			var (left, right) = currentSegment.Split(currentLocation);

			if (isInModifiedRange)
			{
				// We entered a highlighted segment.
				right.ForegroundBrush = Foreground;
				right.BackgroundBrush = Background;
			}
			else
			{
				// We left a highlighted segment.
				right.ForegroundBrush = originalSegment.ForegroundBrush;
				right.BackgroundBrush = originalSegment.BackgroundBrush;
			}

			// Insert the ranges.
			if (left.Range.IsEmpty)
			{
				// Optimization. Just replace the left segment if it is empty.
				line.Segments[index] = right;
			}
			else
			{
				line.Segments[index] = left;
				line.Segments.Insert(index + 1, right);
				index++;
			}

			currentSegment = right;
		}
	}

	#endregion
}