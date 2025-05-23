﻿#region References

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Cornerstone.Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Indentation;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;
using Cornerstone.Internal;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Control that wraps a TextView and adds support for user input and the caret.
/// </summary>
[DoNotNotify]
public class TextArea : CornerstoneTemplatedControl, ITextEditorComponent, IRoutedCommandHandler, ILogicalScrollable
{
	#region Fields

	/// <summary>
	/// Document property.
	/// </summary>
	public static readonly StyledProperty<TextEditorDocument> DocumentProperty;

	/// <summary>
	/// IndentationStrategy property.
	/// </summary>
	public static readonly StyledProperty<IIndentationStrategy> IndentationStrategyProperty;

	public static readonly DirectProperty<TextArea, ObservableCollection<Control>> LeftMarginsProperty;

	/// <summary>
	/// Defines the <see cref="IScrollable.Offset" /> property.
	/// </summary>
	public static readonly DirectProperty<TextArea, Vector> OffsetProperty;

	/// <summary>
	/// The <see cref="OverstrikeMode" /> dependency property.
	/// </summary>
	public static readonly StyledProperty<bool> OverstrikeModeProperty;

	/// <summary>
	/// The <see cref="RightClickMovesCaret" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> RightClickMovesCaretProperty;

	/// <summary>
	/// The <see cref="SelectionBackground" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush> SelectionBackgroundProperty;

	/// <summary>
	/// The <see cref="SelectionBorder" /> property.
	/// </summary>
	public static readonly StyledProperty<Pen> SelectionBorderProperty;

	/// <summary>
	/// The <see cref="SelectionCornerRadius" /> property.
	/// </summary>
	public static readonly StyledProperty<double> SelectionCornerRadiusProperty;

	/// <summary>
	/// The <see cref="SelectionForeground" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush> SelectionForegroundProperty;

	/// <summary>
	/// Settings property.
	/// </summary>
	public static readonly StyledProperty<TextEditorSettings> SettingsProperty;

	internal readonly Selection EmptySelection;
	private ITextAreaInputHandler _activeInputHandler;
	private int _allowCaretOutsideSelection;
	private bool _ensureSelectionValidRequested;
	private readonly TextAreaTextInputMethodClient _imClient;
	private bool _isChangingInputHandler;
	private bool _isMouseCursorHidden;
	private readonly ILogicalScrollable _logicalScrollable;
	private IReadOnlySectionProvider _readOnlySectionProvider;
	private Selection _selection;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new TextArea instance.
	/// </summary>
	public TextArea() : this(new TextView())
	{
		AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
		AddHandler(KeyUpEvent, OnPreviewKeyUp, RoutingStrategies.Tunnel);
	}

	/// <summary>
	/// Creates a new TextArea instance.
	/// </summary>
	protected TextArea(TextView textView)
	{
		if (textView == null)
		{
			throw new ArgumentNullException(nameof(textView));
		}

		_imClient = new();
		_readOnlySectionProvider = NoReadOnlySections.Instance;
		_logicalScrollable = textView;

		RoutedCommandBindings = [];
		LeftMargins = [];
		StackedInputHandlers = System.Collections.Immutable.ImmutableStack<TextAreaStackedInputHandler>.Empty;
		TextView = textView;
		Settings = textView.Settings;

		_selection = EmptySelection = new EmptySelection(this);

		textView.Services.AddService(this);
		textView.LineTransformers.Add(new SelectionColorizer(this));
		textView.InsertLayer(new SelectionLayer(this), KnownLayer.Selection, LayerInsertionPosition.Replace);

		Caret = new Caret(this, GetDispatcher());
		Caret.PositionChanged += (sender, e) => RequestSelectionValidation();
		Caret.PositionChanged += CaretPositionChanged;

		AttachTypingEvents();

		LeftMargins.CollectionChanged += LeftMargins_CollectionChanged;
		DefaultInputHandler = new TextAreaDefaultInputHandler(this);
		ActiveInputHandler = DefaultInputHandler;
	}

