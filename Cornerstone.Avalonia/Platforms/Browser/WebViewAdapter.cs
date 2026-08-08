#region References

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

/// <summary>
/// Browser WebView as a <c>position:fixed</c> DOM overlay on <c>document.body</c>.
/// Avoids Avalonia <c>NativeControlHost</c> and never parents under <c>#out</c> (that caused WASM layout lockups).
/// </summary>
internal class WebViewAdapter : CornerstoneObject, IWebViewAdapter, IDisposable
{
	#region Fields

	private JSObject _content;
	private JSObject _iframe;
	private bool _overlayAttached;
	private JSObject _root;
	private Uri _uri;
	private readonly IPlatformHandle _platformHandle = new PlatformHandle(IntPtr.Zero, "DOM-OVERLAY");

	#endregion

	#region Constructors

	/// <summary>
	/// Parameterless ctor for DI. Marked so <see cref="DependencyProvider"/> can activate this type
	/// (same pattern as Android/Windows WebView adapters).
	/// </summary>
	[DependencyInjectionConstructor]
	public WebViewAdapter()
	{
		// Lazy DOM creation — constructor must not call JS (keeps DI/resolve off the critical path).
		IsNativeSurfaceVisible = true;
	}

	#endregion

	#region Properties

	public bool CanGoBack => false;

	public bool CanGoForward => false;

	public string Content
	{
		get => string.Empty;
		set => NavigateToString(value);
	}

	public byte[] Favicon { get; internal set; }

	public bool IsNativeSurfaceVisible { get; private set; }

	public IPlatformHandle PlatformHandle => _platformHandle;

	public string Title { get; internal set; }

	public Uri Uri
	{
		get => _uri;
		set
		{
			_uri = value;
			if (value != null)
			{
				Navigate(value);
			}
		}
	}

	#endregion

	#region Methods

	public void AttachOverlay()
	{
		EnsureDom();

		if (_overlayAttached)
		{
			return;
		}

		try
		{
			BrowserInterop.AttachOverlay(_root);
			_overlayAttached = true;
			ApplyRootVisibility();
		}
		catch (Exception ex)
		{
			Console.WriteLine("WebViewAdapter.AttachOverlay failed: " + ex);
		}
	}

	public void AttachTo(IntPtr handleHandle)
	{
	}

	public Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		return Task.FromResult(NativeSurfaceSnapshot.Failed("Browser WebView snapshot is not supported."));
	}

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

	public void DetachOverlay()
	{
		if (!_overlayAttached || (_root == null))
		{
			_overlayAttached = false;
			return;
		}

		try
		{
			BrowserInterop.DetachOverlay(_root);
		}
		catch (Exception ex)
		{
			Console.WriteLine("WebViewAdapter.DetachOverlay failed: " + ex);
		}

		_overlayAttached = false;
	}

	public void Dispose()
	{
		DetachOverlay();
		_iframe = null;
		_content = null;
		_root = null;
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
		return false;
	}

	public bool GoForward()
	{
		return false;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
	}

	public void Initialize(string profileName)
	{
	}

	public Task<string> InvokeScriptAsync(string scriptName)
	{
		return Task.FromResult(string.Empty);
	}

	public void Navigate(Uri uri)
	{
		if (uri == null)
		{
			return;
		}

		_uri = uri;
		EnsureDom();

		try
		{
			BrowserInterop.HideElement(_content);
			_content.SetProperty("innerHTML", string.Empty);
			_iframe.SetProperty("src", uri.AbsoluteUri);
			BrowserInterop.ShowElement(_iframe);
			ApplyRootVisibility();
		}
		catch (Exception ex)
		{
			Console.WriteLine("WebViewAdapter.Navigate failed: " + ex);
		}
	}

	public string NavigateToString(string text)
	{
		_uri = null;
		EnsureDom();
		text ??= string.Empty;

		try
		{
			BrowserInterop.HideElement(_iframe);
			_iframe.SetProperty("src", "about:blank");
			_content.SetProperty("innerHTML", text);
			BrowserInterop.ShowElement(_content);
			ApplyRootVisibility();
		}
		catch (Exception ex)
		{
			Console.WriteLine("WebViewAdapter.NavigateToString failed: " + ex);
		}

		return text;
	}

	public void Reload()
	{
		if (_uri == null)
		{
			return;
		}

		var current = _uri;
		try
		{
			EnsureDom();
			_iframe.SetProperty("src", "about:blank");
			_iframe.SetProperty("src", current.AbsoluteUri);
		}
		catch (Exception ex)
		{
			Console.WriteLine("WebViewAdapter.Reload failed: " + ex);
		}
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		ApplyRootVisibility();
	}

	public void SetOverlayBounds(double x, double y, double width, double height)
	{
		if (!_overlayAttached || (_root == null))
		{
			return;
		}

		try
		{
			BrowserInterop.SetOverlayBounds(_root, x, y, width, height);
		}
		catch (Exception ex)
		{
			Console.WriteLine("WebViewAdapter.SetOverlayBounds failed: " + ex);
		}
	}

	public void Stop()
	{
		if (_iframe == null)
		{
			return;
		}

		try
		{
			_iframe.SetProperty("src", "about:blank");
		}
		catch
		{
			// ignore
		}
	}

	protected internal virtual void OnNavigationCompleted(WebViewNavigationEventArgs e)
	{
		NavigationCompleted?.Invoke(this, e);
	}

	protected internal virtual void OnNavigationStarted(WebViewNavigationEventArgs e)
	{
		NavigationStarted?.Invoke(this, e);
	}

	protected virtual void OnNewWindowRequested(WebViewNewWindowEventArgs e)
	{
		NewWindowRequested?.Invoke(this, e);
	}

	private void ApplyRootVisibility()
	{
		if (!_overlayAttached || (_root == null))
		{
			return;
		}

		try
		{
			if (IsNativeSurfaceVisible)
			{
				BrowserInterop.ShowElement(_root);
			}
			else
			{
				BrowserInterop.HideElement(_root);
			}
		}
		catch
		{
			// ignore
		}
	}

	private void EnsureDom()
	{
		if (_root != null)
		{
			return;
		}

		// Prefer module helpers; fall back to global createElement if needed.
		_root = BrowserInterop.CreateElement("div");
		_iframe = BrowserInterop.CreateElement(_root, "iframe");
		_content = BrowserInterop.CreateElement(_root, "div");

		// Safer defaults for embedding third-party pages.
		_iframe.SetProperty("loading", "lazy");
		_iframe.SetProperty("referrerPolicy", "no-referrer");

		BrowserInterop.HideElement(_iframe);
		BrowserInterop.HideElement(_content);
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}
