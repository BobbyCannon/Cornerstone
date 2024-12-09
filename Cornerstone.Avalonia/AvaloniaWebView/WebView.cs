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

namespace Cornerstone.Avalonia.AvaloniaWebView;

[DoNotNotify]
public class WebView : NativeControlHost, IWebView, IDisposable
{
	#region Fields

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
	}

	static WebView()
	{
		IsNavigatingProperty = AvaloniaProperty.Register<WebView, bool>(nameof(IsNavigating));
		UriProperty = AvaloniaProperty.Register<WebView, Uri>(nameof(Uri));
	}

	#endregion

	#region Properties

	public bool CanGoBack => _webViewAdapter?.CanGoBack ?? false;

	public bool CanGoForward => _webViewAdapter?.CanGoForward ?? false;

	public byte[] Favicon => _webViewAdapter?.Favicon;

	public string Title => _webViewAdapter?.Title;

	public bool IsNavigating
	{
		get => GetValue(IsNavigatingProperty);
		set => SetValue(IsNavigatingProperty, value);
	}

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
		return _webViewAdapter.InvokeScriptAsync(script);
	}

	public void Navigate(string uri)
	{
		_webViewAdapter.Navigate(new Uri(uri));
	}

	public void Navigate(Uri uri)
	{
		_webViewAdapter.Navigate(uri);
	}

	public void NavigateToString(string text)
	{
		_webViewAdapter.NavigateToString(text);
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler = this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void Reload()
	{
		_webViewAdapter.Reload();
	}

	public void Stop()
	{
		_webViewAdapter.Stop();
	}

	public Task WaitForNativeHost()
	{
		return _webViewReadyCompletion.Task;
	}

	/// <inheritdoc />
	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		if (_webViewAdapter != null)
		{
			return _webViewAdapter.PlatformHandle;
		}

		_webViewAdapter = CornerstoneApplication
			.DependencyProvider
			.GetInstance<IWebViewAdapter>();

		_webViewAdapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged += WebViewAdapterOnPropertyChanged;
		_webViewReadyCompletion.TrySetResult();
		_webViewAdapter.Uri = Uri;

		return _webViewAdapter.PlatformHandle;
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

		if (_webViewAdapter is null)
		{
			return;
		}

		_webViewReadyCompletion = new TaskCompletionSource();
		_webViewAdapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged -= WebViewAdapterOnPropertyChanged;

		if (_webViewAdapter is IDisposable d)
		{
			d.Dispose();
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = _webViewAdapter?.HandleKeyDown(e.Key, e.KeyModifiers) ?? false;
		base.OnKeyDown(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property != BoundsProperty)
		{
			return;
		}

		var newValue = change.GetNewValue<Rect>();
		var scaling = (float) (VisualRoot?.RenderScaling ?? 1.0f);

		_webViewAdapter?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
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
			case nameof(_webViewAdapter.Title):
			{
				OnPropertyChanged(nameof(Title));
				break;
			}
			case nameof(_webViewAdapter.Uri):
			{
				Uri = _webViewAdapter.Uri;
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