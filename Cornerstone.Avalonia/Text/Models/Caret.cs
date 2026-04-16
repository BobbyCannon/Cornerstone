#region References

using System;
using Avalonia;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers;
using Cornerstone.Reflection;
using Cornerstone.Search;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public partial class Caret : Notifiable
{
	#region Fields

	internal Rect VisualLayout;
	private bool _blink;
	private readonly TextEditorViewModel _document;
	private Line _line;
	private double _preferredVisualX;
	private bool _wantsVisualStart;

	#endregion

	#region Constructors

	public Caret(TextEditorViewModel document)
	{
		_document = document;
		_blink = false;

		IsVisible = false;
		Selection = new Selection(this);
		VisualLayout = new Rect();
	}

	#endregion

	#region Properties

	/// <summary>
	/// When word-wrap is enabled and a line is wrapped at a position where there is no space character;
	/// then both the end of the first Line and the beginning of the second Line refer to the same position
	/// in the document. In this case, the IsAtEndOfLine property is used to distinguish between the two cases:
	/// - the value True indicates that the position refers to the end of the previous Line.
	/// - the value False indicates that the position refers to the beginning of the Line.
	/// If this position is not at such a wrapping position, the value of this property has no effect.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsAtEndOfLine { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsVisible { get; set; }

	/// <summary>
	/// Represents the line the caret is on.
	/// </summary>
	public Line Line
	{
		get
		{
			if (_line != null)
			{
				return _line;
			}

			// note: There must be a better way of doing this.
			_line = _document.Lines.GetLineFromOffset(Offset);
			OnPropertyChanged();
			return _line;
		}
	}

	/// <summary>
	/// Represents the document offset the caret represents.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Offset { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool OverstrikeMode { get; set; }

	/// <summary>
	/// Represents the selection the caret owns.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Selection Selection { get; set; }

	/// <summary>
	/// Represents the token where the caret is located.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Token Token { get; set; }

	#endregion

	#region Methods

	public void Move(int offset)
	{
		Move(offset, true, false);
	}

	public void Move(CaretMoveDirection direction, bool extendSelection)
	{
		var currentOffset = Offset;
		switch (direction)
		{
			case CaretMoveDirection.CharLeft:
			{
				MoveLeft();
				break;
			}
			case CaretMoveDirection.CharRight:
			{
				MoveRight();
				break;
			}
			case CaretMoveDirection.DocumentEnd:
			{
				Move(_document.DocumentLength);
				break;
			}
			case CaretMoveDirection.DocumentStart:
			{
				Move(0);
				break;
			}
			case CaretMoveDirection.LineDown:
			{
				MoveLineDown();
				break;
			}
			case CaretMoveDirection.LineUp:
			{
				MoveLineUp();
				break;
			}
			case CaretMoveDirection.LineStart:
			{
				MoveToLineStart();
				break;
			}
			case CaretMoveDirection.LineSmartStart:
			{
				MoveToSmartLineStart();
				break;
			}
			case CaretMoveDirection.LineEnd:
			{
				MoveToLineEnd();
				break;
			}
			case CaretMoveDirection.PageDown:
			{
				MovePageDown();
				break;
			}
			case CaretMoveDirection.PageUp:
			{
				MovePageUp();
				break;
			}
		}

		if (extendSelection)
		{
			Selection.Update(Selection.Length > 0 ? Selection.StartOffset : currentOffset, Offset);
		}
	}

	public void MoveLeft()
	{
		var offset = Offset - 1;
		var buffer = _document.Buffer;

		if (offset < 0)
		{
			return;
		}

		if ((buffer[offset] == '\n')
			&& (offset > 0)
			&& (buffer[offset - 1] == '\r'))
		{
			Move(offset - 1);
		}
		else if ((buffer[offset] == '\r')
				&& ((offset + 1) < buffer.Count)
				&& (buffer[offset + 1] == '\n'))
		{
			Move(offset);
		}
		else
		{
			Move(offset);
		}
	}

	public void MoveLineDown()
	{
		var visualY = VisualLayout.Bottom + 1;
		if (!_document.Lines.TryGetLineForOffset(_preferredVisualX, visualY, out var line))
		{
			return;
		}

		var preferredVisualX = IsAtEndOfLine ? int.MaxValue : _preferredVisualX;
		var newOffset = line.GetNearestOffsetAtVisual(preferredVisualX, visualY, IsAtEndOfLine);
		var isAtEndOfLine = (line.WrappedStartOffsets.AsSpan().Contains(newOffset) || (newOffset == line.EndOffset)) && IsAtEndOfLine;
		IntegerExtensions.EnsureRange(ref newOffset, line.StartOffset, line.EndOffset - line.LineEndingLength);
		Move(newOffset, false, isAtEndOfLine);
	}

	public void MoveLineUp()
	{
		var visualY = VisualLayout.Top - 1;
		if (!_document.Lines.TryGetLineForOffset(_preferredVisualX, visualY, out var line))
		{
			return;
		}

		var preferredVisualX = IsAtEndOfLine ? int.MaxValue : _preferredVisualX;
		var newOffset = line.GetNearestOffsetAtVisual(preferredVisualX, visualY, IsAtEndOfLine);
		var isAtEndOfLine = (line.WrappedStartOffsets.AsSpan().Contains(newOffset) || (newOffset == line.EndOffset)) && IsAtEndOfLine;
		IntegerExtensions.EnsureRange(ref newOffset, line.StartOffset, line.EndOffset - line.LineEndingLength);
		Move(newOffset, false, isAtEndOfLine);
	}

	public void MovePageDown()
	{
		var visualY = VisualLayout.Top + _document.ViewMetrics.Viewport.Height + _document.ViewMetrics.CharacterHeight;
		if (!_document.Lines.TryGetLineForOffset(_preferredVisualX, visualY, out var line))
		{
			return;
		}

		var newOffset = line.GetNearestOffsetAtVisual(_preferredVisualX, visualY, IsAtEndOfLine);
		IntegerExtensions.EnsureRange(ref newOffset, line.StartOffset, line.EndOffset - line.LineEndingLength);
		Move(newOffset, false, IsAtEndOfLine);
	}

	public void MovePageUp()
	{
		var visualY = VisualLayout.Top - _document.ViewMetrics.Viewport.Height - _document.ViewMetrics.CharacterHeight;
		if (!_document.Lines.TryGetLineForOffset(_preferredVisualX, visualY, out var line))
		{
			return;
		}

		var newOffset = line.GetNearestOffsetAtVisual(_preferredVisualX, visualY, IsAtEndOfLine);
		IntegerExtensions.EnsureRange(ref newOffset, line.StartOffset, line.EndOffset - line.LineEndingLength);
		Move(newOffset, false, IsAtEndOfLine);
	}

	public void MoveRight()
	{
		var offset = Offset;
		var buffer = _document.Buffer;

		if (offset >= buffer.Count)
		{
			return;
		}

		if ((buffer[offset] == '\r')
			&& ((offset + 1) < buffer.Count)
			&& (buffer[offset + 1] == '\n'))
		{
			Move(offset + 2);
		}
		else
		{
			Move(offset + 1);
		}
	}

	public void MoveToLineEnd()
	{
		var line = Line;
		var lineEndOffset = line.GetLineEnd(Offset, IsAtEndOfLine);
		Move(lineEndOffset, true, true);
	}

	public void MoveToLineStart()
	{
		var line = Line;
		var lineStartOffset = line.GetLineStart(this);
		_wantsVisualStart = !_wantsVisualStart;
		Move(lineStartOffset, true, false);
	}

	/// <summary>
	/// Smart line start with per-visual-subline support.
	/// - First press: first non-whitespace char on the current visual subline.
	/// - Subsequent presses: toggle between that position and the visual subline start.
	/// </summary>
	public void MoveToSmartLineStart(bool extendSelection = false)
	{
		var currentOffset = Offset;
		var line = Line;
		var buffer = _document.Buffer;

		// Determine the current visual subline start and end
		var subLineStart = GetCurrentVisualSubLineStart(line);
		var subLineEnd = GetCurrentVisualSubLineEnd(line);

		// Find first non-whitespace on this visual subline
		var firstNonWhite = subLineStart;
		while (firstNonWhite < subLineEnd)
		{
			var ch = buffer[firstNonWhite];
			if (!char.IsWhiteSpace(ch))
			{
				break;
			}
			firstNonWhite++;
		}

		// If the entire subline is whitespace, treat it as the subline start
		if (firstNonWhite >= subLineEnd)
		{
			firstNonWhite = subLineStart;
		}

		int targetOffset;

		if (currentOffset == firstNonWhite)
		{
			// Already at first non-whitespace, go to visual subline start
			targetOffset = subLineStart;
		}
		else if (currentOffset == subLineStart)
		{
			// Already at visual start, go to first non-whitespace
			targetOffset = firstNonWhite;
		}
		else
		{
			// Anywhere else, go to first non-whitespace (standard first action)
			targetOffset = firstNonWhite;
		}

		Move(targetOffset, true, false);

		if (extendSelection)
		{
			Selection.Update(Selection.Length > 0 ? Selection.StartOffset : currentOffset, Offset);
		}
	}

	public void Reset()
	{
		Move(0);
		Selection.Reset();
	}

	public bool ToggleBlink()
	{
		_blink = !_blink;
		return IsVisible && _blink;
	}

	internal void OnCaretMoved(int offset)
	{
		if (Selection.IsSelecting)
		{
			Selection.EndOffset = offset;
		}

		CaretMoved?.Invoke(this, EventArgs.Empty);
	}

	internal void UpdateVisualLayout()
	{
		_line = null;

		var line = Line;
		if (line != null)
		{
			VisualLayout = line.UpdateCaretVisual(this);
		}
	}

	/// <summary>
	/// Returns the exclusive end offset of the current visual subline.
	/// </summary>
	private int GetCurrentVisualSubLineEnd(Line line)
	{
		if (line.WrappedStartOffsets.Count == 0)
		{
			return line.EndOffset - line.LineEndingLength;
		}

		var index = BinarySearch.FindCeilIndex(line.WrappedStartOffsets, Offset + 1);
		if ((index >= 0) && (index < line.WrappedStartOffsets.Count))
		{
			return line.WrappedStartOffsets[index];
		}

		return line.EndOffset - line.LineEndingLength;
	}

	/// <summary>
	/// Returns the document offset of the start of the current visual subline
	/// the caret is on (respects wrapping).
	/// </summary>
	private int GetCurrentVisualSubLineStart(Line line)
	{
		if (line.WrappedStartOffsets.Count == 0)
		{
			return line.StartOffset; // no wrapping
		}

		// Find the largest wrapped start offset that is <= current caret position
		var index = BinarySearch.FindFloorIndex(line.WrappedStartOffsets, Offset);
		return index >= 0
			? line.WrappedStartOffsets[index]
			: line.StartOffset;
	}

	private void Move(int offset, bool updatePreferred, bool isEndOfLine)
	{
		IntegerExtensions.EnsureRange(ref offset, 0, _document.Buffer.Count);

		IsAtEndOfLine = isEndOfLine;
		Offset = offset;
		UpdateVisualLayout();

		if (updatePreferred)
		{
			_preferredVisualX = VisualLayout.X;
		}

		OnCaretMoved(offset);
	}

	#endregion

	#region Events

	public event EventHandler CaretMoved;

	#endregion
}