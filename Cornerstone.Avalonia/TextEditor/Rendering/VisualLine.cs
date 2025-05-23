﻿#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Internal;
using Cornerstone.Weaver;
using LogicalDirection = Cornerstone.Avalonia.TextEditor.Document.LogicalDirection;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Represents a visual line in the document.
/// A visual line usually corresponds to one DocumentLine, but it can span multiple lines if
/// all but the first are collapsed.
/// </summary>
public sealed class VisualLine
{
	#region Constants

	public const int LENGTH_LIMIT = 3000;

	#endregion

	#region Fields

	internal bool HasInlineObjects;
	private List<VisualLineElement> _elements;
	private LifetimePhase _phase;

	private ReadOnlyCollection<TextLine> _textLines;

	private readonly TextView _textView;

	private VisualLineDrawingVisual _visual;

	#endregion

	#region Constructors

	internal VisualLine(TextView textView, DocumentLine firstDocumentLine)
	{
		Debug.Assert(textView != null);
		Debug.Assert(firstDocumentLine != null);
		_textView = textView;
		Document = textView.Document;
		FirstDocumentLine = firstDocumentLine;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the document to which this VisualLine belongs.
	/// </summary>
	public TextEditorDocument Document { get; }

	/// <summary>
	/// Gets a read-only collection of line elements.
	/// </summary>
	public ReadOnlyCollection<VisualLineElement> Elements { get; private set; }

	/// <summary>
	/// Gets the first document line displayed by this visual line.
	/// </summary>
	public DocumentLine FirstDocumentLine { get; }

	/// <summary>
	/// Gets the height of the visual line in device-independent pixels.
	/// </summary>
	public double Height { get; private set; }

	/// <summary>
	/// Gets whether the visual line was disposed.
	/// </summary>
	public bool IsDisposed => _phase == LifetimePhase.Disposed;

	/// <summary>
	/// Gets the last document line displayed by this visual line.
	/// </summary>
	public DocumentLine LastDocumentLine { get; private set; }

	/// <summary>
	/// Gets the start offset of the VisualLine inside the document.
	/// This is equivalent to <c> FirstDocumentLine.Offset </c>.
	/// </summary>
	public int StartOffset => FirstDocumentLine.StartIndex;

	/// <summary>
	/// Gets a read-only collection of text lines.
	/// </summary>
	public ReadOnlyCollection<TextLine> TextLines
	{
		get
		{
			if (_phase < LifetimePhase.Live)
			{
				throw new InvalidOperationException();
			}
			return _textLines;
		}
	}

	/// <summary>
	/// Length in visual line coordinates.
	/// </summary>
	public int VisualLength { get; private set; }

	/// <summary>
	/// Length in visual line coordinates including the end of line marker, if TextEditorOptions.ShowEndOfLine is enabled.
	/// </summary>
	public int VisualLengthWithEndOfLineMarker
	{
		get
		{
			var length = VisualLength;
			if (_textView.Settings.ShowEndOfLine && (LastDocumentLine.NextLine != null))
			{
				length++;
			}
			return length;
		}
	}

	/// <summary>
	/// Gets the Y position of the line. This is measured in device-independent pixels relative to the start of the document.
	/// </summary>
	public double VisualTop { get; internal set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the next possible caret position after visualColumn, or -1 if there is no caret position.
	/// </summary>
	public int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode, bool allowVirtualSpace)
	{
		if (!HasStopsInVirtualSpace(mode))
		{
			allowVirtualSpace = false;
		}

		if (_elements.Count == 0)
		{
			// special handling for empty visual lines:
			if (allowVirtualSpace)
			{
				if (direction == LogicalDirection.Forward)
				{
					return Math.Max(0, visualColumn + 1);
				}
				if (visualColumn > 0)
				{
					return visualColumn - 1;
				}
				return -1;
			}
			// even though we don't have any elements,
			// there's a single caret stop at visualColumn 0
			if ((visualColumn < 0) && (direction == LogicalDirection.Forward))
			{
				return 0;
			}
			if ((visualColumn > 0) && (direction == LogicalDirection.Backward))
			{
				return 0;
			}
			return -1;
		}

		int i;
		if (direction == LogicalDirection.Backward)
		{
			// Search Backwards:
			// If the last element doesn't handle line borders, return the line end as caret stop

			if ((visualColumn > VisualLength) && !_elements[_elements.Count - 1].HandlesLineBorders && HasImplicitStopAtLineEnd())
			{
				if (allowVirtualSpace)
				{
					return visualColumn - 1;
				}
				return VisualLength;
			}
			// skip elements that start after or at visualColumn
			for (i = _elements.Count - 1; i >= 0; i--)
			{
				if (_elements[i].VisualColumn < visualColumn)
				{
					break;
				}
			}
			// search last element that has a caret stop
			for (; i >= 0; i--)
			{
				var pos = _elements[i].GetNextCaretPosition(
					Math.Min(visualColumn, _elements[i].VisualColumn + _elements[i].VisualLength + 1),
					direction, mode);
				if (pos >= 0)
				{
					return pos;
				}
			}
			// If we've found nothing, and the first element doesn't handle line borders,
			// return the line start as normal caret stop.
			if ((visualColumn > 0) && !_elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode))
			{
				return 0;
			}
		}
		else
		{
			// Search Forwards:
			// If the first element doesn't handle line borders, return the line start as caret stop
			if ((visualColumn < 0) && !_elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode))
			{
				return 0;
			}
			// skip elements that end before or at visualColumn
			for (i = 0; i < _elements.Count; i++)
			{
				if ((_elements[i].VisualColumn + _elements[i].VisualLength) > visualColumn)
				{
					break;
				}
			}
			// search first element that has a caret stop
			for (; i < _elements.Count; i++)
			{
				var pos = _elements[i].GetNextCaretPosition(
					Math.Max(visualColumn, _elements[i].VisualColumn - 1),
					direction, mode);
				if (pos >= 0)
				{
					return pos;
				}
			}
			// if we've found nothing, and the last element doesn't handle line borders,
			// return the line end as caret stop
			if ((allowVirtualSpace || !_elements[_elements.Count - 1].HandlesLineBorders) && HasImplicitStopAtLineEnd())
			{
				if (visualColumn < VisualLength)
				{
					return VisualLength;
				}
				if (allowVirtualSpace)
				{
					return visualColumn + 1;
				}
			}
		}
		// we've found nothing, return -1 and let the caret search continue in the next line
		return -1;
	}

	/// <summary>
	/// Gets the document offset (relative to the first line start) from a visual column.
	/// </summary>
	public int GetRelativeOffset(int visualColumn)
	{
		ThrowUtil.CheckNotNegative(visualColumn, "visualColumn");
		var documentLength = 0;
		foreach (var element in _elements)
		{
			if ((element.VisualColumn <= visualColumn)
				&& ((element.VisualColumn + element.VisualLength) > visualColumn))
			{
				return element.GetRelativeOffset(visualColumn);
			}
			documentLength += element.DocumentLength;
		}
		return documentLength;
	}

	/// <summary>
	/// Gets the text line containing the specified visual column.
	/// </summary>
	public TextLine GetTextLine(int visualColumn)
	{
		return GetTextLine(visualColumn, false);
	}

	/// <summary>
	/// Gets the text line containing the specified visual column.
	/// </summary>
	public TextLine GetTextLine(int visualColumn, bool isAtEndOfLine)
	{
		if (visualColumn < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(visualColumn));
		}
		if (visualColumn >= VisualLengthWithEndOfLineMarker)
		{
			return TextLines[TextLines.Count - 1];
		}
		foreach (var line in TextLines)
		{
			if (isAtEndOfLine ? visualColumn <= line.Length : visualColumn < line.Length)
			{
				return line;
			}
			visualColumn -= line.Length;
		}
		throw new InvalidOperationException("Shouldn't happen (VisualLength incorrect?)");
	}

	/// <summary>
	/// Gets a TextLine by the visual position.
	/// </summary>
	public TextLine GetTextLineByVisualYPosition(double visualTop)
	{
		const double epsilon = 0.0001;
		var pos = VisualTop;
		foreach (var tl in TextLines)
		{
			pos += tl.Height;
			if ((visualTop + epsilon) < pos)
			{
				return tl;
			}
		}

		return TextLines[TextLines.Count - 1];
	}

	/// <summary>
	/// Gets the start visual column from the specified text line.
	/// </summary>
	public int GetTextLineVisualStartColumn(TextLine textLine)
	{
		if (!TextLines.Contains(textLine))
		{
			throw new ArgumentException("textLine is not a line in this VisualLine");
		}

		return TextLines.TakeWhile(tl => tl != textLine).Sum(tl => tl.Length);
	}

	/// <summary>
	/// Gets the distance to the left border of the text area of the specified visual column.
	/// The visual column must belong to the specified text line.
	/// </summary>
	public double GetTextLineVisualXPosition(TextLine textLine, int visualColumn)
	{
		if (textLine == null)
		{
			throw new ArgumentNullException(nameof(textLine));
		}

		var xPos = textLine.GetDistanceFromCharacterHit(new CharacterHit(Math.Min(visualColumn,
			VisualLengthWithEndOfLineMarker)));

		if (visualColumn > VisualLengthWithEndOfLineMarker)
		{
			xPos += (visualColumn - VisualLengthWithEndOfLineMarker) * _textView.WideSpaceWidth;
		}

		return xPos;
	}

	/// <summary>
	/// Gets the visual top from the specified text line.
	/// </summary>
	/// <returns>
	/// Distance in device-independent pixels
	/// from the top of the document to the top of the specified text line.
	/// </returns>
	public double GetTextLineVisualYPosition(TextLine textLine, VisualYPosition yPositionMode)
	{
		if (textLine == null)
		{
			throw new ArgumentNullException(nameof(textLine));
		}
		var pos = VisualTop;
		foreach (var tl in TextLines)
		{
			if (tl == textLine)
			{
				switch (yPositionMode)
				{
					case VisualYPosition.LineTop:
						return pos;
					case VisualYPosition.LineMiddle:
						return pos + (tl.Height / 2);
					case VisualYPosition.LineBottom:
						return pos + tl.Height;
					case VisualYPosition.TextTop:
						return (pos + tl.Baseline) - _textView.DefaultBaseline;
					case VisualYPosition.TextBottom:
						return ((pos + tl.Baseline) - _textView.DefaultBaseline) + _textView.DefaultLineHeight;
					case VisualYPosition.TextMiddle:
						return ((pos + tl.Baseline) - _textView.DefaultBaseline) + (_textView.DefaultLineHeight / 2);
					case VisualYPosition.Baseline:
						return pos + tl.Baseline;
					default:
						throw new ArgumentException("Invalid yPositionMode:" + yPositionMode);
				}
			}
			pos += tl.Height;
		}
		throw new ArgumentException("textLine is not a line in this VisualLine");
	}

	/// <summary>
	/// Gets the text view position from the specified visual column.
	/// </summary>
	public TextViewPosition GetTextViewPosition(int visualColumn)
	{
		var documentOffset = GetRelativeOffset(visualColumn) + FirstDocumentLine.StartIndex;
		return new TextViewPosition(Document.GetLocation(documentOffset), visualColumn);
	}

	/// <summary>
	/// Gets the text view position from the specified visual position.
	/// If the position is within a character, it is rounded to the next character boundary.
	/// </summary>
	/// <param name="visualPosition">
	/// The position in device-independent pixels relative
	/// to the top left corner of the document.
	/// </param>
	/// <param name="allowVirtualSpace"> Controls whether positions in virtual space may be returned. </param>
	public TextViewPosition GetTextViewPosition(Point visualPosition, bool allowVirtualSpace)
	{
		var visualColumn = GetVisualColumn(visualPosition, allowVirtualSpace, out var isAtEndOfLine);
		var documentOffset = GetRelativeOffset(visualColumn) + FirstDocumentLine.StartIndex;
		var pos = new TextViewPosition(Document.GetLocation(documentOffset), visualColumn)
		{
			IsAtEndOfLine = isAtEndOfLine
		};
		return pos;
	}

	/// <summary>
	/// Gets the text view position from the specified visual position.
	/// If the position is inside a character, the position in front of the character is returned.
	/// </summary>
	/// <param name="visualPosition">
	/// The position in device-independent pixels relative
	/// to the top left corner of the document.
	/// </param>
	/// <param name="allowVirtualSpace"> Controls whether positions in virtual space may be returned. </param>
	public TextViewPosition GetTextViewPositionFloor(Point visualPosition, bool allowVirtualSpace)
	{
		var visualColumn = GetVisualColumnFloor(visualPosition, allowVirtualSpace, out var isAtEndOfLine);
		var documentOffset = GetRelativeOffset(visualColumn) + FirstDocumentLine.StartIndex;
		var pos = new TextViewPosition(Document.GetLocation(documentOffset), visualColumn)
		{
			IsAtEndOfLine = isAtEndOfLine
		};
		return pos;
	}

	/// <summary>
	/// Gets the visual column from a document offset relative to the first line start.
	/// </summary>
	public int GetVisualColumn(int relativeTextOffset)
	{
		ThrowUtil.CheckNotNegative(relativeTextOffset, "relativeTextOffset");
		foreach (var element in _elements)
		{
			if ((element.RelativeTextOffset <= relativeTextOffset)
				&& ((element.RelativeTextOffset + element.DocumentLength) >= relativeTextOffset))
			{
				return element.GetVisualColumn(relativeTextOffset);
			}
		}
		return VisualLength;
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, rounds to the nearest column.
	/// </summary>
	public int GetVisualColumn(Point point)
	{
		return GetVisualColumn(point, _textView.Settings.EnableVirtualSpace);
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, rounds to the nearest column.
	/// </summary>
	public int GetVisualColumn(Point point, bool allowVirtualSpace)
	{
		return GetVisualColumn(GetTextLineByVisualYPosition(point.Y), point.X, allowVirtualSpace);
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, rounds to the nearest column.
	/// </summary>
	public int GetVisualColumn(TextLine textLine, double xPos, bool allowVirtualSpace)
	{
		if (xPos > textLine.WidthIncludingTrailingWhitespace)
		{
			if (allowVirtualSpace && (textLine == TextLines[TextLines.Count - 1]))
			{
				var virtualX = (int) Math.Round((xPos - textLine.WidthIncludingTrailingWhitespace) / _textView.WideSpaceWidth, MidpointRounding.AwayFromZero);
				return VisualLengthWithEndOfLineMarker + virtualX;
			}
		}

		var ch = textLine.GetCharacterHitFromDistance(xPos);

		return ch.FirstCharacterIndex + ch.TrailingLength;
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, returns the first of those columns.
	/// </summary>
	public int GetVisualColumnFloor(Point point)
	{
		return GetVisualColumnFloor(point, _textView.Settings.EnableVirtualSpace);
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, returns the first of those columns.
	/// </summary>
	public int GetVisualColumnFloor(Point point, bool allowVirtualSpace)
	{
		return GetVisualColumnFloor(point, allowVirtualSpace, out _);
	}

	/// <summary>
	/// Gets the visual position from the specified visualColumn.
	/// </summary>
	/// <returns>
	/// Position in device-independent pixels
	/// relative to the top left of the document.
	/// </returns>
	public Point GetVisualPosition(int visualColumn, VisualYPosition yPositionMode)
	{
		var textLine = GetTextLine(visualColumn);
		var xPos = GetTextLineVisualXPosition(textLine, visualColumn);
		var yPos = GetTextLineVisualYPosition(textLine, yPositionMode);
		return new Point(xPos, yPos);
	}

	/// <summary>
	/// Replaces the single element at <paramref name="elementIndex" /> with the specified elements.
	/// The replacement operation must preserve the document length, but may change the visual length.
	/// </summary>
	/// <remarks>
	/// This method may only be called by line transformers.
	/// </remarks>
	public void ReplaceElement(int elementIndex, params VisualLineElement[] newElements)
	{
		ReplaceElement(elementIndex, 1, newElements);
	}

	/// <summary>
	/// Replaces <paramref name="count" /> elements starting at <paramref name="elementIndex" /> with the specified elements.
	/// The replacement operation must preserve the document length, but may change the visual length.
	/// </summary>
	/// <remarks>
	/// This method may only be called by line transformers.
	/// </remarks>
	public void ReplaceElement(int elementIndex, int count, params VisualLineElement[] newElements)
	{
		if (_phase != LifetimePhase.Transforming)
		{
			throw new InvalidOperationException("This method may only be called by line transformers.");
		}
		var oldDocumentLength = 0;
		for (var i = elementIndex; i < (elementIndex + count); i++)
		{
			oldDocumentLength += _elements[i].DocumentLength;
		}
		var newDocumentLength = 0;
		foreach (var newElement in newElements)
		{
			newDocumentLength += newElement.DocumentLength;
		}
		if (oldDocumentLength != newDocumentLength)
		{
			throw new InvalidOperationException("Old elements have document length " + oldDocumentLength + ", but new elements have length " + newDocumentLength);
		}
		_elements.RemoveRange(elementIndex, count);
		_elements.InsertRange(elementIndex, newElements);
		CalculateOffsets();
	}

	/// <summary>
	/// Validates the visual column and returns the correct one.
	/// </summary>
	public int ValidateVisualColumn(TextViewPosition position, bool allowVirtualSpace)
	{
		return ValidateVisualColumn(Document.GetOffset(position.Location), position.VisualColumn, allowVirtualSpace);
	}

	/// <summary>
	/// Validates the visual column and returns the correct one.
	/// </summary>
	public int ValidateVisualColumn(int offset, int visualColumn, bool allowVirtualSpace)
	{
		var firstDocumentLineOffset = FirstDocumentLine.StartIndex;
		if (visualColumn < 0)
		{
			return GetVisualColumn(offset - firstDocumentLineOffset);
		}
		var offsetFromVisualColumn = GetRelativeOffset(visualColumn);
		offsetFromVisualColumn += firstDocumentLineOffset;
		if (offsetFromVisualColumn != offset)
		{
			return GetVisualColumn(offset - firstDocumentLineOffset);
		}
		if ((visualColumn > VisualLength) && !allowVirtualSpace)
		{
			return VisualLength;
		}
		return visualColumn;
	}

	internal void ConstructVisualElements(ITextRunConstructionContext context, IReadOnlyList<VisualLineElementGenerator> generators)
	{
		Debug.Assert(_phase == LifetimePhase.Generating);
		foreach (var g in generators)
		{
			g.StartGeneration(context);
		}
		_elements = [];
		PerformVisualElementConstruction(generators);
		foreach (var g in generators)
		{
			g.FinishGeneration();
		}

		var globalTextRunProperties = context.GlobalTextRunProperties;
		foreach (var element in _elements)
		{
			element.SetTextRunProperties(new VisualLineElementTextRunProperties(globalTextRunProperties));
		}
		Elements = new ReadOnlyCollection<VisualLineElement>(_elements);
		CalculateOffsets();
		_phase = LifetimePhase.Transforming;
	}

	internal void Dispose()
	{
		if (_phase == LifetimePhase.Disposed)
		{
			return;
		}

		Debug.Assert(_phase == LifetimePhase.Live);

		_phase = LifetimePhase.Disposed;

		if (_visual != null)
		{
			((ISetLogicalParent) _visual).SetParent(null);
		}
	}

	internal int GetVisualColumn(Point point, bool allowVirtualSpace, out bool isAtEndOfLine)
	{
		var textLine = GetTextLineByVisualYPosition(point.Y);
		var vc = GetVisualColumn(textLine, point.X, allowVirtualSpace);
		isAtEndOfLine = vc >= (GetTextLineVisualStartColumn(textLine) + textLine.Length);
		return vc;
	}

	internal int GetVisualColumnFloor(Point point, bool allowVirtualSpace, out bool isAtEndOfLine)
	{
		var textLine = GetTextLineByVisualYPosition(point.Y);
		if (point.X > textLine.WidthIncludingTrailingWhitespace)
		{
			isAtEndOfLine = true;
			if (allowVirtualSpace && (textLine == TextLines[TextLines.Count - 1]))
			{
				// clicking virtual space in the last line
				var virtualX = (int) ((point.X - textLine.WidthIncludingTrailingWhitespace) / _textView.WideSpaceWidth);
				return VisualLengthWithEndOfLineMarker + virtualX;
			}

			// GetCharacterHitFromDistance returns a hit with FirstCharacterIndex=last character in line
			// and TrailingLength=1 when clicking behind the line, so the floor function needs to handle this case
			// specially and return the line's end column instead.
			return GetTextLineVisualStartColumn(textLine) + textLine.Length;
		}

		isAtEndOfLine = false;

		var ch = textLine.GetCharacterHitFromDistance(point.X);

		return ch.FirstCharacterIndex;
	}

	internal Point GetVisualPosition(int visualColumn, bool isAtEndOfLine, VisualYPosition yPositionMode)
	{
		var textLine = GetTextLine(visualColumn, isAtEndOfLine);
		var xPos = GetTextLineVisualXPosition(textLine, visualColumn);
		var yPos = GetTextLineVisualYPosition(textLine, yPositionMode);
		return new Point(xPos, yPos);
	}

	internal VisualLineDrawingVisual Render()
	{
		Debug.Assert(_phase == LifetimePhase.Live);

		if (_visual == null)
		{
			_visual = new VisualLineDrawingVisual(this);

			((ISetLogicalParent) _visual).SetParent(_textView);
		}

		return _visual;
	}

	internal void RunTransformers(ITextRunConstructionContext context, IReadOnlyList<IVisualLineTransformer> transformers)
	{
		Debug.Assert(_phase == LifetimePhase.Transforming);
		foreach (var transformer in transformers)
		{
			transformer.Transform(context, _elements);
		}
		_phase = LifetimePhase.Live;
	}

	internal void SetTextLines(List<TextLine> textLines)
	{
		_textLines = new ReadOnlyCollection<TextLine>(textLines);
		Height = 0;
		foreach (var line in textLines)
		{
			Height += line.Height;
		}
	}

	private void CalculateOffsets()
	{
		var visualOffset = 0;
		var textOffset = 0;
		var tabOffset = 0;
		foreach (var element in _elements)
		{
			element.VisualColumn = visualOffset;
			element.RelativeTextOffset = textOffset;
			visualOffset += element.VisualLength;
			textOffset += element.DocumentLength;
			
			if (element is SingleCharacterElementGenerator.TabTextElement textElement)
			{
				textElement.TabSize = 4 - (tabOffset % 4);
				tabOffset = 0;
			}
			else
			{
				tabOffset += element.DocumentLength;
			}
		}
		VisualLength = visualOffset;
		Debug.Assert(textOffset == (LastDocumentLine.EndIndex - FirstDocumentLine.StartIndex));
	}

	private static bool HasImplicitStopAtLineEnd()
	{
		return true;
	}

	private static bool HasImplicitStopAtLineStart(CaretPositioningMode mode)
	{
		return (mode == CaretPositioningMode.Normal) || (mode == CaretPositioningMode.EveryCodepoint);
	}

	private static bool HasStopsInVirtualSpace(CaretPositioningMode mode)
	{
		return (mode == CaretPositioningMode.Normal) || (mode == CaretPositioningMode.EveryCodepoint);
	}

	private void PerformVisualElementConstruction(IReadOnlyList<VisualLineElementGenerator> generators)
	{
		var lineLength = FirstDocumentLine.Length;
		var offset = FirstDocumentLine.StartIndex;
		var currentLineEnd = offset + lineLength;
		LastDocumentLine = FirstDocumentLine;
		var askInterestOffset = 0; // 0 or 1

		while ((offset + askInterestOffset) <= currentLineEnd)
		{
			var textPieceEndOffset = currentLineEnd;
			foreach (var g in generators)
			{
				g.CachedInterest = g.GetFirstInterestedOffset(offset + askInterestOffset);
				if (g.CachedInterest != -1)
				{
					if (g.CachedInterest < offset)
					{
						throw new ArgumentOutOfRangeException(g.GetType().Name + ".GetFirstInterestedOffset",
							g.CachedInterest,
							"GetFirstInterestedOffset must not return an offset less than startOffset. Return -1 to signal no interest.");
					}
					if (g.CachedInterest < textPieceEndOffset)
					{
						textPieceEndOffset = g.CachedInterest;
					}
				}
			}
			Debug.Assert(textPieceEndOffset >= offset);
			if (textPieceEndOffset > offset)
			{
				var textPieceLength = textPieceEndOffset - offset;

				_elements.Add(new VisualLineText(this, textPieceLength));

				offset = textPieceEndOffset;
			}
			// If no elements constructed / only zero-length elements constructed:
			// do not asking the generators again for the same location (would cause endless loop)
			askInterestOffset = 1;
			foreach (var g in generators)
			{
				if (g.CachedInterest == offset)
				{
					var element = g.ConstructElement(offset);
					if (element != null)
					{
						_elements.Add(element);
						if (element.DocumentLength > 0)
						{
							// a non-zero-length element was constructed
							askInterestOffset = 0;
							offset += element.DocumentLength;
							if (offset > currentLineEnd)
							{
								var newEndLine = Document.GetLineByOffset(offset);
								currentLineEnd = newEndLine.StartIndex + newEndLine.Length;
								LastDocumentLine = newEndLine;
								if (currentLineEnd < offset)
								{
									throw new InvalidOperationException(
										"The VisualLineElementGenerator " + g.GetType().Name +
										" produced an element which ends within the line delimiter");
								}
							}
							break;
						}
					}
				}
			}
		}
	}

	#endregion

	#region Enumerations

	private enum LifetimePhase : byte
	{
		Generating,
		Transforming,
		Live,
		Disposed
	}

	#endregion
}

// TODO: can inherit from Layoutable, but dev tools crash
[DoNotNotify]
internal sealed class VisualLineDrawingVisual : Control
{
	#region Constructors

	public VisualLineDrawingVisual(VisualLine visualLine)
	{
		VisualLine = visualLine;
		LineHeight = VisualLine.TextLines.Sum(textLine => textLine.Height);
	}

	#endregion

	#region Properties

	public double LineHeight { get; }
	public VisualLine VisualLine { get; }
	internal bool IsAdded { get; set; }

	#endregion

	#region Methods

	public override void Render(DrawingContext context)
	{
		double pos = 0;
		foreach (var textLine in VisualLine.TextLines)
		{
			textLine.Draw(context, new Point(0, pos));
			pos += textLine.Height;
		}
	}

	#endregion
}