#region References

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Extensions;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Handles selection of text using the mouse.
/// </summary>
internal sealed class SelectionMouseHandler : ITextAreaInputHandler
{
	#region Constants

	public const int MinimumHorizontalDragDistance = 2;
	public const int MinimumVerticalDragDistance = 2;

	#endregion

	#region Fields

	private object _currentDragDescriptor;
	private bool _enableTextDragDrop;
	private SelectionMode _mode;
	private Point _possibleDragStartMousePos;
	private AnchorSegment _startWord;

	#endregion

	#region Constructors

	public SelectionMouseHandler(TextArea textArea)
	{
		TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
	}

	#endregion

	#region Properties

	public TextArea TextArea { get; }

	#endregion

	#region Methods

	public void Attach()
	{
		TextArea.PointerPressed += TextAreaMouseLeftButtonDown;
		TextArea.PointerMoved += TextAreaMouseMove;
		TextArea.PointerReleased += TextAreaMouseLeftButtonUp;
		//textArea.QueryCursor += textArea_QueryCursor;
		TextArea.OptionChanged += TextAreaOptionChanged;

		_enableTextDragDrop = TextArea.Options.EnableTextDragDrop;
		if (_enableTextDragDrop)
		{
			AttachDragDrop();
		}
	}

	public void Detach()
	{
		_mode = SelectionMode.None;
		TextArea.PointerPressed -= TextAreaMouseLeftButtonDown;
		TextArea.PointerMoved -= TextAreaMouseMove;
		TextArea.PointerReleased -= TextAreaMouseLeftButtonUp;
		//textArea.QueryCursor -= textArea_QueryCursor;
		TextArea.OptionChanged -= TextAreaOptionChanged;

		if (_enableTextDragDrop)
		{
			DetachDragDrop();
		}
	}

	/// <inheritdoc />
	public void RemoveKey(Key key, KeyModifiers modifiers)
	{
	}

	private void AttachDragDrop()
	{
		DragDrop.SetAllowDrop(TextArea, true);
		TextArea.AddHandler(DragDrop.DragOverEvent, TextAreaDragOver);
		TextArea.AddHandler(DragDrop.DropEvent, TextAreaDrop);

		//TextArea.GiveFeedback += textArea_GiveFeedback;
		//TextArea.QueryContinueDrag += textArea_QueryContinueDrag;
		//TextArea.DragEnter += textArea_DragEnter;
		//TextArea.DragLeave += textArea_DragLeave;
	}

	private void DetachDragDrop()
	{
		DragDrop.SetAllowDrop(TextArea, false);
		TextArea.RemoveHandler(DragDrop.DragOverEvent, TextAreaDragOver);
		TextArea.RemoveHandler(DragDrop.DropEvent, TextAreaDrop);

		//TextArea.GiveFeedback -= textArea_GiveFeedback;
		//TextArea.QueryContinueDrag -= textArea_QueryContinueDrag;
		//TextArea.DragEnter -= textArea_DragEnter;
		//TextArea.DragLeave -= textArea_DragLeave;
	}

	private void ExtendSelectionToMouse(PointerEventArgs e)
	{
		var oldPosition = TextArea.Caret.Position;
		if ((_mode == SelectionMode.Normal) || (_mode == SelectionMode.Rectangular))
		{
			SetCaretOffsetToMousePosition(e);
			if ((_mode == SelectionMode.Normal) && TextArea.Selection is RectangleSelection)
			{
				TextArea.Selection = new SimpleSelection(TextArea, oldPosition, TextArea.Caret.Position);
			}
			else if ((_mode == SelectionMode.Rectangular) && !(TextArea.Selection is RectangleSelection))
			{
				TextArea.Selection = new RectangleSelection(TextArea, oldPosition, TextArea.Caret.Position);
			}
			else
			{
				TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
			}
		}
		else if ((_mode == SelectionMode.WholeWord) || (_mode == SelectionMode.WholeLine))
		{
			var newWord = _mode == SelectionMode.WholeLine ? GetLineAtMousePosition(e) : GetWordAtMousePosition(e);
			if ((newWord != SegmentExtensions.Invalid) && (_startWord != null))
			{
				TextArea.Selection = Selection.Create(TextArea,
					Math.Min(newWord.Offset, _startWord.Offset),
					Math.Max(newWord.EndOffset, _startWord.EndOffset));

				// moves caret to start or end of selection
				TextArea.Caret.Offset = newWord.Offset < _startWord.Offset ? newWord.Offset : Math.Max(newWord.EndOffset, _startWord.EndOffset);
			}
		}
		TextArea.Caret.BringCaretToView(5.0);
	}

