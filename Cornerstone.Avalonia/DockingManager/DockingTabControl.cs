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
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[DoNotNotify]
[PseudoClasses(":active")]
public class DockingTabControl : TabControl
{
	#region Fields

	/// <summary>
	/// Define the <see cref="IsActive" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsActiveProperty;

	/// <summary>
	/// Defines the <see cref="NewTabCommand" /> property.
	/// </summary>
	public static readonly StyledProperty<ICommand> NewTabCommandProperty;

	private ContentPresenter _contentPresenterPart;
	private readonly DockingManager _dockingManager;
	private DraggedOutTabHandler _draggedOutTabHandler;
	private (DockableTabItem tabItem, Point offset)? _draggedTab;
	private (Rect bounds, int index)? _draggedTabGhost;
	private readonly RearrangePreventFlicker _dragRearrangePreventFlicker;
	private ItemsPresenter _itemsPresenterPart;
	private readonly SpeedyList<DockableTabItem> _selectedOrder;

	#endregion

	#region Constructors

	public DockingTabControl() : this(null)
	{
	}

	public DockingTabControl(DockingManager dockingManager)
	{
		_dockingManager = dockingManager;
		_dragRearrangePreventFlicker = new();
		_selectedOrder = new SpeedyList<DockableTabItem>(null, new OrderBy<DockableTabItem>(x => x.LastSelectedOn)) { DistinctCheck = (x, y) => x.TabModel.Id == y.TabModel.Id };

		WeakEventManager.Add<ItemCollection, DockingTabControl, NotifyCollectionChangedEventArgs>(Items, nameof(Items.CollectionChanged), this, ItemsCollectionChanged);

		IsHitTestVisible = true;
		Margin = new Thickness(0);
		Padding = new Thickness(0);

		UpdatePseudoClasses(IsActive);
	}

	static DockingTabControl()
	{
		IsActiveProperty = AvaloniaProperty.Register<DockingTabControl, bool>(nameof(IsActive));
		NewTabCommandProperty = AvaloniaProperty.Register<DockingTabControl, ICommand>(nameof(NewTabCommand), enableDataValidation: true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets if this is the currently active dockable.
	/// </summary>
	public bool IsActive
	{
		get => GetValue(IsActiveProperty);
		set => SetValue(IsActiveProperty, value);
	}

	/// <summary>
	/// Gets or sets if the new NewTab command.
	/// </summary>
	public ICommand NewTabCommand
	{
		get => GetValue(NewTabCommandProperty);
		set => SetValue(NewTabCommandProperty, value);
	}

	public DockingState State { get; protected set; }

	protected override Type StyleKeyOverride => typeof(DockingTabControl);

	#endregion

	#region Methods

	public void Add(DockableTabModel tabModel)
	{
		tabModel.Initialize();
		var tabItem = new DockableTabItem(tabModel);
		Items.Add(tabItem);
		SelectedItem = tabItem;
		_dockingManager?.OnTabModelAdded(tabModel);
	}

	public void CloseAllTabs()
	{
		var items = Items.ToList();
		foreach (var item in items)
		{
			if (item is DockableTabItem tabItem)
			{
				tabItem.Close(false);
			}
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

	public void UnregisterDraggedOutTabHandler()
	{
		_draggedOutTabHandler = null;
	}

	protected internal void UpdateState(DockingState state)
	{
		State = state;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_itemsPresenterPart = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
		_contentPresenterPart = e.NameScope.Find<ContentPresenter>("PART_SelectedContentHost");
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		if (_draggedTab == null)
		{
			return;
		}

		var hitPoint = e.GetPosition(this);
		var tabBarHovered = TryGetTabBarRect(out var rect) && rect.Contains(hitPoint);

		if (!tabBarHovered)
		{
			OnTabBarLeft(e);
			return;
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
			.OfType<DockableTabItem>()
			.LastOrDefault(x =>
				this.GetBoundsOf(x)
					.Contains(hitPoint)
			);

		if (tabItem == null)
		{
			return;
		}

		var topLeft = tabItem.TranslatePoint(new Point(0, 0), this)!.Value;

		_draggedTab = (tabItem, topLeft - hitPoint);
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
			UpdatePseudoClasses(change.GetNewValue<bool>());
		}
		if (change.Property == SelectedValueProperty)
		{
			if (change.NewValue is DockableTabItem tab)
			{
				tab.LastSelectedOn = DateTimeProvider.RealTime.UtcNow;
			}
		}

		base.OnPropertyChanged(change);
	}

	private void DockableTabItemClosed(object sender, RoutedEventArgs e)
	{
		var closeableTabItem = (DockableTabItem) sender!;
		SelectedItem = _selectedOrder.FirstOrDefault(x => x != closeableTabItem);
		Items.Remove(closeableTabItem);
		var tabModel = closeableTabItem.Content as DockableTabModel;
		tabModel?.UpdateState(DockingState.IsClosing);
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
			foreach (var item in e.OldItems.OfType<DockableTabItem>())
			{
				item.Closed -= DockableTabItemClosed;
				_selectedOrder.Remove(item);
			}
		}
		if (e.NewItems != null)
		{
			foreach (var item in e.NewItems.OfType<DockableTabItem>())
			{
				item.TabControl = this;
				item.Closed += DockableTabItemClosed;
				_selectedOrder.Add(item);
			}
		}
	}

	private void OnDragToRearrange(PointerEventArgs e)
	{
		var (draggedTab, _) = _draggedTab!.Value;

		#region handle invalid state

		if (Items.Contains(_draggedTab))
		{
			Debug.Fail("Dragged tab is not an Item of this TabControl");
			return;
		}

		#endregion

		if (!TryGetHoveredTabItem(e, out var hoveredIndex, out var hovered))
		{
			return; //only rearrange when hovering a tab item
		}

		_dragRearrangePreventFlicker.Evaluate(hovered, out var isHoveredValid);

		if (hovered == draggedTab)
		{
			return; // it can not be the same item as the one dragged
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
			//don't count the tab hovered after rearrange to prevent flickering
			return;
		}

		// see <see cref="RearrangePreventFlicker"/>

		var draggedTabIndex = Items.IndexOf(draggedTab);

		var isAfter = hoveredIndex > draggedTabIndex;

		_draggedTabGhost = (this.GetBoundsOf(draggedTab), draggedTabIndex);

		Items.RemoveAt(draggedTabIndex);

		//insert before or after hovered depending on which "direction" you are dragging
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

	private void OnTabBarLeft(PointerEventArgs e)
	{
		var (tabItem, offset) = _draggedTab!.Value;

		if (_draggedOutTabHandler == null)
		{
			return;
		}

		// modifying Items has side effects, so we can't rely on the handler still having a value
		var handler = _draggedOutTabHandler;
		Items.Remove(tabItem);

		_draggedTab = null;
		_draggedTabGhost = null;

		handler.Invoke(this, e, tabItem, offset, _contentPresenterPart?.Bounds.Size ?? Bounds.Size);
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

	private void UpdatePseudoClasses(bool isActive)
	{
		PseudoClasses.Set(":active", isActive);
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

	public delegate void DraggedOutTabHandler(object sender, PointerEventArgs e, DockableTabItem itemRef, Point offset, Size contentSize);

	#endregion
}