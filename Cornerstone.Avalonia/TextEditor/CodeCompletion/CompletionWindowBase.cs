#region References

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Text.Document;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

/// <summary>
/// Base class for completion windows. Handles positioning the window at the caret.
/// </summary>
[DoNotNotify]
public class CompletionWindowBase : Popup
{
	#region Fields

	private TextEditorDocument _document;

	private InputHandler _myInputHandler;

	private readonly Window _parentWindow;

	private Point _visualLocation;
	private Point _visualLocationTop;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new CompletionWindowBase.
	/// </summary>
	public CompletionWindowBase(TextArea textArea)
	{
		TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
		_parentWindow = textArea.GetVisualRoot() as Window;

		AddHandler(PointerReleasedEvent, OnMouseUp, handledEventsToo: true);

		StartOffset = EndOffset = TextArea.Caret.Offset;

		PlacementTarget = TextArea.TextView;
		Placement = PlacementMode.AnchorAndGravity;
		PlacementAnchor = PopupAnchor.TopLeft;
		PlacementGravity = PopupGravity.BottomRight;

		Closed += OnClosed;

		AttachEvents();
		Initialize();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the end of the text range in which the completion window stays open.
	/// This text portion is used to determine the text used to select an entry in the completion list by typing.
	/// </summary>
	public int EndOffset { get; set; }

	/// <summary>
	/// Gets/sets whether the completion window should expect text insertion at the start offset,
	/// which not go into the completion region, but before it.
	/// </summary>
	/// <remarks>
	/// This property allows only a single insertion, it is reset to false
	/// when that insertion has occurred.
	/// </remarks>
	public bool ExpectInsertionBeforeStart { get; set; }

	/// <summary>
	/// Gets/Sets the start of the text range in which the completion window stays open.
	/// This text portion is used to determine the text used to select an entry in the completion list by typing.
	/// </summary>
	public int StartOffset { get; set; }

	/// <summary>
	/// Gets the parent TextArea.
	/// </summary>
	public TextArea TextArea { get; }

	/// <summary>
	/// Gets whether the completion window should automatically close when the text editor looses focus.
	/// </summary>
	protected virtual bool CloseOnFocusLost => true;

	protected override Type StyleKeyOverride => typeof(PopupRoot);

	private bool IsTextAreaFocused => _parentWindow is not { IsActive: false } && TextArea.IsFocused;

	#endregion

	#region Methods

	public void Hide()
	{
		Close();
		OnClosed(this, EventArgs.Empty);
	}

	public void Show()
	{
		UpdatePosition();

		Open();
		Height = double.NaN;
		MinHeight = 0;
	}

	/// <summary>
	/// Activates the parent window.
	/// </summary>
	protected virtual void ActivateParentWindow()
	{
		_parentWindow?.Activate();
	}

	/// <summary>
	/// Detaches events from the text area.
	/// </summary>
	protected virtual void DetachEvents()
	{
		((ISetLogicalParent) this).SetParent(null);

		if (_document != null)
		{
			_document.Changing -= TextAreaDocumentChanging;
		}
		TextArea.LostFocus -= TextAreaLostFocus;
		TextArea.TextView.ScrollOffsetChanged -= TextViewScrollOffsetChanged;
		TextArea.DocumentChanged -= TextAreaDocumentChanged;
		if (_parentWindow != null)
		{
			_parentWindow.PositionChanged -= ParentWindowLocationChanged;
			_parentWindow.Deactivated -= ParentWindowDeactivated;
		}
		TextArea.PopStackedInputHandler(_myInputHandler);
	}

	protected virtual void OnClosed(object sender, EventArgs e)
	{
		DetachEvents();
	}

	/// <inheritdoc />
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (!e.Handled && (e.Key == Key.Escape))
		{
			e.Handled = true;
			Hide();
		}
	}

