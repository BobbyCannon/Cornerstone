#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Input;
using Cornerstone.Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;
using Cornerstone.Internal;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Rectangular selection ("box selection").
/// </summary>
public sealed class RectangleSelection : Selection
{
	#region Constants

	/// <summary>
	/// Gets the name of the entry in the DataObject that signals rectangle selections.
	/// </summary>
	public const string DataFormat = "AvalonEditRectangularSelection";

	#endregion

	#region Fields

	/// <summary>
	/// Expands the selection down by one line, creating a rectangular selection.
	/// Key gesture: Alt+Shift+Down
	/// </summary>
	public static readonly RoutedCommand BoxSelectDownByLine = new(nameof(BoxSelectDownByLine));

	/// <summary>
	/// Expands the selection left by one character, creating a rectangular selection.
	/// Key gesture: Alt+Shift+Left
	/// </summary>
	public static readonly RoutedCommand BoxSelectLeftByCharacter = new(nameof(BoxSelectLeftByCharacter));

	/// <summary>
	/// Expands the selection left by one word, creating a rectangular selection.
	/// Key gesture: Ctrl+Alt+Shift+Left
	/// </summary>
	public static readonly RoutedCommand BoxSelectLeftByWord = new(nameof(BoxSelectLeftByWord));

	/// <summary>
	/// Expands the selection right by one character, creating a rectangular selection.
	/// Key gesture: Alt+Shift+Right
	/// </summary>
	public static readonly RoutedCommand BoxSelectRightByCharacter = new(nameof(BoxSelectRightByCharacter));

	/// <summary>
	/// Expands the selection right by one word, creating a rectangular selection.
	/// Key gesture: Ctrl+Alt+Shift+Right
	/// </summary>
	public static readonly RoutedCommand BoxSelectRightByWord = new(nameof(BoxSelectRightByWord));

	/// <summary>
	/// Expands the selection to the end of the line, creating a rectangular selection.
	/// Key gesture: Alt+Shift+End
	/// </summary>
	public static readonly RoutedCommand BoxSelectToLineEnd = new(nameof(BoxSelectToLineEnd));

	/// <summary>
	/// Expands the selection to the start of the line, creating a rectangular selection.
	/// Key gesture: Alt+Shift+Home
	/// </summary>
	public static readonly RoutedCommand BoxSelectToLineStart = new(nameof(BoxSelectToLineStart));

	/// <summary>
	/// Expands the selection up by one line, creating a rectangular selection.
	/// Key gesture: Alt+Shift+Up
	/// </summary>
	public static readonly RoutedCommand BoxSelectUpByLine = new(nameof(BoxSelectUpByLine));

	private readonly int _bottomRightOffset;

	private TextEditorDocument _document;
	private readonly int _endLine;
	private readonly double _endXPos;

