#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

/// <summary>
/// The code completion window.
/// </summary>
public class CompletionWindow : CompletionWindowBase
{
	#region Fields

	private PopupWithCustomPosition _toolTip;
	private CompletionTipContentControl _toolTipContent;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new code completion window.
	/// </summary>
	public CompletionWindow(TextArea textArea, string prefix, Key[] completionsKeys, params ICompletionData[] suggestions) : base(textArea)
	{
		CompletionList = new CompletionList(prefix, suggestions, completionsKeys);

		// keep height automatic
		CloseAutomatically = true;
		MaxHeight = 225;
		Width = 175;
		Child = CompletionList;

		// prevent user from resizing window to 0x0
		MinHeight = 15;
		MinWidth = 30;

		_toolTipContent = new CompletionTipContentControl();
		_toolTip = new PopupWithCustomPosition
		{
			IsLightDismissEnabled = true,
			PlacementTarget = this,
			Child = _toolTipContent
		};

		LogicalChildren.Add(_toolTip);

		AttachEvents();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets whether the completion window should close automatically.
	/// The default value is true.
	/// </summary>
	public bool CloseAutomatically { get; set; }

	/// <summary>
	/// When this flag is set, code completion closes if the caret moves to the
	/// beginning of the allowed range. This is useful in Ctrl+Space and "complete when typing",
	/// but not in dot-completion.
	/// Has no effect if CloseAutomatically is false.
	/// </summary>
	public bool CloseWhenCaretAtBeginning { get; set; }

	/// <summary>
	/// Gets the completion list used in this completion window.
	/// </summary>
	public CompletionList CompletionList { get; }

	/// <inheritdoc />
	protected override bool CloseOnFocusLost => CloseAutomatically;

	#endregion

	#region Methods

	public void CompleteRequest(ICompletionData item)
	{
		var segment = GetSegment(item);
		item?.Complete(TextArea, segment);
	}

	/// <inheritdoc />
	protected override void DetachEvents()
	{
		CompletionList.InsertionRequested -= CompletionListInsertionRequested;
		CompletionList.SelectionChanged -= CompletionListSelectionChanged;
		TextArea.Caret.PositionChanged -= CaretPositionChanged;
		TextArea.PointerWheelChanged -= TextAreaMouseWheel;
		TextArea.TextInput -= TextAreaPreviewTextInput;
		base.DetachEvents();
	}

	/// <inheritdoc />
	protected override void OnClosed(object sender, EventArgs e)
	{
		base.OnClosed(sender, e);

		if (_toolTip != null)
		{
			_toolTip.IsOpen = false;
			_toolTip.Child = null;
			_toolTip = null;
			_toolTipContent = null;
		}
	}

	/// <inheritdoc />
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (!e.Handled)
		{
			CompletionList.HandleKey(e);
		}
	}

	internal ISegment GetSegment(ICompletionData item)
	{
		if (item == null)
		{
			return null;
		}

		var startOffset = StartOffset - CompletionList.Prefix.Length;
		var nextCharacterIndex = EndOffset;

		if ((TextArea.Document.TextLength <= nextCharacterIndex)
			|| !item.CompletionText.StartsWith(CompletionList.Prefix))
		{
			return new AnchorSegment(TextArea.Document, startOffset, EndOffset - startOffset);
		}

		var extra = item.CompletionText.Substring(CompletionList.Prefix.Length);
		if ((extra.Length > 0) && (TextArea.Document.GetCharAt(nextCharacterIndex) == extra.Last()))
		{
			return new AnchorSegment(TextArea.Document, startOffset, (EndOffset - startOffset) + 1);
		}

		return new AnchorSegment(TextArea.Document, startOffset, EndOffset - startOffset);
	}

	private void AttachEvents()
	{
		CompletionList.InsertionRequested += CompletionListInsertionRequested;
		CompletionList.SelectionChanged += CompletionListSelectionChanged;
		TextArea.Caret.PositionChanged += CaretPositionChanged;
		TextArea.PointerWheelChanged += TextAreaMouseWheel;
		TextArea.TextInput += TextAreaPreviewTextInput;
	}

	private void CaretPositionChanged(object sender, EventArgs e)
	{
		var offset = TextArea.Caret.Offset;
		if (offset == StartOffset)
		{
			if (CloseAutomatically && CloseWhenCaretAtBeginning)
			{
				Hide();
			}
			else
			{
				CompletionList.SelectItem(string.Empty);
				IsVisible = CompletionList.ListBox.ItemCount != 0;
			}

			return;
		}

		if ((offset < StartOffset) || (offset > EndOffset))
		{
			if (CloseAutomatically)
			{
				Hide();
			}
			return;
		}

		var document = TextArea.Document;
		if (document == null)
		{
			return;
		}

		CompletionList.SelectItem(document.GetText(StartOffset, offset - StartOffset));

		if (CompletionList.ListBox?.ItemCount == 0)
		{
			Hide();
		}
	}

	private void CompletionListInsertionRequested(object sender, EventArgs e)
	{
		Hide();

		// The window must close before Complete() is called.
		// If the Complete callback pushes stacked input handlers, we don't want to pop those when the CC window closes.
		var item = CompletionList.SelectedItem;
		if (item != null)
		{
			CompleteRequest(item);
		}
	}

	private void CompletionListSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_toolTipContent == null)
		{
			return;
		}

		var item = CompletionList.SelectedItem;
		var description = item?.Description;

		if ((description != null) && Host is Control placementTarget && (CompletionList.CurrentList != null))
		{
			_toolTipContent.Content = description;

			var yOffset = 0.0;
			var selectedIndex = CompletionList.ListBox.SelectedIndex;
			var itemContainer = CompletionList.ListBox.ContainerFromIndex(selectedIndex);

			if (itemContainer != null)
			{
				var position = itemContainer.TranslatePoint(new Point(0, 0), placementTarget);
				if (position.HasValue)
				{
					yOffset = position.Value.Y;
				}
				_toolTip.Offset = new Point(2, yOffset);
				_toolTip.Placement = PlacementMode.RightEdgeAlignedTop;
			}
			else
			{
				// When scrolling down the container is not always ready
				// If that happens we align the tooltip at the bottom or top
				_toolTip.Offset = new Point(placementTarget.Width + 2, yOffset);
				_toolTip.Placement = CompletionList.ListBox.FirstVisibleItem < selectedIndex
					? PlacementMode.RightEdgeAlignedBottom
					: PlacementMode.RightEdgeAlignedTop;
			}

			_toolTip.PlacementTarget = placementTarget;
			_toolTip.IsOpen = true;
		}
		else
		{
			_toolTip.IsOpen = false;
		}
	}

	private Control GetScrollEventTarget()
	{
		if (CompletionList == null)
		{
			return this;
		}
		return CompletionList.ScrollViewer ?? CompletionList.ListBox ?? (Control) CompletionList;
	}

	private void TextAreaMouseWheel(object sender, PointerWheelEventArgs e)
	{
		e.Handled = RaiseEventPair(GetScrollEventTarget(), null, PointerWheelChangedEvent, e);
	}

	private void TextAreaPreviewTextInput(object sender, TextInputEventArgs e)
	{
		e.Handled = RaiseEventPair(this, null, TextInputEvent, new TextInputEventArgs { Text = e.Text });
	}

	#endregion
}