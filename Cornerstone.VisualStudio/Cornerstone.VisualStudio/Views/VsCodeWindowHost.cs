#region References

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IObjectWithSite = Microsoft.VisualStudio.OLE.Interop.IObjectWithSite;

#endregion

namespace Cornerstone.VisualStudio.Views;

/// <summary>
/// Hosts a full <see cref="IVsCodeWindow"/> so native editor chrome works —
/// including the code split grip (Window → Split / scrollbar split) and secondary view.
/// </summary>
/// <remarks>
/// Reparenting only <see cref="Microsoft.VisualStudio.Text.Editor.IWpfTextViewHost.HostControl"/>
/// strips the code-window layout that owns the splitter. This control keeps the whole pane.
/// </remarks>
internal sealed class VsCodeWindowHost : ContentControl, IDisposable
{
	#region Fields

	private readonly IVsCodeWindow _codeWindow;
	private readonly IOleServiceProvider _oleServiceProvider;
	private HwndHost _hwndHost;
	private bool _disposed;
	private bool _hosted;
	private int _hostQueued;

	#endregion

	#region Constructors

	public VsCodeWindowHost(IVsCodeWindow codeWindow, IOleServiceProvider oleServiceProvider)
	{
		_codeWindow = codeWindow ?? throw new ArgumentNullException(nameof(codeWindow));
		_oleServiceProvider = oleServiceProvider ?? throw new ArgumentNullException(nameof(oleServiceProvider));
		_hostQueued = 0;
		Focusable = true;
		Loaded += OnLoaded;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True after the code window UI was successfully attached.
	/// </summary>
	public bool IsHosted => _hosted;

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		Loaded -= OnLoaded;
		SizeChanged -= OnHostSized;
		HostFailed = null;

		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (_codeWindow is IVsWindowPane pane)
			{
				pane.ClosePane();
			}
		}
		catch
		{
			// Best-effort shutdown; the editor factory may already have closed the window.
		}

		if (_hwndHost is IDisposable disposable)
		{
			disposable.Dispose();
		}