	private readonly List<SelectionRange> _segments = [];
	private readonly int _startLine;
	private readonly double _startXPos;
	private readonly int _topLeftOffset;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new rectangular selection.
	/// </summary>
	public RectangleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end)
		: base(textArea)
	{
		InitDocument();
		_startLine = start.Line;
		_endLine = end.Line;
		_startXPos = GetXPos(textArea, start);
		_endXPos = GetXPos(textArea, end);
		CalculateSegments();
		_topLeftOffset = _segments.First().StartIndex;
		_bottomRightOffset = _segments.Last().EndIndex;

		StartPosition = start;
		EndPosition = end;
	}

	private RectangleSelection(TextArea textArea, int startLine, double startXPos, TextViewPosition end)
		: base(textArea)
	{
		InitDocument();
		_startLine = startLine;
		_endLine = end.Line;
		_startXPos = startXPos;
		_endXPos = GetXPos(textArea, end);
		CalculateSegments();
		_topLeftOffset = _segments.First().StartIndex;
		_bottomRightOffset = _segments.Last().EndIndex;

		StartPosition = GetStart();
		EndPosition = end;
	}

	private RectangleSelection(TextArea textArea, TextViewPosition start, int endLine, double endXPos)
		: base(textArea)
	{
		InitDocument();
		_startLine = start.Line;
		_endLine = endLine;
		_startXPos = GetXPos(textArea, start);
		_endXPos = endXPos;
		CalculateSegments();
		_topLeftOffset = _segments.First().StartIndex;
		_bottomRightOffset = _segments.Last().EndIndex;

		StartPosition = start;
		EndPosition = GetEnd();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override bool EnableVirtualSpace => true;

	/// <inheritdoc />
	public override TextViewPosition EndPosition { get; }

	/// <inheritdoc />
	public override int Length
	{
		get { return Segments.Sum(s => s.Length); }
	}

	/// <inheritdoc />
	public override IEnumerable<SelectionRange> Segments => _segments;

	/// <inheritdoc />
	public override TextViewPosition StartPosition { get; }

	/// <inheritdoc />
	public override IRange SurroundingRange => new SimpleRange(_topLeftOffset, _bottomRightOffset - _topLeftOffset);

	#endregion

	#region Methods

	/// <inheritdoc />
	public override DataObject CreateDataObject(TextArea textArea)
	{
		var data = new DataObject();
		// Ensure we use the appropriate newline sequence for the OS
		var text = TextUtilities.NormalizeNewLines(GetText(), Environment.NewLine);
		data.Set(DataFormat, text);
		return data;
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		var r = obj as RectangleSelection;
		// ReSharper disable CompareOfFloatsByEqualityOperator
		return (r != null) && (r.TextArea == TextArea)
			&& (r._topLeftOffset == _topLeftOffset) && (r._bottomRightOffset == _bottomRightOffset)
			&& (r._startLine == _startLine) && (r._endLine == _endLine)
			&& (r._startXPos == _startXPos) && (r._endXPos == _endXPos);
		// ReSharper restore CompareOfFloatsByEqualityOperator
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return _topLeftOffset ^ _bottomRightOffset;
	}

	/// <inheritdoc />
	public override string GetText()
	{
		var b = new StringBuilder();
		foreach (var s in Segments)
		{
			if (b.Length > 0)
			{
				b.AppendLine();
			}
			b.Append(_document.GetText(s));
		}
		return b.ToString();
	}

	/// <summary>
	/// Performs a rectangular paste operation.
	/// </summary>
	public static bool PerformRectangularPaste(TextArea textArea, TextViewPosition startPosition, string text, bool selectInsertedText)
	{
		if (textArea == null)
		{
			throw new ArgumentNullException(nameof(textArea));
		}
		if (text == null)
		{
			throw new ArgumentNullException(nameof(text));
		}
		var newLineCount = text.AsEnumerable().Count(c => c == '\n'); // TODO might not work in all cases, but single \r line endings are really rare today.
		var endLocation = new TextLocation(startPosition.Line + newLineCount, startPosition.Column);
		if (endLocation.Line <= textArea.Document.LineCount)
		{
			var endOffset = textArea.Document.GetOffset(endLocation);
			if (textArea.Selection.EnableVirtualSpace || (textArea.Document.GetLocation(endOffset) == endLocation))
			{
				var selection = textArea.Selection;
				var rectangleSelection = new RectangleSelection(textArea, startPosition, endLocation.Line, GetXPos(textArea, startPosition));
				rectangleSelection.ReplaceSelectionWithText(text);
				if (selectInsertedText && selection is RectangleSelection sel)
				{
					// bug: this is broken, won't reselect
					textArea.Selection = new RectangleSelection(textArea, startPosition, sel._endLine, sel._endXPos);
				}
				return true;
			}
		}
		return false;
	}

	/// <inheritdoc />
	public override void ReplaceSelectionWithText(string newText)
	{
		if (newText == null)
		{
			throw new ArgumentNullException(nameof(newText));
		}
		using (TextArea.Document.RunUpdate())
		{
			int insertionLength;
			var firstInsertionLength = 0;
			var editOffset = Math.Min(_topLeftOffset, _bottomRightOffset);
			TextViewPosition pos;
			if (NewLineFinder.NextNewLine(newText, 0) == SegmentExtensions.Invalid)
			{
				// insert same text into every line
				foreach (var lineSegment in Segments.Reverse())
				{
					ReplaceSingleLineText(TextArea, lineSegment, newText, out insertionLength);
					firstInsertionLength = insertionLength;
				}

				pos = new TextViewPosition(_document.GetLocation(editOffset + firstInsertionLength));

				TextArea.Selection = new RectangleSelection(TextArea, pos, Math.Max(_startLine, _endLine), GetXPos(TextArea, pos));
			}
			else
			{
				var lines = newText.Split(NewLineFinder.NewlineStrings, _segments.Count, StringSplitOptions.None);
				for (var i = lines.Length - 1; i >= 0; i--)
				{
					ReplaceSingleLineText(TextArea, _segments[i], lines[i], out insertionLength);
					firstInsertionLength = insertionLength;
				}
				pos = new TextViewPosition(_document.GetLocation(editOffset + firstInsertionLength));
				TextArea.ClearSelection();
			}
			TextArea.Caret.Position = new TextViewPosition(Math.Max(_startLine, _endLine), pos.Column);
		}
	}

	/// <inheritdoc />
	public override Selection SetEndpoint(TextViewPosition endPosition)
	{
		return new RectangleSelection(TextArea, _startLine, _startXPos, endPosition);
	}

	/// <inheritdoc />
	public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
	{
		return SetEndpoint(endPosition);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		// It's possible that ToString() gets called on old (invalid) selections, e.g. for "change from... to..." debug message
		// make sure we don't crash even when the desired locations don't exist anymore.
		return $"[RectangleSelection {_startLine} {_topLeftOffset} {_startXPos} to {_endLine} {_bottomRightOffset} {_endXPos}]";
	}

	/// <inheritdoc />
	public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
	{
		var newStartLocation = TextArea.Document.GetLocation(e.GetNewOffset(_topLeftOffset, AnchorMovementType.AfterInsertion));
		var newEndLocation = TextArea.Document.GetLocation(e.GetNewOffset(_bottomRightOffset, AnchorMovementType.BeforeInsertion));

		return new RectangleSelection(TextArea,
			new TextViewPosition(newStartLocation, GetVisualColumnFromXPos(newStartLocation.Line, _startXPos)),
			new TextViewPosition(newEndLocation, GetVisualColumnFromXPos(newEndLocation.Line, _endXPos)));
	}

	private void CalculateSegments()
	{
		var nextLine = _document.GetLineByNumber(Math.Min(_startLine, _endLine));
		do
		{
			var vl = TextArea.TextView.GetOrConstructVisualLine(nextLine);
			var startVc = vl.GetVisualColumn(new Point(_startXPos, 0), true);
			var endVc = vl.GetVisualColumn(new Point(_endXPos, 0), true);

			var baseOffset = vl.FirstDocumentLine.StartIndex;
			var startOffset = baseOffset + vl.GetRelativeOffset(startVc);
			var endOffset = baseOffset + vl.GetRelativeOffset(endVc);
			_segments.Add(new SelectionRange(startOffset, startVc, endOffset, endVc));

			nextLine = vl.LastDocumentLine.NextLine;
		} while ((nextLine != null) && (nextLine.LineNumber <= Math.Max(_startLine, _endLine)));
	}

	private TextViewPosition GetEnd()
	{
		var segment = _startLine < _endLine ? _segments.Last() : _segments.First();
		if (_startXPos < _endXPos)
		{
			return new TextViewPosition(_document.GetLocation(segment.EndIndex), segment.EndVisualColumn);
		}
		return new TextViewPosition(_document.GetLocation(segment.StartIndex), segment.StartVisualColumn);
	}

	private TextViewPosition GetStart()
	{
		var segment = _startLine < _endLine ? _segments.First() : _segments.Last();
		if (_startXPos < _endXPos)
		{
			return new TextViewPosition(_document.GetLocation(segment.StartIndex), segment.StartVisualColumn);
		}
		return new TextViewPosition(_document.GetLocation(segment.EndIndex), segment.EndVisualColumn);
	}

	private int GetVisualColumnFromXPos(int line, double xPos)
	{
		var vl = TextArea.TextView.GetOrConstructVisualLine(TextArea.Document.GetLineByNumber(line));
		return vl.GetVisualColumn(new Point(xPos, 0), true);
	}

	private static double GetXPos(TextArea textArea, TextViewPosition pos)
	{
		var documentLine = textArea.Document.GetLineByNumber(pos.Line);
		var visualLine = textArea.TextView.GetOrConstructVisualLine(documentLine);
		var vc = visualLine.ValidateVisualColumn(pos, true);
		var textLine = visualLine.GetTextLine(vc, pos.IsAtEndOfLine);
		return visualLine.GetTextLineVisualXPosition(textLine, vc);
	}

	private void InitDocument()
	{
		_document = TextArea.Document;
		if (_document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
	}

	private void ReplaceSingleLineText(TextArea textArea, SelectionRange lineRange, string newText, out int insertionLength)
	{
		if (lineRange.Length == 0)
		{
			if ((newText.Length > 0) && textArea.ReadOnlySectionProvider.CanInsert(lineRange.StartIndex))
			{
				newText = AddSpacesIfRequired(newText, new TextViewPosition(_document.GetLocation(lineRange.StartIndex), lineRange.StartVisualColumn), new TextViewPosition(_document.GetLocation(lineRange.EndIndex), lineRange.EndVisualColumn));
				textArea.Document.Insert(lineRange.StartIndex, newText);
			}
		}
		else
		{
			var segmentsToDelete = textArea.GetDeletableSegments(lineRange);
			for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
			{
				if (i == (segmentsToDelete.Length - 1))
				{
					if ((segmentsToDelete[i].StartIndex == SurroundingRange.StartIndex)
						&& (segmentsToDelete[i].Length == SurroundingRange.Length))
					{
						newText = AddSpacesIfRequired(newText, new TextViewPosition(_document.GetLocation(lineRange.StartIndex), lineRange.StartVisualColumn), new TextViewPosition(_document.GetLocation(lineRange.EndIndex), lineRange.EndVisualColumn));
					}
					textArea.Document.Replace(segmentsToDelete[i], newText);
				}
				else
				{
					textArea.Document.Remove(segmentsToDelete[i]);
				}
			}
		}
		insertionLength = newText.Length;
	}

	#endregion
}