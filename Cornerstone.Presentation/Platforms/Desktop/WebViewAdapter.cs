#region References

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Runtime;
using Key = Cornerstone.Presentation.Input.Key;
using Cornerstone.Presentation.Controls.Web;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Presentation.Platforms.Desktop;

/// <summary>
/// macOS WKWebView hosted as an NSView through libCornerstoneNative.
/// </summary>
internal sealed class WebViewAdapter : CornerstoneObject, IWebViewAdapter, IDisposable
{
	#region Fields

	private bool _disposed;
	private readonly IntPtr _view;
	private readonly WebViewPlatformHandle _platformHandle;
	private Uri _uri;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WebViewAdapter()
	{
		_view = CsnWkWebViewCreate();
		if (_view == IntPtr.Zero)
			throw new InvalidOperationException("CsnWkWebViewCreate returned null.");
		_platformHandle = new WebViewPlatformHandle(_view);
		_uri = new Uri("about:blank");
	}

	#endregion

	#region Properties

	public bool CanGoBack => _view != IntPtr.Zero && CsnWkWebViewCanGoBack(_view) != 0;

	public bool CanGoForward => _view != IntPtr.Zero && CsnWkWebViewCanGoForward(_view) != 0;

	public string Content => GetContent();

	public byte[] Favicon => null;

	public bool IsNativeSurfaceVisible { get; private set; } = true;

	public IPlatformHandle PlatformHandle => _platformHandle;

	public string Title => CopyUtf8(CsnWkWebViewCopyTitle(_view));

	public Uri Uri
	{
		get => _uri;
		set => Navigate(value);
	}

	#endregion

	#region Methods

	public Task ClearBrowsingDataAsync()
	{
		return Task.CompletedTask;
	}

	public void DeleteAllCookies()
	{
	}

	public void DeleteCookie(string name, string uri)
	{
	}

	public void DeleteProfile(string profileName)
	{
	}

	public void Dispose()
	{
		if (_disposed)
			return;
		_disposed = true;
		if (_view != IntPtr.Zero)
			CsnWkWebViewRelease(_view);
	}

	public IEnumerable<string> GetAvailableProfiles()
	{
		return [];
	}

	public string GetContent()
	{
		return string.Empty;
	}

	public Task<IEnumerable<WebViewCookie>> GetCookiesAsync()
	{
		return Task.FromResult((IEnumerable<WebViewCookie>) []);
	}

	public bool GoBack()
	{
		return CsnWkWebViewGoBack(_view) != 0;
	}

	public bool GoForward()
	{
		return CsnWkWebViewGoForward(_view) != 0;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.F5)
		{
			Reload();
			return true;
		}

		if (key == Key.BrowserBack && CanGoBack)
		{
			GoBack();
			return true;
		}

		if (key == Key.BrowserForward && CanGoForward)
		{
			GoForward();
			return true;
		}

		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
		// ShowInBounds owns size/position.
	}

	public void Initialize(string profileName)
	{
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		return Task.FromResult(string.Empty);
	}

	public void Navigate(Uri uri)
	{
		if (uri == null)
			return;
		_uri = uri;
		CsnWkWebViewLoadUrl(_view, uri.ToString());
		NotifyComputedPropertyChanged(nameof(Uri));
		NavigationStarted?.Invoke(this, new WebViewNavigationEventArgs { Uri = _uri });
	}

	public string NavigateToString(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		_uri = new Uri("about:blank");
		CsnWkWebViewLoadHtml(_view, text);
		NotifyComputedPropertyChanged(nameof(Uri));
		return text;
	}

	public void Reload()
	{
		CsnWkWebViewReload(_view);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		CsnWkWebViewSetHidden(_view, visible ? 0 : 1);
	}

	public void Stop()
	{
		CsnWkWebViewStop(_view);
	}

	void IWebViewAdapter.Initialize(string profileName)
	{
	}

	private static string CopyUtf8(IntPtr pointer)
	{
		if (pointer == IntPtr.Zero)
			return string.Empty;
		try
		{
			return Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
		}
		finally
		{
			CsnFree(pointer);
		}
	}

	#endregion

	#region Events

	#pragma warning disable CS0067
	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;
	#pragma warning restore CS0067

	#endregion

	#region Native

	[DllImport("libCornerstoneNative")]
	private static extern IntPtr CsnWkWebViewCreate();

	[DllImport("libCornerstoneNative")]
	private static extern void CsnWkWebViewRelease(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern void CsnWkWebViewLoadUrl(IntPtr view, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

	[DllImport("libCornerstoneNative")]
	private static extern void CsnWkWebViewLoadHtml(IntPtr view, [MarshalAs(UnmanagedType.LPUTF8Str)] string html);

	[DllImport("libCornerstoneNative")]
	private static extern void CsnWkWebViewReload(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern void CsnWkWebViewStop(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern int CsnWkWebViewGoBack(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern int CsnWkWebViewGoForward(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern int CsnWkWebViewCanGoBack(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern int CsnWkWebViewCanGoForward(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern void CsnWkWebViewSetHidden(IntPtr view, int hidden);

	[DllImport("libCornerstoneNative")]
	private static extern IntPtr CsnWkWebViewCopyTitle(IntPtr view);

	[DllImport("libCornerstoneNative")]
	private static extern void CsnFree(IntPtr pointer);

	#endregion

	#region Classes

	private sealed class WebViewPlatformHandle : IPlatformHandle
	{
		public WebViewPlatformHandle(IntPtr handle)
		{
			Handle = handle;
		}

		public IntPtr Handle { get; }

		public string HandleDescriptor => "NSView";
	}

	#endregion
}
