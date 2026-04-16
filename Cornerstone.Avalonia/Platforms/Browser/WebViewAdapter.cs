#region References

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;
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

	public bool CanGoBack => false;

	public bool CanGoForward => false;

	public string Content
	{
		get => GetContent();
		set => NavigateToString(Content);
	}

	public byte[] Favicon { get; internal set; }

	public IPlatformHandle PlatformHandle => _jsObjectControl;

	public string Title { get; internal set; }

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
		return InvokeScriptAsync("document.documentElement.outerHTML;").GetAwaiter().GetResult();
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
		BrowserInterop.HideElement(_content);
		_content.SetProperty("innerHTML", string.Empty);

		_iframe.SetProperty("src", uri.ToString());
		BrowserInterop.ShowElement(_iframe);
	}

	public string NavigateToString(string text)
	{
		BrowserInterop.HideElement(_iframe);
		_iframe.SetProperty("src", string.Empty);

		_content.SetProperty("innerHTML", text);
		BrowserInterop.ShowElement(_content);
		return text;
	}

	public void Reload()
	{
		//_webView.Reload();
	}

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

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}