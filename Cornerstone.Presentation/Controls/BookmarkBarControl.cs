#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.BookmarkBar;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Bookmark strip with overlay folder flyouts. Windowed MenuItem popups put children on another HWND, so Windows OLE drag never delivers them to the bar.
/// </summary>
[SourceReflection]
public partial class BookmarkBarControl : TemplatedControl
{
	#region Fields

	private static readonly DataFormat<IBookmarkBarItem> BookmarkDragFormat;

	private const double DragThreshold = 5;
	private bool _isDragging;
	private bool _dragStarted;
	private BookmarkBarItem _dropHintItem;
	private PointerPressedEventArgs _pressArgs;
	private Point _pressPosition;
	private IBookmarkBarItem _potentialDragItem;
	private TopLevel _dragTopLevel;
	private HashSet<TopLevel> _popupTopLevels;
	private BookmarkBarItem _hoverFolderItem;
	private DispatcherTimer _folderHoverTimer;

	#endregion

	#region Constructors

	static BookmarkBarControl()
	{
		BookmarkDragFormat = DataFormat.CreateInProcessFormat<IBookmarkBarItem>("cornerstone-bookmark-bar");
	}

	public BookmarkBarControl()
	{
		DragDrop.SetAllowDrop(this, true);
		_isDragging = false;
		_dragStarted = false;
		_dropHintItem = null;
		_pressArgs = null;
		_pressPosition = default;
		_potentialDragItem = null;
		_dragTopLevel = null;
		_popupTopLevels = [];
		_hoverFolderItem = null;
		_folderHoverTimer = null;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		DetachTopLevelHandlers();
		_dragTopLevel = TopLevel.GetTopLevel(this);
		if (_dragTopLevel == null)
		{
			return;
		}

		AttachDragHandlers(_dragTopLevel);
		EnsureFolderHoverTimer();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		StopFolderHoverTimer();
		DetachTopLevelHandlers();
		base.OnDetachedFromVisualTree(e);
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial ICommand AddFolderCommand { get; set; }

	[StyledProperty]
	public partial IEnumerable Bookmarks { get; set; }

	[StyledProperty]
	public partial ICommand EditCommand { get; set; }

	[StyledProperty]
	public partial ICommand MoveCommand { get; set; }

	[StyledProperty]
	public partial ICommand NavigateCommand { get; set; }

	[StyledProperty]
	public partial ICommand RemoveCommand { get; set; }

	[StyledProperty]
	public partial ICommand SetShowIconOnlyCommand { get; set; }

	#endregion

	#region Methods

	internal void AddPopupTopLevel(TopLevel topLevel)
	{
		if ((topLevel == null) || (topLevel == _dragTopLevel) || !_popupTopLevels.Add(topLevel))
		{
			return;
		}

		AttachDragHandlers(topLevel);
	}

	internal void RemovePopupTopLevel(TopLevel topLevel)
	{
		if ((topLevel == null) || !_popupTopLevels.Remove(topLevel))
		{
			return;
		}

		DetachDragHandlers(topLevel);
	}

	internal void OnItemEntered(BookmarkBarItem item)
	{
		if (_isDragging || _dragStarted || (item == null))
		{
			return;
		}

		if (!AnyFolderOpen())
		{
			return;
		}

		if (item.IsFolder)
		{
			OpenFolder(item);
			return;
		}

		CloseFoldersOutside(item);
	}

	internal void OnItemMoved(BookmarkBarItem item, PointerEventArgs e)
	{
		if ((_potentialDragItem == null) || _isDragging || _dragStarted || (_pressArgs == null))
		{
			return;
		}

		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			return;
		}

		var origin = DragPosition(e);
		var dx = origin.X - _pressPosition.X;
		var dy = origin.Y - _pressPosition.Y;
		if (Math.Sqrt((dx * dx) + (dy * dy)) <= DragThreshold)
		{
			return;
		}

		_dragStarted = true;
		_isDragging = true;
		ClearFolderHover();
		e.Pointer.Capture(null);
		var data = new DataTransfer();
		data.Add(DataTransferItem.Create(BookmarkDragFormat, _potentialDragItem));
		_ = StartDragAsync(_pressArgs, data);
	}