	private DragDropEffects GetEffect(DragEventArgs e)
	{
		if (e.Data.Contains(DataFormats.Text)
			|| e.Data.Contains(RectangleSelection.DataFormat))
		{
			e.Handled = true;

			var offset = GetOffsetFromMousePosition(e.GetPosition(TextArea.TextView), out var visualColumn, out var isAtEndOfLine);
			if (offset >= 0)
			{
				TextArea.Caret.Position = new TextViewPosition(TextArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
				TextArea.Caret.DesiredXPos = double.NaN;
				if (TextArea.ReadOnlySectionProvider.CanInsert(offset))
				{
					if (((e.DragEffects & DragDropEffects.Move) == DragDropEffects.Move)
						&& ((e.KeyModifiers & KeyModifiers.Control) != KeyModifiers.Control))
					{
						return DragDropEffects.Move;
					}

					return e.DragEffects & DragDropEffects.Copy;
				}
			}
		}

		return DragDropEffects.None;
	}

	private SimpleSegment GetLineAtMousePosition(PointerEventArgs e)
	{
		var textView = TextArea.TextView;
		if (textView == null)
		{
			return SegmentExtensions.Invalid;
		}
		var pos = e.GetPosition(textView);
		if (pos.Y < 0)
		{
			pos = pos.WithY(0);
		}
		if (pos.Y > textView.Bounds.Height)
		{
			pos = pos.WithY(textView.Bounds.Height);
		}
		pos += textView.ScrollOffset;
		var line = textView.GetVisualLineFromVisualTop(pos.Y);
		return (line != null) && (line.TextLines != null)
			? new SimpleSegment(line.StartOffset, line.LastDocumentLine.EndOffset - line.StartOffset)
			: SegmentExtensions.Invalid;
	}

	private int GetOffsetFromMousePosition(PointerEventArgs e, out int visualColumn, out bool isAtEndOfLine)
	{
		return GetOffsetFromMousePosition(e.GetPosition(TextArea.TextView), out visualColumn, out isAtEndOfLine);
	}

	private int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn, out bool isAtEndOfLine)
	{
		visualColumn = 0;
		var textView = TextArea.TextView;
		var pos = positionRelativeToTextView;
		if (pos.Y < 0)
		{
			pos = pos.WithY(0);
		}
		if (pos.Y > textView.Bounds.Height)
		{
			pos = pos.WithY(textView.Bounds.Height);
		}
		pos += textView.ScrollOffset;
		if (pos.Y >= textView.DocumentHeight)
		{
			pos = pos.WithY(textView.DocumentHeight - ExtensionMethods.Epsilon);
		}
		var line = textView.GetVisualLineFromVisualTop(pos.Y);
		if ((line != null) && (line.TextLines != null))
		{
			visualColumn = line.GetVisualColumn(pos, TextArea.Selection.EnableVirtualSpace, out isAtEndOfLine);
			return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
		}
		isAtEndOfLine = false;
		return -1;
	}

	private int GetOffsetFromMousePositionFirstTextLineOnly(Point positionRelativeToTextView, out int visualColumn)
	{
		visualColumn = 0;
		var textView = TextArea.TextView;
		var pos = positionRelativeToTextView;
		if (pos.Y < 0)
		{
			pos = pos.WithY(0);
		}
		if (pos.Y > textView.Bounds.Height)
		{
			pos = pos.WithY(textView.Bounds.Height);
		}
		pos += textView.ScrollOffset;
		if (pos.Y >= textView.DocumentHeight)
		{
			pos = pos.WithY(textView.DocumentHeight - ExtensionMethods.Epsilon);
		}
		var line = textView.GetVisualLineFromVisualTop(pos.Y);
		if ((line != null) && (line.TextLines != null))
		{
			visualColumn = line.GetVisualColumn(line.TextLines.First(), pos.X, TextArea.Selection.EnableVirtualSpace);
			return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
		}
		return -1;
	}

