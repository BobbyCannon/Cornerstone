#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.BookmarkBar;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// One bookmark or folder chip. Folder flyouts are windowed popups; ignore pointer events that originated on a nested chip, because PopupRoot routes those to this parent.
/// </summary>
[SourceReflection]
[TemplatePart(PartHeader, typeof(Border))]
[TemplatePart(PartInsertLeading, typeof(Border))]
[TemplatePart(PartInsertTrailing, typeof(Border))]
[TemplatePart(PartFolderPopup, typeof(Popup))]
[TemplatePart(PartEdit, typeof(MenuItem))]
[TemplatePart(PartRemove, typeof(MenuItem))]
[TemplatePart(PartShowIconOnly, typeof(MenuItem))]
[TemplatePart(PartAddFolder, typeof(MenuItem))]
public partial class BookmarkBarItem : TemplatedControl
{
	#region Fields

	public const string PartAddFolder = "PART_AddFolder";
	public const string PartEdit = "PART_Edit";
	public const string PartFolderPopup = "PART_FolderPopup";
	public const string PartHeader = "PART_Header";
	public const string PartInsertLeading = "PART_InsertLeading";
	public const string PartInsertTrailing = "PART_InsertTrailing";
	public const string PartRemove = "PART_Remove";
	public const string PartShowIconOnly = "PART_ShowIconOnly";

	private MenuItem _addFolderMenuItem;
	private BookmarkBarControl _bar;
	private MenuItem _editMenuItem;
	private Popup _folderPopup;
	private TopLevel _folderPopupTopLevel;
	private Border _header;
	private Border _insertLeading;
	private Border _insertTrailing;
	private MenuItem _removeMenuItem;
	private MenuItem _showIconOnlyMenuItem;

	#endregion

	#region Constructors