	internal void OnItemPressed(BookmarkBarItem item, PointerPressedEventArgs e)
	{
		var origin = FindItem(e.Source);
		if ((origin != null) && (origin != item))
		{
			return;
		}

		_potentialDragItem = item?.Bookmark;
		_pressArgs = e;
		_pressPosition = DragPosition(e);
		_isDragging = false;
		_dragStarted = false;
	}

	internal void OnItemReleased(BookmarkBarItem item, PointerReleasedEventArgs e)
	{
		var origin = FindItem(e.Source);
		if ((origin != null) && (origin != item) && (e.Pointer.Captured != item))
		{
			return;
		}

		var started = _dragStarted || _isDragging;
		_potentialDragItem = null;
		_pressArgs = null;
		_dragStarted = false;
		if (started || (item?.Bookmark == null) || (e.InitialPressMouseButton != MouseButton.Left))
		{
			return;
		}

		if (item.IsFolder)
		{
			ToggleFolder(item);
			return;
		}

		CloseAllFolders();
		NavigateCommand?.Execute(item.Bookmark);
	}

	private async Task StartDragAsync(PointerPressedEventArgs pressArgs, DataTransfer data)
	{
		try
		{
			await DragDrop.DoDragDropAsync(pressArgs, data, DragDropEffects.Move);
		}
		finally
		{
			_isDragging = false;
			_dragStarted = false;
			_potentialDragItem = null;
			_pressArgs = null;
			ClearDropHint();
			ClearFolderHover();
			CloseAllFolders();
		}
	}

	private Point DragPosition(PointerEventArgs e)
	{
		return _dragTopLevel != null ? e.GetPosition(_dragTopLevel) : e.GetPosition(this);
	}

	private void OnTopLevelPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (_isDragging || _dragStarted)
		{
			return;
		}

		if (FindItem(e.Source) != null)
		{
			return;
		}

