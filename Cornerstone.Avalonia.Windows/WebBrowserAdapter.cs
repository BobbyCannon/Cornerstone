#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.AvaloniaWebView;
using Cornerstone.Data;
using Microsoft.Win32;

#endregion

namespace Cornerstone.Avalonia.Windows;

[SupportedOSPlatform("Windows")]
internal sealed class WebBrowserAdapter : Notifiable, IWebViewAdapter, IDisposable
{
	#region Fields

	private readonly WebBrowser _webBrowser;

	#endregion

	#region Constructors

	public WebBrowserAdapter()
	{
		_webBrowser = new WebBrowser
		{
			ScriptErrorsSuppressed = true,
			IsWebBrowserContextMenuEnabled = false
		};

		_webBrowser.Navigated += OnWebBrowserOnNavigated;
		_webBrowser.Navigating += WebBrowserOnNavigating;
		_webBrowser.NewWindow += WebBrowserOnNewWindow;
	}

	static WebBrowserAdapter()
	{
		using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
		{
			var app = Path.GetFileName(Application.ExecutablePath);
			key?.SetValue(app, 11001, RegistryValueKind.DWord);
			key?.Close();
		}
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webBrowser.CanGoBack;

	public bool CanGoForward => _webBrowser.CanGoForward;

	public byte[] Favicon { get; internal set; }

	public IPlatformHandle PlatformHandle => new PlatformHandle(_webBrowser.Handle, "HWND");

	public string Title { get; internal set; }

	public Uri Uri
	{
		get => _webBrowser.Url;
		set => _webBrowser.Url = value;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		_webBrowser.Navigated -= OnWebBrowserOnNavigated;
		_webBrowser.Navigating -= WebBrowserOnNavigating;
		_webBrowser.NewWindow -= WebBrowserOnNewWindow;
		WinApi.DestroyWindow(_webBrowser.Handle);
		_webBrowser.Dispose();
	}

	public bool GoBack()
	{
		return _webBrowser.GoBack();
	}

	public bool GoForward()
	{
		return _webBrowser.GoForward();
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
		_webBrowser.Width = width;
		_webBrowser.Height = height;
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		var result = _webBrowser.Document?.InvokeScript("eval", [script]) as string;
		return Task.FromResult(result);
	}

	public void Navigate(Uri url)
	{
		_webBrowser.Navigate(url);
	}

	public void NavigateToString(string text)
	{
		_webBrowser.DocumentText = "0";
		_webBrowser.Document?.OpenNew(true);
		_webBrowser.Document?.Write(text);
		_webBrowser.Refresh();
	}

	public void Reload()
	{
		_webBrowser.Refresh();
	}

	public void Stop()
	{
		_webBrowser.Stop();
	}

	private void OnWebBrowserOnNavigated(object s, WebBrowserNavigatedEventArgs args)
	{
		OnPropertyChanged(nameof(CanGoBack));
		OnPropertyChanged(nameof(CanGoForward));
		OnPropertyChanged(nameof(Uri));

		Title = _webBrowser.DocumentTitle;

		NavigationCompleted?.Invoke(this, new WebViewNavigationEventArgs { Request = args.Url });
	}

	private void WebBrowserOnNavigating(object sender, WebBrowserNavigatingEventArgs args)
	{
		NavigationStarted?.Invoke(this, new WebViewNavigationEventArgs { Request = args.Url });
	}

	private void WebBrowserOnNewWindow(object sender, CancelEventArgs e)
	{
		var handler = NewWindowRequested;
		if (handler == null)
		{
			return;
		}

		var args = new WebViewNewWindowEventArgs();
		handler.Invoke(this, args);
		e.Cancel = args.Handled;
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion

	#region Classes

	private static class WinApi
	{
		#region Methods

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyWindow(IntPtr hwnd);

		#endregion
	}

	#endregion
}