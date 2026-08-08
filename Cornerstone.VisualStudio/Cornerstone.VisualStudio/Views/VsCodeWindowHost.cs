#region References

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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

	#endregion

	#region Constructors

	public VsCodeWindowHost(IVsCodeWindow codeWindow, IOleServiceProvider oleServiceProvider)
	{
		_codeWindow = codeWindow ?? throw new ArgumentNullException(nameof(codeWindow));
		_oleServiceProvider = oleServiceProvider ?? throw new ArgumentNullException(nameof(oleServiceProvider));
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
		if (_disposed || _hosted)
		{
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
		private IntPtr _hwnd = IntPtr.Zero;

		public CodeWindowHwndHost(IVsWindowPane pane, IOleServiceProvider site)
		{
			_pane = pane;
			_site = site;
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

			_hwnd = IntPtr.Zero;
		}

		protected override void OnWindowPositionChanged(Rect rcBoundingBox)
		{
			base.OnWindowPositionChanged(rcBoundingBox);

			if (_hwnd == IntPtr.Zero)
			{
				return;
			}

			var width = Math.Max(1, (int) rcBoundingBox.Width);
			var height = Math.Max(1, (int) rcBoundingBox.Height);
			SetWindowPos(
				_hwnd,
				IntPtr.Zero,
				0,
				0,
				width,
				height,
				SWP_NOZORDER | SWP_NOACTIVATE);
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
	}

	#endregion
}