	public BookmarkBarItem()
	{
		_addFolderMenuItem = null;
		_bar = null;
		_editMenuItem = null;
		_folderPopup = null;
		_folderPopupTopLevel = null;
		_header = null;
		_insertLeading = null;
		_insertTrailing = null;
		_removeMenuItem = null;
		_showIconOnlyMenuItem = null;
		IsNested = false;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial IBookmarkBarItem Bookmark { get; set; }

	[StyledProperty]
	public partial bool IsFolderOpen { get; set; }

	[StyledProperty]
	public partial bool IsNested { get; set; }

	internal bool IsFolder => Bookmark is { IsParent: true };

	internal BookmarkBarControl Bar => _bar;

	#endregion

	#region Methods

	internal void ClearDropHint()
	{
		Classes.Set("drop-into", false);
		ShowInsertLine(_insertLeading, false);
		ShowInsertLine(_insertTrailing, false);
	}

	internal bool HostsPopup(TopLevel topLevel)
	{
		return (topLevel != null) && ReferenceEquals(_folderPopupTopLevel, topLevel);
	}

	internal bool IsOverHeader(Point pointOnItem)
	{
		return (pointOnItem.X >= 0)
			&& (pointOnItem.Y >= 0)
			&& (pointOnItem.X <= Bounds.Width)
			&& (pointOnItem.Y <= Bounds.Height);
	}

	internal void SetDropHint(bool insertBefore, bool intoFolder)
	{
		Classes.Set("drop-into", intoFolder);
		ShowInsertLine(_insertLeading, !intoFolder && insertBefore);
		ShowInsertLine(_insertTrailing, !intoFolder && !insertBefore);
	}

	internal BookmarkBarItem FindNextSibling()
	{
		Visual current = this;
		while (current != null)
		{
			var parent = current.GetVisualParent();
			if (parent is Panel panel)
			{
				var index = -1;
				for (var i = 0; i < panel.Children.Count; i++)
				{
					if (ReferenceEquals(panel.Children[i], current))
					{
						index = i;
						break;
					}
				}

				if ((index < 0) || (index >= (panel.Children.Count - 1)))
				{
					return null;
				}

				return FindItemIn(panel.Children[index + 1]);
			}

			current = parent;
		}

		return null;
	}

	private static BookmarkBarItem FindItemIn(Visual visual)
	{
		if (visual is BookmarkBarItem item)
		{
			return item;
		}

		foreach (var descendant in visual.GetVisualDescendants())
		{
			if (descendant is BookmarkBarItem found)
			{
				return found;
			}
		}

		return null;
	}

	private void ShowInsertLine(Border line, bool show)
	{
		if (line == null)
		{
			return;
		}

		if (IsNested)
		{
			line.Width = double.NaN;
			line.Height = 3;
			line.HorizontalAlignment = HorizontalAlignment.Stretch;
			line.VerticalAlignment = ReferenceEquals(line, _insertLeading)
				? VerticalAlignment.Top
				: VerticalAlignment.Bottom;
		}
		else
		{
			line.Width = 3;
			line.Height = double.NaN;
			line.HorizontalAlignment = ReferenceEquals(line, _insertLeading)
				? HorizontalAlignment.Left
				: HorizontalAlignment.Right;
			line.VerticalAlignment = VerticalAlignment.Stretch;
		}

		line.IsVisible = show;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		DetachTemplate();
		_header = e.NameScope.Find<Border>(PartHeader);
		_insertLeading = e.NameScope.Find<Border>(PartInsertLeading);
		_insertTrailing = e.NameScope.Find<Border>(PartInsertTrailing);
		_folderPopup = e.NameScope.Find<Popup>(PartFolderPopup);
		_editMenuItem = e.NameScope.Find<MenuItem>(PartEdit);
		_removeMenuItem = e.NameScope.Find<MenuItem>(PartRemove);
		_showIconOnlyMenuItem = e.NameScope.Find<MenuItem>(PartShowIconOnly);
		_addFolderMenuItem = e.NameScope.Find<MenuItem>(PartAddFolder);

		if (_folderPopup != null)
		{
			_folderPopup.Opened += OnFolderPopupOpened;
			_folderPopup.Closed += OnFolderPopupClosed;
		}

		if (_editMenuItem != null)
		{
			_editMenuItem.Click += OnEdit;
		}

		if (_removeMenuItem != null)
		{
			_removeMenuItem.Click += OnRemove;
		}

		if (_showIconOnlyMenuItem != null)
		{
			_showIconOnlyMenuItem.Click += OnSetShowIconOnly;
		}

		if (_addFolderMenuItem != null)
		{
			_addFolderMenuItem.Click += OnAddFolder;
		}

		UpdateClasses();
	}

	private void DetachTemplate()
	{
		if (_folderPopup != null)
		{
			_folderPopup.Opened -= OnFolderPopupOpened;
			_folderPopup.Closed -= OnFolderPopupClosed;
			_folderPopup = null;
		}

		if (_editMenuItem != null)
		{
			_editMenuItem.Click -= OnEdit;
			_editMenuItem = null;
		}

		if (_removeMenuItem != null)
		{
			_removeMenuItem.Click -= OnRemove;
			_removeMenuItem = null;
		}

		if (_showIconOnlyMenuItem != null)
		{
			_showIconOnlyMenuItem.Click -= OnSetShowIconOnly;
			_showIconOnlyMenuItem = null;
		}

		if (_addFolderMenuItem != null)
		{
			_addFolderMenuItem.Click -= OnAddFolder;
			_addFolderMenuItem = null;
		}

		_header = null;
		_insertLeading = null;
		_insertTrailing = null;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		_bar = this.FindLogicalAncestorOfType<BookmarkBarControl>();
		UpdateClasses();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_bar = null;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !IsPressOnThisChip(e.Source))
		{
			return;
		}

		e.Pointer.Capture(this);
		e.Handled = true;
		_bar?.OnItemPressed(this, e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		if (!IsPressOnThisChip(e.Source) && (e.Pointer.Captured != this))
		{
			return;
		}

		_bar?.OnItemMoved(this, e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (!IsPressOnThisChip(e.Source) && (e.Pointer.Captured != this))
		{
			return;
		}

		if (e.Pointer.Captured == this)
		{
			e.Pointer.Capture(null);
		}

		e.Handled = true;
		_bar?.OnItemReleased(this, e);
	}

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		base.OnPointerEntered(e);
		if (!IsPressOnThisChip(e.Source))
		{
			return;
		}

		_bar?.OnItemEntered(this);
	}

	private bool IsPressOnThisChip(object source)
	{
		return BookmarkBarControl.FindItem(source) == this;
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if ((change.Property == BookmarkProperty) || (change.Property == IsFolderOpenProperty))
		{
			UpdateClasses();
		}
	}

	private void OnAddFolder(object sender, RoutedEventArgs e)
	{
		_bar?.AddFolderCommand?.Execute(Bookmark);
	}

	private void OnEdit(object sender, RoutedEventArgs e)
	{
		_bar?.EditCommand?.Execute(Bookmark);
	}

	private void OnRemove(object sender, RoutedEventArgs e)
	{
		_bar?.RemoveCommand?.Execute(Bookmark);
	}

	private void OnSetShowIconOnly(object sender, RoutedEventArgs e)
	{
		_bar?.SetShowIconOnlyCommand?.Execute(Bookmark);
	}

	private void UpdateClasses()
	{
		if (!IsNested)
		{
			IsNested = (this.FindLogicalAncestorOfType<BookmarkBarItem>() != null) || IsHostedInFolderPopup();
		}

		HorizontalAlignment = IsNested ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
		Classes.Set("folder", IsFolder);
		Classes.Set("nested", IsNested);
		Classes.Set("open", IsFolderOpen);
		ApplyFolderPlacement();
		if (IsFolderOpen && !IsFolder)
		{
			IsFolderOpen = false;
		}
	}

	private bool IsHostedInFolderPopup()
	{
		var itemTopLevel = TopLevel.GetTopLevel(this);
		if (itemTopLevel == null)
		{
			return false;
		}

		if (itemTopLevel is PopupRoot)
		{
			return true;
		}

		if (_bar == null)
		{
			return false;
		}

		var barTopLevel = TopLevel.GetTopLevel(_bar);
		return (barTopLevel != null) && (itemTopLevel != barTopLevel);
	}

	private void ApplyFolderPlacement()
	{
		if (_folderPopup == null)
		{
			return;
		}

		_folderPopup.PlacementTarget = _header;
		_folderPopup.Placement = IsNested
			? PlacementMode.RightEdgeAlignedTop
			: PlacementMode.BottomEdgeAlignedLeft;
	}

	private void OnFolderPopupOpened(object sender, EventArgs e)
	{
		ApplyFolderPlacement();
		var child = _folderPopup.Child as Visual;
		_folderPopupTopLevel = child == null ? null : TopLevel.GetTopLevel(child);
		if (_folderPopupTopLevel != null)
		{
			_bar?.AddPopupTopLevel(_folderPopupTopLevel);
		}
	}

	private void OnFolderPopupClosed(object sender, EventArgs e)
	{
		if (_folderPopupTopLevel == null)
		{
			return;
		}

		_bar?.RemovePopupTopLevel(_folderPopupTopLevel);
		_folderPopupTopLevel = null;
	}

	#endregion
}
