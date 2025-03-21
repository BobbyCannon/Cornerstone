﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Helper for creating a PathGeometry.
/// </summary>
public sealed class BackgroundGeometryBuilder
{
	#region Fields

	private PathFigure _figure;

	private readonly PathFigures _figures = [];
	private int _insertionIndex;
	private double _lastLeft, _lastRight;
	private double _lastTop, _lastBottom;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new BackgroundGeometryBuilder instance.
	/// </summary>
	public BackgroundGeometryBuilder()
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets whether to align to whole pixels.
	/// If BorderThickness is set to 0, the geometry is aligned to whole pixels.
	/// If BorderThickness is set to a non-zero value, the outer edge of the border is aligned
	/// to whole pixels.
	/// The default value is <c> false </c>.
	/// </summary>
	public bool AlignToWholePixels { get; set; }

	/// <summary>
	/// Gets/sets the border thickness.
	/// This property only has an effect if <c> AlignToWholePixels </c> is enabled.
	/// When using the resulting geometry to paint a border, set this property to the border thickness.
	/// Otherwise, leave the property set to the default value <c> 0 </c>.
	/// </summary>
	public double BorderThickness { get; set; }

	/// <summary>
	/// Gets/sets the radius of the rounded corners.
	/// </summary>
	public double CornerRadius { get; set; }

