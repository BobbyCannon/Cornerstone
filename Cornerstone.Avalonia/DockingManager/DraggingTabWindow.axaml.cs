#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

internal partial class DraggingTabWindow : CornerstoneWindow
{
	#region Fields

	private IPen _borderPen;
	private DragInfo _dragInfo;
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

		TabControl = new HookedTabControl();
		TabControl.Items.Add(tabView);
		TabControl.Padding = new Thickness(0);

		MinHeight = 300;
		MinWidth = 300;

		SizeToContent = SizeToContent.WidthAndHeight;

		InitializeComponent();
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

		TabControl.Items.Clear();

		return TabView;
	}

	public void OnCaptureLost(PointerCaptureLostEventArgs e)
	{
		if (_lastPointerEvent != null)
		{
			OnDragEnd(_lastPointerEvent);
		}
	}

	public void OnDragEnd(PointerEventArgs e)
	{
		_dragInfo = null;
		_lastPointerEvent = null;

		DragEnd?.Invoke(this, e);
		WindowDecorations = WindowDecorations.Full;
	}

	public void OnDragging(PointerEventArgs e)
	{
		if (_dragInfo == null)
		{
			return;
		}

		_lastPointerEvent = e;

		var offset = _dragInfo.Offset;

		Position = this.PointToScreen(e.GetPosition(this) - offset);
		Dragging?.Invoke(this, e);
	}

	public void OnDragStart(PointerEventArgs e)
	{
		WindowDecorations = WindowDecorations.None;
		_dragInfo = new(e.GetPosition(this));
		_lastPointerEvent = e;
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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
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

	internal class HookedTabControl : TabControl
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

	#region Records

	private record DragInfo(Point Offset)
	{
		#region Properties

		public Point Offset { get; } = Offset;

		#endregion
	}

	#endregion
}