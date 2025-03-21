#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

internal class DockTabWindow : CornerstoneWindow
{
	#region Fields

	private IPen _borderPen;
	private readonly Size _contentSize;
	private DragInfo _dragInfo;
	private bool _isTabItemClosed;
	private PointerEventArgs _lastPointerEvent;
	private IBrush _tabBackground;
	private readonly HookedTabControl _tabControl;

	#endregion

	#region Constructors

	public DockTabWindow(DockableTabItem tabItem, Size contentSize, IDispatcher dispatcher) : base(dispatcher)
	{
		_contentSize = contentSize;

		TabItem = tabItem;
		TabItem.Closed += TabItemClosed;
		TabItem.PointerPressed += TabItemPointerPressed;
		TabItem.PointerMoved += TabItemPointerMoved;
		TabItem.PointerReleased += TabItemPointerReleased;
		TabItem.PointerCaptureLost += TabItemPointerCaptureLost;

		_tabControl = new HookedTabControl();
		_tabControl.Items.Add(tabItem);
		_tabControl.LayoutUpdated += TabControlLayoutUpdated;
		_tabControl.Padding = new Thickness(0);

		MinHeight = 300;
		MinWidth = 300;

		SizeToContent = SizeToContent.WidthAndHeight;
		Content = _tabControl;
	}

	#endregion

	#region Properties

	public Size TabContentSize { get; private set; }

	public Size TabControlSize { get; private set; }

	public object TabHeader => TabItem.Header;

	public Size TabItemSize { get; private set; }

	internal DockableTabItem TabItem { get; }

	#endregion

	#region Methods

	public DockableTabItem DetachTabItem()
	{
		TabItem.Closed -= TabItemClosed;
		TabItem.PointerPressed -= TabItemPointerPressed;
		TabItem.PointerMoved -= TabItemPointerMoved;
		TabItem.PointerReleased -= TabItemPointerReleased;
		TabItem.PointerCaptureLost -= TabItemPointerCaptureLost;

		_tabControl.Items.Clear();

		return TabItem;
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
		DragEnd?.Invoke(this, e);
		SystemDecorations = SystemDecorations.Full;
		_lastPointerEvent = null;
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
		SystemDecorations = SystemDecorations.None;
		_dragInfo = new(e.GetPosition(this));
		_lastPointerEvent = e;
	}

	public override void Render(DrawingContext context)
	{
		var topLeft = TabItem.TranslatePoint(new Point(0, 0), this)!.Value;
		var bottomRight = TabItem.TranslatePoint(new Point(TabItem.Bounds.Width, TabItem.Bounds.Height), this)!.Value;
		var rect = Bounds.WithY(bottomRight.Y).WithHeight(Bounds.Height - bottomRight.Y);
		context.FillRectangle(_tabBackground!, new Rect(topLeft, bottomRight));
		context.FillRectangle(_tabBackground!, rect);
		context.DrawRectangle(_borderPen, rect);
		base.Render(context);
	}

	protected override void ArrangeCore(Rect finalRect)
	{
		base.ArrangeCore(finalRect);
		TabContentSize = _tabControl.ContentPresenter?.Bounds.Size ?? _tabControl.Bounds.Size;
		TabItemSize = TabItem.Bounds.Size;
		TabControlSize = _tabControl.Bounds.Size;
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		if (!_tabControl.Items.Contains(TabItem))
		{
			// The tabItem is not part of the window anymore
			return;
		}

		if (TabItem is not { } tabItem)
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

		if ((change.Property != SystemDecorationsProperty)
			|| (_tabBackground == null))
		{
			return;
		}

		Background = SystemDecorations == SystemDecorations.None ? null : _tabBackground;
	}

	private void TabControlLayoutUpdated(object sender, EventArgs e)
	{
		_tabControl.LayoutUpdated -= TabControlLayoutUpdated;

		if (_tabControl.Bounds.Size == default)
		{
			return;
		}

		var presenter = _tabControl.ContentPresenter;
		if (presenter != null)
		{
			var extraWidth = _tabControl.Bounds.Width - presenter.Bounds.Width;
			var extraHeight = _tabControl.Bounds.Height - presenter.Bounds.Height;

			extraWidth += Width - _tabControl.Bounds.Width;
			extraHeight += Height - _tabControl.Bounds.Height;

			var newWidth = Math.Max(Width, _contentSize.Width + extraWidth);
			var newHeight = Math.Max(Height, _contentSize.Height + extraHeight);

			SizeToContent = SizeToContent.Manual;

			Width = newWidth;
			Height = newHeight;
		}
	}

	private void TabItemClosed(object sender, RoutedEventArgs e)
	{
		_isTabItemClosed = true;

		Close();
	}

	private void TabItemPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
	{
		OnCaptureLost(e);
	}

	private void TabItemPointerMoved(object sender, PointerEventArgs e)
	{
		OnDragging(e);
	}

	private void TabItemPointerPressed(object sender, PointerEventArgs e)
	{
		OnDragStart(e);
	}

	private void TabItemPointerReleased(object sender, PointerEventArgs e)
	{
		OnDragEnd(e);
	}

	#endregion

	#region Events

	public event EventHandler<PointerEventArgs> DragEnd;

	public event EventHandler<PointerEventArgs> Dragging;

	#endregion

	#region Classes

	[DoNotNotify]
	private class HookedTabControl : TabControl
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

	private record DragInfo(Point Offset)
	{
		public Point Offset { get; init; } = Offset;
	}
}