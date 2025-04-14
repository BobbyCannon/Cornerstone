#region References

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.WebView;

[DoNotNotify]
public class WebView : NativeControlHost, IWebView, IDisposable
{
	#region Fields

	public static readonly StyledProperty<string> ContentProperty;
	public static readonly StyledProperty<bool> IsNavigatingProperty;
	public static readonly StyledProperty<Uri> UriProperty;

	private PropertyChangedEventHandler _propertyChangedHandler;
	private IWebViewAdapter _platformAdapter;
	private TaskCompletionSource _webViewReadyCompletion;

	#endregion

	#region Constructors

	public WebView()
	{
		_webViewReadyCompletion = new();
	}

	static WebView()
	{
		ContentProperty = AvaloniaProperty.Register<WebView, string>(nameof(Content));
		IsNavigatingProperty = AvaloniaProperty.Register<WebView, bool>(nameof(IsNavigating));
		UriProperty = AvaloniaProperty.Register<WebView, Uri>(nameof(Uri));
	}

	#endregion

	#region Properties

	public bool CanGoBack => _platformAdapter?.CanGoBack ?? false;

	public bool CanGoForward => _platformAdapter?.CanGoForward ?? false;

	public string Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public byte[] Favicon => _platformAdapter?.Favicon;

	public bool IsNavigating
	{
		get => GetValue(IsNavigatingProperty);
		set => SetValue(IsNavigatingProperty, value);
	}

	public string Title => _platformAdapter?.Title;

	public Uri Uri
	{
		get => GetValue(UriProperty);
		set => SetValue(UriProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public string GetContent()
	{
		return _platformAdapter?.GetContent();
	}

	public bool GoBack()
	{
		return _platformAdapter?.GoBack() ?? false;
	}

	public bool GoForward()
	{
		return _platformAdapter?.GoForward() ?? false;
	}

	public Task<string> InvokeScriptAsync(string script)
	{
		return _platformAdapter?.InvokeScriptAsync(script);
	}

	public void Navigate(string uri)
	{
		_platformAdapter?.Navigate(new Uri(uri));
	}

	public void Navigate(Uri uri)
	{
		_platformAdapter?.Navigate(uri);
	}

	public string NavigateToString(string text)
	{
		_platformAdapter?.NavigateToString(text);
		return text;
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler = this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void Reload()
	{
		_platformAdapter.Reload();
	}

	public void Stop()
	{
		_platformAdapter.Stop();
	}

	public Task WaitForNativeHost()
	{
		return _webViewReadyCompletion.Task;
	}

	/// <inheritdoc />
	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		if (_platformAdapter != null)
		{
			return _platformAdapter.PlatformHandle;
		}

		_platformAdapter = CornerstoneApplication
			.DependencyProvider
			.GetInstance<IWebViewAdapter>();

		_platformAdapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
		_platformAdapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
		_platformAdapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
		_platformAdapter.PropertyChanged += WebViewAdapterOnPropertyChanged;
		_webViewReadyCompletion.TrySetResult();
		_platformAdapter.Uri = Uri;

		if (!string.IsNullOrWhiteSpace(Content))
		{
			_platformAdapter.NavigateToString(Content);
		}

		return _platformAdapter.PlatformHandle;
	}

	protected override void DestroyNativeControlCore(IPlatformHandle control)
	{
		// See dispose, we want to keep state.
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		if (_platformAdapter is null)
		{
			return;
		}

		_webViewReadyCompletion = new TaskCompletionSource();
		_platformAdapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
		_platformAdapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
		_platformAdapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
		_platformAdapter.PropertyChanged -= WebViewAdapterOnPropertyChanged;

		if (_platformAdapter is IDisposable d)
		{
			d.Dispose();
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = _platformAdapter?.HandleKeyDown(e.Key, e.KeyModifiers) ?? false;
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
		var scaling = (float) (VisualRoot?.RenderScaling ?? 1.0f);

		_platformAdapter?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
	}

	private void WebViewAdapterOnNavigationCompleted(object sender, WebViewNavigationEventArgs e)
	{
		CornerstoneApplication.Dispatcher.Dispatch(() => { IsNavigating = false; });
		NavigationCompleted?.Invoke(this, e);
	}

	private void WebViewAdapterOnNavigationStarted(object sender, WebViewNavigationEventArgs e)
	{
		CornerstoneApplication.Dispatcher.Dispatch(() => IsNavigating = true);
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
			case nameof(_platformAdapter.CanGoBack):
			{
				OnPropertyChanged(nameof(CanGoBack));
				break;
			}
			case nameof(_platformAdapter.CanGoForward):
			{
				OnPropertyChanged(nameof(CanGoForward));
				break;
			}
			case nameof(_platformAdapter.Favicon):
			{
				OnPropertyChanged(nameof(Favicon));
				break;
			}
			case nameof(_platformAdapter.Content):
			{
				Content = _platformAdapter.Content;
				break;
			}
			case nameof(_platformAdapter.Title):
			{
				OnPropertyChanged(nameof(Title));
				break;
			}
			case nameof(_platformAdapter.Uri):
			{
				Uri = _platformAdapter.Uri;
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