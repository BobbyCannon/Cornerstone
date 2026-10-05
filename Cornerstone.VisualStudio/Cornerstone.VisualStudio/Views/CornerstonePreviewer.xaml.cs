#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cornerstone.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.Views;

public partial class CornerstonePreviewer : UserControl, IDisposable
{
	#region Fields

	private bool _centerPreviewer;

	private ScrollBar _horizontalScroll;
	private readonly WeakReference<BitmapSource> _lastBitmap = new(null);
	private Size _lastBitmapSize;
	private double _lastFullScaling = double.NaN;
	private double _lastMarginH = double.NaN;
	private double _lastMarginV = double.NaN;
	private Size? _lastSize;
	private Point _lastPointerPosition = new(double.NaN, double.NaN);
	private PreviewerProcess _process;
	private ScrollBar _verticalScroll;

	#endregion

	#region Constructors

	public CornerstonePreviewer()
	{
		InitializeComponent();
		Update(null);

		Loaded += CornerstonePreviewer_Loaded;

		BuildButton.Click += BuildButton_Click;
		PreviewScroller.ScrollChanged += PreviewScroller_ScrollChanged;

		SizeChanged += (_, _) => _lastSize = null;
	}

	#endregion

	#region Properties

	public PreviewerProcess Process
	{
		get => _process;
		set
		{
			if (_process != null)
			{
				_process.ErrorChanged -= Update;
				_process.FrameReceived -= Update;
			}

			_process = value;

			if (_process != null)
			{
				_process.ErrorChanged += Update;
				_process.FrameReceived += Update;
			}

			BindRemoteView();
			Update(_process?.Bitmap);
		}
	}

	public Project SelectedProject { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		Process = null;
		RemotePreview.Session = null;
		Update(null);
	}

	public Size GetViewportSize(int padding)
	{
		if (_lastSize is null)
		{
			var height = PreviewScroller.ActualHeight;
			var width = PreviewScroller.ActualWidth;
			if (PreviewScroller.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
			{
				if (_horizontalScroll is null)
				{
					_horizontalScroll = PreviewScroller.FindDescendants<ScrollBar>()
						.First(b => b.Orientation == Orientation.Horizontal);
				}
				height -= _horizontalScroll.Height;
			}
			if (PreviewScroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
			{
				if (_verticalScroll == null)
				{
					_verticalScroll = PreviewScroller.FindDescendants<ScrollBar>()
						.First(b => b.Orientation == Orientation.Vertical);
				}
				width -= _verticalScroll.Width;
			}
			_lastSize = new(width - (padding * 2), height - (padding * 2));
		}
		return _lastSize.Value;
	}

	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
	{
		// Force layout recalculation on DPI change.
		_lastBitmapSize = default;
		_lastFullScaling = double.NaN;
		Update(_process?.Bitmap);
	}

	protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
		{
			PreviewScroller.ScrollToHorizontalOffset(
				PreviewScroller.HorizontalOffset - (((2 * e.Delta) / 120) * 48));

			e.Handled = true;
		}
		else if (Keyboard.Modifiers == ModifierKeys.Control)
		{
			var designer = FindParent<CornerstoneDesigner>(this);

			if (designer.TryProcessZoomLevelValue(out var currentZoomLevel))
			{
				currentZoomLevel += e.Delta > 0 ? 0.25 : -0.25;

				if (currentZoomLevel < 0.125)
				{
					currentZoomLevel = 0.125;
				}
				else if (currentZoomLevel > 8)
				{
					currentZoomLevel = 8;
				}

				designer.ZoomLevel = ZoomLevels.FmtZoomLevel(currentZoomLevel * 100);

				e.Handled = true;
			}
		}

		base.OnPreviewMouseWheel(e);
	}

	private void CornerstonePreviewer_Loaded(object sender, RoutedEventArgs e)
	{
		// Debugging will cause Loaded/Unloaded events to fire, we only want to do this
		// the first time the designer is loaded, so unsub
		Loaded -= CornerstonePreviewer_Loaded;
		_centerPreviewer = true;
	}

	private async void BuildButton_Click(object sender, RoutedEventArgs e)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (SelectedProject == null)
		{
			return;
		}

		var dte = (DTE) Package.GetGlobalService(typeof(DTE));
		var solutionBuild = dte?.Solution?.SolutionBuild;
		if ((solutionBuild?.ActiveConfiguration == null) ||
			(solutionBuild.BuildState == vsBuildState.vsBuildStateInProgress))
		{
			return;
		}

		try
		{
			solutionBuild.BuildProject(solutionBuild.ActiveConfiguration.Name, SelectedProject.UniqueName);
		}
		catch (System.Runtime.InteropServices.COMException ex)
		{
			// A build can start between the BuildState check and BuildProject (designer reload, second click).
			Log.Logger.Debug(ex, "Previewer build skipped because a build is already active.");
		}
	}

