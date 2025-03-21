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
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Serialization;
using Cornerstone.Text;
using Cornerstone.Weaver;
using ControlCollection = Avalonia.Controls.Controls;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class DockingManager : DockSplitPanel, IDispatchable
{
	#region Fields

	public static readonly StyledProperty<float> DockIndicatorFieldCornerRadiusProperty;
	public static readonly StyledProperty<IBrush> DockIndicatorFieldFillProperty;
	public static readonly StyledProperty<IBrush> DockIndicatorFieldHoveredFillProperty;
	public static readonly StyledProperty<double> DockIndicatorFieldSizeProperty;
	public static readonly StyledProperty<double> DockIndicatorFieldSpacingProperty;
	public static readonly StyledProperty<IBrush> DockIndicatorFieldStrokeProperty;
	public static readonly StyledProperty<double> DockIndicatorFieldStrokeThicknessProperty;
	private readonly IDispatcher _dispatcher;

	private DockTabWindow _draggedWindow;
	private readonly HashSet<SplitPanel> _ignoreModified;
	private DockingOverlayWindow _overlayWindow;
	private readonly Dictionary<SplitPanel, NotifyCollectionChangedEventHandler> _registeredSplitPanels;
	private readonly Dictionary<DockingTabControl, NotifyCollectionChangedEventHandler> _registeredTabControls;

	#endregion

	#region Constructors

	public DockingManager() : this(null)
	{
	}

	public DockingManager(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		_ignoreModified = [];
		_registeredSplitPanels = [];
		_registeredTabControls = [];

		DependencyProvider = new ActivatorAsDependencyProvider();
		TabWindows = new List<DockTabWindow>();

		DockIndicatorStrokePen = new Pen(
			DockIndicatorFieldStrokeProperty.GetDefaultValue(typeof(DockingTabControl)),
			DockIndicatorFieldStrokeThicknessProperty.GetDefaultValue(typeof(DockingTabControl))
		);
	}

	static DockingManager()
	{
		DockIndicatorFieldCornerRadiusProperty = AvaloniaProperty.Register<DockingTabControl, float>(nameof(DockIndicatorFieldCornerRadius), 5);
		DockIndicatorFieldFillProperty = AvaloniaProperty.Register<DockingTabControl, IBrush>(nameof(DockIndicatorFieldFill), new SolidColorBrush(Colors.CornflowerBlue, 0.5));
		DockIndicatorFieldHoveredFillProperty = AvaloniaProperty.Register<DockingTabControl, IBrush>(nameof(DockIndicatorFieldHoveredFill), new SolidColorBrush(Colors.CornflowerBlue));
		DockIndicatorFieldSizeProperty = AvaloniaProperty.Register<DockingTabControl, double>(nameof(DockIndicatorFieldSize), 40);
		DockIndicatorFieldSpacingProperty = AvaloniaProperty.Register<DockingTabControl, double>(nameof(DockIndicatorFieldSpacing), 10);
		DockIndicatorFieldStrokeProperty = AvaloniaProperty.Register<DockingTabControl, IBrush>(nameof(DockIndicatorFieldStroke), Brushes.CornflowerBlue);
		DockIndicatorFieldStrokeThicknessProperty = AvaloniaProperty.Register<DockingTabControl, double>(nameof(DockIndicatorFieldStrokeThickness), 1);
	}

	#endregion

	#region Properties

	public IDependencyProvider DependencyProvider { get; set; }

	public float DockIndicatorFieldCornerRadius
	{
		get => GetValue(DockIndicatorFieldCornerRadiusProperty);
		set => SetValue(DockIndicatorFieldCornerRadiusProperty, value);
	}

	public IBrush DockIndicatorFieldFill
	{
		get => GetValue(DockIndicatorFieldFillProperty);
		set => SetValue(DockIndicatorFieldFillProperty, value);
	}

	public IBrush DockIndicatorFieldHoveredFill
	{
		get => GetValue(DockIndicatorFieldHoveredFillProperty);
		set => SetValue(DockIndicatorFieldHoveredFillProperty, value);
	}

	public double DockIndicatorFieldSize
	{
		get => GetValue(DockIndicatorFieldSizeProperty);
		set => SetValue(DockIndicatorFieldSizeProperty, value);
	}

	public double DockIndicatorFieldSpacing
	{
		get => GetValue(DockIndicatorFieldSpacingProperty);
		set => SetValue(DockIndicatorFieldSpacingProperty, value);
	}

	public IBrush DockIndicatorFieldStroke
	{
		get => GetValue(DockIndicatorFieldStrokeProperty);
		set => SetValue(DockIndicatorFieldStrokeProperty, value);
	}

	public double DockIndicatorFieldStrokeThickness
	{
		get => GetValue(DockIndicatorFieldStrokeThicknessProperty);
		set => SetValue(DockIndicatorFieldStrokeThicknessProperty, value);
	}

	public IPen DockIndicatorStrokePen { get; private set; }

	public TabInfo? DraggedTabInfo =>
		_draggedWindow == null
			? null
			: new TabInfo(_draggedWindow.TabHeader, _draggedWindow.TabItemSize,
				_draggedWindow.TabContentSize, _draggedWindow.TabControlSize);

	public ICommand NewTabCommand { get; set; }

	internal List<DockTabWindow> TabWindows { get; }

	#endregion

	#region Methods

	public void AddTab(DockableTabModel tabModel)
	{
		var tabControl = GetTabControls(Children).FirstOrDefault();
		if (tabControl == null)
		{
			tabControl = CreateTabControl();
			Children.Add(tabControl);
		}

		tabControl.Add(tabModel);
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
		return control is not TabControl tabControl
			// we don't want to split empty TabControls as they are not supposed to be children of SplitPanel
			|| tabControl.Items.Any(x => x is not DummyTabItem);
	}

	/// <summary>
	/// Close the docking manager (all children, all child windows)
	/// </summary>
	public void Close()
	{
		CloseTabs();
		Children.Clear();

		TabWindows.ForEach(x => x.Close());
		TabWindows.Clear();
	}

	public void CloseTab(DockableTabModel tabModel)
	{
		var tabControls = GetTabControls(Children).ToList();

		foreach (var tabControl in tabControls)
		{
			foreach (var tab in tabControl.Items.OfType<DockableTabItem>())
			{
				if (tab.TabModel != tabModel)
				{
					continue;
				}

				tab.Close(true);
				return;
			}
		}
	}

	public void FromDockLayout(DockLayoutItem dockLayout)
	{
		Close();
		ProcessChildren(this, dockLayout);
	}

	public void FromDockLayoutJson(string json)
	{
		var layout = json.FromJson<DockLayoutItem>();
		FromDockLayout(layout);
	}

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public void ReplaceTab(DockableTabModel oldModel, DockableTabModel newModel)
	{
		var tabControls = GetTabControls(Children).ToList();

		foreach (var tabControl in tabControls)
		{
			foreach (var tabItem in tabControl.Items.OfType<DockableTabItem>())
			{
				if (tabItem.TabModel != oldModel)
				{
					continue;
				}

				tabControl.Add(newModel);
				tabItem.Close(true);
				return;
			}
		}
	}

	public DockLayoutItem ToDockLayout()
	{
		return DockLayoutItem.From(this);
	}

	public string ToDockLayoutJson()
	{
		var dockLayout = ToDockLayout();
		return dockLayout.ToJson(new SerializationSettings
		{
			TextFormat = TextFormat.Indented,
			IgnoreNullValues = true,
			IgnoreDefaultValues = true
		});
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
		TabModelAdded?.Invoke(this, e);
	}

	/// <summary>
	/// DockableTabModel was removed to one of the child TabControls.
	/// </summary>
	protected internal virtual void OnTabModelRemoved(DockableTabModel e)
	{
		TabModelRemoved?.Invoke(this, e);
	}

	protected override void ArrangeCore(Rect finalRect)
	{
		base.ArrangeCore(finalRect);
		_overlayWindow?.UpdateAreas();
	}

	protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		base.ChildrenChanged(sender, e);
		HandleChildrenModified(this, e);
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

	private DockableTabItem CloseAndDetachWindow(DockTabWindow window)
	{
		var tabItem = window.DetachTabItem();
		window.Close();
		TabWindows.Remove(window);
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

	/// <summary>
	/// Creates a <see cref="DockingTabControl" /> that has been set up for Docking
	/// </summary>
	private DockingTabControl CreateTabControl(DockableTabItem initialTabItem = null)
	{
		var tabControl = new DockingTabControl(this);
		tabControl.UpdateState(DockingState.Created);
		tabControl.NewTabCommand = new RelayCommand(TabControlNewTabRequested, tabControl);
		if (initialTabItem != null)
		{
			tabControl.Items.Add(initialTabItem);
		}
		return tabControl;
	}

	private static DockFlags DockAsFlags(Dock dock)
	{
		return dock switch
		{
			Dock.Left => DockFlags.Left,
			Dock.Right => DockFlags.Right,
			Dock.Top => DockFlags.Top,
			Dock.Bottom => DockFlags.Bottom,
			_ => throw null!
		};
	}

	private void DockTabWindowDragEnd(object sender, PointerEventArgs e)
	{
		Debug.Assert(DraggedTabInfo.HasValue);
		Debug.Assert(_overlayWindow != null);

		var tabInfo = DraggedTabInfo.Value;
		var overlayResult = _overlayWindow.GetResult();

		_overlayWindow.Close();
		_overlayWindow = null;
		_draggedWindow = null;

		if (overlayResult.IsInsertTab(out var tabControl, out var index))
		{
			var tabItem = CloseAndDetachWindow((DockTabWindow) sender);
			var items = tabControl.Items;
			items.Insert(index, tabItem);
		}
		else if (overlayResult.IsFillControl(out var target))
		{
			if (target is not DockingTabControl tabControl2)
			{
				Debug.Fail("Invalid dropTarget for control");
				return;
			}

			var tabItem = CloseAndDetachWindow((DockTabWindow) sender);
			var items = tabControl2.Items;
			items.Add(tabItem);
		}
		else if (overlayResult.IsSplitControl(out target, out var dock))
		{
			var tabItem = CloseAndDetachWindow((DockTabWindow) sender);
			TabControl newTabControl = CreateTabControl(tabItem);
			var dockSize = CalculateDockRect(tabInfo, new Rect(default, target.Bounds.Size), dock).Size;
			ApplySplitDock(target, dock, dockSize, newTabControl);
		}
		else if (overlayResult.IsInsertNextTo(out target, out dock))
		{
			var tabItem = CloseAndDetachWindow((DockTabWindow) sender);
			if (!CanDockNextTo(target, out var dockFlags, out var insertIndex)
				|| !dockFlags.HasFlag(DockAsFlags(dock)))
			{
				throw new Exception("Layout has changed since tab has been dragged");
			}

			TabControl newTabControl = CreateTabControl(tabItem);
			newTabControl.SetValue(DockProperty, dock);

			if (dock is Dock.Left or Dock.Right)
			{
				newTabControl.Width = tabInfo.ContentSize.Width;
			}
			else
			{
				newTabControl.Height = tabInfo.ContentSize.Height;
			}

			Children.Insert(insertIndex, newTabControl);
		}
		else if (overlayResult.IsInsertOuter(out dock))
		{
			var tabItem = CloseAndDetachWindow((DockTabWindow) sender);
			TabControl newTabControl = CreateTabControl(tabItem);
			newTabControl.SetValue(DockProperty, dock);

			if (dock is Dock.Left or Dock.Right)
			{
				newTabControl.Width = tabInfo.ContentSize.Width;
			}
			else
			{
				newTabControl.Height = tabInfo.ContentSize.Height;
			}
			Children.Insert(0, newTabControl);
		}
	}

	private void DockTabWindowDragging(object sender, PointerEventArgs e)
	{
		_draggedWindow = (DockTabWindow) sender!;

		if (_overlayWindow == null)
		{
			_overlayWindow = new DockingOverlayWindow(this, GetDispatcher())
			{
				SystemDecorations = SystemDecorations.None,
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
	}

	private Window GetHostWindow()
	{
		if (VisualRoot is Window window)
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

	private void HandleChildrenModified(Control control, NotifyCollectionChangedEventArgs e)
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
				ProcessChildren(response, item);
				return response;
			}
			case nameof(DockingTabControl):
			{
				var response = CreateTabControl();
				response.Height = item.Height;
				response.Width = item.Width;
				ProcessChildren(response, item);
				response.SelectedItem = response.Items
					.OfType<DockableTabItem>()
					.FirstOrDefault(x => x.TabModel.Id == item.SelectedTab);
				return response;
			}
			case nameof(DockableTabItem):
			{
				DockableTabModel model = null;

				if (item.DataModelType.TryGetType(out var modelType))
				{
					model = (DockableTabModel) DependencyProvider.GetInstance(modelType);
					model.RestoreLayoutData(item.Data);
				}

				if ((model == null) || (model.Id == Guid.Empty))
				{
					return null;
				}

				model.Initialize();
				OnTabModelAdded(model);
				var response = new DockableTabItem(model);
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

	private void ProcessChildren(DockSplitPanel splitPanel, DockLayoutItem dockLayout)
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
			splitPanel.Children.Add(child);
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
				splitPanel.Children.Add(child);
			}
		}
	}

	private void RegisterSplitPanelForDocking(SplitPanel splitPanel)
	{
		Debug.Assert(!_registeredSplitPanels.ContainsKey(splitPanel));
		NotifyCollectionChangedEventHandler handler = (s, e) => SplitPanelChildrenModified(splitPanel, e);

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

		HandleChildrenModified(splitPanel, e);

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

			// panel is still part of the UI Tree and as such can trigger a cascade of unwanted changes,
			// and we can't remove it without triggering ChildrenModified
			// or replacing it with a Dummy Control so we have to:
			_ignoreModified.Add(splitPanel);

			if (child is DockingTabControl childTabControl)
			{
				UnregisterTabControl(childTabControl);
			}
			else if (child is SplitPanel childSplitPanel)
			{
				UnregisterSplitPanel(childSplitPanel);
			}

			splitPanel.Children.Clear();
			_ignoreModified.Remove(splitPanel);

			parentPanel.Children[indexInParent] = child;
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
		if (e.OldItems != null)
		{
			foreach (var item in e.OldItems)
			{
				if (item is DockableTabItem { Content: DockableTabModel tabModel })
				{
					Debug.WriteLine(tabModel.GetType().ToAssemblyName());
				}
			}
		}

		if (e.NewItems != null)
		{
			foreach (var item in e.NewItems)
			{
				if (item is DockableTabItem { Content: DockableTabModel tabModel })
				{
					Debug.WriteLine(tabModel.GetType().ToAssemblyName());
				}
			}
		}

		var items = tabControl.Items;
		if (items.Count > 0)
		{
			return;
		}

		if (tabControl.Parent is not Panel parent)
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

		if (parent is SplitPanel parentSplitPanel)
		{
			if (indexInParent < parentSplitPanel.SlotCount)
			{
				parentSplitPanel.RemoveSlot(indexInParent);
			}
		}
		else if ((parent == this) && (tabControl == Children[^1]))
		{
		}
		else
		{
			parent.Children.RemoveAt(indexInParent);
		}
	}

	private void TabControlDraggedOutTab(object sender, PointerEventArgs e,
		DockableTabItem tabItem, Point offset, Size contentSize)
	{
		var tabControl = (DockingTabControl) sender!;
		var hostWindow = GetHostWindow();
		var window = new DockTabWindow(tabItem, contentSize, GetDispatcher())
		{
			DataContext = hostWindow.DataContext,
			Width = tabControl.Bounds.Width,
			Height = tabControl.Bounds.Height,
			SystemDecorations = SystemDecorations.None,
			Position = hostWindow.PointToScreen(e.GetPosition(hostWindow) + offset)
		};

		window.Show(hostWindow);

		TabWindows.Add(window);

		ChildWindowMoveHandler.Hookup(hostWindow, window);

		_draggedWindow = window;

		window.OnDragStart(e);
		window.Dragging += DockTabWindowDragging;
		window.DragEnd += DockTabWindowDragEnd;
	}

	private void TabControlNewTabRequested(object obj)
	{
		if (NewTabCommand.CanExecute(obj))
		{
			NewTabCommand.Execute(obj);
		}
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

	#endregion

	#region Events

	public event EventHandler<DockableTabModel> TabModelAdded;
	public event EventHandler<DockableTabModel> TabModelRemoved;

	#endregion

	#region Classes

	private class ChildWindowMoveHandler
	{
		#region Fields

		private readonly Window _child;
		private (PixelPoint topLeft, Size size) _lastParentWindowBounds;
		private readonly Window _parent;

		#endregion

		#region Constructors

		private ChildWindowMoveHandler(Window parent, Window child)
		{
			_child = child;
			_parent = parent;
			_lastParentWindowBounds = (parent.Position, parent.FrameSize.GetValueOrDefault());
		}

		#endregion

		#region Methods

		public static void Hookup(Window parent, Window child)
		{
			var handler = new ChildWindowMoveHandler(parent, child);
			parent.PositionChanged += handler.ParentPositionChanged;
			child.Closed += handler.ChildClosed;
		}

		private void ChildClosed(object sender, EventArgs e)
		{
			_parent.PositionChanged -= ParentPositionChanged;
			_child.Closed -= ChildClosed;
		}

		private void ParentPositionChanged(object sender, PixelPointEventArgs e)
		{
			var position = _parent.Position;
			var size = _parent.FrameSize.GetValueOrDefault();

			if (_lastParentWindowBounds.size != size)
			{
				_lastParentWindowBounds = (position, size);
				return;
			}

			var delta = position - _lastParentWindowBounds.topLeft;

			_child.Position += delta;
			_lastParentWindowBounds = (position, size);
		}

		#endregion
	}

	[DoNotNotify]
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

	public record struct TabInfo(object Header, Size TabItemSize, Size ContentSize, Size TabControlSize)
	{
		public object Header { get; set; } = Header;

		public Size TabItemSize { get; set; } = TabItemSize;

		public Size ContentSize { get; set; } = ContentSize;

		public Size TabControlSize { get; set; } = TabControlSize;
	}
}