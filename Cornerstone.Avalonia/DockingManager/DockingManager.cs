#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using ControlCollection = Avalonia.Controls.Controls;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[SourceReflection]
public partial class DockingManager : DockSplitPanel
{
	#region Fields

	private DraggingTabWindow _draggedWindow;
	private readonly HashSet<SplitPanel> _ignoreModified;
	private DockingOverlayWindow _overlayWindow;
	private readonly Dictionary<SplitPanel, NotifyCollectionChangedEventHandler> _registeredSplitPanels;
	private readonly Dictionary<DockingTabControl, NotifyCollectionChangedEventHandler> _registeredTabControls;
	private DockingManager _rootDockingManager;
	private readonly Dictionary<string, SourceTypeInfo> _tabAssemblyNamesLookup;

	#endregion

	#region Constructors

	public DockingManager() : this(CornerstoneApplication.DependencyProvider, CornerstoneApplication.RuntimeInformation, [])
	{
	}

	[DependencyInjectionConstructor]
	public DockingManager(IDependencyProvider dependencyProvider, IRuntimeInformation runtimeInformation)
		: this(dependencyProvider, runtimeInformation, [])
	{
	}

	public DockingManager(
		IDependencyProvider dependencyProvider,
		IRuntimeInformation runtimeInformation,
		params Type[] allowedDockTypes)
	{
		_ignoreModified = [];
		_registeredSplitPanels = [];
		_registeredTabControls = [];
		_tabAssemblyNamesLookup = [];

		DefaultDropDock = Dock.Bottom;
		DependencyProvider = dependencyProvider;
		RootTabControl = CreateTabControl(allowedDockTypes: allowedDockTypes);
		RuntimeInformation = runtimeInformation;
		Windows = [];

		DockIndicatorBackground = new SolidColorBrush(Colors.CornflowerBlue, 0.1);
		DockIndicatorFieldCornerRadius = 5;
		DockIndicatorFieldFill = new SolidColorBrush(Colors.CornflowerBlue, 0.5);
		DockIndicatorFieldHoveredFill = new SolidColorBrush(Colors.CornflowerBlue);
		DockIndicatorFieldSize = 40;
		DockIndicatorFieldSpacing = 10;
		DockIndicatorFieldStroke = Brushes.CornflowerBlue;
		DockIndicatorFieldStrokeThickness = 1;
		DockIndicatorStrokePen = new Pen(DockIndicatorFieldStroke, DockIndicatorFieldStrokeThickness);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Default dock side used when a tab is dropped in an invalid location
	/// (instead of creating a floating window).
	/// </summary>
	[StyledProperty]
	public partial Dock DefaultDropDock { get; set; }

	public IDependencyProvider DependencyProvider { get; set; }

	[StyledProperty]
	public partial IBrush DockIndicatorBackground { get; set; }

	[StyledProperty]
	public partial float DockIndicatorFieldCornerRadius { get; set; }

	[StyledProperty]
	public partial IBrush DockIndicatorFieldFill { get; set; }

	[StyledProperty]
	public partial IBrush DockIndicatorFieldHoveredFill { get; set; }

	[StyledProperty]
	public partial double DockIndicatorFieldSize { get; set; }

	[StyledProperty]
	public partial double DockIndicatorFieldSpacing { get; set; }

	[StyledProperty]
	public partial IBrush DockIndicatorFieldStroke { get; set; }

	[StyledProperty]
	public partial double DockIndicatorFieldStrokeThickness { get; set; }

	public IPen DockIndicatorStrokePen { get; private set; }

	public TabInfo? DraggedTabInfo =>
		_draggedWindow == null
			? null
			: new TabInfo(_draggedWindow.TabHeader, _draggedWindow.TabItemSize,
				_draggedWindow.TabContentSize, _draggedWindow.TabControlSize,
				_draggedWindow.TabView.TabModel);

	/// <summary>
	/// Gets or sets if the new NewTab command.
	/// </summary>
	[StyledProperty(EnableDataValidation = true)]
	public partial ICommand NewTabCommand { get; set; }

	public DockingManager RootDockingManager => _rootDockingManager ?? this;

	public DockingTabControl RootTabControl { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	public Bitmap WindowsIcon { get; set; }

	[StyledProperty]
	public partial string WindowTitle { get; set; }

	internal PresentationList<DockingWindow> Windows { get; }

	#endregion

	#region Methods

	public void Add(string tabAssemblyName)
	{
		if (_tabAssemblyNamesLookup.TryGetValue(tabAssemblyName, out var info))
		{
			Add(info.Type);
		}
	}

	public void Add<T>() where T : DockableTabModel
	{
		Add(typeof(T));
	}

	public void Add(Type type)
	{
		if (DependencyProvider.IsSingleton(type)
			&& TrySelectTab(x => x.GetType() == type))
		{
			return;
		}
		var instance = (DockableTabModel) DependencyProvider.GetInstance(type);
		Add(instance);
	}

	public void Add(DockableTabModel tabModel)
	{
		if (tabModel == null)
		{
			return;
		}

		// Try to add to an existing compatible tab control
		var existingTabControl = GetTabControls(Children)
			.FirstOrDefault(x => x.CanAcceptTabModel(tabModel));

		if (existingTabControl != null)
		{
			existingTabControl.Add(tabModel);
			return;
		}

		// No compatible tab control found, we need to create a new one
		var newTabControl = CreateTabControl(allowedDockTypes: GetAllowedDockTypes(tabModel));
		newTabControl.Add(tabModel);

		// There are already other tab controls, split the layout Bottom by default
		SplitNewTabControl(newTabControl, Dock.Bottom);
	}

	public static Rect CalculateDockRect(TabInfo tabInfo, Rect fitBounds, Dock dock)
	{
		var clampedWidth = Math.Min(tabInfo.TabControlSize.Width, fitBounds.Width / 2);
		var clampedHeight = Math.Min(tabInfo.TabControlSize.Height, fitBounds.Height / 2);

		return dock switch
		{
			Dock.Left => fitBounds.WithWidth(clampedWidth),
			Dock.Top => fitBounds.WithHeight(clampedHeight),
			Dock.Right => new Rect(
				fitBounds.TopLeft.WithX(fitBounds.Right - clampedWidth),
				fitBounds.BottomRight),
			Dock.Bottom => new Rect(
				fitBounds.TopLeft.WithY(fitBounds.Bottom - clampedHeight),
				fitBounds.BottomRight),
			_ => throw new InvalidEnumArgumentException(nameof(dock), (int) dock, typeof(Dock))
		};
	}

	public bool CanDockNextTo(Control target, out DockFlags dockFlags, out int insertIndex)
	{
		if (target.Parent is not SplitPanel panel)
		{
			if (target.Parent != this)
			{
				dockFlags = DockFlags.None;
				insertIndex = -1;
				return false;
			}

			insertIndex = Children.IndexOf(target);

			Debug.Assert(insertIndex >= 0);

			if (target == Children[^1])
			{
				dockFlags = DockFlags.All;
				return true;
			}
			dockFlags = GetDock(target) switch
			{
				Dock.Left => DockFlags.Left,
				Dock.Right => DockFlags.Right,
				Dock.Top => DockFlags.Top,
				Dock.Bottom => DockFlags.Bottom,
				_ => DockFlags.None
			};
			return true;
		}

		var isVertical = panel.Orientation == Orientation.Vertical;

		if (!CanDockNextTo(panel, out dockFlags, out insertIndex))
		{
			return false;
		}

		var mask = DockFlags.None;
		if (!isVertical && (target == panel.GetControlAtSlot(0)))
		{
			mask |= DockFlags.Left;
		}
		if (!isVertical && (target == panel.GetControlAtSlot(^1)))
		{
			mask |= DockFlags.Right;
		}
		if (isVertical && (target == panel.GetControlAtSlot(0)))
		{
			mask |= DockFlags.Top;
		}
		if (isVertical && (target == panel.GetControlAtSlot(^1)))
		{
			mask |= DockFlags.Bottom;
		}

		if (isVertical)
		{
			mask |= DockFlags.Left | DockFlags.Right;
		}
		else
		{
			mask |= DockFlags.Top | DockFlags.Bottom;
		}

		dockFlags &= mask;
		return dockFlags != DockFlags.None;
	}

	public bool CanFill(Control control)
	{
		return control is TabControl;
	}

	public bool CanSplit(Control control)
	{
		// we don't want to split empty TabControls as they are not supposed to be children of SplitPanel
		return control is TabControl tabControl
			&& ((tabControl.ItemCount > 0)
				|| tabControl.Items.All(x => x is DummyTabItem)
				|| (tabControl == RootTabControl));
	}

	/// <summary>
	/// Close the docking manager (all tabs (all non-primary tab controls), all child windows)
	/// </summary>
	public void Close()
	{
		CloseTabs();
		CloseWindows();
	}

	public void CloseTab(DockableTabModel tabModel)
	{
		var tabControls = GetTabControls(Children).ToList();

		foreach (var tabControl in tabControls)
		{
			foreach (var tab in tabControl.Items.OfType<DockableTabView>())
			{
				if (tab.TabModel != tabModel)
				{
					continue;
				}

				tab.Close(true);
				tabControl.Items.Remove(tab);
				return;
			}
		}
	}

	/// <summary>
	/// Returns all DockableTabView instances in the main docking tree + all floating windows.
	/// </summary>
	public IEnumerable<DockableTabView> GetAllTabViews()
	{
		foreach (var tabControl in GetTabControls(Children))
		{
			foreach (var item in tabControl.Items.OfType<DockableTabView>())
			{
				yield return item;
			}
		}

		foreach (var window in Windows)
		{
			foreach (var tabView in window.DockingManager.GetAllTabViews())
			{
				yield return tabView;
			}
		}
	}

	public (DockingTabControl tabControl, int index)? GetTabControlContaining(DockableTabModel tabModel)
	{
		var tabControls = GetTabControls(Children);
		foreach (var tabControl in tabControls)
		{
			for (var index = 0; index < tabControl.Items.Count; index++)
			{
				var tabView = (DockableTabView) tabControl.Items[index];
				if (tabView?.TabModel == tabModel)
				{
					return (tabControl, index);
				}
			}
		}
		foreach (var window in Windows)
		{
			var result = window.DockingManager.GetTabControlContaining(tabModel);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}

	public void Initialize(Type[] allowedDockTypes)
	{
		Windows.ListUpdated += WindowsOnListUpdated;
		Children.CollectionChanged += ChildrenOnCollectionChanged;
		RootTabControl.AllowedDockTypes = allowedDockTypes;
		Children.Add(RootTabControl);
	}

	public void RegisterTab<T>()
	{
		var typeInfo = SourceReflector.GetRequiredSourceType<T>();
		var id = typeInfo.DeclaredFields.First(x => x.Name == "TypeId");
		_tabAssemblyNamesLookup.TryAdd((string) id.GetValue(null), typeInfo);
	}

	public bool ReplaceTab(DockableTabModel oldModel, string tabId)
	{
		if (_tabAssemblyNamesLookup.TryGetValue(tabId, out var info))
		{
			return ReplaceTab(oldModel, (DockableTabModel) DependencyProvider.GetInstance(info.Type));
		}

		return false;
	}

	public bool ReplaceTab<T>(DockableTabModel oldModel) where T : DockableTabModel
	{
		return ReplaceTab(oldModel, DependencyProvider.GetInstance<T>());
	}

	public bool ReplaceTab(DockableTabModel oldModel, DockableTabModel newModel)
	{
		foreach (var tabControl in GetTabControls(Children))
		{
			for (var index = 0; index < tabControl.Items.Count; index++)
			{
				var item = tabControl.Items[index];
				if (item is not DockableTabView tabItem)
				{
					continue;
				}

				if (tabItem.TabModel != oldModel)
				{
					continue;
				}

				tabControl.Insert(index, newModel);
				tabItem.Close(true);
				return true;
			}
		}

		foreach (var window in Windows)
		{
			if (window.DockingManager.ReplaceTab(oldModel, newModel))
			{
				return true;
			}
		}

		return false;
	}

	public void RestoreDockLayout(DockLayoutItem dockLayout)
	{
		Close();
		RestoreDockLayoutChildren(this, dockLayout);
		RestoreDockingWindows(dockLayout);
	}

	public void SelectTab(DockableTabModel tabModel)
	{
		foreach (var tabControl in GetTabControls(Children))
		{
			foreach (var tabView in tabControl.Items.OfType<DockableTabView>())
			{
				if (tabView.TabModel != tabModel)
				{
					continue;
				}

				tabControl.SelectedItem = tabView;
				return;
			}
		}
	}

	public DockLayoutItem ToDockLayout()
	{
		return DockLayoutItem.From(this);
	}

	public bool TryFind<T>(Func<T, bool> predicate, out DockableTabModel tabModel)
		where T : DockableTabModel
	{
		foreach (var tabView in GetAllTabViews())
		{
			if (tabView.TabModel is not T tModel
				|| !predicate(tModel))
			{
				continue;
			}

			tabModel = tabView.TabModel;
			return true;
		}

		tabModel = null;
		return false;
	}

	public bool TrySelectTab(Func<DockableTabModel, bool> check)
	{
		return TrySelectTab<DockableTabModel>(check);
	}

	public bool TrySelectTab<T>(Func<T, bool> check)
		where T : DockableTabModel
	{
		foreach (var tabView in GetAllTabViews())
		{
			if (tabView.TabModel is not T tModel || !check(tModel))
			{
				continue;
			}

			// Bring the containing tab control to front / activate window if needed
			var containingWindow = FindContainingWindow(tabView);
				
			// or Focus, BringToFront, etc.
			containingWindow?.Activate();

			// assuming you expose TabControl on DockableTabView
			tabView.TabControl?.SelectedItem = tabView; 
			return true;
		}
		return false;
	}

	public void Uninitialize()
	{
		Windows.ListUpdated -= WindowsOnListUpdated;
		Children.CollectionChanged -= ChildrenOnCollectionChanged;
	}

	public void VisitDockingTreeNodes<T>(Action<T> visitor)
		where T : Control
	{
		foreach (var child in Children)
		{
			switch (child)
			{
				case SplitPanel childSplitPanel:
				{
					VisitDockingTreeNodes(childSplitPanel, visitor);
					break;
				}
				case T dockingTabControl:
				{
					visitor(dockingTabControl);
					break;
				}
			}
		}
	}

	public static void VisitDockingTreeNodes<T>(SplitPanel splitPanel, Action<T> visitor)
		where T : Control
	{
		foreach (var child in splitPanel.Children)
		{
			switch (child)
			{
				case SplitPanel childSplitPanel:
				{
					VisitDockingTreeNodes(childSplitPanel, visitor);
					break;
				}
				case T node:
				{
					visitor(node);
					break;
				}
			}
		}
	}

	/// <summary>
	/// DockableTabModel was added to one of the child TabControls.
	/// </summary>
	protected internal virtual void OnTabModelAdded(DockableTabModel e)
	{
		OnTabModelAdded(this, e);
	}

	/// <summary>
	/// DockableTabModel was removed to one of the child TabControls.
	/// </summary>
	protected internal virtual void OnTabModelRemoved(DockableTabModel e)
	{
		OnTabModelRemoved(this, e);
	}

	protected override void ArrangeCore(Rect finalRect)
	{
		base.ArrangeCore(finalRect);
		_overlayWindow?.UpdateAreas();
	}

	protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		base.ChildrenChanged(sender, e);
		HandleChildrenModified(e);
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		_draggedWindow?.OnCaptureLost(e);
		_draggedWindow = null;
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		_draggedWindow?.OnDragging(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		_draggedWindow?.OnDragEnd(e);
		_draggedWindow = null;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == DockIndicatorFieldStrokeProperty) ||
			(change.Property == DockIndicatorFieldStrokeThicknessProperty))
		{
			DockIndicatorStrokePen = new Pen(DockIndicatorFieldStroke, DockIndicatorFieldStrokeThickness);
			InvalidateVisual();
			return;
		}
		if (change.Property == DockIndicatorFieldHoveredFillProperty)
		{
			InvalidateVisual();
		}
	}

	protected virtual void OnTabDraggedOut(DockableTabView tabView)
	{
		TabDraggedOut?.Invoke(this, tabView);
	}

	protected virtual void OnTabDropped(DockableTabView e)
	{
		TabDropped?.Invoke(this, e);
	}

	/// <summary>
	/// Returns the top-level DockingWindow that contains the given tab view, or null if it's in the main docking area.
	/// </summary>
	internal DockingWindow FindContainingWindow(DockableTabView tabView)
	{
		if (tabView == null)
		{
			return null;
		}

		// Check main tree first (not in any floating window)
		if (GetTabControls(Children).Any(tc => tc.Items.Contains(tabView)))
		{
			return null; // It's in the main window, not a floating one
		}

		// Search through all floating windows
		foreach (var window in Windows)
		{
			if (window.DockingManager.ContainsTabView(tabView))
			{
				return window;
			}
		}

		return null;
	}

	private void AddTab(DockableTabView tabView)
	{
		var tabControl = GetTabControls(Children).FirstOrDefault();
		if (tabControl == null)
		{
			tabControl = CreateTabControl();
			Children.Add(tabControl);
		}

		if (tabView != null)
		{
			tabControl.Add(tabView);
		}
	}

	private static void ApplySplitDock(Control targetControl, Dock dock, Size dockSize, Control controlToInsert)
	{
		if (targetControl.Parent is not Panel parent)
		{
			throw new InvalidOperationException();
		}

		var splitOrientation = dock switch
		{
			Dock.Left or Dock.Right => Orientation.Horizontal,
			Dock.Top or Dock.Bottom => Orientation.Vertical,
			_ => throw null!
		};

		var slotSize = targetControl.Bounds.Size;

		var (insertSlotSize, otherSlotSize) = dock switch
		{
			Dock.Left or Dock.Right => ((int) dockSize.Width, (int) (slotSize.Width - dockSize.Width)),
			Dock.Top or Dock.Bottom => ((int) dockSize.Height, (int) (slotSize.Height - dockSize.Height)),
			_ => throw null!
		};

		Action<SplitPanel> insertAction;

		if (parent is SplitPanel splitPanel)
		{
			var dropSlot = splitPanel.Children.IndexOf(targetControl);
			if (splitPanel.TrySplitSlot(dropSlot, (dock, insertSlotSize, controlToInsert), otherSlotSize))
			{
				return;
			}

			insertAction = panel => parent.Children[dropSlot] = panel;
		}
		else
		{
			insertAction = s =>
			{
				var targetDock = GetDock(targetControl);
				if (targetDock != DockProperty.GetDefaultValue(typeof(Control)))
				{
					s.SetValue(DockProperty, targetDock);
				}

				parent.Children[parent.Children.IndexOf(targetControl)] = s;
			};
		}

		if (dock is Dock.Left or Dock.Top)
		{
			InsertSplitPanel(splitOrientation,
				(insertSlotSize, controlToInsert), (otherSlotSize, targetControl),
				insertAction);
		}
		else
		{
			InsertSplitPanel(splitOrientation,
				(otherSlotSize, targetControl), (insertSlotSize, controlToInsert),
				insertAction);
		}
	}

	private void ChildrenOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Replace)
		{
			return;
		}