	/// <summary>
	/// Gets/Sets whether to extend the rectangles to full width at line end.
	/// </summary>
	public bool ExtendToFullWidthAtLineEnd { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds a rectangle to the geometry.
	/// </summary>
	/// <remarks>
	/// This overload will align the coordinates according to
	/// <see cref="AlignToWholePixels" />.
	/// Use the <see cref="AddRectangle(double,double,double,double)" />-overload instead if the coordinates should not be aligned.
	/// </remarks>
	public void AddRectangle(TextView textView, Rect rectangle)
	{
		AddRectangle(PixelSnapHelpers.GetPixelSize(textView), rectangle);
	}

	/// <summary>
	/// Adds a rectangle to the geometry.
	/// </summary>
	/// <remarks>
	/// This overload assumes that the coordinates are aligned properly
	/// (see <see cref="AlignToWholePixels" />).
	/// Use the <see cref="AddRectangle(TextView,Rect)" />-overload instead if the coordinates are not yet aligned.
	/// </remarks>
	public void AddRectangle(double left, double top, double right, double bottom)
	{
		if (!top.IsClose(_lastBottom))
		{
			CloseFigure();
		}
		if (_figure == null)
		{
			_figure = new PathFigure();
			_figure.StartPoint = new Point(left, top + CornerRadius);
			if (Math.Abs(left - right) > CornerRadius)
			{
				_figure.Segments.Add(MakeArc(left + CornerRadius, top, SweepDirection.Clockwise));
				_figure.Segments.Add(MakeLineSegment(right - CornerRadius, top));
				_figure.Segments.Add(MakeArc(right, top + CornerRadius, SweepDirection.Clockwise));
			}
			_figure.Segments.Add(MakeLineSegment(right, bottom - CornerRadius));
			_insertionIndex = _figure.Segments.Count;
			//figure.Segments.Add(MakeArc(left, bottom - cornerRadius, SweepDirection.Clockwise));
		}
		else
		{
			if (!_lastRight.IsClose(right))
			{
				var cr = right < _lastRight ? -CornerRadius : CornerRadius;
				var dir1 = right < _lastRight ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
				var dir2 = right < _lastRight ? SweepDirection.CounterClockwise : SweepDirection.Clockwise;
				_figure.Segments.Insert(_insertionIndex++, MakeArc(_lastRight + cr, _lastBottom, dir1));
				_figure.Segments.Insert(_insertionIndex++, MakeLineSegment(right - cr, top));
				_figure.Segments.Insert(_insertionIndex++, MakeArc(right, top + CornerRadius, dir2));
			}
			_figure.Segments.Insert(_insertionIndex++, MakeLineSegment(right, bottom - CornerRadius));
			_figure.Segments.Insert(_insertionIndex, MakeLineSegment(_lastLeft, _lastTop + CornerRadius));
			if (!_lastLeft.IsClose(left))
			{
				var cr = left < _lastLeft ? CornerRadius : -CornerRadius;
				var dir1 = left < _lastLeft ? SweepDirection.CounterClockwise : SweepDirection.Clockwise;
				var dir2 = left < _lastLeft ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
				_figure.Segments.Insert(_insertionIndex, MakeArc(_lastLeft, _lastBottom - CornerRadius, dir1));
				_figure.Segments.Insert(_insertionIndex, MakeLineSegment(_lastLeft - cr, _lastBottom));
				_figure.Segments.Insert(_insertionIndex, MakeArc(left + cr, _lastBottom, dir2));
			}
		}
		_lastTop = top;
		_lastBottom = bottom;
		_lastLeft = left;
		_lastRight = right;
	}

	/// <summary>
	/// Adds the specified segment to the geometry.
	/// </summary>
	public void AddSegment(TextView textView, IRange range)
	{
		if (textView == null)
		{
			throw new ArgumentNullException("textView");
		}
		var pixelSize = PixelSnapHelpers.GetPixelSize(textView);
		foreach (var r in GetRectsForSegment(textView, range, ExtendToFullWidthAtLineEnd))
		{
			AddRectangle(pixelSize, r);
		}
	}

	/// <summary>
	/// Closes the current figure.
	/// </summary>
	public void CloseFigure()
	{
		if (_figure != null)
		{
			_figure.Segments.Insert(_insertionIndex, MakeLineSegment(_lastLeft, _lastTop + CornerRadius));
			if (Math.Abs(_lastLeft - _lastRight) > CornerRadius)
			{
				_figure.Segments.Insert(_insertionIndex, MakeArc(_lastLeft, _lastBottom - CornerRadius, SweepDirection.Clockwise));
				_figure.Segments.Insert(_insertionIndex, MakeLineSegment(_lastLeft + CornerRadius, _lastBottom));
				_figure.Segments.Insert(_insertionIndex, MakeArc(_lastRight - CornerRadius, _lastBottom, SweepDirection.Clockwise));
			}

			_figure.IsClosed = true;
			_figures.Add(_figure);
			_figure = null;
		}
	}

	/// <summary>
	/// Creates the geometry.
	/// Returns null when the geometry is empty!
	/// </summary>
	public Geometry CreateGeometry()
	{
		CloseFigure();
		return _figures.Count != 0 ? new PathGeometry { Figures = _figures } : null;
	}

	/// <summary>
	/// Calculates the list of rectangle where the segment in shown.
	/// This method usually returns one rectangle for each line inside the segment
	/// (but potentially more, e.g. when bidirectional text is involved).
	/// </summary>
	public static IEnumerable<Rect> GetRectsForSegment(TextView textView, IRange range, bool extendToFullWidthAtLineEnd = false)
	{
		if (textView == null)
		{
			throw new ArgumentNullException("textView");
		}
		if (range == null)
		{
			throw new ArgumentNullException("range");
		}
		return GetRectsForSegmentImpl(textView, range, extendToFullWidthAtLineEnd);
	}

	/// <summary>
	/// Calculates the rectangles for the visual column segment.
	/// This returns one rectangle for each line inside the segment.
	/// </summary>
	public static IEnumerable<Rect> GetRectsFromVisualSegment(TextView textView, VisualLine line, int startVc, int endVc)
	{
		if (textView == null)
		{
			throw new ArgumentNullException("textView");
		}
		if (line == null)
		{
			throw new ArgumentNullException("line");
		}
		return ProcessTextLines(textView, line, startVc, endVc);
	}

	private void AddRectangle(Size pixelSize, Rect r)
	{
		if (AlignToWholePixels)
		{
			var halfBorder = 0.5 * BorderThickness;
			AddRectangle(PixelSnapHelpers.Round(r.Left - halfBorder, pixelSize.Width) + halfBorder,
				PixelSnapHelpers.Round(r.Top - halfBorder, pixelSize.Height) + halfBorder,
				PixelSnapHelpers.Round(r.Right + halfBorder, pixelSize.Width) - halfBorder,
				PixelSnapHelpers.Round(r.Bottom + halfBorder, pixelSize.Height) - halfBorder);
			//Debug.WriteLine(r.ToString() + " -> " + new Rect(lastLeft, lastTop, lastRight-lastLeft, lastBottom-lastTop).ToString());
		}
		else
		{
			AddRectangle(r.Left, r.Top, r.Right, r.Bottom);
		}
	}

	private static IEnumerable<Rect> GetRectsForSegmentImpl(TextView textView, IRange range, bool extendToFullWidthAtLineEnd)
	{
		var segmentStart = range.StartIndex;
		var segmentEnd = range.StartIndex + range.Length;

		segmentStart = segmentStart.CoerceValue(0, textView.Document.TextLength);
		segmentEnd = segmentEnd.CoerceValue(0, textView.Document.TextLength);

		TextViewPosition start;
		TextViewPosition end;

		if (range is SelectionRange)
		{
			var sel = (SelectionRange) range;
			start = new TextViewPosition(textView.Document.GetLocation(sel.StartIndex), sel.StartVisualColumn);
			end = new TextViewPosition(textView.Document.GetLocation(sel.EndIndex), sel.EndVisualColumn);
		}
		else
		{
			start = new TextViewPosition(textView.Document.GetLocation(segmentStart));
			end = new TextViewPosition(textView.Document.GetLocation(segmentEnd));
		}

		foreach (var vl in textView.VisualLines)
		{
			var vlStartOffset = vl.FirstDocumentLine.StartIndex;
			if (vlStartOffset > segmentEnd)
			{
				break;
			}
			var vlEndOffset = vl.LastDocumentLine.StartIndex + vl.LastDocumentLine.Length;
			if (vlEndOffset < segmentStart)
			{
				continue;
			}

			int segmentStartVc;
			if (segmentStart < vlStartOffset)
			{
				segmentStartVc = 0;
			}
			else
			{
				segmentStartVc = vl.ValidateVisualColumn(start, extendToFullWidthAtLineEnd);
			}

			int segmentEndVc;
			if (segmentEnd > vlEndOffset)
			{
				segmentEndVc = extendToFullWidthAtLineEnd ? int.MaxValue : vl.VisualLengthWithEndOfLineMarker;
			}
			else
			{
				segmentEndVc = vl.ValidateVisualColumn(end, extendToFullWidthAtLineEnd);
			}

			foreach (var rect in ProcessTextLines(textView, vl, segmentStartVc, segmentEndVc))
			{
				yield return rect;
			}
		}
	}

	private ArcSegment MakeArc(double x, double y, SweepDirection dir)
	{
		var arc = new ArcSegment
		{
			Point = new Point(x, y),
			Size = new Size(CornerRadius, CornerRadius),
			SweepDirection = dir
		};
		return arc;
	}

	private static LineSegment MakeLineSegment(double x, double y)
	{
		return new LineSegment { Point = new Point(x, y) };
	}

	private static IEnumerable<Rect> ProcessTextLines(TextView textView, VisualLine visualLine, int segmentStartVc, int segmentEndVc)
	{
		var lastTextLine = visualLine.TextLines.Last();
		var scrollOffset = textView.ScrollOffset;

		for (var i = 0; i < visualLine.TextLines.Count; i++)
		{
			var line = visualLine.TextLines[i];
			var y = visualLine.GetTextLineVisualYPosition(line, VisualYPosition.TextTop);
			var visualStartCol = visualLine.GetTextLineVisualStartColumn(line);
			var visualEndCol = visualStartCol + line.Length;
			if (line == lastTextLine)
			{
				visualEndCol -= 1; // 1 position for the TextEndOfParagraph
			}
			else
			{
				visualEndCol -= line.TrailingWhitespaceLength;
			}

			if (segmentEndVc < visualStartCol)
			{
				break;
			}
			if ((lastTextLine != line) && (segmentStartVc > visualEndCol))
			{
				continue;
			}
			var segmentStartVcInLine = Math.Max(segmentStartVc, visualStartCol);
			var segmentEndVcInLine = Math.Min(segmentEndVc, visualEndCol);
			y -= scrollOffset.Y;
			Rect lastRect = default;
			if (segmentStartVcInLine == segmentEndVcInLine)
			{
				// GetTextBounds crashes for length=0, so we'll handle this case with GetDistanceFromCharacterHit
				// We need to return a rectangle to ensure empty lines are still visible
				var pos = visualLine.GetTextLineVisualXPosition(line, segmentStartVcInLine);
				pos -= scrollOffset.X;
				// The following special cases are necessary to get rid of empty rectangles at the end of a TextLine if "Show Spaces" is active.
				// If not excluded once, the same rectangle is calculated (and added) twice (since the offset could be mapped to two visual positions; end/start of line), if there is no trailing whitespace.
				// Skip this TextLine segment, if it is at the end of this line and this line is not the last line of the VisualLine and the selection continues and there is no trailing whitespace.
				if ((segmentEndVcInLine == visualEndCol) && (i < (visualLine.TextLines.Count - 1)) && (segmentEndVc > segmentEndVcInLine) && (line.TrailingWhitespaceLength == 0))
				{
					continue;
				}
				if ((segmentStartVcInLine == visualStartCol) && (i > 0) && (segmentStartVc < segmentStartVcInLine) && (visualLine.TextLines[i - 1].TrailingWhitespaceLength == 0))
				{
					continue;
				}
				lastRect = new Rect(pos, y, textView.EmptyLineSelectionWidth, line.Height);
			}
			else
			{
				if (segmentStartVcInLine <= visualEndCol)
				{
					foreach (var b in line.GetTextBounds(segmentStartVcInLine, segmentEndVcInLine - segmentStartVcInLine))
					{
						var left = b.Rectangle.Left - scrollOffset.X;
						var right = b.Rectangle.Right - scrollOffset.X;
						if (lastRect != default)
						{
							yield return lastRect;
						}
						// left>right is possible in RTL languages
						lastRect = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
					}
				}
			}
			// If the segment ends in virtual space, extend the last rectangle with the rectangle the portion of the selection
			// after the line end.
			// Also, when word-wrap is enabled and the segment continues into the next line, extend lastRect up to the end of the line.
			if (segmentEndVc > visualEndCol)
			{
				double left, right;
				if (segmentStartVc > visualLine.VisualLengthWithEndOfLineMarker)
				{
					// segmentStartVC is in virtual space
					left = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentStartVc);
				}
				else
				{
					// Otherwise, we already processed the rects from segmentStartVC up to visualEndCol,
					// so we only need to do the remainder starting at visualEndCol.
					// For word-wrapped lines, visualEndCol doesn't include the whitespace hidden by the wrap,
					// so we'll need to include it here.
					// For the last line, visualEndCol already includes the whitespace.
					left = line == lastTextLine ? line.WidthIncludingTrailingWhitespace : line.Width;
				}
				if ((line != lastTextLine) || (segmentEndVc == int.MaxValue))
				{
					// If word-wrap is enabled and the segment continues into the next line,
					// or if the extendToFullWidthAtLineEnd option is used (segmentEndVC == int.MaxValue),
					// we select the full width of the viewport.
					right = Math.Max(((ILogicalScrollable) textView).Extent.Width, ((ILogicalScrollable) textView).Viewport.Width);
				}
				else
				{
					right = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentEndVc);
				}
				var extendSelection = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
				if (lastRect != default)
				{
					if (extendSelection.Intersects(lastRect))
					{
						lastRect.Union(extendSelection);
						yield return lastRect;
					}
					else
					{
						// If the end of the line is in an RTL segment, keep lastRect and extendSelection separate.
						yield return lastRect;
						yield return extendSelection;
					}
				}
				else
				{
					yield return extendSelection;
				}
			}
			else
			{
				yield return lastRect;
			}
		}
	}

	#endregion
}