#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class WebView : NativeControlHost, IWebView, IDisposable
{
	#region Constants

	public const string DefaultProfileName = "Default";

	#endregion

	#region Fields

	public static readonly StyledProperty<string> ContentProperty;
	public static readonly StyledProperty<bool> IsNavigatingProperty;
	public static readonly StyledProperty<Uri> UriProperty;

	private PropertyChangedEventHandler _propertyChangedHandler;
	private IWebViewAdapter _webViewAdapter;
	private TaskCompletionSource _webViewReadyCompletion;

	#endregion

	#region Constructors

	public WebView()
	{
		_webViewReadyCompletion = new();

		Cookies = [];
		Profile = DefaultProfileName;
	}

	static WebView()
	{
		ContentProperty = AvaloniaProperty.Register<WebView, string>(nameof(Content));
		IsNavigatingProperty = AvaloniaProperty.Register<WebView, bool>(nameof(IsNavigating));
		UriProperty = AvaloniaProperty.Register<WebView, Uri>(nameof(Uri));
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webViewAdapter?.CanGoBack ?? false;

	public bool CanGoForward => _webViewAdapter?.CanGoForward ?? false;

	public string Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public PresentationList<WebViewCookie> Cookies { get; }

	public byte[] Favicon => _webViewAdapter?.Favicon;

	public bool IsNavigating
	{
		get => GetValue(IsNavigatingProperty);
		set => SetValue(IsNavigatingProperty, value);
	}

	public string Profile { get; set; }

	public string Title => _webViewAdapter?.Title;

	public Uri Uri
	{
		get => GetValue(UriProperty);
		set => SetValue(UriProperty, value);
	}

	#endregion

	#region Methods

	public void ClearBrowsingData()
	{
		_webViewAdapter?.ClearBrowsingDataAsync();
		Cookies.Clear();
	}

	public void DeleteAllCookies()
	{
		_webViewAdapter?.DeleteAllCookies();
		Cookies.Clear();
	}

	public void DeleteCookie(WebViewCookie cookie)
	{
		_webViewAdapter?.DeleteCookie(cookie.Name, Uri.AbsoluteUri);
		Cookies.Remove(x => x.Name == cookie.Name);
	}

	public void DeleteProfile(string profileName)
	{
		_webViewAdapter?.DeleteProfile(profileName);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public IEnumerable<string> GetAvailableProfiles()
	{
		return _webViewAdapter?.GetAvailableProfiles() ?? [];
	}

	public string GetContent()
	{
		return _webViewAdapter?.GetContent();
	}

	public bool GoBack()
	{
		return _webViewAdapter?.GoBack() ?? false;
	}

	public bool GoForward()
	{
		return _webViewAdapter?.GoForward() ?? false;
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		return CornerstoneApplication.CornerstoneDispatcher.Dispatch(() => _webViewAdapter?.InvokeScriptAsync(script));
	}

	public void Navigate(string uri)
	{
		_webViewAdapter?.Navigate(new Uri(uri));
	}

	public void Navigate(Uri uri)
	{
		_webViewAdapter?.Navigate(uri);
	}

	public string NavigateToString(string text)
	{
		_webViewAdapter?.NavigateToString(text);
		return text;
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void Reload()
	{
		_webViewAdapter.Reload();
	}

	public void ScrollToBottom()
	{
		InvokeScriptAsync("window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });");
	}

	public void Stop()
	{
		_webViewAdapter.Stop();
	}

	public Task WaitForNativeHost()
	{
		return _webViewReadyCompletion.Task;
	}

	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		if (_webViewAdapter != null)
		{
			return _webViewAdapter.PlatformHandle;
		}

		_webViewAdapter = CornerstoneApplication
			.DependencyProvider
			.GetInstance<IWebViewAdapter>();

		_webViewAdapter.Initialize(Profile);
		_webViewAdapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged += WebViewAdapterOnPropertyChanged;
		_webViewReadyCompletion.TrySetResult();

		if (!string.IsNullOrWhiteSpace(Uri?.OriginalString))
		{
			_webViewAdapter.Uri = Uri;
		}

		if (!string.IsNullOrWhiteSpace(Content))
		{
			_webViewAdapter.NavigateToString(Content);
		}

		return _webViewAdapter.PlatformHandle;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		if (_webViewAdapter is null)
		{
			return;
		}

		_webViewReadyCompletion = new TaskCompletionSource();
		_webViewAdapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged -= WebViewAdapterOnPropertyChanged;

		DisposableExtensions.TryDispose(_webViewAdapter);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = _webViewAdapter?.HandleKeyDown(e.Key, e.KeyModifiers) ?? false;
		base.OnKeyDown(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ContentProperty)
		{
			NavigateToString(Content);
		}
		else if (change.Property == UriProperty)
		{
			Navigate(Uri);
		}

		base.OnPropertyChanged(change);

		if (change.Property != BoundsProperty)
		{
			return;
		}

		var newValue = change.GetNewValue<Rect>();
		var scaling = 1.0f;

		_webViewAdapter?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
	}

	private void RefreshCookies()
	{
		_webViewAdapter
			.GetCookiesAsync()
			.ContinueWith(x =>
			{
				CornerstoneApplication.CornerstoneDispatcher
					.Dispatch(() => Cookies.Load(x.Result));
			});
	}

	private void WebViewAdapterOnNavigationCompleted(object sender, WebViewNavigationEventArgs e)
	{
		RefreshCookies();

		CornerstoneApplication.CornerstoneDispatcher
			.Dispatch(() =>
			{
				IsNavigating = false;
				OnPropertyChanged(nameof(Uri));
			});
		NavigationCompleted?.Invoke(this, e);
	}

	private void WebViewAdapterOnNavigationStarted(object sender, WebViewNavigationEventArgs e)
	{
		CornerstoneApplication.CornerstoneDispatcher.Dispatch(() =>
		{
			IsNavigating = true;
			OnPropertyChanged(nameof(Uri));
		});
		NavigationStarted?.Invoke(this, e);
	}

	private void WebViewAdapterOnNewWindowRequested(object sender, WebViewNewWindowEventArgs e)
	{
		NewWindowRequested?.Invoke(this, e);
	}

	private void WebViewAdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(_webViewAdapter.CanGoBack):
			{
				OnPropertyChanged(nameof(CanGoBack));
				break;
			}
			case nameof(_webViewAdapter.CanGoForward):
			{
				OnPropertyChanged(nameof(CanGoForward));
				break;
			}
			case nameof(_webViewAdapter.Favicon):
			{
				OnPropertyChanged(nameof(Favicon));
				break;
			}
			case nameof(_webViewAdapter.Content):
			{
				Content = _webViewAdapter.Content;
				break;
			}
			case nameof(_webViewAdapter.Title):
			{
				OnPropertyChanged(nameof(Title));
				break;
			}
			case nameof(_webViewAdapter.Uri):
			{
				CornerstoneApplication.CornerstoneDispatcher.Dispatch(() =>
				{
					Uri = _webViewAdapter.Uri;
					OnPropertyChanged(nameof(Uri));
				});
				break;
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}