		_hwndHost = null;
		Content = null;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Loaded -= OnLoaded;
		ScheduleHost();
	}

	/// <summary>
	/// Attaches the code window after Loaded has returned, and only once this
	/// control has a real size. Creating the text view inside Loaded arranges it
	/// at zero size. Visual Studio then calls ScrollIntoViewWhenPossiblyUnloaded
	/// and CaretAdornmentLayer.QueueScroll on that stack, and those calls do not
	/// return until another window message arrives.
	/// </summary>
	private void ScheduleHost()
	{
		if (_disposed || _hosted || (_hostQueued != 0))
		{
			return;
		}

		_hostQueued = 1;
		try
		{
			#pragma warning disable VSTHRD001
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(HostAfterLayout));
			#pragma warning restore VSTHRD001
		}
		catch (InvalidOperationException)
		{
			_hostQueued = 0;
		}
	}

	private void HostAfterLayout()
	{
		_hostQueued = 0;
		if (_disposed || _hosted)
		{
			return;
		}

		if ((ActualWidth < 1) || (ActualHeight < 1))
		{
			SizeChanged -= OnHostSized;
			SizeChanged += OnHostSized;
			return;
		}

		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Prefer pure WPF hosting when the adapter exposes IVsUIElementPane (VS 2017+).
			if (TryHostAsUiElement())
			{
				_hosted = true;
				return;
			}

			// Fallback: classic HWND pane under an HwndHost.
			if (TryHostAsHwndPane())
			{
				_hosted = true;
				return;
			}

			HostFailed?.Invoke(this, new Exception(
				"IVsCodeWindow does not implement IVsUIElementPane or IVsWindowPane."));
		}
		catch (Exception ex)
		{
			HostFailed?.Invoke(this, ex);
		}
	}

	private void OnHostSized(object sender, SizeChangedEventArgs e)
	{
		if (_disposed || _hosted || (ActualWidth < 1) || (ActualHeight < 1))
		{
			return;
		}

		SizeChanged -= OnHostSized;
		ScheduleHost();
	}

	private bool TryHostAsUiElement()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (_codeWindow is not IVsUIElementPane uiPane)
		{
			return false;
		}

		// Site before creating the element so the pane can resolve shell services.
		var siteHr = uiPane.SetUIElementSite(_oleServiceProvider);
		if (ErrorHandler.Failed(siteHr) && uiPane is IObjectWithSite objectWithSite)
		{
			objectWithSite.SetSite(_oleServiceProvider);
		}

		var hr = uiPane.CreateUIElementPane(out var uiElement);
		if (ErrorHandler.Failed(hr) || uiElement == null)
		{
			return false;
		}

		if (uiElement is FrameworkElement fe)
		{
			Content = fe;
			return true;
		}

		// Some builds wrap WPF in IVsUIWpfElement.
		if (uiElement is IVsUIWpfElement wpfElement)
		{
			hr = wpfElement.GetFrameworkElement(out var frameworkElement);
			if (ErrorHandler.Succeeded(hr) && frameworkElement is FrameworkElement element)
			{
				Content = element;
				return true;
			}
		}

		return false;
	}

	private bool TryHostAsHwndPane()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (_codeWindow is not IVsWindowPane pane)
		{
			return false;
		}

		_hwndHost = new CodeWindowHwndHost(pane, _oleServiceProvider);
		Content = _hwndHost;
		return true;
	}

	#endregion

	#region Events

	/// <summary>
	/// Raised when the code window cannot be hosted; caller should fall back to text-view-only.
	/// </summary>
	public event EventHandler<Exception> HostFailed;

	#endregion

	#region Nested types

	/// <summary>
	/// HWND host for <see cref="IVsWindowPane.CreatePaneWindow"/>.
	/// </summary>
	private sealed class CodeWindowHwndHost : HwndHost
	{
		private readonly IVsWindowPane _pane;
		private readonly IOleServiceProvider _site;
		private IntPtr _hwnd;
		private Rect _pendingBounds;
		private bool _positionQueued;
		private bool _applied;
		private int _appliedX;
		private int _appliedY;
		private int _appliedWidth;
		private int _appliedHeight;
		private int _generation;
		private bool _resizeRunning;
		private readonly object _resizeGate;

		public CodeWindowHwndHost(IVsWindowPane pane, IOleServiceProvider site)
		{
			_pane = pane;
			_site = site;
			_hwnd = IntPtr.Zero;
			_pendingBounds = Rect.Empty;
			_positionQueued = false;
			_applied = false;
			_appliedX = 0;
			_appliedY = 0;
			_appliedWidth = 0;
			_appliedHeight = 0;
			_generation = 0;
			_resizeRunning = false;
			_resizeGate = new object();
		}

		protected override HandleRef BuildWindowCore(HandleRef hwndParent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_pane.SetSite(_site);

			// Initial size is replaced by OnWindowPositionChanged as soon as layout runs.
			var hr = _pane.CreatePaneWindow(
				hwndParent.Handle,
				0,
				0,
				Math.Max(1, (int) ActualWidth),
				Math.Max(1, (int) ActualHeight),
				out _hwnd);

			ErrorHandler.ThrowOnFailure(hr);
			return new HandleRef(this, _hwnd);
		}

		protected override void DestroyWindowCore(HandleRef hwnd)
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				_pane.ClosePane();
			}
			catch
			{
				// Ignore double-close.
			}

			lock (_resizeGate)
			{
				_hwnd = IntPtr.Zero;
				_generation++;
			}

			_positionQueued = false;
			_applied = false;
		}

		/// <summary>
		/// Records the layout rect. The editor is resized off this thread.
		/// </summary>
		/// <remarks>
		/// Do not call the base method, and do not call SetWindowPos on the UI
		/// thread. The editor window belongs to this thread, so SWP_ASYNCWINDOWPOS
		/// does not post here: the size handler runs nested in the caller, and the
		/// shell stays stuck until a later move. HwndHost calls this on every
		/// layout pass. A thread-pool SetWindowPos with that flag posts the size
		/// onto the UI thread's normal message pump.
		/// </remarks>
		protected override void OnWindowPositionChanged(Rect rcBoundingBox)
		{
			_pendingBounds = rcBoundingBox;
			if (_positionQueued || (_hwnd == IntPtr.Zero))
			{
				return;
			}

			_positionQueued = true;
			try
			{
				#pragma warning disable VSTHRD001
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyPendingBounds));
				#pragma warning restore VSTHRD001
			}
			catch (InvalidOperationException)
			{
				_positionQueued = false;
			}
		}

		private void ApplyPendingBounds()
		{
			_positionQueued = false;
			if (_hwnd == IntPtr.Zero)
			{
				return;
			}

			var bounds = _pendingBounds;
			var x = (int) bounds.X;
			var y = (int) bounds.Y;
			var width = (int) bounds.Width;
			var height = (int) bounds.Height;
			if (width < 0)
			{
				width = 0;
			}

			if (height < 0)
			{
				height = 0;
			}

			if (_applied &&
				(x == _appliedX) &&
				(y == _appliedY) &&
				(width == _appliedWidth) &&
				(height == _appliedHeight))
			{
				return;
			}

			var start = false;
			lock (_resizeGate)
			{
				_applied = true;
				_appliedX = x;
				_appliedY = y;
				_appliedWidth = width;
				_appliedHeight = height;
				_generation++;
				if (!_resizeRunning)
				{
					_resizeRunning = true;
					start = true;
				}
			}

			if (start)
			{
				ThreadPool.UnsafeQueueUserWorkItem(ResizeOffThread, null);
			}
		}

		private void ResizeOffThread(object state)
		{
			while (true)
			{
				IntPtr hwnd;
				int x;
				int y;
				int width;
				int height;
				int generation;
				lock (_resizeGate)
				{
					hwnd = _hwnd;
					x = _appliedX;
					y = _appliedY;
					width = _appliedWidth;
					height = _appliedHeight;
					generation = _generation;
				}

				if (hwnd != IntPtr.Zero)
				{
					SetWindowPos(
						hwnd,
						IntPtr.Zero,
						x,
						y,
						width,
						height,
						SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOCOPYBITS | SWP_ASYNCWINDOWPOS);
				}

				lock (_resizeGate)
				{
					if ((hwnd == IntPtr.Zero) || (generation == _generation))
					{
						_resizeRunning = false;
						return;
					}
				}
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int x,
			int y,
			int cx,
			int cy,
			uint uFlags);

		private const uint SWP_NOZORDER = 0x0004;
		private const uint SWP_NOACTIVATE = 0x0010;
		private const uint SWP_NOCOPYBITS = 0x0100;
		private const uint SWP_ASYNCWINDOWPOS = 0x4000;
	}

	#endregion
}
