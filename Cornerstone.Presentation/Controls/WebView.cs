#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Web;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Cross-platform web view.
/// </summary>
public class WebView : PausableNativeHost, IWebView
{
	#region Constants

	public const string DefaultProfileName = "Default";

	#endregion

	#region Fields

	public static readonly StyledProperty<string> ContentProperty =
		PresentationProperty.Register<WebView, string>(nameof(Content));

	public static readonly StyledProperty<bool> IsNavigatingProperty =
		PresentationProperty.Register<WebView, bool>(nameof(IsNavigating));

	public static readonly StyledProperty<Uri> UriProperty =
		PresentationProperty.Register<WebView, Uri>(nameof(Uri));

	private bool _nativeEngineUnavailable;
	private int _cookieRequest;
	private bool _suppressNavigationPropertyHandler;
	private IWebViewAdapter _webViewAdapter;

	#endregion

	#region Constructors

	public WebView()
	{
		Cookies = [];
		Profile = DefaultProfileName;
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

	/// <inheritdoc />
	protected override bool UseDefaultNativeChildWhenNull => !_nativeEngineUnavailable && base.UseDefaultNativeChildWhenNull;

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
		if ((cookie == null) || (Uri == null))
		{
			return;
		}

		_webViewAdapter?.DeleteCookie(cookie.Name, Uri.AbsoluteUri);
		Cookies.Remove(x => x.Name == cookie.Name);
	}

	public void DeleteCookiesForCurrentUri()
	{
		if (Uri == null)
		{
			return;
		}

		_webViewAdapter?.DeleteCookie(string.Empty, Uri.AbsoluteUri);
		Cookies.Clear();
	}

	public void DeleteProfile(string profileName)
	{
		_webViewAdapter?.DeleteProfile(profileName);
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
		var adapter = _webViewAdapter;
		if (adapter == null)
		{
			return Task.FromResult(string.Empty);
		}

		return Dispatcher.Dispatch(() =>
				adapter.InvokeScriptAsync(script) ?? Task.FromResult(string.Empty))
			?? Task.FromResult(string.Empty);
	}

	public void Navigate(string uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
		{
			return;
		}

		Navigate(new Uri(uri));
	}

	public void Navigate(Uri uri)
	{
		if (uri == null)
		{
			return;
		}

		_suppressNavigationPropertyHandler = true;
		try
		{
			SetCurrentValue(ContentProperty, string.Empty);
			SetCurrentValue(UriProperty, uri);
		}
		finally
		{
			_suppressNavigationPropertyHandler = false;
		}

		_webViewAdapter?.Navigate(uri);
	}

	public string NavigateToString(string text)
	{
		text ??= string.Empty;

		_suppressNavigationPropertyHandler = true;
		try
		{
			SetCurrentValue(UriProperty, null);
			SetCurrentValue(ContentProperty, text);
		}
		finally
		{
			_suppressNavigationPropertyHandler = false;
		}

		_webViewAdapter?.NavigateToString(text);
		return text;
	}

	public void Reload()
	{
		_webViewAdapter?.Reload();
	}

	public void ScrollToBottom()
	{
		InvokeScriptAsync("window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });");
	}

	public void Stop()
	{
		_webViewAdapter?.Stop();
	}

	/// <inheritdoc />
	protected override void AfterDestroyNativeControlCore(IPlatformHandle control)
	{
		if (AppBootstrap.RuntimeInformation.DevicePlatform == DevicePlatform.Android)
		{
			ReleaseAdapter();
		}
	}

