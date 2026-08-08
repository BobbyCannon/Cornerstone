#region References

using System;
using System.Collections.Generic;
using Avalonia;
using Cornerstone.Avalonia.Text.Rendering;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Search;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public partial class Line : TextRange
{
	#region Fields

	private readonly LineManager _lineManager;

	#endregion

	#region Constructors

	public Line(LineManager lineManager)
	{
		_lineManager = lineManager;

		WrappedStartOffsets = new SpeedyList<int>(16);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The amount of line ending characters.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int LineEndingLength { get; set; }

	/// <summary>
	/// The number of the line.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int LineNumber { get; set; }

	/// <summary>
	/// The inclusive offset (start) of the selection.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Rect VisualLayout { get; private set; }

	/// <summary>
	/// The indexes of wrap line breaks. The beginning of new virtual lines.
	/// </summary>
	internal SpeedyList<int> WrappedStartOffsets { get; }

	#endregion

	#region Methods

	public int GetLineEnd(int offset, bool isAtEndOfLine)
	{
		if (isAtEndOfLine)
		{
			return offset;
		}

		var index = BinarySearch.FindCeilIndex(WrappedStartOffsets, offset + 1);
		if ((index >= 0) && (WrappedStartOffsets.Count > 0))
		{
			return WrappedStartOffsets[index];
		}

		return LineEndingLength > 0 ? EndOffset - LineEndingLength : EndOffset;
	}

	public int GetLineStart(Caret caret)
	{
		return BinarySearch.FindFloor(WrappedStartOffsets, caret.IsAtEndOfLine ? caret.Offset - 1 : caret.Offset, StartOffset);
	}

	/// <summary>
	/// Returns the offset in this line using visual position
	/// closest to the given visual location (visualX, visualY).
	/// </summary>
	public int GetNearestOffsetAtVisual(double visualX, double visualY, bool isAtEndOfLine)
	{
		var relativeY = Math.Clamp(visualY - VisualLayout.Y, 0, VisualLayout.Height);
		var subLineIndex = (int) (relativeY / _lineManager.ViewModel.ViewMetrics.CharacterHeight);
		subLineIndex = Math.Min(subLineIndex, WrappedStartOffsets.Count);

		// Start of this visual subline (absolute offset)
		var start = subLineIndex == 0
			? StartOffset
			: WrappedStartOffsets[subLineIndex - 1];

		// End of this visual subline (exclusive)
		var endExclusive = subLineIndex < WrappedStartOffsets.Count
			? WrappedStartOffsets[subLineIndex]
			: EndOffset;

		if (isAtEndOfLine || (EndOffset == _lineManager.ViewModel.DocumentLength))
		{
			endExclusive += 1;
		}

		if (start >= endExclusive)
		{
			return start;
		}

		var currentX = 0.0;
		var spans = _lineManager.ViewModel.Buffer.GetReadOnlySpans(start, endExclusive - start - 1);
		var response = start;

		// Process characters in this subline only
		foreach (var c in spans.BeforeGap)
		{
			if (CheckVisualX(c))
			{
				return response;
			}
		}

		foreach (var c in spans.AfterGap)
		{
			if (CheckVisualX(c))
			{
				return response;
			}
		}

		// Clicked past the end of this wrapped subline
		if ((endExclusive == EndOffset)
			&& (LineEndingLength > 0))
		{
			return EndOffset - LineEndingLength;
		}

		return response;

		bool CheckVisualX(char c)
		{
			var advance = _lineManager.ViewModel.ViewMetrics.GetAdvance(c);

			// Split the character's visual width in half
			var midpoint = currentX + (advance / 2.0);
			if (visualX < midpoint)
			{
				// Clicked on the left half, caret before this character
				return true;
			}

			currentX += advance;
			response++;

			if (visualX < currentX)
			{
				// Clicked on the left half, caret after this character
				return true;
			}

			return false;
		}
	}

	public void Reset(int lineNumber, int startOffset)
	{
		LineNumber = lineNumber;
		StartOffset = startOffset;
		EndOffset = startOffset;
		LineEndingLength = 0;
		VisualLayout = new Rect();
	}

	public override string ToString()
	{
		// todo: add more checks
		return Length > 0
			? _lineManager.ViewModel.Buffer.Substring(StartOffset, Length)
			: string.Empty;
	}

	/// <summary>
	/// Returns the visual rectangle (position + size) of the caret at the given document offset.
	/// The returned X/Y are relative to the line's visual origin (consistent with GetNearestOffsetAtVisual).
	/// </summary>
	public Rect UpdateCaretVisual(Caret caret)
	{
		if (Length == 0)
		{
			return new Rect(
				VisualLayout.Left,
				VisualLayout.Y,
				_lineManager.ViewModel.ViewMetrics.CharacterWidth,
				_lineManager.ViewModel.ViewMetrics.CharacterHeight
			);
		}

		// Limit to line end offset
		var offset = Math.Min(caret.Offset, EndOffset);
		var subLineIndex = BinarySearch.FindFloorIndex(WrappedStartOffsets, caret.Offset);

		if (caret.IsAtEndOfLine
			&& (subLineIndex >= 0)
			&& (subLineIndex < WrappedStartOffsets.Count)
			&& (WrappedStartOffsets[subLineIndex] == caret.Offset))
		{
			// Caret offset is at the beginning of a virtual line but wants to be at
			// the end of the previous virtual line.
			subLineIndex -= 1;
		}

		var start = (subLineIndex >= 0) && (subLineIndex < WrappedStartOffsets.Count) ? WrappedStartOffsets[subLineIndex] : StartOffset;
		var metrics = _lineManager.ViewModel.ViewMetrics;
		var subLine = subLineIndex + 1;
		var y = VisualLayout.Y + (subLine * metrics.CharacterHeight);
		var x = 0.0;

		var charsToMeasure = offset - start;
		if (charsToMeasure > 0)
		{
			var spans = _lineManager.ViewModel.Buffer.GetReadOnlySpans(start, charsToMeasure);
			foreach (var c in spans.BeforeGap)
			{
				x += metrics.GetAdvance(c);
			}

			foreach (var c in spans.AfterGap)
			{
				x += metrics.GetAdvance(c);
			}
		}

		return new Rect(x, y, metrics.CharacterWidth, metrics.CharacterHeight);
	}

	/// <summary>
	/// Number of visual rows this logical line occupies (1 when not wrapped).
	/// </summary>
	public int VisualSubLineCount => WrappedStartOffsets.Count + 1;

	/// <summary>
	/// Document offsets for a visual subline. <paramref name="endExclusive"/> is exclusive.
	/// </summary>
	public void GetVisualSubLineRange(int subIndex, out int start, out int endExclusive)
	{
		var count = VisualSubLineCount;
		if ((subIndex < 0) || (subIndex >= count))
		{
			start = StartOffset;
			endExclusive = StartOffset;
			return;
		}

		start = subIndex == 0 ? StartOffset : WrappedStartOffsets[subIndex - 1];
		endExclusive = subIndex < WrappedStartOffsets.Count
			? WrappedStartOffsets[subIndex]
			: EndOffset;
	}

	/// <summary>
	/// Horizontal advance in document pixels from a visual subline start to <paramref name="documentOffset"/>,
	/// using the same <see cref="ViewMetrics.GetAdvance"/> rules as caret and wrap layout.
	/// </summary>
	public double GetVisualX(int subLineStartOffset, int documentOffset)
	{
		var metrics = _lineManager.ViewModel.ViewMetrics;
		var end = Math.Clamp(documentOffset, subLineStartOffset, EndOffset);
		var charsToMeasure = end - subLineStartOffset;
		if (charsToMeasure <= 0)
		{
			return 0;
		}

		var x = 0.0;
		var spans = _lineManager.ViewModel.Buffer.GetReadOnlySpans(subLineStartOffset, charsToMeasure);
		foreach (var c in spans.BeforeGap)
		{
			x += metrics.GetAdvance(c);
		}

		foreach (var c in spans.AfterGap)
		{
			x += metrics.GetAdvance(c);
		}

		return x;
	}

	/// <summary>
	/// Selection rectangles in document space (not screen space) for the intersection of
	/// [selectionStart, selectionEnd) with this logical line, one rect per visual subline.
	/// Uses <see cref="WrappedStartOffsets"/> + <see cref="ViewMetrics.GetAdvance"/> so
	/// highlights track soft wrap the same way as caret and text paint.
	/// </summary>
	public IEnumerable<Rect> GetSelectionRects(int selectionStart, int selectionEnd)
	{
		if (selectionEnd <= selectionStart)
		{
			yield break;
		}

		var metrics = _lineManager.ViewModel.ViewMetrics;
		var height = metrics.CharacterHeight;
		if (height <= 0)
		{
			yield break;
		}

		var lineSelStart = Math.Max(selectionStart, StartOffset);
		var lineSelEnd = Math.Min(selectionEnd, EndOffset);
		if (lineSelStart >= lineSelEnd)
		{
			yield break;
		}

		var subLineCount = VisualSubLineCount;
		for (var sub = 0; sub < subLineCount; sub++)
		{
			GetVisualSubLineRange(sub, out var subStart, out var subEnd);
			var selStart = Math.Max(lineSelStart, subStart);
			var selEnd = Math.Min(lineSelEnd, subEnd);
			if (selStart >= selEnd)
			{
				continue;
			}

			var x0 = GetVisualX(subStart, selStart);
			var x1 = GetVisualX(subStart, selEnd);
			var width = Math.Max(0, x1 - x0);
			if (width <= 0)
			{
				// Newlines and other zero-advance spans produce no visible fill.
				continue;
			}

			var y = VisualLayout.Y + (sub * height);
			yield return new Rect(x0, y, width, height);
		}
	}

	internal IEnumerable<string> GetLines()
	{
		var buffer = _lineManager.ViewModel.Buffer;
		var subLineCount = VisualSubLineCount;

		for (var sub = 0; sub < subLineCount; sub++)
		{
			GetVisualSubLineRange(sub, out var start, out var endExclusive);
			yield return buffer.Substring(start, endExclusive - start);
		}
	}

	internal void UpdateLineMetrics(double offsetY, double? visualMaxWidth)
	{
		var maxWidth = 0.0;
		var currentWidth = 0.0;
		var lines = 1;
		var lastBreakIndex = -1;
		var lastBreakWidth = 0.0;
		var spans = _lineManager.ViewModel.Buffer.GetReadOnlySpans(StartOffset, Length);

		WrappedStartOffsets.Clear();

		var logicalOffset = StartOffset;
		var atLineStart = true;

		if (!spans.BeforeGap.IsEmpty)
		{
			ProcessSpan(spans.BeforeGap, logicalOffset);
			logicalOffset += spans.BeforeGap.Length;
		}
		if (!spans.AfterGap.IsEmpty)
		{
			ProcessSpan(spans.AfterGap, logicalOffset);
		}

		if (maxWidth == 0)
		{
			maxWidth = currentWidth;
		}

		VisualLayout = new Rect(0, offsetY, maxWidth, lines * _lineManager.ViewModel.ViewMetrics.CharacterHeight);
		return;

		void ProcessSpan(ReadOnlySpan<char> span, int baseOffset)
		{
			var end = span.Length - 1;

			for (var i = 0; i <= end; i++)
			{
				var c = span[i];
				var n = i < end ? span[i + 1] : '\0';

				var advance = _lineManager.ViewModel.ViewMetrics.GetAdvance(c);

				if (!atLineStart && char.IsWhiteSpace(c))
				{
					lastBreakIndex = baseOffset + i;
					lastBreakWidth = currentWidth + advance;
				}
				else
				{
					atLineStart = false;
				}

				currentWidth += advance;

				if (!visualMaxWidth.HasValue
					|| !((currentWidth + advance) > visualMaxWidth.Value))
				{
					continue;
				}

				// Line needs to break
				if ((lastBreakIndex >= StartOffset)
					&& !char.IsWhiteSpace(n))
				{
					maxWidth = Math.Max(maxWidth, lastBreakWidth);
					currentWidth -= lastBreakWidth;

					lines++;
					WrappedStartOffsets.Add(lastBreakIndex + 1);
				}
				else
				{
					maxWidth = Math.Max(maxWidth, currentWidth);
					currentWidth = 0;
					lines++;
					atLineStart = true;
					WrappedStartOffsets.Add(baseOffset + i + 1);
				}

				lastBreakIndex = -1;
				lastBreakWidth = 0;
			}
		}
	}

	#endregion
}