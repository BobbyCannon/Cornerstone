#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Platforms.Android.Clients;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Key = Cornerstone.Presentation.Input.Key;
using Object = Java.Lang.Object;
using AndroidWebView = Android.Webkit.WebView;
using AndroidApplication = Android.App.Application;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Web;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Presentation.Platforms.Android;

[SourceReflection]
internal class WebViewAdapter : CornerstoneObject, IWebViewAdapter, IDisposable
{
	#region Fields

	private readonly CornerstoneWebChromeClient _webChromeClient;
	private bool _disposed;
	private readonly AndroidWebView _webView;
	private readonly CornerstoneWebViewClient _webViewClient;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WebViewAdapter()
	{
		// Application.Context is typically a light theme (isLightTheme=true) even when
		// the system/activity is in night mode. Chromium maps prefers-color-scheme from
		// that attribute, so GitHub and similar sites stay in light CSS unless we use
		// the activity (local night mode) and a DayNight wrapper.
		var parentContext = (global::Android.Content.Context) AndroidHost.Activity
			?? AndroidApplication.Context;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
		{
			parentContext = new ContextThemeWrapper(
				parentContext,
				global::Android.Resource.Style.ThemeDeviceDefaultDayNight);
		}

		var webView = new AndroidWebView(parentContext);
		_webViewClient = new CornerstoneWebViewClient(this);
		_webChromeClient = new CornerstoneWebChromeClient(this);
		webView.SetWebViewClient(_webViewClient);
		webView.SetWebChromeClient(_webChromeClient);

		var settings = webView.Settings;
		settings.JavaScriptEnabled = true;

		PlatformHandle = new PlatformHandle(webView.Handle, "HWND");

		_webView = webView;
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webView.CanGoBack();

	public bool CanGoForward => _webView.CanGoForward();

	public string Content
	{
		get => GetContent();
		set => NavigateToString(value);
	}

	public byte[] Favicon { get; internal set; }

	public bool IsNativeSurfaceVisible { get; private set; } = true;

	public IPlatformHandle PlatformHandle { get; }

	public string Title { get; internal set; }

	public Uri Uri
	{
		get => Uri.TryCreate(_webView.Url, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
		set => Navigate(value);
	}

	#endregion

	#region Methods

	public void AttachTo(IntPtr handleHandle)
	{
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

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		var webView = _webView;
		if (webView == null)
		{
			return;
		}

		// Handle == Zero means the JNI peer was already disposed, not that Chromium
		// was Destroy()'d.
		try
		{
			try
			{
				// Android WebView.Destroy() throws IllegalStateException if the view is
				// still in a ViewGroup. Tile Dispose and Grid OnDetachedFromVisualTree
				// both run before Cornerstone's native attachment unparents us.
				if (webView.Parent is ViewGroup parent)
				{
					parent.RemoveView(webView);
				}

				webView.SetWebViewClient(null);
				webView.SetWebChromeClient(null);
			}
			finally
			{
				webView.StopLoading();
				webView.Destroy();
			}
		}
		catch (Exception)
		{
		}
	}

	public IEnumerable<string> GetAvailableProfiles()
	{
		return [];
	}

	public string GetContent()
	{
		return InvokeScriptAsync("document.documentElement.outerHTML;")
			.ConfigureAwait(true).GetAwaiter().GetResult();
	}

	public Task<IEnumerable<WebViewCookie>> GetCookiesAsync()
	{
		return Task.FromResult((IEnumerable<WebViewCookie>) []);
	}

	public bool GoBack()
	{
		return _webView.CanGoBack();
	}

	public bool GoForward()
	{
		return _webView.CanGoForward();
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
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
		{
			var callback = new ValueCallback();
			_webView.EvaluateJavascript(scriptName, callback);

			return Task.Run(() => WaitForCallback(callback));
		}

		_webView.LoadUrl($"javascript:{scriptName}");

		return Task.FromResult(string.Empty);
	}

	public void Navigate(Uri uri)
	{
		_webView.LoadUrl(uri.ToString());
	}

	public string NavigateToString(string text)
	{
		_webView.LoadData(text, null, null);
		return text;
	}

	public void Reload()
	{
		_webView.Reload();
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_webView.Visibility = visible
			? ViewStates.Visible
			: ViewStates.Invisible;
	}

	public void Stop()
	{
		_webView.StopLoading();
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

	private string WaitForCallback(ValueCallback callback)
	{
		if (!Utility.WaitUntil(() => callback.HasReceivedCallback, 1000, 10))
		{
			#if DEBUG
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			#endif
		}

		return callback.ReceivedValue;
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion

	#region Classes

	internal class ValueCallback : Object, IValueCallback
	{
		#region Properties

		public bool HasReceivedCallback { get; private set; }

		public string ReceivedValue { get; private set; }

		#endregion

		#region Methods

		public void OnReceiveValue(Object value)
		{
			ReceivedValue = value?.ToString();
			HasReceivedCallback = true;
		}

		#endregion
	}

	#endregion
}
