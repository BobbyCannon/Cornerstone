#region References

using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Diagnostics.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

#endregion

namespace Avalonia.Diagnostics.Views;

public partial class TreePageView : UserControl
{
	#region Fields

	private IDisposable _adorner;
	private TreeViewItem _hovered;
	private readonly TreeView _tree;

	#endregion

	#region Constructors

	public TreePageView()
	{
		InitializeComponent();

		_tree = this.GetControl<TreeView>("Tree");
	}

	#endregion

	#region Methods

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == DataContextProperty)
		{
			if (change.GetOldValue<object>() is TreePageViewModel oldViewModel)
			{
				oldViewModel.ClipboardCopyRequested -= OnClipboardCopyRequested;
			}
			if (change.GetNewValue<object>() is TreePageViewModel newViewModel)
			{
				newViewModel.ClipboardCopyRequested += OnClipboardCopyRequested;
			}
		}
	}

	protected void UpdateAdorner(object sender, PointerEventArgs e)
	{
		if (e.Source is not StyledElement source)
		{
			return;
		}

		var item = source.FindLogicalAncestorOfType<TreeViewItem>();
		if (item == _hovered)
		{
			return;
		}

		_adorner?.Dispose();

		if (item is null || (item.TreeViewOwner != _tree))
		{
			_hovered = null;
			return;
		}

		_hovered = item;

		var visual = (item.DataContext as TreeNode)?.Visual as Visual;
		var shouldVisualizeMarginPadding = (DataContext as TreePageViewModel)?.MainView.ShouldVisualizeMarginPadding;
		if (visual is null || shouldVisualizeMarginPadding is null)
		{
			return;
		}

		_adorner = ControlHighlightAdorner.Add(visual, shouldVisualizeMarginPadding == true);
	}

	private void OnClipboardCopyRequested(object sender, string selector)
	{
		if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
		{
			var dataTransferItem = new DataTransferItem();
			dataTransferItem.SetText(ToText(selector));
			dataTransferItem.Set(DevToolsDataFormats.Selector, selector);

			var dataTransfer = new DataTransfer();
			dataTransfer.Add(dataTransferItem);
			clipboard.SetDataAsync(dataTransfer);
		}
	}

	private void RemoveAdorner(object sender, PointerEventArgs e)
	{
		_adorner?.Dispose();
		_adorner = null;
	}

	private static string ToText(string text)
	{
		var sb = new StringBuilder();
		var bufferStartIndex = -1;
		for (var ic = 0; ic < text.Length; ic++)
		{
			var c = text[ic];
			switch (c)
			{
				case '{':
				{
					bufferStartIndex = sb.Length;
					break;
				}
				case '}' when bufferStartIndex > -1:
				{
					sb.Remove(bufferStartIndex, sb.Length - bufferStartIndex);
					bufferStartIndex = sb.Length;
					break;
				}
				default:
				{
					sb.Append(c);
					break;
				}
			}
		}
		return sb.ToString();
	}

	#endregion
}