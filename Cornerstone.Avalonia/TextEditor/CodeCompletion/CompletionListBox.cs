#region References

using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

/// <summary>
/// The list box used inside the CompletionList.
/// </summary>
[DoNotNotify]
public class CompletionListBox : ListBox
{
	#region Fields

	internal ScrollViewer ScrollViewer;

	#endregion
	
	#region Properties

	/// <summary>
	/// Gets the number of the first visible item.
	/// </summary>
	public int FirstVisibleItem
	{
		get
		{
			if ((ScrollViewer == null) || (ScrollViewer.Extent.Height == 0))
			{
				return 0;
			}

			return (int) ((ItemCount * ScrollViewer.Offset.Y) / ScrollViewer.Extent.Height);
		}
		set
		{
			value = value.CoerceValue(0, ItemCount - VisibleItemCount);
			if (ScrollViewer != null)
			{
				ScrollViewer.Offset = ScrollViewer.Offset.WithY(((double) value / ItemCount) * ScrollViewer.Extent.Height);
			}
		}
	}

	/// <summary>
	/// Gets the number of visible items.
	/// </summary>
	public int VisibleItemCount
	{
		get
		{
			if ((ScrollViewer == null) || (ScrollViewer.Extent.Height == 0))
			{
				return 10;
			}
			return Math.Max(
				3,
				(int) Math.Ceiling((ItemCount * ScrollViewer.Viewport.Height)
					/ ScrollViewer.Extent.Height));
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Centers the view on the item with the specified index.
	/// </summary>
	public void CenterViewOn(int index)
	{
		FirstVisibleItem = index - (VisibleItemCount / 2);
	}

	/// <summary>
	/// Removes the selection.
	/// </summary>
	public void ClearSelection()
	{
		SelectedIndex = -1;
	}

	/// <summary>
	/// Selects the item with the specified index and scrolls it into view.
	/// </summary>
	public void SelectIndex(int index)
	{
		if (index >= ItemCount)
		{
			index = ItemCount - 1;
		}
		if (index < 0)
		{
			index = 0;
		}
		SelectedIndex = index;
		ScrollIntoView(SelectedItem);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		ScrollViewer = e.NameScope.Find("PART_ScrollViewer") as ScrollViewer;
	}

	#endregion
}