	static TextArea()
	{
		DocumentProperty = TextView.DocumentProperty.AddOwner<TextArea>();
		FocusableProperty.OverrideDefaultValue<TextArea>(true);
		IndentationStrategyProperty = AvaloniaProperty.Register<TextArea, IIndentationStrategy>(nameof(IndentationStrategy), new DefaultIndentationStrategy());
		LeftMarginsProperty = AvaloniaProperty.RegisterDirect<TextArea, ObservableCollection<Control>>(nameof(LeftMargins), c => c.LeftMargins);
		OffsetProperty = AvaloniaProperty.RegisterDirect<TextArea, Vector>(nameof(IScrollable.Offset), o => (o as IScrollable).Offset, (o, v) => (o as IScrollable).Offset = v);
		SettingsProperty = TextView.SettingsProperty.AddOwner<TextArea>();
		OverstrikeModeProperty = AvaloniaProperty.Register<TextArea, bool>(nameof(OverstrikeMode));
		RightClickMovesCaretProperty = AvaloniaProperty.Register<TextArea, bool>(nameof(RightClickMovesCaret));
		SelectionBackgroundProperty = AvaloniaProperty.Register<TextArea, IBrush>(nameof(SelectionBackground));
		SelectionBorderProperty = AvaloniaProperty.Register<TextArea, Pen>(nameof(SelectionBorder));
		SelectionCornerRadiusProperty = AvaloniaProperty.Register<TextArea, double>(nameof(SelectionCornerRadius), 3.0);
		SelectionForegroundProperty = AvaloniaProperty.Register<TextArea, IBrush>(nameof(SelectionForeground));
		KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TextArea>(KeyboardNavigationMode.None);

		DocumentProperty.Changed.Subscribe(OnDocumentChanged);
		SettingsProperty.Changed.Subscribe(OnOptionsChanged);

		AffectsArrange<TextArea>(OffsetProperty);
		AffectsRender<TextArea>(OffsetProperty);

		TextInputMethodClientRequestedEvent.AddClassHandler<TextArea>((ta, e) =>
		{
			if (!ta.IsReadOnly)
			{
				e.Client = ta._imClient;
			}
		});
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the active input handler.
	/// This property does not return currently active stacked input handlers. Setting this property detached all stacked input handlers.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="ITextAreaInputHandler" />
	/// </remarks>
	public ITextAreaInputHandler ActiveInputHandler
	{
		get => _activeInputHandler;
		set
		{
			if ((value != null) && (value.TextArea != this))
			{
				throw new ArgumentException("The input handler was created for a different text area than this one.");
			}
			if (_isChangingInputHandler)
			{
				throw new InvalidOperationException("Cannot set ActiveInputHandler recursively");
			}
			if (_activeInputHandler != value)
			{
				_isChangingInputHandler = true;
				try
				{
					// pop the whole stack
					PopStackedInputHandler(StackedInputHandlers.LastOrDefault());
					Debug.Assert(StackedInputHandlers.IsEmpty);

					_activeInputHandler?.Detach();
					_activeInputHandler = value;
					value?.Attach();
				}
				finally
				{
					_isChangingInputHandler = false;
				}
				ActiveInputHandlerChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Gets the Caret used for this text area.
	/// </summary>
	public Caret Caret { get; }

	/// <summary>
	/// Gets the default input handler.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="ITextAreaInputHandler" />
	/// </remarks>
	public TextAreaDefaultInputHandler DefaultInputHandler { get; }

	/// <summary>
	/// Gets/Sets the document displayed by the text editor.
	/// </summary>
	public TextEditorDocument Document
	{
		get => GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	/// <summary>
	/// Gets/Sets the indentation strategy used when inserting new lines.
	/// </summary>
	public IIndentationStrategy IndentationStrategy
	{
		get => GetValue(IndentationStrategyProperty);
		set => SetValue(IndentationStrategyProperty, value);
	}

	/// <summary>
	/// Gets if the document displayed by the text editor is readonly
	/// </summary>
	public bool IsReadOnly => ReadOnlySectionProvider == ReadOnlySectionDocument.Instance;

	/// <summary>
	/// Gets the collection of margins displayed to the left of the text view.
	/// </summary>
	public ObservableCollection<Control> LeftMargins { get; }

	/// <summary>
	/// Gets/Sets whether overstrike mode is active.
	/// </summary>
	public bool OverstrikeMode
	{
		get => GetValue(OverstrikeModeProperty);
		set => SetValue(OverstrikeModeProperty, value);
	}

	/// <summary>
	/// Gets/Sets an object that provides read-only sections for the text area.
	/// </summary>
	public IReadOnlySectionProvider ReadOnlySectionProvider
	{
		get => _readOnlySectionProvider;
		set => _readOnlySectionProvider = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Determines whether caret position should be changed to the mouse position when you right-click or not.
	/// </summary>
	public bool RightClickMovesCaret
	{
		get => GetValue(RightClickMovesCaretProperty);
		set => SetValue(RightClickMovesCaretProperty, value);
	}

	public SpeedyList<RoutedCommandBinding> RoutedCommandBindings { get; }

	/// <summary>
	/// Gets/Sets the selection in this text area.
	/// </summary>

	public Selection Selection
	{
		get => _selection;
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (value.TextArea != this)
			{
				throw new ArgumentException("Cannot use a Selection instance that belongs to another text area.");
			}
			if (!Equals(_selection, value))
			{
				if (TextView != null)
				{
					var oldSegment = _selection.SurroundingRange;
					var newSegment = value.SurroundingRange;

					if (!Selection.EnableVirtualSpace
						&& _selection is SimpleSelection
						&& value is SimpleSelection
						&& (oldSegment != null)
						&& (newSegment != null))
					{
						// perf optimization:
						// When a simple selection changes, don't redraw the whole selection, but only the changed parts.
						var oldSegmentOffset = oldSegment.StartIndex;
						var newSegmentOffset = newSegment.StartIndex;
						if (oldSegmentOffset != newSegmentOffset)
						{
							TextView.Redraw(Math.Min(oldSegmentOffset, newSegmentOffset),
								Math.Abs(oldSegmentOffset - newSegmentOffset));
						}
						var oldSegmentEndOffset = oldSegment.EndIndex;
						var newSegmentEndOffset = newSegment.EndIndex;
						if (oldSegmentEndOffset != newSegmentEndOffset)
						{
							TextView.Redraw(Math.Min(oldSegmentEndOffset, newSegmentEndOffset),
								Math.Abs(oldSegmentEndOffset - newSegmentEndOffset));
						}
					}
					else
					{
						TextView.Redraw(oldSegment);
						TextView.Redraw(newSegment);
					}
				}
				_selection = value;
				OnPropertyChanged(nameof(Selection));
				SelectionChanged?.Invoke(this, EventArgs.Empty);
				// a selection change causes commands like copy/paste/etc. to change status
				//CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the background brush used for the selection.
	/// </summary>
	public IBrush SelectionBackground
	{
		get => GetValue(SelectionBackgroundProperty);
		set => SetValue(SelectionBackgroundProperty, value);
	}

	/// <summary>
	/// Gets/Sets the pen used for the border of the selection.
	/// </summary>
	public Pen SelectionBorder
	{
		get => GetValue(SelectionBorderProperty);
		set => SetValue(SelectionBorderProperty, value);
	}

	/// <summary>
	/// Gets/Sets the corner radius of the selection.
	/// </summary>
	public double SelectionCornerRadius
	{
		get => GetValue(SelectionCornerRadiusProperty);
		set => SetValue(SelectionCornerRadiusProperty, value);
	}

	/// <summary>
	/// Gets/Sets the foreground brush used for selected text.
	/// </summary>
	public IBrush SelectionForeground
	{
		get => GetValue(SelectionForegroundProperty);
		set => SetValue(SelectionForegroundProperty, value);
	}

	/// <summary>
	/// Gets/Sets the document displayed by the text editor.
	/// </summary>
	public TextEditorSettings Settings
	{
		get => GetValue(SettingsProperty);
		set => SetValue(SettingsProperty, value);
	}

	/// <summary>
	/// Gets the list of currently active stacked input handlers.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="ITextAreaInputHandler" />
	/// </remarks>
	public System.Collections.Immutable.ImmutableStack<TextAreaStackedInputHandler> StackedInputHandlers { get; private set; }

	/// <summary>
	/// Gets the text view used to display text in this text area.
	/// </summary>
	public TextView TextView { get; }

	bool ILogicalScrollable.CanHorizontallyScroll
	{
		get => _logicalScrollable?.CanHorizontallyScroll ?? default(bool);
		set
		{
			if (_logicalScrollable != null)
			{
				_logicalScrollable.CanHorizontallyScroll = value;
			}
		}
	}

	bool ILogicalScrollable.CanVerticallyScroll
	{
		get => _logicalScrollable?.CanVerticallyScroll ?? default(bool);
		set
		{
			if (_logicalScrollable != null)
			{
				_logicalScrollable.CanVerticallyScroll = value;
			}
		}
	}

	Size IScrollable.Extent => _logicalScrollable?.Extent ?? default(Size);

	bool ILogicalScrollable.IsLogicalScrollEnabled => _logicalScrollable?.IsLogicalScrollEnabled ?? default(bool);

	Vector IScrollable.Offset
	{
		get => _logicalScrollable?.Offset ?? default(Vector);
		set
		{
			if (_logicalScrollable != null)
			{
				_logicalScrollable.Offset = value;
			}
		}
	}

	Size ILogicalScrollable.PageScrollSize => _logicalScrollable?.PageScrollSize ?? default(Size);

	Size ILogicalScrollable.ScrollSize => _logicalScrollable?.ScrollSize ?? default(Size);

	Size IScrollable.Viewport => _logicalScrollable?.Viewport ?? default(Size);

	#endregion

	#region Methods

	/// <summary>
	/// Temporarily allows positioning the caret outside the selection.
	/// Dispose the returned IDisposable to revert the allowance.
	/// </summary>
	/// <remarks>
	/// The text area only forces the caret to be inside the selection when other events
	/// have finished running (using the dispatcher), so you don't have to use this method
	/// for temporarily positioning the caret in event handlers.
	/// This method is only necessary if you want to run the dispatcher, e.g. if you
	/// perform a drag'n'drop operation.
	/// </remarks>
	public IDisposable AllowCaretOutsideSelection()
	{
		VerifyAccess();
		_allowCaretOutsideSelection++;
		return new CallbackOnDispose(
			delegate
			{
				VerifyAccess();
				_allowCaretOutsideSelection--;
				RequestSelectionValidation();
			});
	}

	public bool BringIntoView(Control target, Rect targetRect)
	{
		return _logicalScrollable?.BringIntoView(target, targetRect) ?? default(bool);
	}

	/// <summary>
	/// Clears the current selection.
	/// </summary>
	public void ClearSelection()
	{
		Selection = EmptySelection;
	}

	/// <summary>
	/// Gets the requested service.
	/// </summary>
	/// <returns> Returns the requested service instance, or null if the service cannot be found. </returns>
	public virtual object GetService(Type serviceType)
	{
		return TextView.GetService(serviceType);
	}

	/// <summary>
	/// Performs text input.
	/// This raises the <see cref="TextEntering" /> event, replaces the selection with the text,
	/// and then raises the <see cref="TextEntered" /> event.
	/// </summary>
	public void PerformTextInput(string text)
	{
		var e = new TextInputEventArgs
		{
			Text = text,
			RoutedEvent = TextInputEvent
		};
		PerformTextInput(e);
	}

	/// <summary>
	/// Performs text input.
	/// This raises the <see cref="TextEntering" /> event, replaces the selection with the text,
	/// and then raises the <see cref="TextEntered" /> event.
	/// </summary>
	public void PerformTextInput(TextInputEventArgs e)
	{
		if (e == null)
		{
			throw new ArgumentNullException(nameof(e));
		}
		if (Document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		OnTextEntering(e);
		if (!e.Handled)
		{
			if ((e.Text == "\n") || (e.Text == "\r") || (e.Text == "\r\n"))
			{
				ReplaceSelectionWithNewLine();
			}
			else
			{
				if (OverstrikeMode
					&& Selection.IsEmpty
					&& (Document.GetLineByNumber(Caret.Line).EndIndex > Caret.Offset))
				{
					EditingCommands.SelectRightByCharacter.Execute(null, this);
				}

				ReplaceSelectionWithText(e.Text);
			}
			OnTextEntered(e);
			Caret.BringCaretToView();
		}
	}

	/// <summary>
	/// Pops the stacked input handler (and all input handlers above it).
	/// If <paramref name="inputHandler" /> is not found in the currently stacked input handlers, or is null, this method
	/// does nothing.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="ITextAreaInputHandler" />
	/// </remarks>
	public void PopStackedInputHandler(TextAreaStackedInputHandler inputHandler)
	{
		if (StackedInputHandlers.Any(i => i == inputHandler))
		{
			ITextAreaInputHandler oldHandler;
			do
			{
				oldHandler = StackedInputHandlers.Peek();
				StackedInputHandlers = StackedInputHandlers.Pop();
				oldHandler.Detach();
			} while (oldHandler != inputHandler);
		}
	}

	/// <summary>
	/// Pushes an input handler onto the list of stacked input handlers.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="ITextAreaInputHandler" />
	/// </remarks>
	public void PushStackedInputHandler(TextAreaStackedInputHandler inputHandler)
	{
		if (inputHandler == null)
		{
			throw new ArgumentNullException(nameof(inputHandler));
		}
		StackedInputHandlers = StackedInputHandlers.Push(inputHandler);
		inputHandler.Attach();
	}

	public void RaiseScrollInvalidated(EventArgs e)
	{
		_logicalScrollable?.RaiseScrollInvalidated(e);
	}

	/// <summary>
	/// Scrolls the text view so that the requested line is in the middle.
	/// If the textview can be scrolled.
	/// </summary>
	/// <param name="line"> The line to scroll to. </param>
	public void ScrollToLine(int line)
	{
		var viewPortLines = (int) (this as IScrollable).Viewport.Height;

		if (viewPortLines < Document.LineCount)
		{
			ScrollToLine(line, 2, viewPortLines / 2);
		}
	}

	/// <summary>
	/// Scrolls the textview to a position with n lines above and below it.
	/// </summary>
	/// <param name="line"> the requested line number. </param>
	/// <param name="linesEitherSide"> The number of lines above and below. </param>
	public void ScrollToLine(int line, int linesEitherSide)
	{
		ScrollToLine(line, linesEitherSide, linesEitherSide);
	}

	/// <summary>
	/// Scrolls the textview to a position with n lines above and below it.
	/// </summary>
	/// <param name="line"> the requested line number. </param>
	/// <param name="linesAbove"> The number of lines above. </param>
	/// <param name="linesBelow"> The number of lines below. </param>
	public void ScrollToLine(int line, int linesAbove, int linesBelow)
	{
		var offset = line - linesAbove;

		if (offset < 0)
		{
			offset = 0;
		}

		this.BringIntoView(new Rect(1, offset, 0, 1));

		offset = line + linesBelow;

		if (offset >= 0)
		{
			this.BringIntoView(new Rect(1, offset, 0, 1));
		}
	}

	protected internal void MoveCaret(CaretMovementType direction)
	{
		CaretNavigationCommandHandler.MoveCaret(this, direction);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		if (e.NameScope.Find("PART_CP") is ContentPresenter contentPresenter)
		{
			contentPresenter.Content = TextView;
		}
	}

	protected override void OnGotFocus(GotFocusEventArgs e)
	{
		base.OnGotFocus(e);

		Caret.Show();

		_imClient.SetTextArea(this);
	}

	// Make life easier for text editor extensions that use a different cursor based on the pressed modifier keys.
	/// <inheritdoc />
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		TextView.InvalidateCursorIfPointerWithinTextView();
	}

	/// <inheritdoc />
	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);
		TextView.InvalidateCursorIfPointerWithinTextView();
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);

		Caret.Hide();

		_imClient.SetTextArea(null);
	}

	/// <summary>
	/// Raises the <see cref="OptionChanged" /> event.
	/// </summary>
	protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
	{
		OptionChanged?.Invoke(this, e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		Focus();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == SelectionBackgroundProperty)
			|| (change.Property == SelectionBorderProperty)
			|| (change.Property == SelectionForegroundProperty)
			|| (change.Property == SelectionCornerRadiusProperty))
		{
			TextView.Redraw();
		}
		else if (change.Property == OverstrikeModeProperty)
		{
			Caret.UpdateIfVisible();
		}
	}

	/// <summary>
	/// Raises the TextEntered event.
	/// </summary>
	protected virtual void OnTextEntered(TextInputEventArgs e)
	{
		TextEntered?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the TextEntering event.
	/// </summary>
	protected virtual void OnTextEntering(TextInputEventArgs e)
	{
		TextEntering?.Invoke(this, e);
	}

	protected override void OnTextInput(TextInputEventArgs e)
	{
		base.OnTextInput(e);
		if (!e.Handled && (Document != null))
		{
			if (string.IsNullOrEmpty(e.Text) || (e.Text == "\x1b") || (e.Text == "\b") || (e.Text == "\u007f"))
			{
				// TODO: check this
				// ASCII 0x1b = ESC.
				// produces a TextInput event with that old ASCII control char
				// when Escape is pressed. We'll just ignore it.

				// A dead key followed by backspace causes a text input event for the BS character.

				// Similarly, some shortcuts like Alt+Space produce an empty TextInput event.
				// We have to ignore those (not handle them) to keep the shortcut working.
				return;
			}
			HideMouseCursor();
			PerformTextInput(e);
			e.Handled = true;
		}
	}

	internal void AddChild(Visual visual)
	{
		VisualChildren.Add(visual);
		InvalidateArrange();
	}

	internal IRange[] GetDeletableSegments(IRange range)
	{
		if (range == null)
		{
			return [];
		}

		var deletableSegments = ReadOnlySectionProvider.GetDeletableSegments(range);
		if (deletableSegments == null)
		{
			throw new InvalidOperationException("ReadOnlySectionProvider.GetDeletableSegments returned null");
		}
		var array = deletableSegments.ToArray();
		var lastIndex = range.StartIndex;
		foreach (var t in array)
		{
			if (t.StartIndex < lastIndex)
			{
				throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
			}
			lastIndex = t.EndIndex;
		}
		if (lastIndex > range.EndIndex)
		{
			throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
		}
		return array;
	}

	internal void OnTextCopied(TextEventArgs e)
	{
		TextCopied?.Invoke(this, e);
	}

	internal void OnTextInputFromTextEditor(TextInputEventArgs e)
	{
		OnTextInput(e);
	}

	internal void RemoveChild(Visual visual)
	{
		VisualChildren.Remove(visual);
	}

	internal void RemoveSelectedText()
	{
		if (Document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		_selection.ReplaceSelectionWithText(string.Empty);
		#if DEBUG
		if (!_selection.IsEmpty)
		{
			foreach (var s in _selection.Segments)
			{
				Debug.Assert(!ReadOnlySectionProvider.GetDeletableSegments(s).Any());
			}
		}
		#endif
	}

	internal void ReplaceSelectionWithText(string newText)
	{
		if (newText == null)
		{
			throw new ArgumentNullException(nameof(newText));
		}
		if (Document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		_selection.ReplaceSelectionWithText(newText);
	}

	private void AttachTypingEvents()
	{
		// Use the PreviewMouseMove event in case some other editor layer consumes the MouseMove event (e.g. SD's InsertionCursorLayer)
		PointerEntered += delegate { ShowMouseCursor(); };
		PointerExited += delegate { ShowMouseCursor(); };
	}

	private void CaretPositionChanged(object sender, EventArgs e)
	{
		if (TextView == null)
		{
			return;
		}

		TextView.HighlightedLine = Caret.Line;

		ScrollToLine(Caret.Line, 2);

		Dispatcher.UIThread.InvokeAsync(() => (this as ILogicalScrollable).RaiseScrollInvalidated(EventArgs.Empty));
	}

	/// <summary>
	/// Code that updates only the caret but not the selection can cause confusion when
	/// keys like 'Delete' delete the (possibly invisible) selected text and not the
	/// text around the caret.
	/// So we'll ensure that the caret is inside the selection.
	/// (when the caret is not in the selection, we'll clear the selection)
	/// This method is invoked using the Dispatcher so that code may temporarily violate this rule
	/// (e.g. most 'extend selection' methods work by first setting the caret, then the selection),
	/// it's sufficient to fix it after any event handlers have run.
	/// </summary>
	private void EnsureSelectionValid()
	{
		_ensureSelectionValidRequested = false;
		if (_allowCaretOutsideSelection == 0)
		{
			if (!_selection.IsEmpty && !_selection.Contains(Caret.Offset))
			{
				ClearSelection();
			}
		}
	}

	Control ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control from)
	{
		return _logicalScrollable?.GetControlInDirection(direction, from);
	}

	private void HideMouseCursor()
	{
		if (Settings.HideCursorWhileTyping && !_isMouseCursorHidden && IsPointerOver)
		{
			_isMouseCursorHidden = true;
		}
	}

	private void LeftMargins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems != null)
		{
			foreach (var c in e.OldItems.OfType<ITextViewConnect>())
			{
				c.RemoveFromTextView(TextView);
			}
		}
		if (e.NewItems != null)
		{
			foreach (var c in e.NewItems.OfType<ITextViewConnect>())
			{
				c.AddToTextView(TextView);
			}
		}
	}

	private static void OnDocumentChanged(AvaloniaPropertyChangedEventArgs e)
	{
		(e.Sender as TextArea)?.OnDocumentChanged((TextEditorDocument) e.OldValue, (TextEditorDocument) e.NewValue);
	}

	private void OnDocumentChanged(TextEditorDocument oldValue, TextEditorDocument newValue)
	{
		if (oldValue != null)
		{
			TextDocumentWeakEventManager.Changing.RemoveHandler(oldValue, OnDocumentChanging);
			TextDocumentWeakEventManager.Changed.RemoveHandler(oldValue, OnDocumentChanged);
			TextDocumentWeakEventManager.UpdateStarted.RemoveHandler(oldValue, OnUpdateStarted);
			TextDocumentWeakEventManager.UpdateFinished.RemoveHandler(oldValue, OnUpdateFinished);
		}
		TextView.Document = newValue;
		if (newValue != null)
		{
			TextDocumentWeakEventManager.Changing.AddHandler(newValue, OnDocumentChanging);
			TextDocumentWeakEventManager.Changed.AddHandler(newValue, OnDocumentChanged);
			TextDocumentWeakEventManager.UpdateStarted.AddHandler(newValue, OnUpdateStarted);
			TextDocumentWeakEventManager.UpdateFinished.AddHandler(newValue, OnUpdateFinished);

			InvalidateArrange();
		}
		// Reset caret location and selection: this is necessary because the caret/selection might be invalid
		// in the new document (e.g. if new document is shorter than the old document).
		Caret.Location = new TextLocation(1, 1);
		ClearSelection();
		DocumentChanged?.Invoke(this, new DocumentChangedEventArgs(oldValue, newValue));
		//CommandManager.InvalidateRequerySuggested();
	}

	private void OnDocumentChanged(object sender, DocumentChangeEventArgs e)
	{
		Caret.OnDocumentChanged(e);
		Selection = _selection.UpdateOnDocumentChange(e);
	}

	private void OnDocumentChanging(object sender, DocumentChangeEventArgs e)
	{
		Caret.OnDocumentChanging();
	}

	private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
	{
		OnOptionChanged(e);
	}

	private static void OnOptionsChanged(AvaloniaPropertyChangedEventArgs e)
	{
		(e.Sender as TextArea)?.OnOptionsChanged((TextEditorSettings) e.OldValue, (TextEditorSettings) e.NewValue);
	}

	private void OnOptionsChanged(TextEditorSettings oldValue, TextEditorSettings newValue)
	{
		if (oldValue != null)
		{
			PropertyChangedWeakEventManager.RemoveHandler(oldValue, OnOptionChanged);
		}
		TextView.Settings = newValue;
		if (newValue != null)
		{
			PropertyChangedWeakEventManager.AddHandler(newValue, OnOptionChanged);
		}
		OnOptionChanged(new PropertyChangedEventArgs(null));
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		foreach (var h in StackedInputHandlers)
		{
			if (e.Handled)
			{
				break;
			}
			h.OnPreviewKeyDown(e);
		}
	}

	private void OnPreviewKeyUp(object sender, KeyEventArgs e)
	{
		foreach (var h in StackedInputHandlers)
		{
			if (e.Handled)
			{
				break;
			}
			h.OnPreviewKeyUp(e);
		}
	}

	private void OnUpdateFinished(object sender, EventArgs e)
	{
		Caret.OnDocumentUpdateFinished();
	}

	private void OnUpdateStarted(object sender, EventArgs e)
	{
		Document.UndoStack.PushOptional(new RestoreCaretAndSelectionUndoAction(this));
	}

	private void ReplaceSelectionWithNewLine()
	{
		var newLine = TextUtilities.GetNewLineFromDocument(Document, Caret.Line);
		using (Document.RunUpdate())
		{
			ReplaceSelectionWithText(newLine);
			if (IndentationStrategy != null)
			{
				var line = Document.GetLineByNumber(Caret.Line);
				var deletable = GetDeletableSegments(line);
				if ((deletable.Length == 1) && (deletable[0].StartIndex == line.StartIndex) && (deletable[0].Length == line.Length))
				{
					// use indentation strategy only if the line is not read-only
					IndentationStrategy.IndentLine(Document, line);
				}
			}
		}
	}

	private void RequestSelectionValidation()
	{
		if (!_ensureSelectionValidRequested && (_allowCaretOutsideSelection == 0))
		{
			_ensureSelectionValidRequested = true;
			Dispatcher.UIThread.Post(EnsureSelectionValid);
		}
	}

	private void ShowMouseCursor()
	{
		if (_isMouseCursorHidden)
		{
			_isMouseCursorHidden = false;
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Occurs when the ActiveInputHandler property changes.
	/// </summary>
	public event EventHandler ActiveInputHandlerChanged;

	/// <inheritdoc />
	public event EventHandler<DocumentChangedEventArgs> DocumentChanged;

	/// <summary>
	/// Occurs when a text editor option has changed.
	/// </summary>
	public event PropertyChangedEventHandler OptionChanged;

	/// <summary>
	/// Occurs when the selection has changed.
	/// </summary>
	public event EventHandler SelectionChanged;

	/// <summary>
	/// Occurs when text inside the TextArea was copied.
	/// </summary>
	public event EventHandler<TextEventArgs> TextCopied;

	/// <summary>
	/// Occurs when the TextArea receives text input.
	/// but occurs immediately after the TextArea handles the TextInput event.
	/// </summary>
	public event EventHandler<TextInputEventArgs> TextEntered;

	/// <summary>
	/// Occurs when the TextArea receives text input.
	/// but occurs immediately before the TextArea handles the TextInput event.
	/// </summary>
	public event EventHandler<TextInputEventArgs> TextEntering;

	event EventHandler ILogicalScrollable.ScrollInvalidated
	{
		add
		{
			if (_logicalScrollable != null)
			{
				_logicalScrollable.ScrollInvalidated += value;
			}
		}
		remove
		{
			if (_logicalScrollable != null)
			{
				_logicalScrollable.ScrollInvalidated -= value;
			}
		}
	}

	#endregion

	#region Classes

	private sealed class RestoreCaretAndSelectionUndoAction : IUndoableOperation
	{
		#region Fields

		private readonly TextViewPosition _caretPosition;

		private readonly Selection _selection;

		// keep textarea in weak reference because the IUndoableOperation is stored with the document
		private readonly WeakReference _textAreaReference;

		#endregion

		#region Constructors

		public RestoreCaretAndSelectionUndoAction(TextArea textArea)
		{
			_textAreaReference = new WeakReference(textArea);
			// Just save the old caret position, no need to validate here.
			// If we restore it, we'll validate it anyways.
			_caretPosition = textArea.Caret.NonValidatedPosition;
			_selection = textArea.Selection;
		}

		#endregion

		#region Methods

		public void Redo()
		{
			// redo=undo: we just restore the caret/selection state
			Undo();
		}

		public void Undo()
		{
			var textArea = (TextArea) _textAreaReference.Target;
			if (textArea != null)
			{
				textArea.Caret.Position = _caretPosition;
				textArea.Selection = _selection;
			}
		}

		#endregion
	}

	private class TextAreaTextInputMethodClient : TextInputMethodClient
	{
		#region Fields

		private TextArea _textArea;

		#endregion

		#region Properties

		public override Rect CursorRectangle
		{
			get
			{
				if (_textArea == null)
				{
					return default;
				}

				var transform = _textArea.TextView.TransformToVisual(_textArea);

				if (transform == null)
				{
					return default;
				}

				var rect = _textArea.Caret.CalculateCaretRectangle().TransformToAABB(transform.Value);
				var scrollOffset = _textArea.TextView.ScrollOffset;
				rect = rect.WithX(rect.X - scrollOffset.X).WithY(rect.Y - scrollOffset.Y);
				return rect;
			}
		}

		public override TextSelection Selection
		{
			get
			{
				if ((_textArea == null) || (_textArea.Selection.Length <= 0))
				{
					return new TextSelection(0, 0);
				}
				return new TextSelection(_textArea.Caret.Position.Column, _textArea.Caret.Position.Column + _textArea.Selection.Length);
			}
			set
			{
				if (_textArea == null)
				{
					return;
				}
				var selection = _textArea.Selection;
				if (selection.StartPosition.Line == 0)
				{
					return;
				}

				_textArea.Selection = selection.StartSelectionOrSetEndpoint(
					new TextViewPosition(selection.StartPosition.Line, value.Start),
					new TextViewPosition(selection.StartPosition.Line, value.End));
			}
		}

		public override bool SupportsPreedit => false;

		public override bool SupportsSurroundingText => true;

		public override string SurroundingText
		{
			get
			{
				if (_textArea == null)
				{
					return default;
				}

				var lineIndex = _textArea.Caret.Line;

				var documentLine = _textArea.Document.GetLineByNumber(lineIndex);

				var text = _textArea.Document.GetText(documentLine.StartIndex, documentLine.Length);

				return text;
			}
		}

		public override Visual TextViewVisual => _textArea;

		#endregion

		#region Methods

		public override void SetPreeditText(string text)
		{
		}

		public void SetTextArea(TextArea textArea)
		{
			if (_textArea != null)
			{
				_textArea.Caret.PositionChanged -= Caret_PositionChanged;
			}

			_textArea = textArea;

			if (_textArea != null)
			{
				_textArea.Caret.PositionChanged += Caret_PositionChanged;
			}

			RaiseTextViewVisualChanged();

			RaiseCursorRectangleChanged();

			RaiseSurroundingTextChanged();
		}

		private void Caret_PositionChanged(object sender, EventArgs e)
		{
			RaiseCursorRectangleChanged();
			RaiseSurroundingTextChanged();
			RaiseSelectionChanged();
		}

		#endregion
	}

	#endregion
}

/// <summary>
/// EventArgs with text.
/// </summary>
public class TextEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// Creates a new TextEventArgs instance.
	/// </summary>
	public TextEventArgs(string text)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the text.
	/// </summary>
	public string Text { get; }

	#endregion
}