		if (e.OldItems != null)
		{
			foreach (var item in e.OldItems)
			{
				if (item is DockingTabControl tabControl
					&& !ReferenceEquals(tabControl, RootTabControl))
				{
					tabControl.Uninitialize();
				}
			}
		}
	}

	private DockableTabView CloseDraggingWindow(DraggingTabWindow window)
	{
		var tabItem = window.DetachTabItem();
		window.Close();
		return tabItem;
	}

	private void CloseTabs()
	{
		var children = GetTabControls(Children).ToList();

		foreach (var child in children)
		{
			child.CloseAllTabs();
		}
	}

	private void CloseWindows()
	{
		foreach (var window in Windows)
		{
			window.Close();
		}
	}

	/// <summary>
	/// Helper to check if a specific DockableTabView exists anywhere in this DockingManager's tree.
	/// </summary>
	private bool ContainsTabView(DockableTabView tabView)
	{
		return GetTabControls(Children).Any(tc => tc.Items.Contains(tabView));
	}

	/// <summary>
	/// Creates a <see cref="DockingTabControl" /> that has been set up for Docking
	/// </summary>
	private DockingTabControl CreateTabControl(DockableTabView initialTabView = null, params Type[] allowedDockTypes)
	{
		var tabControl = new DockingTabControl(allowedDockTypes);
		tabControl.Initialize(this);
		if ((allowedDockTypes.Length == 0)
			|| allowedDockTypes.Contains(typeof(DocumentTabModel)))
		{
			tabControl.Bind(DockingTabControl.NewTabCommandProperty, new Binding(nameof(NewTabCommand)) { Mode = BindingMode.OneWay, Source = this });
		}
		tabControl.Bind(TemplatedControl.BackgroundProperty, new Binding(nameof(Background)) { Mode = BindingMode.OneWay, Source = this });
		if (initialTabView != null)
		{
			tabControl.Add(initialTabView);
		}
		return tabControl;
	}

	private void DockTabWindowDragEnd(object sender, PointerEventArgs e)
	{
		Debug.Assert(DraggedTabInfo.HasValue);

		var tabInfo = DraggedTabInfo.Value;
		var droppedResults = GetDroppedResult();
		RootDockingManager.HideOverlay();

		var draggingWindow = (DraggingTabWindow) sender;
		var draggingWindowPosition = draggingWindow.Position;
		var tabItem = CloseDraggingWindow(draggingWindow);

		if (droppedResults.IsInsertTab(out var tabControl, out var index)
			&& (tabControl is not DockingTabControl dockingTabControl
				|| dockingTabControl.CanAcceptTabModel(tabItem.TabModel)))
		{
			var items = tabControl.Items;
			items.Insert(index, tabItem);
		}
		else if (droppedResults.IsFillControl(out var target))
		{
			if (target is not DockingTabControl tabControl2
				|| !tabControl2.CanAcceptTabModel(tabItem.TabModel))
			{
				Debug.Fail("Invalid dropTarget for control or cannot accept model");
				return;
			}

			var items = tabControl2.Items;
			items.Add(tabItem);
		}
		else if (droppedResults.IsSplitControl(out target, out var dock))
		{
			TabControl newTabControl = CreateTabControl(tabItem, GetAllowedDockTypes(tabItem.TabModel));
			var dockSize = CalculateDockRect(tabInfo, new Rect(default, target.Bounds.Size), dock).Size;
			ApplySplitDock(target, dock, dockSize, newTabControl);
		}
		else
		{
			var window = GetDockingWindow(
				draggingWindow.Height, draggingWindow.Width,
				draggingWindowPosition.X, draggingWindowPosition.Y
			);

			// Set the root docking manager, there is only one root manager
			window.DockingManager._rootDockingManager = RootDockingManager;
			window.DockingManager.Initialize(GetAllowedDockTypes(tabItem.TabModel));
			window.DockingManager.AddTab(tabItem);
			RootDockingManager.Windows.Add(window);
			window.Show();
		}

		OnTabDropped(tabItem);
	}

	private void DockTabWindowDragging(object sender, PointerEventArgs e)
	{
		var draggedWindow = (DraggingTabWindow) sender!;
		RootDockingManager.ShowOverlay(draggedWindow, e);
	}

	private Type[] GetAllowedDockTypes(DockableTabModel tabModel)
	{
		Type[] allowedDockTypes = tabModel is ToolbarTabModel ? [typeof(ToolbarTabModel)] : [typeof(DocumentTabModel)];
		return allowedDockTypes;
	}

	private DockableTabModel GetDockableTabModel(DockLayoutItem item)
	{
		DockableTabModel model = null;
		if (item.DataModelType.TryGetType(out var modelType))
		{
			model = (DockableTabModel) DependencyProvider.GetInstance(modelType);
			model.RestoreLayoutData(item.Data);
			model.Initialize();
		}
		return model;
	}

	private DockingWindow GetDockingWindow(double height, double width, int left, int top)
	{
		var hostWindow = GetHostWindow();
		var response = new DockingWindow
		{
			DataContext = hostWindow.DataContext,
			Height = height,
			Position = new PixelPoint(left, top),
			WindowDecorations = WindowDecorations.Full,
			Title = WindowTitle ?? "Window",
			Icon = WindowsIcon,
			Width = width,
			DockingManager =
			{
				[!BackgroundProperty] = this[!BackgroundProperty],
				[!DockIndicatorFieldFillProperty] = this[!DockIndicatorFieldFillProperty],
				[!DockIndicatorFieldHoveredFillProperty] = this[!DockIndicatorFieldHoveredFillProperty],
				[!NewTabCommandProperty] = this[!NewTabCommandProperty]
			}
		};
		return response;
	}

	private DockingOverlayWindow.Result GetDroppedResult()
	{
		var response = _overlayWindow.GetDroppedResult();
		if (response.IsTarget())
		{
			return response;
		}

		foreach (var w in Windows)
		{
			var childResult = w.DockingManager._overlayWindow.GetDroppedResult();
			if (childResult.IsTarget())
			{
				return childResult;
			}
		}

		return RootDockingManager != this
			? RootDockingManager.GetDroppedResult()
			: response;
	}

	private Window GetHostWindow()
	{
		if (VisualRoot?.Parent is Window window)
		{
			return window;
		}

		throw new InvalidOperationException($"This {nameof(DockingManager)} is not part of a Window");
	}

	private static IEnumerable<DockingTabControl> GetTabControls(ControlCollection collection)
	{
		foreach (var item in collection)
		{
			switch (item)
			{
				case DockingTabControl sValue:
				{
					yield return sValue;
					break;
				}
				case DockSplitPanel sValue:
				{
					var controls = GetTabControls(sValue.Children);
					if (controls != null)
					{
						foreach (var tabControl in controls)
						{
							yield return tabControl;
						}
					}
					break;
				}
				case SplitPanel sValue:
				{
					var controls = GetTabControls(sValue.Children);
					if (controls != null)
					{
						foreach (var tabControl in controls)
						{
							yield return tabControl;
						}
					}
					break;
				}
			}
		}
	}

	private void HandleChildrenModified(NotifyCollectionChangedEventArgs e)
	{
		foreach (var child in e.OldItems?.OfType<Control>() ?? [])
		{
			switch (child)
			{
				case SplitPanel splitPanel:
				{
					UnregisterSplitPanel(splitPanel);
					break;
				}
				case DockingTabControl tabControl:
				{
					UnregisterTabControl(tabControl);
					break;
				}
			}
		}

		foreach (var child in e.NewItems?.OfType<Control>() ?? [])
		{
			switch (child)
			{
				case SplitPanel splitPanel:
				{
					RegisterSplitPanelForDocking(splitPanel);
					break;
				}
				case DockingTabControl tabControl:
				{
					RegisterTabControlForDocking(tabControl);
					break;
				}
			}
		}
	}

	private void HideOverlay()
	{
		_overlayWindow.Close();
		_overlayWindow = null;
		_draggedWindow = null;

		foreach (var w in Windows)
		{
			w.DockingManager.HideOverlay();
		}
	}

	/// <summary>
	/// Creates and inserts a <see cref="SplitPanel" /> that has been set up for Docking
	/// <para> It does so in a way that no unwanted side effects are triggered </para>
	/// </summary>
	private static void InsertSplitPanel(Orientation orientation,
		(int fraction, Control child) slot1, (int fraction, Control child) slot2,
		Action<SplitPanel> insertAction)
	{
		var panel = new SplitPanel
		{
			Orientation = orientation,
			Fractions = new SplitFractions(slot1.fraction, slot2.fraction)
		};

		insertAction(panel);
		panel.Children.AddRange([slot1.child, slot2.child]);
	}

	private void OnTabModelAdded(object sender, DockableTabModel e)
	{
		TabModelAdded?.Invoke(sender, e);
	}

	private void OnTabModelRemoved(object sender, DockableTabModel e)
	{
		TabModelRemoved?.Invoke(sender, e);
	}

	private void OverlayWindowAreaEntered(object sender, DockingOverlayWindow.AreaEnteredEventArgs e)
	{
		Debug.Assert(_draggedWindow != null);

		if (e.Control is not DockingTabControl tabControl)
		{
			return;
		}

		var items = tabControl.Items;
		var item = items.OfType<TabItem>()
			.FirstOrDefault(x => x is DummyTabItem);

		if (item != null)
		{
			items.Remove(item);
		}

		items.Add(new DummyTabItem(DockIndicatorStrokePen)
		{
			Width = _draggedWindow.TabItemSize.Width,
			Height = _draggedWindow.TabItemSize.Height,
			Opacity = 0.5
		});
	}

	private void OverlayWindowAreaExited(object sender, DockingOverlayWindow.AreaExitedEventArgs e)
	{
		if (e.Control is not DockingTabControl tabControl)
		{
			return;
		}

		var items = tabControl.Items;
		var item = items.OfType<TabItem>()
			.FirstOrDefault(x => x is DummyTabItem);

		if (item != null)
		{
			items.Remove(item);
		}
	}

	private Control ProcessChild(DockLayoutItem item)
	{
		switch (item.ControlType)
		{
			case nameof(DockSplitPanel):
			{
				var response = new DockSplitPanel();
				RestoreDockLayoutChildren(response, item);
				return response;
			}
			case nameof(DockingTabControl):
			{
				var allowedDockTypeNames = item.AllowedDockTypeNames.Select(Type.GetType).ToArray();
				var response = item.IsRootTabControl
					? RootTabControl
					: CreateTabControl(allowedDockTypes: allowedDockTypeNames);

				response.Height = item.Height;
				response.Width = item.Width;
				ProcessChildren(response, item);
				response.SelectedItem = response.Items
					.OfType<DockableTabView>()
					.FirstOrDefault(x => x.TabModel.Id == item.SelectedTab);
				return response;
			}
			case nameof(DockableTabView):
			{
				var model = GetDockableTabModel(item);
				if ((model == null) || (model.Id == Guid.Empty))
				{
					return null;
				}

				OnTabModelAdded(model);
				var response = new DockableTabView(model);
				return response;
			}
			case nameof(SplitPanel):
			{
				var response = new SplitPanel
				{
					Fractions = item.Fractions,
					Orientation = item.Orientation
				};
				ProcessChildren(response, item);
				return response;
			}
			default:
			{
				return null;
			}
		}
	}

	private void ProcessChildren(DockingTabControl tabControl, DockLayoutItem dockLayout)
	{
		if (dockLayout?.Children == null)
		{
			return;
		}

		foreach (var item in dockLayout.Children)
		{
			var child = ProcessChild(item);
			if (child != null)
			{
				tabControl.Items.Add(child);
			}
		}
	}

	private void ProcessChildren(SplitPanel splitPanel, DockLayoutItem dockLayout)
	{
		if (dockLayout?.Children == null)
		{
			return;
		}

		foreach (var item in dockLayout.Children)
		{
			var child = ProcessChild(item);
			if (child != null)
			{
				if (RootTabControl == child)
				{
					Children.Remove(child);
				}

				splitPanel.Children.Add(child);
			}
		}
	}

	private void RegisterSplitPanelForDocking(SplitPanel splitPanel)
	{
		Debug.Assert(!_registeredSplitPanels.ContainsKey(splitPanel));
		NotifyCollectionChangedEventHandler handler = (_, e) => SplitPanelChildrenModified(splitPanel, e);

		splitPanel.Children.CollectionChanged += handler;
		_registeredSplitPanels[splitPanel] = handler;

		foreach (var child in splitPanel.Children)
		{
			switch (child)
			{
				case DockingTabControl tabControl:
				{
					RegisterTabControlForDocking(tabControl);
					break;
				}
				case SplitPanel childSplitPanel:
				{
					RegisterSplitPanelForDocking(childSplitPanel);
					break;
				}
			}
		}
	}

	private void RegisterTabControlForDocking(DockingTabControl tabControl)
	{
		Debug.Assert(!_registeredTabControls.ContainsKey(tabControl));
		NotifyCollectionChangedEventHandler handler = (_, e) => TabControlCollectionChanged(tabControl, e);

		var items = tabControl.Items;
		if (items is INotifyCollectionChanged collection)
		{
			collection.CollectionChanged += handler;
		}

		_registeredTabControls[tabControl] = handler;

		tabControl.RegisterDraggedOutTabHandler(TabControlDraggedOutTab);
		tabControl.PropertyChanged += TabControlOnPropertyChanged;
		tabControl.IsActive = true;
	}

	private void RestoreDockingWindows(DockLayoutItem dockLayout)
	{
		if (dockLayout?.Windows == null)
		{
			return;
		}

		foreach (var windowLayout in dockLayout.Windows)
		{
			if (windowLayout.Children.Count <= 0)
			{
				continue;
			}

			var window = GetDockingWindow(windowLayout.Height, windowLayout.Width, windowLayout.Left, windowLayout.Top);
			window.DockingManager.RestoreDockLayout(windowLayout);
			window.DockingManager._rootDockingManager = RootDockingManager;
			window.Show();

			Windows.Add(window);
		}
	}

	private void RestoreDockLayoutChildren(DockSplitPanel splitPanel, DockLayoutItem dockLayout)
	{
		if (dockLayout?.Children == null)
		{
			return;
		}

		foreach (var item in dockLayout.Children)
		{
			var child = ProcessChild(item);
			if (child == null)
			{
				continue;
			}

			SetDock(child, item.Dock);

			if (!splitPanel.Children.Contains(child))
			{
				splitPanel.Children.Add(child);
			}
		}
	}

	private void ShowOverlay(DraggingTabWindow draggedWindow, PointerEventArgs e)
	{
		_draggedWindow = draggedWindow;

		if (_overlayWindow == null)
		{
			_overlayWindow = new DockingOverlayWindow(this)
			{
				WindowDecorations = WindowDecorations.None,
				Background = null,
				Opacity = 0.5
			};

			_overlayWindow.AreaEntered += OverlayWindowAreaEntered;
			_overlayWindow.AreaExited += OverlayWindowAreaExited;

			_overlayWindow.Show(GetHostWindow());
			_overlayWindow.Position = this.PointToScreen(new Point());
			_overlayWindow.Width = Bounds.Width;
			_overlayWindow.Height = Bounds.Height;
			_overlayWindow.UpdateAreas();
		}

		_overlayWindow.Dragging(e);

		foreach (var w in Windows)
		{
			w.DockingManager.ShowOverlay(draggedWindow, e);
		}
	}

	/// <summary>
	/// Creates a vertical split (Top/Bottom) and inserts the new tab control.
	/// Uses ApplySplitDock so that registration, cleanup, and un-splitting work correctly.
	/// </summary>
	private void SplitNewTabControl(DockingTabControl newTabControl, Dock dock = Dock.Bottom)
	{
		// Use the primary tab control as the target to split, or fall back to the first one
		var target = RootTabControl ??
			GetTabControls(Children).FirstOrDefault();

		if (target == null)
		{
			Children.Add(newTabControl);
			return;
		}

		// Calculate a reasonable size for the new panel (half the height)
		var tabInfo = new TabInfo(null, default, default, target.Bounds.Size, null);
		var targetRect = new Rect(default, target.Bounds.Size);
		var dockSize = CalculateDockRect(tabInfo, targetRect, dock).Size;

		// This is the key call - it properly creates the SplitPanel and inserts everything
		ApplySplitDock(target, dock, dockSize, newTabControl);
	}

	/// <summary>
	/// Ensures that after a modification there are still no SplitPanels with less than 2 Children
	/// unless it's the root
	/// </summary>
	private void SplitPanelChildrenModified(SplitPanel splitPanel, NotifyCollectionChangedEventArgs e)
	{
		if (_ignoreModified.Contains(splitPanel))
		{
			return;
		}

		HandleChildrenModified(e);

		if (e.Action != NotifyCollectionChangedAction.Remove)
		{
			return;
		}

		if (splitPanel.Parent is not Panel parentPanel)
		{
			return;
		}

		var indexInParent = parentPanel.Children.IndexOf(splitPanel);

		if (splitPanel.Children.Count == 1)
		{
			var child = splitPanel.Children[0];

			// Unregister BEFORE detaching anything
			if (child is DockingTabControl childTabControl)
			{
				UnregisterTabControl(childTabControl);
			}
			else if (child is SplitPanel childSplitPanel)
			{
				UnregisterSplitPanel(childSplitPanel);
			}

			_ignoreModified.Add(splitPanel);

			// Remove the child from the split panel first (so Clear() is safe)
			splitPanel.Children.RemoveAt(0);

			// Now replace the split panel with its former only child
			parentPanel.Children[indexInParent] = child;

			_ignoreModified.Remove(splitPanel);
		}
		else if (splitPanel.Children.Count == 0)
		{
			parentPanel.Children.RemoveAt(indexInParent);
		}
	}

	/// <summary>
	/// Ensures that after a modification there are still no TabControls with no Items in the
	/// Dock Tree unless it's the last child of the DockingHost aka the "Fill" control.
	/// </summary>
	private void TabControlCollectionChanged(TabControl tabControl, NotifyCollectionChangedEventArgs e)
	{
		// Ignore tabs that have items or is the parent tab control.
		if ((tabControl.Items.Count > 0)
			|| (tabControl == RootTabControl)
			|| tabControl.Parent is not Panel parent)
		{
			return;
		}

		// it's not supposed to happen but we better catch it
		if ((e.OldItems?.Cast<object>().Any(x => x is DummyTabItem) == true) &&
			(e.OldItems.Count == 1))
		{
			return;
		}

		var indexInParent = parent.Children.IndexOf(tabControl);
		if (indexInParent == -1)
		{
			Debugger.Break();
			return;
		}

		// Time to permanently remove the tab control
		if (parent is SplitPanel parentSplitPanel)
		{
			if (indexInParent < parentSplitPanel.SlotCount)
			{
				parentSplitPanel.RemoveSlot(indexInParent);
			}
		}
		else
		{
			parent.Children.RemoveAt(indexInParent);
		}
	}

	private void TabControlDraggedOutTab(object sender, PointerEventArgs e, DockableTabView tabView, Point offset)
	{
		var tabControl = (DockingTabControl) sender!;
		var hostWindow = GetHostWindow();
		var window = new DraggingTabWindow(tabView)
		{
			DataContext = hostWindow.DataContext,
			Width = tabControl.Bounds.Width,
			Height = tabControl.Bounds.Height,
			WindowDecorations = WindowDecorations.None,
			Position = hostWindow.PointToScreen(e.GetPosition(hostWindow))
		};

		window.Show();

		_draggedWindow = window;

		window.OnDragStart(e);
		window.Dragging += DockTabWindowDragging;
		window.DragEnd += DockTabWindowDragEnd;

		OnTabDraggedOut(tabView);
	}

	private void TabControlOnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		switch (e.Property.Name)
		{
			case nameof(DockingTabControl.IsActive) when e.GetNewValue<bool>():
			{
				var activeTabControl = (DockingTabControl) sender!;
				var makeInactive = _registeredTabControls
					.Where(item => (item.Key != activeTabControl) && item.Key.IsActive)
					.ToList();

				foreach (var item in makeInactive)
				{
					item.Key.IsActive = false;
				}
				break;
			}
		}
	}

	private void UnregisterSplitPanel(SplitPanel splitPanel)
	{
		_registeredSplitPanels.Remove(splitPanel, out var handler);

		splitPanel.Children.CollectionChanged -= handler;

		foreach (var child in splitPanel.Children)
		{
			if (child is DockingTabControl tabControl)
			{
				UnregisterTabControl(tabControl);
			}
			else if (child is SplitPanel childSplitPanel)
			{
				UnregisterSplitPanel(childSplitPanel);
			}
		}
	}

	private void UnregisterTabControl(DockingTabControl tabControl)
	{
		_registeredTabControls.Remove(tabControl, out var handler);

		tabControl.Items.CollectionChanged -= handler;
		tabControl.UnregisterDraggedOutTabHandler();
	}

	private void WindowOnClosed(object sender, EventArgs e)
	{
		var window = (DockingWindow) sender;
		Windows.Remove(window);
	}

	private void WindowsOnListUpdated(object sender, PresentationListUpdatedEventArg<DockingWindow> e)
	{
		if (e.Removed != null)
		{
			foreach (var w in e.Removed)
			{
				w.DockingManager.TabModelAdded -= OnTabModelAdded;
				w.DockingManager.TabModelRemoved -= OnTabModelRemoved;
				w.Closed -= WindowOnClosed;
			}
		}
		if (e.Added != null)
		{
			foreach (var w in e.Added)
			{
				w.DockingManager.TabModelAdded += OnTabModelAdded;
				w.DockingManager.TabModelRemoved += OnTabModelRemoved;
				w.Closed += WindowOnClosed;
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler<DockableTabView> TabDraggedOut;
	public event EventHandler<DockableTabView> TabDropped;
	public event EventHandler<DockableTabModel> TabModelAdded;
	public event EventHandler<DockableTabModel> TabModelRemoved;

	#endregion

	#region Classes

	private class DummyTabItem(IPen pen) : TabItem
	{
		#region Methods

		public override void Render(DrawingContext ctx)
		{
			ctx.DrawRectangle(pen, Bounds.WithX(0).WithY(0), 4);
		}

		#endregion
	}

	#endregion

	#region Records

	public record struct TabInfo(object Header, Size TabItemSize, Size ContentSize, Size TabControlSize, DockableTabModel TabModel);

	#endregion
}