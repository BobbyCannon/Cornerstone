#region References

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

/// <summary>
/// The code completion window.
/// </summary>
public class CompletionWindow : CompletionWindowBase
{
	#region Fields

	private readonly ICompletionProvider _provider;
	private PopupWithCustomPosition _toolTip;
	private CompletionTipContentControl _toolTipContent;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new code completion window.
	/// </summary>
	public CompletionWindow(TextArea textArea, ICompletionProvider provider) : base(textArea)
	{
		_provider = provider;

		// keep height automatic
		CloseAutomatically = true;
		MaxHeight = 300;
		MaxWidth = 400;

		// Update child / content
		CompletionList = new CompletionList(provider);
		Child = CompletionList;

		// prevent user from resizing window to 0x0
		MinHeight = 15;
		MinWidth = 175;

		_toolTipContent = new CompletionTipContentControl();
		_toolTip = new PopupWithCustomPosition
		{
			IsLightDismissEnabled = true,
			PlacementTarget = this,
			PlacementAnchor = PopupAnchor.TopLeft,
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
		if (item == null)
		{
			return;
		}

		var filter = CompletionList.CurrentFilter;
		var textToReplace = string.IsNullOrEmpty(filter)
			? _provider.TextToReplace
			: new SimpleRange(_provider.TextToReplace.StartIndex, _provider.TextToReplace.Length + filter.Length);

		var results = item.CalculateFinalResult(TextArea, textToReplace);
		TextArea.Document.Replace(textToReplace, results);

		if (item.CaretOffset > 0)
		{
			var finalOffset = TextArea.Caret.Offset + item.CaretOffset;
			finalOffset = int.Min(finalOffset, TextArea.Document.TextLength);
			finalOffset = int.Max(finalOffset, 0);
			TextArea.Caret.Offset = finalOffset;
		}
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
				IsVisible = CompletionList.ListBox?.ItemCount != 0;
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

		CompletionList.SetFilter(document.GetText(StartOffset, offset - StartOffset));

		if (CompletionList.ListBox?.ItemCount == 0)
		{
			Hide();
		}
	}

	private void CompletionListInsertionRequested(object sender, EventArgs e)
	{
		CompleteRequest(CompletionList.SelectedItem);
		Hide();
	}

	private void CompletionListSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateTooltip();
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

	private void UpdateTooltip()
	{
		if (_toolTipContent == null)
		{
			return;
		}

		var item = CompletionList.SelectedItem;
		var description = item?.Description;

		if ((description != null)
			&& Host is Control placementTarget
			&& (CompletionList.ListBox != null))
		{
			_toolTipContent.Content = description;

			double yOffset = 0;
			var selectedIndex = CompletionList.ListBox.SelectedIndex;
			var itemContainer = CompletionList.ListBox.ContainerFromIndex(selectedIndex);

			if (itemContainer != null)
			{
				_toolTip.Placement = PlacementMode.RightEdgeAlignedTop;

				var position = itemContainer.TranslatePoint(new Point(0, 0), placementTarget);
				if (position.HasValue)
				{
					yOffset = position.Value.Y;
				}
			}
			else
			{
				// When scrolling down the container is not always ready
				// If that happens we align the tooltip at the bottom or top
				_toolTip.Placement = CompletionList.ListBox.FirstVisibleItem < selectedIndex
					? PlacementMode.RightEdgeAlignedBottom
					// temp fix: needs to be better.
					: PlacementMode.RightEdgeAlignedTop;

				var position = CompletionList.ListBox.TranslatePoint(new Point(0, 0), placementTarget);
				if (position.HasValue)
				{
					yOffset = position.Value.Y;
				}
			}

			_toolTip.Offset = new Point(0, yOffset);
			_toolTip.PlacementTarget = placementTarget;
			_toolTip.IsOpen = true;

			Debug.WriteLine($"{_toolTip.PlacementTarget.Name} {_toolTip.Offset} {_toolTip.Placement}");
		}
		else
		{
			_toolTip.IsOpen = false;
		}
	}

	#endregion
}