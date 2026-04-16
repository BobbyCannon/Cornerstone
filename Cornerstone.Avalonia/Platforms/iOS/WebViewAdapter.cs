#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;
using CoreGraphics;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Data;
using Foundation;
using UIKit;
using WebKit;
using Key = Avalonia.Input.Key;

#endregion

namespace Cornerstone.Avalonia.Platforms.iOS;

internal class WebViewAdapter : Notifiable, IWebViewAdapter, IDisposable
{
	#region Fields

	private bool _disposed;
	private readonly WebViewPlatformHandle _platformHandle;
	private Uri _uri;
	private readonly WKWebView _webView;

	#endregion

	#region Constructors

	public WebViewAdapter()
	{
		// Initialize WKWebView with default configuration
		var configuration = new WKWebViewConfiguration();
		_webView = new WKWebView(UIScreen.MainScreen.Bounds, configuration)
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
		};

		// Set up navigation delegate to handle events
		_webView.NavigationDelegate = new WebViewNavigationDelegate(this);

		// Initialize platform handle
		_platformHandle = new WebViewPlatformHandle(_webView);

		// Initialize properties
		_uri = new Uri("about:blank");
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webView.CanGoBack;

	public bool CanGoForward => _webView.CanGoForward;

	public string Content => GetContent();

	public byte[] Favicon => null;

	public IPlatformHandle PlatformHandle => _platformHandle;

	public string Title => _webView.Title;

	public Uri Uri
	{
		get => _uri;
		set => Navigate(value);
	}

	IPlatformHandle IWebViewAdapter.PlatformHandle { get; }

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
		{
			return;
		}

		_webView.NavigationDelegate?.Dispose();
		_webView.Dispose();
		_disposed = true;
	}

	public IEnumerable<string> GetAvailableProfiles()
	{
		return [];
	}

	public string GetContent()
	{
		// Asynchronously get HTML content
		var task = _webView.EvaluateJavaScriptAsync("document.documentElement.outerHTML");
		task.Wait();
		return task.Result?.ToString() ?? string.Empty;
	}

	public Task<IEnumerable<WebViewCookie>> GetCookiesAsync()
	{
		return Task.FromResult((IEnumerable<WebViewCookie>) []);
	}

	public bool GoBack()
	{
		if (_webView.CanGoBack)
		{
			_webView.GoBack();
			return true;
		}
		return false;
	}

	public bool GoForward()
	{
		if (_webView.CanGoForward)
		{
			_webView.GoForward();
			return true;
		}
		return false;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		// Basic key handling; extend based on requirements
		if (key == Key.F5)
		{
			Reload();
			return true;
		}
		if ((key == Key.BrowserBack) && CanGoBack)
		{
			GoBack();
			return true;
		}
		if ((key == Key.BrowserForward) && CanGoForward)
		{
			GoForward();
			return true;
		}
		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
		// Update WKWebView frame and scale
		_webView.Frame = new CGRect(0, 0, width, height);
		_webView.EvaluateJavaScriptAsync($"document.body.style.zoom = '{zoom}';");
	}

	public void Initialize(string profileName)
	{
	}

	public async Task<string> InvokeScriptAsync(string script)
	{
		var result = await _webView.EvaluateJavaScriptAsync(script);
		return result?.ToString() ?? string.Empty;
	}

	public void Navigate(Uri uri)
	{
		if (uri == null)
		{
			return;
		}

		_uri = uri;
		if (uri.Scheme == "about")
		{
			_webView.LoadRequest(new NSUrlRequest(new NSUrl(uri.ToString())));
		}
		else
		{
			var request = new NSUrlRequest(new NSUrl(uri.AbsoluteUri));
			_webView.LoadRequest(request);
		}
		OnPropertyChanged(nameof(Uri));
	}

	public string NavigateToString(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		_uri = new Uri("about:blank");
		_webView.LoadHtmlString(text, new NSUrl("about:blank"));
		OnPropertyChanged(nameof(Uri));
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

	void IWebViewAdapter.Initialize(string profileName)
	{
		throw new NotImplementedException();
	}

	private void OnNavigationCompleted(NSUrl url)
	{
		_uri = new Uri(url.AbsoluteString);
		NavigationCompleted?.Invoke(this, new WebViewNavigationEventArgs { Uri = _uri });
		OnPropertyChanged(nameof(Title));
		OnPropertyChanged(nameof(CanGoBack));
		OnPropertyChanged(nameof(CanGoForward));
		OnPropertyChanged(nameof(Uri));
	}

	private void OnNavigationStarted(NSUrl url)
	{
		_uri = new Uri(url.AbsoluteString);
		NavigationStarted?.Invoke(this, new WebViewNavigationEventArgs { Uri = _uri });
		OnPropertyChanged(nameof(Uri));
	}

	private void OnNewWindowRequested(NSUrl url)
	{
		NewWindowRequested?.Invoke(this, new WebViewNewWindowEventArgs { Uri = new Uri(url.AbsoluteString) });
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion

	#region Classes

	private class WebViewNavigationDelegate : WKNavigationDelegate
	{
		#region Fields

		private readonly WebViewAdapter _adapter;

		#endregion

		#region Constructors

		public WebViewNavigationDelegate(WebViewAdapter adapter)
		{
			_adapter = adapter;
		}

		#endregion

		#region Methods

		public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
		{
			if (navigationAction.TargetFrame == null)
			{
				// New window requested (e.g., target="_blank")
				if (navigationAction.Request.Url != null)
				{
					_adapter.OnNewWindowRequested(navigationAction.Request.Url);
				}
				decisionHandler(WKNavigationActionPolicy.Cancel);
			}
			else
			{
				decisionHandler(WKNavigationActionPolicy.Allow);
			}
		}

		public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
		{
			// Handle navigation errors if needed
		}

		public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
		{
			if (webView.Url != null)
			{
				_adapter.OnNavigationCompleted(webView.Url);
			}
		}

		public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
		{
			if (webView.Url != null)
			{
				_adapter.OnNavigationStarted(webView.Url);
			}
		}

		#endregion
	}

	private class WebViewPlatformHandle : IPlatformHandle
	{
		#region Fields

		private readonly WKWebView _webView;

		#endregion

		#region Constructors

		public WebViewPlatformHandle(WKWebView webView)
		{
			_webView = webView;
		}

		#endregion

		#region Properties

		public IntPtr Handle => _webView.Handle;

		public string HandleDescriptor => "WKWebView";

		#endregion
	}

	#endregion
}