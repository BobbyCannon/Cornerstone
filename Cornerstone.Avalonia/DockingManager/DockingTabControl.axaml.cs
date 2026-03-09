#region References

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornerstone.Collections;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[PseudoClasses(":active")]
public partial class DockingTabControl : TabControl
{
	#region Fields

	private DockingManager _dockingManager;
	private DraggedOutTabHandler _draggedOutTabHandler;
	private (DockableTabView tabItem, Point offset)? _draggedTab;
	private (Rect bounds, int index)? _draggedTabGhost;
	private readonly RearrangePreventFlicker _dragRearrangePreventFlicker;
	private ItemsPresenter _itemsPresenterPart;
	private readonly SpeedyList<DockableTabView> _selectedOrder;

	#endregion

	#region Constructors

	public DockingTabControl()
	{
		_dragRearrangePreventFlicker = new();
		_selectedOrder = new SpeedyList<DockableTabView>(null, new OrderBy<DockableTabView>(x => x.LastSelectedOn, true));

		IsHitTestVisible = true;
		Margin = new Thickness(0);
		Padding = new Thickness(0);

		UpdatePseudoClasses();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets if this is the currently active dockable.
	/// </summary>
	[StyledProperty]
	public partial bool IsActive { get; set; }

	/// <summary>
	/// Gets or sets if the new NewTab command.
	/// </summary>
	[StyledProperty(EnableDataValidation = true)]
	public partial ICommand NewTabCommand { get; set; }

	protected override Type StyleKeyOverride => typeof(DockingTabControl);

	#endregion

	#region Methods

	public void Add(DockableTabModel tabModel)
	{
		if (!tabModel.IsInitialized)
		{
			tabModel.Initialize();
		}
		var tabView = new DockableTabView(tabModel);
		Add(tabView);
	}

	public void Add(DockableTabView tabView)
	{
		Items.Add(tabView);
		SelectedItem = tabView;

		if (tabView.TabModel != null)
		{
			_dockingManager?.OnTabModelAdded(tabView.TabModel);
		}
	}

	public void CloseAllTabs()
	{
		var items = Items.ToList();
		foreach (var item in items)
		{
			if (item is DockableTabView tabItem)
			{
				tabItem.Close(false);
			}
		}
	}

	public void Initialize(DockingManager dockingManager)
	{
		_dockingManager = dockingManager;
		Items.CollectionChanged += ItemsCollectionChanged;
	}

	public void Insert(int index, DockableTabModel tabModel)
	{
		if (!tabModel.IsInitialized)
		{
			tabModel.Initialize();
		}
		var tabView = new DockableTabView(tabModel);
		Insert(index, tabView);
	}

	public void Insert(int index, DockableTabView tabView)
	{
		Items.Insert(index, tabView);
		SelectedItem = tabView;

		if (tabView.TabModel != null)
		{
			_dockingManager?.OnTabModelAdded(tabView.TabModel);
		}
	}

	public void RegisterDraggedOutTabHandler(DraggedOutTabHandler handler)
	{
		if (_draggedOutTabHandler != null)
		{
			throw new InvalidOperationException(
				$"There is already a {nameof(DraggedOutTabHandler)} registered with this {nameof(DockingTabControl)}\n" +
				$"You must call {nameof(UnregisterDraggedOutTabHandler)} first");
		}

		_draggedOutTabHandler = handler;
	}

	public void Uninitialize()
	{
		Items.CollectionChanged -= ItemsCollectionChanged;
	}

	public void UnregisterDraggedOutTabHandler()
	{
		_draggedOutTabHandler = null;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_itemsPresenterPart = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		_draggedTab = null;
		_draggedTabGhost = null;
		_dragRearrangePreventFlicker.Reset();
		base.OnLostFocus(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		var draggedTab = _draggedTab;
		if (draggedTab == null)
		{
			return;
		}

		// Calculate distance moved
		var hitPoint = e.GetPosition(this);
		var offset = draggedTab.Value.offset;
		var deltaX = hitPoint.X - offset.X;
		var deltaY = hitPoint.Y - offset.Y;
		var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

		// Define drag threshold (in pixels)
		if (distance < 10.0)
		{
			return;
		}

		if (_dockingManager.RuntimeInformation.DevicePlatform == DevicePlatform.Windows)
		{
			var tabBarHovered = TryGetTabBarRect(out var rect) && rect.Contains(hitPoint);
			if (!tabBarHovered)
			{
				OnTabDraggedOut(e);
				return;
			}
		}

		OnDragToRearrange(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		var currentPoint = e.GetCurrentPoint(this);
		if (!currentPoint.Properties.IsLeftButtonPressed)
		{
			return;
		}

		var hitPoint = currentPoint.Position;
		var tabItem = Items
			.OfType<DockableTabView>()
			.LastOrDefault(x =>
				this.GetBoundsOf(x)
					.Contains(hitPoint)
			);

		if (tabItem == null)
		{
			return;
		}

		_draggedTab = (tabItem, hitPoint);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		_draggedTab = null;
		_draggedTabGhost = null;
		_dragRearrangePreventFlicker.Reset();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == IsActiveProperty)
		{
			UpdatePseudoClasses();
		}
		if (change.Property == SelectedValueProperty)
		{
			if (change.NewValue is DockableTabView tab)
			{
				tab.IsSelected = true;
				tab.LastSelectedOn = DateTimeProvider.RealTime.UtcNow;

				_selectedOrder.RefreshOrder();
			}
		}

		base.OnPropertyChanged(change);
	}

	private void DockableTabItemClosed(object sender, RoutedEventArgs e)
	{
		var closeableTabItem = (DockableTabView) sender!;
		SelectedItem = _selectedOrder.FirstOrDefault(x => x != closeableTabItem);
		Items.Remove(closeableTabItem);
		var tabModel = closeableTabItem.Content as DockableTabModel;
		tabModel?.Uninitialize();
		_dockingManager?.OnTabModelRemoved(tabModel);
	}

	private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		//
		// Items Collection Changes when tabs move, this does not necessarily mean the tab is closing.
		//

		if (e.OldItems != null)
		{
			foreach (var item in e.OldItems.OfType<DockableTabView>())
			{
				item.Closed -= DockableTabItemClosed;

				_selectedOrder.Remove(item);
			}
		}
		if (e.NewItems != null)
		{
			foreach (var item in e.NewItems.OfType<DockableTabView>())
			{
				item.TabControl = this;
				item.Closed += DockableTabItemClosed;
				item.LastSelectedOn = DateTimeProvider.RealTime.UtcNow;

				_selectedOrder.Add(item);
				_selectedOrder.RefreshOrder();
			}
		}
	}

	private void OnDragToRearrange(PointerEventArgs e)
	{
		var (draggedTab, _) = _draggedTab!.Value;

		if (Items.Contains(_draggedTab))
		{
			Debug.Fail("Dragged tab is not an Item of this TabControl");
			return;
		}

		if (!TryGetHoveredTabItem(e, out var hoveredIndex, out var hovered))
		{
			//only rearrange when hovering a tab item
			return;
		}

		_dragRearrangePreventFlicker.Evaluate(hovered, out var isHoveredValid);

		if (hovered == draggedTab)
		{
			// it can not be the same item as the one dragged
			return;
		}

		// make dragging back to the last position a lot easier
		if (_draggedTabGhost.HasValue && _draggedTabGhost.Value.bounds.Contains(e.GetPosition(this)))
		{
			Items.Remove(draggedTab);
			Items.Insert(_draggedTabGhost.Value.index, draggedTab);
			return;
		}

		if (!isHoveredValid)
		{
			// don't count the tab hovered after rearrange to prevent flickering
			return;
		}

		// see <see cref="RearrangePreventFlicker"/>

		var draggedTabIndex = Items.IndexOf(draggedTab);
		var isAfter = hoveredIndex > draggedTabIndex;

		_draggedTabGhost = (this.GetBoundsOf(draggedTab), draggedTabIndex);

		Items.RemoveAt(draggedTabIndex);

		// insert before or after hovered depending on which "direction" you are dragging
		if (isAfter)
		{
			Items.Insert(Items.IndexOf(hovered) + 1, draggedTab);
		}
		else
		{
			Items.Insert(Items.IndexOf(hovered), draggedTab);
		}

		_dragRearrangePreventFlicker.SetRearranged();
	}

	private void OnTabDraggedOut(PointerEventArgs e)
	{
		var (tabItem, offset) = _draggedTab!.Value;

		if (_draggedOutTabHandler == null)
		{
			return;
		}

		SelectedItem = _selectedOrder.FirstOrDefault(x => x != tabItem);

		// modifying Items has side effects, so we can't rely on the handler still having a value
		var handler = _draggedOutTabHandler;
		Items.Remove(tabItem);

		_draggedTab = null;
		_draggedTabGhost = null;

		handler.Invoke(this, e, tabItem, offset);
	}

	private bool TryGetHoveredTabItem(PointerEventArgs e, out int index, [NotNullWhen(true)] out TabItem hovered)
	{
		var hitPoint = e.GetPosition(this);

		for (var i = Items.Count - 1; i >= 0; i--)
		{
			var tab = (TabItem) Items[i]!;
			var tabItemBounds = this.GetBoundsOf(tab);

			if (tabItemBounds.Contains(hitPoint))
			{
				hovered = tab;
				index = i;
				return true;
			}
		}

		hovered = null;
		index = -1;
		return false;
	}

	private bool TryGetTabBarRect(out Rect rect)
	{
		var tabBarPanel = _itemsPresenterPart?.Panel;
		if (tabBarPanel is null)
		{
			rect = default;
			return false;
		}

		rect = this.GetBoundsOf(tabBarPanel);
		return true;
	}

	private void UpdatePseudoClasses()
	{
		PseudoClasses.Set(":active", IsActive);
	}

	#endregion

	#region Classes

	private class RearrangePreventFlicker
	{
		#region Fields

		private bool _hasUnaccountedRearrange;
		private object _hoveredObjectAfterRearrange;

		#endregion

		#region Methods

		public void Evaluate(object hovered, out bool isValid)
		{
			if (_hasUnaccountedRearrange)
			{
				_hoveredObjectAfterRearrange = hovered;
				_hasUnaccountedRearrange = false;
				isValid = false;
				return;
			}

			if (hovered == _hoveredObjectAfterRearrange)
			{
				isValid = false;
				return;
			}

			_hoveredObjectAfterRearrange = null;
			isValid = true;
		}

		public void Reset()
		{
			_hoveredObjectAfterRearrange = null;
			_hasUnaccountedRearrange = false;
		}

		public void SetRearranged()
		{
			_hoveredObjectAfterRearrange = null;
			_hasUnaccountedRearrange = true;
		}

		#endregion
	}

	#endregion

	#region Delegates

	public delegate void DraggedOutTabHandler(object sender, PointerEventArgs e, DockableTabView viewRef, Point offset);

	#endregion
}