	/// <inheritdoc />
	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		EnsureAdapterInitialized();
		return _webViewAdapter?.AttachToHost(parent, this);
	}

	/// <inheritdoc />
	protected override void DestroyNativeControlCore(IPlatformHandle control)
	{
		// Windows/iOS: keep the adapter across temporary host teardown; Dispose owns lifetime.
		// Android: do not Destroy() here. Cornerstone still owns the native view until
		// NestedNativeHost runs base.DestroyNativeControlCore, then AfterDestroyNativeControlCore.
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			// Prefer native unparent (base) before Android WebView.Destroy in ReleaseAdapter.
			base.Dispose(disposing);
			ReleaseAdapter();
			return;
		}

		base.Dispose(disposing);
	}

	/// <inheritdoc />
	protected override IPausableNativeSurface GetSurface()
	{
		return _webViewAdapter;
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_webViewAdapter?.DetachFromHost();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = _webViewAdapter?.HandleKeyDown(e.Key, e.KeyModifiers) ?? false;
		base.OnKeyDown(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (!_suppressNavigationPropertyHandler)
		{
			if (change.Property == ContentProperty)
			{
				_webViewAdapter?.NavigateToString(Content ?? string.Empty);
			}
			else if (change.Property == UriProperty)
			{
				if (Uri != null)
				{
					_webViewAdapter?.Navigate(Uri);
				}
			}
		}

		base.OnPropertyChanged(change);
	}

	private IPlatformHandle EnsureAdapterInitialized()
	{
		if (_webViewAdapter != null)
		{
			return _webViewAdapter.PlatformHandle;
		}

		try
		{
			_webViewAdapter = AppBootstrap.GetInstance<IWebViewAdapter>();
		}
		catch (Exception ex) when (IsLinuxWebViewUnavailable(ex))
		{
			_nativeEngineUnavailable = true;
			_webViewAdapter = new WebViewAdapterStub();
			ShowLinuxWebViewUnavailable(Unwrap(ex).Message);
		}

		_webViewAdapter.Initialize(Profile);
		_webViewAdapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged += WebViewAdapterOnPropertyChanged;

		_suppressNavigationPropertyHandler = true;
		try
		{
			if (!string.IsNullOrWhiteSpace(Uri?.OriginalString))
			{
				_webViewAdapter.Navigate(Uri);
			}
			else if (!string.IsNullOrWhiteSpace(Content))
			{
				_webViewAdapter.NavigateToString(Content);
			}
		}
		finally
		{
			_suppressNavigationPropertyHandler = false;
		}

		return _webViewAdapter.PlatformHandle;
	}

	private static bool IsLinuxWebViewUnavailable(Exception ex)
	{
		return Unwrap(ex) is WebViewUnavailableForLinuxException;
	}

	private void RefreshCookies()
	{
		if (_webViewAdapter == null)
		{
			return;
		}

		var request = _cookieRequest;
		var adapter = _webViewAdapter;
		adapter
			.GetCookiesAsync()
			.ContinueWith(x =>
			{
				if (x.IsFaulted || x.IsCanceled || (request != _cookieRequest))
				{
					return;
				}

				Dispatcher.Dispatch(() =>
				{
					if ((request != _cookieRequest) || !ReferenceEquals(_webViewAdapter, adapter))
					{
						return;
					}

					Cookies.Load(x.Result ?? []);
				});
			});
	}

	private void ReleaseAdapter()
	{
		if (_webViewAdapter is null)
		{
			return;
		}

		ResetNativeHostReady();
		_webViewAdapter.DetachFromHost();
		_webViewAdapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged -= WebViewAdapterOnPropertyChanged;

		DisposableExtensions.TryDispose(_webViewAdapter);
		_webViewAdapter = null;
	}

	private void ShowLinuxWebViewUnavailable(string message)
	{
		Children.Add(new Border
		{
			Padding = new Thickness(16),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = new SelectableTextBlock
			{
				Text = message,
				TextWrapping = TextWrapping.Wrap
			}
		});
	}

	private static Exception Unwrap(Exception ex)
	{
		while (ex is TargetInvocationException { InnerException: { } inner })
		{
			ex = inner;
		}
		return ex;
	}

	private void WebViewAdapterOnNavigationCompleted(object sender, WebViewNavigationEventArgs e)
	{
		RefreshCookies();

		Dispatcher
			.Dispatch(() =>
			{
				IsNavigating = false;
				OnPropertyChanged(nameof(IsNavigating));
				OnPropertyChanged(nameof(Uri));
				if (!IsPaused)
				{
					RequestWarmUnderlay();
				}
			});
		NavigationCompleted?.Invoke(this, e);
	}

	private void WebViewAdapterOnNavigationStarted(object sender, WebViewNavigationEventArgs e)
	{
		_cookieRequest++;
		Dispatcher.Dispatch(() =>
		{
			Cookies.Clear();
			IsNavigating = true;
			OnPropertyChanged(nameof(IsNavigating));
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
				_suppressNavigationPropertyHandler = true;
				try
				{
					Content = _webViewAdapter.Content;
				}
				finally
				{
					_suppressNavigationPropertyHandler = false;
				}
				break;
			}
			case nameof(_webViewAdapter.Title):
			{
				OnPropertyChanged(nameof(Title));
				break;
			}
			case nameof(_webViewAdapter.Uri):
			{
				Dispatcher.Dispatch(() =>
				{
					_suppressNavigationPropertyHandler = true;
					try
					{
						Uri = _webViewAdapter.Uri;
					}
					finally
					{
						_suppressNavigationPropertyHandler = false;
					}
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