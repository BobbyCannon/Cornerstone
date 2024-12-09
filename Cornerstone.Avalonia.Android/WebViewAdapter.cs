#region References

using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Webkit;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Android.Clients;
using Cornerstone.Avalonia.AvaloniaWebView;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Key = Avalonia.Input.Key;
using Object = Java.Lang.Object;

#endregion

namespace Cornerstone.Avalonia.Android;

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

	/// <inheritdoc />
	public bool CanGoBack => _webView.CanGoBack();

	/// <inheritdoc />
	public bool CanGoForward => _webView.CanGoForward();

	/// <inheritdoc />
	public byte[] Favicon { get; internal set; }

	/// <inheritdoc />
	public IPlatformHandle PlatformHandle { get; }

	/// <inheritdoc />
	public string Title { get; internal set; }

	/// <inheritdoc />
	public Uri Uri
	{
		get => Uri.TryCreate(_webView.Url, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
		set => Navigate(value);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool GoBack()
	{
		return _webView.CanGoBack();
	}

	/// <inheritdoc />
	public bool GoForward()
	{
		return _webView.CanGoForward();
	}

	/// <inheritdoc />
	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return false;
	}

	/// <inheritdoc />
	public void HandleResize(int width, int height, float zoom)
	{
	}

	/// <inheritdoc />
	public Task<string> InvokeScriptAsync(string scriptName)
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
		{
			var callback = new ValueCallback();
			_webView.EvaluateJavascript(scriptName, null);

			return Task.Run(() => WaitForCallback(callback));
		}

		_webView.LoadUrl($"javascript:{scriptName}");

		return Task.FromResult(string.Empty);
	}

	/// <inheritdoc />
	public void Navigate(Uri uri)
	{
		_webView.LoadUrl(uri.ToString());
	}

	/// <inheritdoc />
	public void NavigateToString(string text)
	{
		_webView.LoadData(text, null, null);
	}

	/// <inheritdoc />
	public void Reload()
	{
		_webView.Reload();
	}

	/// <inheritdoc />
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

	private string WaitForCallback(ValueCallback callback)
	{
		UtilityExtensions.WaitUntil(() => callback.HasReceivedCallback, 1000, 10);
		return callback.ReceivedValue;
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	/// <inheritdoc />
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	/// <inheritdoc />
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