	private static T FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		//get parent item
		var parentObject = VisualTreeHelper.GetParent(child);

		//we've reached the end of the tree
		if (parentObject == null)
		{
			return null;
		}

		//check if the parent matches the type we're looking for
		var parent = parentObject as T;
		if (parent != null)
		{
			return parent;
		}
		return FindParent<T>(parentObject);
	}

	private void BindRemoteView()
	{
		var session = _process?.RemoteSession;
		if (session != null)
		{
			session.AttachDispatcher(Dispatcher);
			RemotePreview.Session = session;
			RemotePreview.Visibility = Visibility.Visible;
			Preview.Visibility = Visibility.Collapsed;
		}
		else
		{
			RemotePreview.Session = null;
			RemotePreview.Visibility = Visibility.Collapsed;
			Preview.Visibility = Visibility.Visible;
		}
	}

	private bool UseRemoteView => _process?.RemoteSession != null;

	private FrameworkElement PreviewSurface => UseRemoteView ? (FrameworkElement) RemotePreview : Preview;

	private double GetScaling()
	{
		var result = (Process?.Scaling ?? 1) / VisualTreeHelper.GetDpi(this).DpiScaleX;
		return result > 0 ? result : 1;
	}

	private void Preview_MouseDown(object sender, MouseButtonEventArgs e)
	{
		var p = e.GetPosition(Preview);
		var scaling = GetScaling();
		_lastPointerPosition = p;

		Process?.SendPointerPressedAsync(p.X / scaling, p.Y / scaling, e).FireAndForget();
	}

	private void Preview_MouseMove(object sender, MouseEventArgs e)
	{
		var p = e.GetPosition(Preview);

		// Skip no-op moves so we do not continuously dirty the host process.
		if (!double.IsNaN(_lastPointerPosition.X) &&
			(Math.Abs(p.X - _lastPointerPosition.X) < 0.5) &&
			(Math.Abs(p.Y - _lastPointerPosition.Y) < 0.5))
		{
			return;
		}

		_lastPointerPosition = p;
		var scaling = GetScaling();

		Process?.SendPointerMovedAsync(p.X / scaling, p.Y / scaling, e).FireAndForget();
	}

	private void Preview_MouseUp(object sender, MouseButtonEventArgs e)
	{
		var p = e.GetPosition(Preview);
		var scaling = GetScaling();
		_lastPointerPosition = p;

		Process?.SendPointerReleasedAsync(p.X / scaling, p.Y / scaling, e).FireAndForget();
	}

	private void PreviewScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		// We can't do this in Update because the Scroll info may not be updated 
		// yet and the scrollable size may still be old
		if (_centerPreviewer)
		{
			if (_lastBitmapSize is { } size && (size.Width < e.ViewportWidth) && (size.Height < e.ViewportHeight))
			{
				PreviewScroller.ScrollToVerticalOffset(PreviewScroller.ScrollableHeight / 2);
			}
			else
			{
				var transform = PreviewSurface.TransformToVisual(PreviewScroller);
				var positionInScrollViewer = transform.TransformBounds(new Rect(0, 0, PreviewSurface.ActualHeight, PreviewSurface.ActualHeight));
				var offset = positionInScrollViewer.Top + e.VerticalOffset;
				PreviewScroller.ScrollToVerticalOffset(offset);
			}
			PreviewScroller.ScrollToHorizontalOffset(PreviewScroller.ScrollableWidth / 2);
			_centerPreviewer = false;
		}
	}

	private void Update(object sender, EventArgs e)
	{
		// FrameReceived is raised on the UI thread after a size change. ErrorChanged
		// can arrive on a thread-pool thread. Stay at background priority either way:
		// a main-thread switch posts above the keyboard.
		var dispatcher = Dispatcher;
		if ((dispatcher != null) && !dispatcher.CheckAccess())
		{
			// VSTHRD001: SwitchToMainThreadAsync posts above input and freezes the shell.
			#pragma warning disable VSTHRD001
			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyProcessBitmap));
			#pragma warning restore VSTHRD001
			return;
		}

		ApplyProcessBitmap();
	}

	private void ApplyProcessBitmap()
	{
		try
		{
			Update(_process?.DisplayBitmap);
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "Error updating previewer");
		}
	}

	private void Update(BitmapSource bitmap)
	{
		if (Process is null)
		{
			return;
		}

		// While markup is invalid, keep showing the last good frame under the error overlay.
		if (Process.IsMarkupPaused)
		{
			if (!_lastBitmap.TryGetTarget(out var frozen) || frozen is null)
			{
				frozen = bitmap;
			}

			if (!UseRemoteView && frozen is not null && !ReferenceEquals(Preview.Source, frozen))
			{
				Preview.Source = frozen;
			}

			// Error overlay is owned by CornerstoneDesigner.ShowError; keep the scroller visible
			// so the frozen frame remains behind the semi-transparent banner.
			if (PreviewScroller.Visibility != Visibility.Visible)
			{
				PreviewScroller.Visibility = Visibility.Visible;
			}

			return;
		}

		if (bitmap is null)
		{
			_lastBitmap.TryGetTarget(out bitmap);
		}

		var surface = PreviewSurface;

		// Pixel data is written into the same WriteableBitmap instance across frames.
		// Reassign only when the instance changes; WritePixels already invalidates it.
		if (!UseRemoteView && !ReferenceEquals(Preview.Source, bitmap))
		{
			Preview.Source = bitmap;
		}

		if (bitmap is not null)
		{
			var scaling = VisualTreeHelper.GetDpi(this).DpiScaleX;
			if (scaling <= 0)
			{
				scaling = 1;
			}

			var width = bitmap.Width / scaling;
			var height = bitmap.Height / scaling;
			var sizeChanged =
				(Math.Abs(surface.Width - width) > 0.5) ||
				(Math.Abs(surface.Height - height) > 0.5);

			if (sizeChanged)
			{
				surface.Width = width;
				surface.Height = height;
			}

			if (Error.Visibility != Visibility.Collapsed)
			{
				Error.Visibility = Visibility.Collapsed;
			}
			if (PreviewScroller.Visibility != Visibility.Visible)
			{
				PreviewScroller.Visibility = Visibility.Visible;
			}

			var processScaling = Process.Scaling > 0 ? Process.Scaling : 1;
			var fullScaling = scaling * processScaling;
			var hScale = (surface.Width * 2) / fullScaling;
			var vScale = (surface.Height * 2) / fullScaling;

			// Only update margin when size or scaling actually changes — avoids layout
			// thrashing on every animated frame.
			if (sizeChanged ||
				(Math.Abs(fullScaling - _lastFullScaling) > 0.0001) ||
				(Math.Abs(hScale - _lastMarginH) > 0.5) ||
				(Math.Abs(vScale - _lastMarginV) > 0.5))
			{
				PreviewGrid.Margin = new Thickness(hScale, vScale, hScale, vScale);
				_lastFullScaling = fullScaling;
				_lastMarginH = hScale;
				_lastMarginV = vScale;
			}

			// The bitmap size only changes if
			// 1- The design size changes
			// 2- The scaling changes from zoom factor
			// 3- The DPI changes
			// To ensure we don't have the ScrollViewer end up in a weird place,
			// recenter the content if the size changes
			if ((Math.Abs(surface.Width - _lastBitmapSize.Width) > 0.5) ||
				(Math.Abs(surface.Height - _lastBitmapSize.Height) > 0.5))
			{
				_centerPreviewer = true;
				_lastBitmapSize = new Size(surface.Width, surface.Height);
			}

			_lastBitmap.SetTarget(bitmap);
		}
	}

	#endregion
}