	/// <summary>
	/// Raises a tunnel/bubble event pair for a control.
	/// </summary>
	/// <param name="target"> The control for which the event should be raised. </param>
	/// <param name="previewEvent"> The tunneling event. </param>
	/// <param name="event"> The bubbling event. </param>
	/// <param name="args"> The event args to use. </param>
	/// <returns> The <see cref="RoutedEventArgs.Handled" /> value of the event args. </returns>
	[SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
	protected static bool RaiseEventPair(Control target, RoutedEvent previewEvent, RoutedEvent @event, RoutedEventArgs args)
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target));
		}
		if (args == null)
		{
			throw new ArgumentNullException(nameof(args));
		}
		if (previewEvent != null)
		{
			args.RoutedEvent = previewEvent;
			target.RaiseEvent(args);
		}
		args.RoutedEvent = @event ?? throw new ArgumentNullException(nameof(@event));
		target.RaiseEvent(args);
		return args.Handled;
	}

	/// <summary>
	/// Positions the completion window at the specified position.
	/// </summary>
	protected void SetPosition(TextViewPosition position)
	{
		var textView = TextArea.TextView;

		_visualLocation = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		_visualLocationTop = textView.GetVisualPosition(position, VisualYPosition.LineTop);

		UpdatePosition();
	}

	/// <summary>
	/// Updates the position of the CompletionWindow based on the parent TextView position and the screen working area.
	/// It ensures that the CompletionWindow is completely visible on the screen.
	/// </summary>
	protected void UpdatePosition()
	{
		var textView = TextArea.TextView;
		var position = _visualLocation - textView.ScrollOffset;

		HorizontalOffset = position.X;
		VerticalOffset = position.Y;
	}

	private void AttachEvents()
	{
		((ISetLogicalParent) this).SetParent(TextArea.GetVisualRoot() as ILogical);

		_document = TextArea.Document;

		if (_document != null)
		{
			_document.Changing += TextAreaDocumentChanging;
		}

		// LostKeyboardFocus seems to be more reliable than PreviewLostKeyboardFocus - see SD-1729
		TextArea.LostFocus += TextAreaLostFocus;
		TextArea.TextView.ScrollOffsetChanged += TextViewScrollOffsetChanged;
		TextArea.DocumentChanged += TextAreaDocumentChanged;
		if (_parentWindow != null)
		{
			_parentWindow.PositionChanged += ParentWindowLocationChanged;
			_parentWindow.Deactivated += ParentWindowDeactivated;
		}

		// close previous completion windows of same type
		foreach (var x in TextArea.StackedInputHandlers.OfType<InputHandler>())
		{
			if (x.Window.GetType() == GetType())
			{
				TextArea.PopStackedInputHandler(x);
			}
		}

		_myInputHandler = new InputHandler(this);
		TextArea.PushStackedInputHandler(_myInputHandler);
	}

	private void CloseIfFocusLost()
	{
		if (CloseOnFocusLost)
		{
			//Debug.WriteLine("CloseIfFocusLost: this.IsFocus=" + IsFocused + " IsTextAreaFocused=" + IsTextAreaFocused);
			if (Child is not { IsKeyboardFocusWithin: true } && !IsTextAreaFocused)
			{
				Hide();
			}
		}
	}

	private void Initialize()
	{
		if ((_document != null) && (StartOffset != TextArea.Caret.Offset))
		{
			SetPosition(new TextViewPosition(_document.GetLocation(StartOffset)));
		}
		else
		{
			SetPosition(TextArea.Caret.Position);
		}
	}

	// Special handler: handledEventsToo
	private void OnMouseUp(object sender, PointerReleasedEventArgs e)
	{
		ActivateParentWindow();
	}

	private void ParentWindowDeactivated(object sender, EventArgs e)
	{
		Hide();
	}

	private void ParentWindowLocationChanged(object sender, EventArgs e)
	{
		UpdatePosition();
	}

	private void TextAreaDocumentChanged(object sender, EventArgs e)
	{
		Hide();
	}

	private void TextAreaDocumentChanging(object sender, DocumentChangeEventArgs e)
	{
		if (((e.Offset + e.RemovalLength) == StartOffset) && (e.RemovalLength > 0))
		{
			Hide(); // removal immediately in front of completion segment: close the window
			// this is necessary when pressing backspace after dot-completion
		}
		if ((e.Offset == StartOffset) && (e.RemovalLength == 0) && ExpectInsertionBeforeStart)
		{
			StartOffset = e.GetNewOffset(StartOffset, AnchorMovementType.AfterInsertion);
			ExpectInsertionBeforeStart = false;
		}
		else
		{
			StartOffset = e.GetNewOffset(StartOffset, AnchorMovementType.BeforeInsertion);
		}
		EndOffset = e.GetNewOffset(EndOffset, AnchorMovementType.AfterInsertion);
	}

	private void TextAreaLostFocus(object sender, RoutedEventArgs e)
	{
		Dispatcher.UIThread.Post(CloseIfFocusLost, DispatcherPriority.Background);
	}

	private void TextViewScrollOffsetChanged(object sender, EventArgs e)
	{
		ILogicalScrollable textView = TextArea;
		var visibleRect = new Rect(textView.Offset.X, textView.Offset.Y, textView.Viewport.Width, textView.Viewport.Height);
		//close completion window when the user scrolls so far that the anchor position is leaving the visible area
		if (visibleRect.Contains(_visualLocation) || visibleRect.Contains(_visualLocationTop))
		{
			UpdatePosition();
		}
		else
		{
			Hide();
		}
	}

	#endregion

	#region Classes

	/// <summary>
	/// A dummy input handler (that just invokes the default input handler).
	/// This is used to ensure the completion window closes when any other input handler
	/// becomes active.
	/// </summary>
	private sealed class InputHandler : TextAreaStackedInputHandler
	{
		#region Fields

		internal readonly CompletionWindowBase Window;

		#endregion

		#region Constructors

		public InputHandler(CompletionWindowBase window)
			: base(window.TextArea)
		{
			Debug.Assert(window != null);
			Window = window;
		}

		#endregion

		#region Methods

		public override void Detach()
		{
			base.Detach();
			Window.Hide();
		}

		public override void OnPreviewKeyDown(KeyEventArgs e)
		{
			// prevents crash when typing dead char while CC window is open
			if (e.Key == Key.DeadCharProcessed)
			{
				return;
			}
			e.Handled = RaiseEventPair(Window, null, KeyDownEvent,
				new KeyEventArgs { Key = e.Key });
		}

		public override void OnPreviewKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.DeadCharProcessed)
			{
				return;
			}
			e.Handled = RaiseEventPair(Window, null, KeyUpEvent,
				new KeyEventArgs { Key = e.Key });
		}

		#endregion
	}

	#endregion
}