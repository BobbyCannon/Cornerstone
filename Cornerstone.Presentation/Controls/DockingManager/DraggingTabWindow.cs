#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Presenters;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Naming;

#endregion

namespace Cornerstone.Presentation.Controls.DockingManager;

internal class DraggingTabWindow : Window
{
	#region Fields

	private IPen _borderPen;
	private bool _dragEnded;
	private PixelPoint _dragPointerOffset;
	private bool _isDragging;
	private bool _isTabItemClosed;
	private PointerEventArgs _lastPointerEvent;
	private IBrush _tabBackground;

	#endregion

	#region Constructors

	public DraggingTabWindow(DockableTabView tabView)
	{
		TabView = tabView;
		TabView.Closed += TabViewClosed;
		TabView.PointerPressed += TabViewPointerPressed;
		TabView.PointerMoved += TabViewPointerMoved;
		TabView.PointerReleased += TabViewPointerReleased;
		TabView.PointerCaptureLost += TabViewPointerCaptureLost;

		TabControl = new DraggingTabWindow.HookedTabControl();
		TabControl.Items.Add(tabView);
		TabControl.Padding = new Thickness(0);

		MinHeight = 300;
		MinWidth = 300;

		SizeToContent = SizeToContent.WidthAndHeight;

		// Same as the overlay: the theme title bar would replace the tab header
		// and read "Window" for the whole drag. A transparent frame lets the
		// tabs that stayed in the dock show around this preview.
		ExtendClientAreaToDecorationsHint = false;
		// Stay above the owner without becoming the foreground window. Taking
		// activation here makes Windows pick another app when the preview closes.
		ShowActivated = false;
		Title = string.Empty;
		WindowDecorations = WindowDecorations.None;
		TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
		TransparencyBackgroundFallback = null;
		Content = TabControl;
	}

	#endregion

	#region Properties

	public Size TabContentSize { get; private set; }

	public HookedTabControl TabControl { get; }

	public Size TabControlSize { get; private set; }

	public object TabHeader => TabView.Header;

	public Size TabItemSize { get; private set; }

	internal DockableTabView TabView { get; }

	#endregion

	#region Methods

	public DockableTabView DetachTabItem()
	{
		TabView.Closed -= TabViewClosed;
		TabView.PointerPressed -= TabViewPointerPressed;
		TabView.PointerMoved -= TabViewPointerMoved;
		TabView.PointerReleased -= TabViewPointerReleased;
		TabView.PointerCaptureLost -= TabViewPointerCaptureLost;

		if (TabControl.Items.Contains(TabView))
		{
			TabControl.Items.Clear();
		}

		return TabView;
	}

	public void OnCaptureLost(PointerCaptureLostEventArgs e)
	{
		// Tear-off reparents the tab and recaptures onto this window. That fires
		// CaptureLost on the source dock while the button is still down. Ending
		// here immediately materializes a real window and aborts the drag.
		if (_isDragging && !_dragEnded)
		{
			e?.Pointer?.Capture(TabView);
			return;
		}

		OnDragEnd(_lastPointerEvent);
	}

	public void OnDragEnd(PointerEventArgs e)
	{
		if (_dragEnded)
		{
			return;
		}

		_dragEnded = true;
		_isDragging = false;
		_lastPointerEvent = null;
		e?.Pointer?.Capture(null);

		DragEnd?.Invoke(this, e);
		WindowDecorations = WindowDecorations.Full;
	}

	public void OnDragging(PointerEventArgs e)
	{
		if (!_isDragging)
		{
			return;
		}

		_lastPointerEvent = e;

		var screen = VisualExtensions.GetPointerScreenPosition(e);
		Position = new PixelPoint(screen.X - _dragPointerOffset.X, screen.Y - _dragPointerOffset.Y);
		Dragging?.Invoke(this, e);
	}

	public void OnDragStart(PointerEventArgs e)
	{
		WindowDecorations = WindowDecorations.None;
		_dragEnded = false;
		_isDragging = true;
		_lastPointerEvent = e;

		var screen = VisualExtensions.GetPointerScreenPosition(e);
		_dragPointerOffset = new PixelPoint(screen.X - Position.X, screen.Y - Position.Y);
		e?.Pointer?.Capture(TabView);
	}

	public override void Render(DrawingContext context)
	{
		var topLeft = TabView.TranslatePoint(new Point(0, 0), this)!.Value;
		var bottomRight = TabView.TranslatePoint(new Point(TabView.Bounds.Width, TabView.Bounds.Height), this)!.Value;
		var rect = Bounds.WithY(bottomRight.Y).WithHeight(Bounds.Height - bottomRight.Y);
		context.FillRectangle(_tabBackground!, new Rect(topLeft, bottomRight));
		context.FillRectangle(_tabBackground!, rect);
		context.DrawRectangle(_borderPen, rect);
		base.Render(context);
	}

	protected override void ArrangeCore(Rect finalRect)
	{
		base.ArrangeCore(finalRect);
		TabContentSize = TabControl.ContentPresenter?.Bounds.Size ?? TabControl.Bounds.Size;
		TabItemSize = TabView.Bounds.Size;
		TabControlSize = TabControl.Bounds.Size;
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		if (!TabControl.Items.Contains(TabView))
		{
			// The tabItem is not part of the window anymore
			return;
		}

		if (TabView is not { } tabItem)
		{
			e.Cancel = true;
			return;
		}

		if (!_isTabItemClosed)
		{
			e.Cancel = true;
			tabItem.Close(true);
		}
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		_tabBackground = Background;
		_borderPen = new Pen(BorderBrush ?? Brushes.DimGray);

		Background = null;

		InvalidateVisual();
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property != WindowDecorationsProperty)
			|| (_tabBackground == null))
		{
			return;
		}

		Background = WindowDecorations == WindowDecorations.None ? null : _tabBackground;
	}

	private void TabViewClosed(object sender, RoutedEventArgs e)
	{
		_isTabItemClosed = true;

		Close();
	}

	private void TabViewPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
	{
		OnCaptureLost(e);
	}

	private void TabViewPointerMoved(object sender, PointerEventArgs e)
	{
		OnDragging(e);
	}

	private void TabViewPointerPressed(object sender, PointerEventArgs e)
	{
		OnDragStart(e);
	}

	private void TabViewPointerReleased(object sender, PointerEventArgs e)
	{
		OnDragEnd(e);
	}

	#endregion

	#region Events

	public event EventHandler<PointerEventArgs> DragEnd;
	public event EventHandler<PointerEventArgs> Dragging;

	#endregion

	#region Classes

	/// <summary>
	/// DockingTabControl so tab chrome bindings ($parent[DockingTabControl]) resolve
	/// while the tab is hosted in the tear-off preview.
	/// </summary>
	internal class HookedTabControl : DockingTabControl
	{
		#region Properties

		public ContentPresenter ContentPresenter { get; private set; }

		protected override Type StyleKeyOverride => typeof(TabControl);

		#endregion

		#region Methods

		protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
		{
			base.OnApplyTemplate(e);
			ContentPresenter = e.NameScope.Find<ContentPresenter>("PART_SelectedContentHost");
		}

		#endregion
	}

	#endregion

}