#region References

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.WebView;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

internal class WebViewAdapter : Notifiable, IWebViewAdapter
{
	#region Fields

	private readonly JSObject _content;
	private readonly JSObject _iframe;
	private readonly JSObjectControlHandle _jsObjectControl;
	private readonly JSObject _root;
	private Uri _uri;

	#endregion

	#region Constructors

	public WebViewAdapter()
	{
		_root = BrowserInterop.CreateElement("div");
		_iframe = BrowserInterop.CreateElement(_root, "iframe");
		_content = BrowserInterop.CreateElement(_root, "div");
		_jsObjectControl = new JSObjectControlHandle(_root);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool CanGoBack => false;

	/// <inheritdoc />
	public bool CanGoForward => false;

	public string Content
	{
		get => GetContent();
		set => NavigateToString(Content);
	}

	/// <inheritdoc />
	public byte[] Favicon { get; internal set; }

	/// <inheritdoc />
	public IPlatformHandle PlatformHandle => _jsObjectControl;

	/// <inheritdoc />
	public string Title { get; internal set; }

	/// <inheritdoc />
	public Uri Uri
	{
		get => _uri;
		set
		{
			_uri = value;
			Navigate(value);
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public string GetContent()
	{
		return InvokeScriptAsync("document.documentElement.outerHTML;").AwaitResults();
	}

	/// <inheritdoc />
	public bool GoBack()
	{
		//return _webView.CanGoBack();
		return false;
	}

	/// <inheritdoc />
	public bool GoForward()
	{
		//return _webView.CanGoForward();
		return false;
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
		return Task.FromResult(string.Empty);
	}

	/// <inheritdoc />
	public void Navigate(Uri uri)
	{
		BrowserInterop.HideElement(_content);
		_content.SetProperty("innerHTML", string.Empty);

		_iframe.SetProperty("src", uri.ToString());
		BrowserInterop.ShowElement(_iframe);
	}

	/// <inheritdoc />
	public string NavigateToString(string text)
	{
		BrowserInterop.HideElement(_iframe);
		_iframe.SetProperty("src", string.Empty);

		_content.SetProperty("innerHTML", text);
		BrowserInterop.ShowElement(_content);
		return text;
	}

	/// <inheritdoc />
	public void Reload()
	{
		//_webView.Reload();
	}

	/// <inheritdoc />
	public void Stop()
	{
		//_webView.StopLoading();
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

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	/// <inheritdoc />
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	/// <inheritdoc />
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}