		CloseAllFolders();
	}

	private void ClearDropHint()
	{
		_dropHintItem?.ClearDropHint();
		_dropHintItem = null;
	}

	private void AttachDragHandlers(TopLevel topLevel)
	{
		if (topLevel == null)
		{
			return;
		}

		topLevel.AddHandler(DragDrop.DragEnterEvent, OnDragOver, handledEventsToo: true);
		topLevel.AddHandler(DragDrop.DragOverEvent, OnDragOver, handledEventsToo: true);
		topLevel.AddHandler(DragDrop.DropEvent, OnDrop, handledEventsToo: true);
		topLevel.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, handledEventsToo: true);
	}

	private void DetachDragHandlers(TopLevel topLevel)
	{
		if (topLevel == null)
		{
			return;
		}

		topLevel.RemoveHandler(DragDrop.DragEnterEvent, OnDragOver);
		topLevel.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
		topLevel.RemoveHandler(DragDrop.DropEvent, OnDrop);
		topLevel.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);
	}

	private void DetachTopLevelHandlers()
	{
		foreach (var popup in _popupTopLevels.ToArray())
		{
			RemovePopupTopLevel(popup);
		}

		if (_dragTopLevel == null)
		{
			return;
		}

		DetachDragHandlers(_dragTopLevel);
		_dragTopLevel = null;
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		var source = e.DataTransfer?.TryGetValue(BookmarkDragFormat);
		if (source == null)
		{
			return;
		}

		e.Handled = true;
		var classified = TryClassify(e, source, out var item, out var insertBefore, out var intoFolder, out var overBar);
		if ((MoveCommand == null) || !classified)
		{
			e.DragEffects = DragDropEffects.None;
			ClearDropHint();
			ClearFolderHover();
			if (item != null)
			{
				CloseFoldersOutside(item);
			}
			else if (overBar)
			{
				CloseFoldersOutside(null);
			}
			else
			{
				CloseAllFolders();
			}

			return;
		}

		e.DragEffects = DragDropEffects.Move;
		CloseFoldersOutside(item);
		ApplyDropHint(item, insertBefore, intoFolder);
		UpdateFolderHover(item, source);
	}

	private void OnDrop(object sender, DragEventArgs e)
	{
		var source = e.DataTransfer?.TryGetValue(BookmarkDragFormat);
		if (source == null)
		{
			return;
		}

		e.Handled = true;
		var classified = TryClassify(e, source, out var item, out var insertBefore, out var intoFolder, out _);
		ClearDropHint();
		ClearFolderHover();

		if (!classified || (MoveCommand == null) || (Bookmarks == null))
		{
			return;
		}

		var child = BookmarkBarDrop.FindItem(Bookmarks, source.SyncId);
		if (child == null)
		{
			return;
		}

		var dropTarget = item?.Bookmark == null
			? null
			: BookmarkBarDrop.FindItem(Bookmarks, item.Bookmark.SyncId);
		if ((dropTarget != null) && BookmarkBarDrop.CannotDropOn(child, dropTarget))
		{
			return;
		}

		MoveCommand.Execute(new BookmarkBarMove
		{
			Source = child,
			Target = dropTarget,
			InsertBefore = insertBefore,
			IntoFolder = intoFolder
		});
	}

	private bool TryClassify(
		DragEventArgs e,
		IBookmarkBarItem source,
		out BookmarkBarItem item,
		out bool insertBefore,
		out bool intoFolder,
		out bool overBar)
	{
		item = FindItem(e.Source);
		insertBefore = true;
		intoFolder = false;
		var barPoint = e.GetPosition(this);
		overBar = (barPoint.X >= 0)
			&& (barPoint.Y >= 0)
			&& (barPoint.X <= Bounds.Width)
			&& (barPoint.Y <= Bounds.Height);

		if (item == null)
		{
			return overBar;
		}

		if ((item.Bookmark != null) && BookmarkBarDrop.CannotDropOn(source, item.Bookmark))
		{
			return false;
		}

		if (!item.IsOverHeader(e.GetPosition(item)))
		{
			if (!item.IsFolder)
			{
				item = item.FindLogicalAncestorOfType<BookmarkBarItem>();
				if ((item == null) || !item.IsFolder)
				{
					return overBar;
				}
			}

			intoFolder = true;
			insertBefore = false;
			return true;
		}

		var vertical = item.IsNested;
		var along = vertical ? e.GetPosition(item).Y : e.GetPosition(item).X;
		var extent = vertical ? item.Bounds.Height : item.Bounds.Width;
		BookmarkBarDrop.Classify(along, extent, item.IsFolder, out insertBefore, out intoFolder);
		return true;
	}

	private void ApplyDropHint(BookmarkBarItem item, bool insertBefore, bool intoFolder)
	{
		if (item == null)
		{
			ClearDropHint();
			return;
		}

		var lineItem = item;
		var leading = insertBefore;
		if (!intoFolder && !insertBefore)
		{
			var next = item.FindNextSibling();
			if (next != null)
			{
				lineItem = next;
				leading = true;
			}
		}

		if ((_dropHintItem != null) && (_dropHintItem != lineItem))
		{
			ClearDropHint();
		}

		lineItem.SetDropHint(leading, intoFolder);
		_dropHintItem = lineItem;
	}

	private void EnsureFolderHoverTimer()
	{
		if (_folderHoverTimer != null)
		{
			return;
		}

		_folderHoverTimer = new DispatcherTimer();
		_folderHoverTimer.Interval = BookmarkBarDrop.FolderHoverDelay;
		_folderHoverTimer.Tick += OnFolderHoverTick;
	}

	private void StopFolderHoverTimer()
	{
		if (_folderHoverTimer == null)
		{
			return;
		}

		_folderHoverTimer.Stop();
		_folderHoverTimer.Tick -= OnFolderHoverTick;
		_folderHoverTimer = null;
	}

	private void ClearFolderHover()
	{
		_hoverFolderItem = null;
		_folderHoverTimer?.Stop();
	}

	private void UpdateFolderHover(BookmarkBarItem item, IBookmarkBarItem source)
	{
		if ((item == null) || !item.IsFolder || ((source != null) && (item.Bookmark != null) && (source.SyncId == item.Bookmark.SyncId)))
		{
			ClearFolderHover();
			return;
		}

		if (item.IsFolderOpen)
		{
			_hoverFolderItem = item;
			_folderHoverTimer?.Stop();
			return;
		}

		if (_hoverFolderItem == item)
		{
			return;
		}

		_hoverFolderItem = item;
		EnsureFolderHoverTimer();
		_folderHoverTimer.Stop();
		_folderHoverTimer.Start();
	}

	private void OnFolderHoverTick(object sender, EventArgs e)
	{
		_folderHoverTimer?.Stop();
		if ((_hoverFolderItem == null) || _hoverFolderItem.IsFolderOpen)
		{
			return;
		}

		OpenFolder(_hoverFolderItem);
	}

	private void ToggleFolder(BookmarkBarItem item)
	{
		if ((item == null) || !item.IsFolder)
		{
			return;
		}

		if (item.IsFolderOpen)
		{
			item.IsFolderOpen = false;
			CloseFoldersOutside(item.FindLogicalAncestorOfType<BookmarkBarItem>());
			return;
		}

		OpenFolder(item);
	}

	private void OpenFolder(BookmarkBarItem item)
	{
		if ((item == null) || !item.IsFolder)
		{
			return;
		}

		CloseFoldersOutside(item);
		item.IsFolderOpen = true;
	}

	private void CloseAllFolders()
	{
		CloseFoldersOutside(null);
	}

	private void CloseFoldersOutside(BookmarkBarItem keep)
	{
		var keepChain = new HashSet<BookmarkBarItem>();
		for (var current = keep; current != null; current = current.FindLogicalAncestorOfType<BookmarkBarItem>())
		{
			keepChain.Add(current);
		}

		foreach (var item in EnumerateItems())
		{
			if (item.IsFolderOpen && !keepChain.Contains(item))
			{
				item.IsFolderOpen = false;
			}
		}

		if ((_hoverFolderItem is { IsFolderOpen: true }) && !keepChain.Contains(_hoverFolderItem))
		{
			_hoverFolderItem.IsFolderOpen = false;
		}
	}

	private bool AnyFolderOpen()
	{
		foreach (var item in EnumerateItems())
		{
			if (item.IsFolderOpen)
			{
				return true;
			}
		}

		return false;
	}

	private List<BookmarkBarItem> EnumerateItems()
	{
		var items = new List<BookmarkBarItem>();
		var seen = new HashSet<BookmarkBarItem>();
		CollectItems(this, items, seen);
		if (_dragTopLevel != null)
		{
			CollectItems(_dragTopLevel, items, seen);
		}

		foreach (var popup in _popupTopLevels)
		{
			CollectItems(popup, items, seen);
		}

		return items;
	}

	private static void CollectItems(Visual root, List<BookmarkBarItem> items, HashSet<BookmarkBarItem> seen)
	{
		if (root == null)
		{
			return;
		}

		foreach (var item in root.GetVisualDescendants().OfType<BookmarkBarItem>())
		{
			if (seen.Add(item))
			{
				items.Add(item);
			}
		}
	}

	internal static BookmarkBarItem FindItem(object source)
	{
		if (source is BookmarkBarItem item)
		{
			return item;
		}

		if (source is Control control)
		{
			return control.FindAncestorOfType<BookmarkBarItem>()
				?? control.FindLogicalAncestorOfType<BookmarkBarItem>();
		}

		if (source is not Visual visual)
		{
			return null;
		}

		for (var current = visual; current != null; current = current.GetVisualParent())
		{
			if (current is BookmarkBarItem found)
			{
				return found;
			}
		}

		return null;
	}

	#endregion
}
