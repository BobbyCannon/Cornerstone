#region References

using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.AvaloniaWebView;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

#endregion

namespace Cornerstone.Avalonia.Windows;

[SupportedOSPlatform("Windows")]
internal class WebView2Adapter : Notifiable, IWebViewAdapter, IDisposable
{
	#region Fields

	private readonly WebView2 _webView;

	#endregion

	#region Constructors

	public WebView2Adapter(IRuntimeInformation runtimeInformation)
	{
		var defaultBackground = ResourceService.GetColor("Background02").ToDrawingColor();
		var defaultForeground = ResourceService.GetColor("Foreground02").ToDrawingColor();

		_webView = new WebView2();
		_webView.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = runtimeInformation.ApplicationDataLocation };
		_webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
		_webView.NavigationStarting += OnWebViewOnNavigationStarting;
		_webView.NavigationCompleted += OnWebViewOnNavigationCompleted;
		_webView.BackColor = defaultBackground;
		_webView.ForeColor = defaultForeground;
		_webView.DefaultBackgroundColor = defaultBackground;
		_webView.EnsureCoreWebView2Async();

		PlatformHandle = new PlatformHandle(_webView.Handle, "HWND");
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webView.CanGoBack;

	public bool CanGoForward => _webView.CanGoForward;

	public byte[] Favicon { get; private set; }

	public IPlatformHandle PlatformHandle { get; }

	/// <inheritdoc />
	public string Title { get; private set; }

	public Uri Uri
	{
		get => _webView.Source;
		set => _webView.Source = value;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_webView.CoreWebView2 != null)
		{
			_webView.CoreWebView2.DocumentTitleChanged -= OnCoreWebView2OnDocumentTitleChanged;
			_webView.CoreWebView2.FaviconChanged -= OnCoreWebView2OnFaviconChanged;
			_webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
		}

		if (_webView != null)
		{
			_webView.CoreWebView2InitializationCompleted -= OnCoreWebView2InitializationCompleted;
			_webView.NavigationStarting -= OnWebViewOnNavigationStarting;
			_webView.NavigationCompleted -= OnWebViewOnNavigationCompleted;
			_webView.Dispose();
		}
	}

	public bool GoBack()
	{
		_webView.GoBack();
		return true;
	}

	public bool GoForward()
	{
		_webView.GoForward();
		return true;
	}

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		return _webView.ExecuteScriptAsync(script);
	}

	public void Navigate(Uri url)
	{
		_webView.Source = url;
	}

	public void NavigateToString(string text)
	{
		_webView.NavigateToString(text);
	}

	public void Reload()
	{
		_webView.CoreWebView2.Reload();
	}

	public void Stop()
	{
		_webView.Stop();
	}

	private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
	{
		CornerstoneApplication.Dispatcher.Dispatch(
			() =>
			{
				_webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
				_webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
				_webView.CoreWebView2.Settings.IsScriptEnabled = true;

				_webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
				_webView.CoreWebView2.DocumentTitleChanged += OnCoreWebView2OnDocumentTitleChanged;
				_webView.CoreWebView2.FaviconChanged += OnCoreWebView2OnFaviconChanged;
				_webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
			}
		);
	}

	private void OnCoreWebView2OnDocumentTitleChanged(object sender, object e)
	{
		Title = _webView.CoreWebView2.DocumentTitle;
	}

	private void OnCoreWebView2OnFaviconChanged(object sender, object e)
	{
		_ = RefreshFaviconAsync();
	}

	private void OnNewWindowRequested(object s, CoreWebView2NewWindowRequestedEventArgs args)
	{
		var handler = NewWindowRequested;
		if (handler == null)
		{
			return;
		}

		var newArgs = new WebViewNewWindowEventArgs { Request = new Uri(args.Uri) };
		handler.Invoke(this, newArgs);
		args.Handled = newArgs.Handled;
	}

	private void OnWebViewOnNavigationCompleted(object s, CoreWebView2NavigationCompletedEventArgs args)
	{
		OnPropertyChanged(nameof(CanGoBack));
		OnPropertyChanged(nameof(CanGoForward));
		OnPropertyChanged(nameof(Uri));

		NavigationCompleted?.Invoke(this, new WebViewNavigationEventArgs { Request = _webView.Source });
		Title = _webView.CoreWebView2.DocumentTitle;
	}

	private void OnWebViewOnNavigationStarting(object s, CoreWebView2NavigationStartingEventArgs args)
	{
		NavigationStarted?.Invoke(this, new WebViewNavigationEventArgs { Request = new Uri(args.Uri) });
	}

	private async Task RefreshFaviconAsync()
	{
		await using var stream = await _webView.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
		Favicon = stream.ReadByteArray();
		OnPropertyChanged(nameof(Favicon));
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}