	private SimpleSegment GetWordAtMousePosition(PointerEventArgs e)
	{
		var textView = TextArea.TextView;
		if (textView == null)
		{
			return SegmentExtensions.Invalid;
		}
		var pos = e.GetPosition(textView);
		if (pos.Y < 0)
		{
			pos = pos.WithY(0);
		}
		if (pos.Y > textView.Bounds.Height)
		{
			pos = pos.WithY(textView.Bounds.Height);
		}
		pos += textView.ScrollOffset;
		var line = textView.GetVisualLineFromVisualTop(pos.Y);
		if ((line != null) && (line.TextLines != null))
		{
			var visualColumn = line.GetVisualColumn(pos, TextArea.Selection.EnableVirtualSpace);
			var wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, TextArea.Selection.EnableVirtualSpace);
			if (wordStartVc == -1)
			{
				wordStartVc = 0;
			}
			var wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, TextArea.Selection.EnableVirtualSpace);
			if (wordEndVc == -1)
			{
				wordEndVc = line.VisualLength;
			}
			var relOffset = line.FirstDocumentLine.Offset;
			var wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
			var wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;
			return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
		}
		return SegmentExtensions.Invalid;
	}

	private void OnDragException(Exception ex)
	{
		Debug.WriteLine(ex.ToDetailedString());
		Debugger.Break();
	}

	private void SetCaretOffsetToMousePosition(PointerEventArgs e, ISegment allowedSegment = null)
	{
		int visualColumn;
		bool isAtEndOfLine;
		int offset;
		if (_mode == SelectionMode.Rectangular)
		{
			offset = GetOffsetFromMousePositionFirstTextLineOnly(e.GetPosition(TextArea.TextView), out visualColumn);
			isAtEndOfLine = true;
		}
		else
		{
			offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
		}

		if (allowedSegment != null)
		{
			offset = offset.CoerceValue(allowedSegment.Offset, allowedSegment.EndOffset);
		}

		if (offset >= 0)
		{
			TextArea.Caret.Position = new TextViewPosition(TextArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
			TextArea.Caret.DesiredXPos = double.NaN;
		}
	}

	private void StartDrag(PointerEventArgs args)
	{
		// prevent nested StartDrag calls
		_mode = SelectionMode.Drag;

		// mouse capture and Drag'n'Drop doesn't mix
		//TextArea.ReleaseMouseCapture();

		var dataObject = TextArea.Selection.CreateDataObject(TextArea);
		var allowedEffects = DragDropEffects.Move;
		var deleteOnMove = TextArea.Selection.Segments.Select(s => new AnchorSegment(TextArea.Document, s)).ToList();

		foreach (ISegment s in deleteOnMove)
		{
			var result = TextArea.GetDeletableSegments(s);
			if ((result.Length != 1) || (result[0].Offset != s.Offset) || (result[0].EndOffset != s.EndOffset))
			{
				allowedEffects &= ~DragDropEffects.Move;
			}
		}

		//var copyingEventArgs = new DataObjectCopyingEventArgs(dataObject, true);
		//TextArea.RaiseEvent(copyingEventArgs);
		//if (copyingEventArgs.CommandCancelled)
		//{
		//	return;
		//}

		var dragDescriptor = new object();
		_currentDragDescriptor = dragDescriptor;

		DragDropEffects resultEffect;
		using (TextArea.AllowCaretOutsideSelection())
		{
			var oldCaretPosition = TextArea.Caret.Position;
			try
			{
				//Debug.WriteLine("DoDragDrop with allowedEffects=" + allowedEffects);
				resultEffect = DragDrop.DoDragDrop(args, dataObject, allowedEffects).Result;
				//Debug.WriteLine("DoDragDrop done, resultEffect=" + resultEffect);
			}
			catch (COMException ex)
			{
				// ignore COM errors - don't crash on badly implemented drop targets
				Debug.WriteLine("DoDragDrop failed: " + ex);
				return;
			}

			if (resultEffect == DragDropEffects.None)
			{
				// reset caret if drag was aborted
				TextArea.Caret.Position = oldCaretPosition;
			}
		}

		_currentDragDescriptor = null;

		if ((deleteOnMove != null) && (resultEffect == DragDropEffects.Move) && ((allowedEffects & DragDropEffects.Move) == DragDropEffects.Move))
		{
			var draggedInsideSingleDocument = dragDescriptor == TextArea.Document.UndoStack.LastGroupDescriptor;
			if (draggedInsideSingleDocument)
			{
				TextArea.Document.UndoStack.StartContinuedUndoGroup();
			}

			TextArea.Document.BeginUpdate();
			try
			{
				foreach (ISegment s in deleteOnMove)
				{
					TextArea.Document.Remove(s.Offset, s.Length);
				}
			}
			finally
			{
				TextArea.Document.EndUpdate();
				if (draggedInsideSingleDocument)
				{
					TextArea.Document.UndoStack.EndUndoGroup();
				}
			}
		}
	}

	private void TextAreaDragOver(object sender, DragEventArgs e)
	{
		try
		{
			e.DragEffects = GetEffect(e);
		}
		catch (Exception ex)
		{
			OnDragException(ex);
		}
	}

	private void TextAreaDrop(object sender, DragEventArgs e)
	{
		try
		{
			var effect = GetEffect(e);
			e.DragEffects = effect;

			if (!TextArea.IsKeyboardFocusWithin)
			{
				TextArea.Caret.Hide();
			}

			if (effect == DragDropEffects.None)
			{
				return;
			}

			var start = TextArea.Caret.Offset;
			if ((_mode == SelectionMode.Drag) && TextArea.Selection.Contains(start))
			{
				Debug.WriteLine("Drop: did not drop: drop target is inside selection");
				e.DragEffects = DragDropEffects.None;
			}
			else
			{
				// Mark the undo group with the currentDragDescriptor, if the drag
				// is originating from the same control. This allows combining
				// the undo groups when text is moved.
				TextArea.Document.UndoStack.StartUndoGroup(_currentDragDescriptor);
				try
				{
					var rectangular = e.Data.Contains(RectangleSelection.DataFormat);
					if (rectangular)
					{
						var text = e.Data.Get(RectangleSelection.DataFormat)?.ToString();
						if (text != null)
						{
							RectangleSelection.PerformRectangularPaste(TextArea, TextArea.Caret.Position, text, true);
						}
					}
					else
					{
						var text = e.Data.Get(DataFormats.Text)?.ToString();
						if (text != null)
						{
							TextArea.Document.Insert(start, text);
							TextArea.Selection = Selection.Create(TextArea, start, start + text.Length);
						}
					}
				}
				finally
				{
					TextArea.Document.UndoStack.EndUndoGroup();
				}
			}

			e.Handled = true;
		}
		catch (Exception ex)
		{
			OnDragException(ex);
		}
	}

	private void TextAreaMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
	{
		if (e.GetCurrentPoint(TextArea).Properties.IsLeftButtonPressed == false)
		{
			if (TextArea.RightClickMovesCaret && (e.Handled == false))
			{
				SetCaretOffsetToMousePosition(e);
			}
		}
		else
		{
			TextArea.Cursor = Cursor.Parse("IBeam");

			var pointer = e.GetCurrentPoint(TextArea);

			_mode = SelectionMode.None;
			if (!e.Handled)
			{
				var modifiers = e.KeyModifiers;
				var shift = modifiers.HasFlag(KeyModifiers.Shift);
				if (_enableTextDragDrop && (e.ClickCount == 1) && !shift)
				{
					var offset = GetOffsetFromMousePosition(e, out _, out _);
					if (TextArea.Selection.Contains(offset))
					{
						if (TextArea.CapturePointer(e.Pointer))
						{
							_mode = SelectionMode.PossibleDragStart;
							_possibleDragStartMousePos = e.GetPosition(TextArea);
						}
						e.Handled = true;
						return;
					}
				}

				var oldPosition = TextArea.Caret.Position;
				SetCaretOffsetToMousePosition(e);

				if (!shift)
				{
					TextArea.ClearSelection();
				}

				if (TextArea.CapturePointer(e.Pointer))
				{
					if (modifiers.HasFlag(KeyModifiers.Alt) && TextArea.Options.EnableRectangularSelection)
					{
						_mode = SelectionMode.Rectangular;
						if (shift && TextArea.Selection is RectangleSelection)
						{
							TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
						}
					}
					else if (modifiers.HasFlag(KeyModifiers.Control) && (e.ClickCount == 1)) // e.ClickCount == 1
					{
						_mode = SelectionMode.WholeWord;
						if (shift && !(TextArea.Selection is RectangleSelection))
						{
							TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
						}
					}
					else if (pointer.Properties.IsLeftButtonPressed && (e.ClickCount == 1)) // e.ClickCount == 1
					{
						_mode = SelectionMode.Normal;
						if (shift && !(TextArea.Selection is RectangleSelection))
						{
							TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
						}
					}
					else
					{
						SimpleSegment startWord;
						if (e.ClickCount == 3)
						{
							_mode = SelectionMode.WholeLine;
							startWord = GetLineAtMousePosition(e);
						}
						else
						{
							_mode = SelectionMode.WholeWord;
							startWord = GetWordAtMousePosition(e);
						}

						if (startWord == SegmentExtensions.Invalid)
						{
							_mode = SelectionMode.None;
							TextArea.ReleasePointerCapture(e.Pointer);
							return;
						}
						if (shift && !TextArea.Selection.IsEmpty)
						{
							if (startWord.Offset < TextArea.Selection.SurroundingSegment.Offset)
							{
								TextArea.Selection = TextArea.Selection.SetEndpoint(new TextViewPosition(TextArea.Document.GetLocation(startWord.Offset)));
							}
							else if (startWord.EndOffset > TextArea.Selection.SurroundingSegment.EndOffset)
							{
								TextArea.Selection = TextArea.Selection.SetEndpoint(new TextViewPosition(TextArea.Document.GetLocation(startWord.EndOffset)));
							}
							_startWord = new AnchorSegment(TextArea.Document, TextArea.Selection.SurroundingSegment);
						}
						else
						{
							TextArea.Selection = Selection.Create(TextArea, startWord.Offset, startWord.EndOffset);
							_startWord = new AnchorSegment(TextArea.Document, startWord.Offset, startWord.Length);
						}
					}
				}
				e.Handled = true;
			}
		}
	}

	private void TextAreaMouseLeftButtonUp(object sender, PointerEventArgs e)
	{
		if ((_mode == SelectionMode.None) || e.Handled)
		{
			return;
		}
		e.Handled = true;
		switch (_mode)
		{
			case SelectionMode.PossibleDragStart:
				// this was not a drag start (mouse didn't move after mousedown)
				SetCaretOffsetToMousePosition(e);
				TextArea.ClearSelection();
				break;
			case SelectionMode.Normal:
			case SelectionMode.WholeWord:
			case SelectionMode.WholeLine:
			case SelectionMode.Rectangular:
				if (TextArea.Options.ExtendSelectionOnMouseUp)
				{
					ExtendSelectionToMouse(e);
				}
				break;
		}
		_mode = SelectionMode.None;
		TextArea.ReleasePointerCapture(e.Pointer);
	}

	private void TextAreaMouseMove(object sender, PointerEventArgs e)
	{
		if (e.Handled)
		{
			return;
		}
		if ((_mode == SelectionMode.Normal) || (_mode == SelectionMode.WholeWord) || (_mode == SelectionMode.WholeLine) || (_mode == SelectionMode.Rectangular))
		{
			e.Handled = true;
			if (TextArea.TextView.VisualLinesValid)
			{
				// If the visual lines are not valid, don't extend the selection.
				// Extending the selection forces a VisualLine refresh, and it is sufficient
				// to do that on MouseUp, we don't have to do it every MouseMove.
				ExtendSelectionToMouse(e);
			}
		}
		else if (_mode == SelectionMode.PossibleDragStart)
		{
			e.Handled = true;
			Vector mouseMovement = e.GetPosition(TextArea) - _possibleDragStartMousePos;
			if ((Math.Abs(mouseMovement.X) > MinimumHorizontalDragDistance)
				|| (Math.Abs(mouseMovement.Y) > MinimumVerticalDragDistance))
			{
				StartDrag(e);
			}
		}
	}

	private void TextAreaOptionChanged(object sender, PropertyChangedEventArgs e)
	{
		var newEnableTextDragDrop = TextArea.Options.EnableTextDragDrop;
		if (newEnableTextDragDrop != _enableTextDragDrop)
		{
			_enableTextDragDrop = newEnableTextDragDrop;
		}
	}

	#endregion

	#region Enumerations

	private enum SelectionMode
	{
		/// <summary>
		/// no selection (no mouse button down)
		/// </summary>
		None,

		/// <summary>
		/// left mouse button down on selection, might be normal click
		/// or might be drag'n'drop
		/// </summary>
		PossibleDragStart,

		/// <summary>
		/// dragging text
		/// </summary>
		Drag,

		/// <summary>
		/// normal selection (click+drag)
		/// </summary>
		Normal,

		/// <summary>
		/// whole-word selection (double click+drag or ctrl+click+drag)
		/// </summary>
		WholeWord,

		/// <summary>
		/// whole-line selection (triple click+drag)
		/// </summary>
		WholeLine,

		/// <summary>
		/// rectangular selection (alt+click+drag)
		/// </summary>
		Rectangular
	}

	#endregion
}