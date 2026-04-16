#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.OS;
using Android.Webkit;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Platforms.Android.Clients;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Key = Avalonia.Input.Key;
using Object = Java.Lang.Object;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

internal class WebViewAdapter : Notifiable, IWebViewAdapter
{
	#region Fields

	private readonly CornerstoneWebChromeClient _webChromeClient;
	private readonly AndroidWebView _webView;
	private readonly CornerstoneWebViewClient _webViewClient;

	#endregion

	#region Constructors

	public WebViewAdapter()
	{
		var parentContext = AndroidApplication.Context;
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

	private class ValueCallback : Object, IValueCallback
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