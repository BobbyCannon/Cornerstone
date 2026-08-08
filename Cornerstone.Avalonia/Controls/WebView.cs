#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;
using AvaloniaDispatcherPriority = Avalonia.Threading.DispatcherPriority;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
#if BROWSER
using BrowserWebViewAdapter = Cornerstone.Avalonia.Platforms.Browser.WebViewAdapter;
#endif

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Cross-platform web view. When paused, the native surface is hidden so Avalonia content can
/// paint over this region (snapshot underlay on desktop/mobile; DOM overlay hide on Browser).
/// <para>
/// Desktop/mobile use <see cref="PausableNativeHost" /> for the platform web engine.
/// Browser (WASM) uses a DOM overlay instead — Avalonia's NativeControlHost path has locked up the UI.
/// </para>
/// </summary>
#if BROWSER
public class WebView : Grid, IWebView, INativeHostPausable, IDisposable
#else
public class WebView : PausableNativeHost, IWebView, INativeHostPausable, IDisposable
#endif
{
	#region Constants

	public const string DefaultProfileName = "Default";

	#endregion

	#region Fields

	public static readonly StyledProperty<string> ContentProperty =
		AvaloniaProperty.Register<WebView, string>(nameof(Content));

	public static readonly StyledProperty<bool> IsNavigatingProperty =
		AvaloniaProperty.Register<WebView, bool>(nameof(IsNavigating));

	public static readonly StyledProperty<Uri> UriProperty =
		AvaloniaProperty.Register<WebView, Uri>(nameof(Uri));

	#if BROWSER
	public static readonly StyledProperty<double> BlurRadiusProperty =
		AvaloniaProperty.Register<WebView, double>(nameof(BlurRadius), 30);

	public static readonly StyledProperty<bool> BlurWhenPausedProperty =
		AvaloniaProperty.Register<WebView, bool>(nameof(BlurWhenPaused));

	public static readonly StyledProperty<bool> IsPausedProperty =
		AvaloniaProperty.Register<WebView, bool>(nameof(IsPaused));

	public static readonly StyledProperty<bool> ResumeOnResizeProperty =
		AvaloniaProperty.Register<WebView, bool>(nameof(ResumeOnResize), true);

	private Size _boundsWhenPaused;
	private readonly Border _fallbackBackground;
	private readonly Image _placeholderImage;
	private int _pauseOperationId;
	private bool _suppressResizeResume;
	private TaskCompletionSource _webViewReadyCompletion = new();
	private Rect _lastOverlayBounds;
	private bool _browserHostQueued;
	#endif

	private PropertyChangedEventHandler _propertyChangedHandler;
	private bool _suppressNavigationPropertyHandler;
	private IWebViewAdapter _webViewAdapter;

	#endregion

	#region Constructors

	public WebView()
	{
		Cookies = [];
		Profile = DefaultProfileName;

		#if BROWSER
		_fallbackBackground = new Border
		{
			IsVisible = false,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		_placeholderImage = new Image
		{
			Stretch = Stretch.Fill,
			IsVisible = false,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		Children.Add(_fallbackBackground);
		Children.Add(_placeholderImage);
		#endif
	}

	#endregion

	#region Properties

	#if BROWSER
	/// <summary>
	/// Gaussian blur radius applied when <see cref="BlurWhenPaused" /> is true. Default 30.
	/// </summary>
	public double BlurRadius
	{
		get => GetValue(BlurRadiusProperty);
		set => SetValue(BlurRadiusProperty, value);
	}

	/// <summary>
	/// When true and paused, applies blur to the placeholder. Browser has no snapshot; property is retained for API parity.
	/// </summary>
	public bool BlurWhenPaused
	{
		get => GetValue(BlurWhenPausedProperty);
		set => SetValue(BlurWhenPausedProperty, value);
	}
	#endif

	public bool CanGoBack => _webViewAdapter?.CanGoBack ?? false;

	public bool CanGoForward => _webViewAdapter?.CanGoForward ?? false;

	public string Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public PresentationList<WebViewCookie> Cookies { get; }

	public byte[] Favicon => _webViewAdapter?.Favicon;

	#if BROWSER
	/// <summary>
	/// Whether the native web surface is currently painting.
	/// </summary>
	public bool IsNativeSurfaceVisible => _webViewAdapter?.IsNativeSurfaceVisible ?? true;
	#endif

	public bool IsNavigating
	{
		get => GetValue(IsNavigatingProperty);
		set => SetValue(IsNavigatingProperty, value);
	}

	#if BROWSER
	/// <summary>
	/// When true, hides the DOM overlay so Avalonia content can appear over this region.
	/// </summary>
	public bool IsPaused
	{
		get => GetValue(IsPausedProperty);
		set => SetValue(IsPausedProperty, value);
	}
	#endif

	public string Profile { get; set; }

	#if BROWSER
	/// <summary>
	/// When true (default), a significant size change while paused can restore the live surface.
	/// Auto-resume is currently disabled; property is retained for API compatibility.
	/// </summary>
	public bool ResumeOnResize
	{
		get => GetValue(ResumeOnResizeProperty);
		set => SetValue(ResumeOnResizeProperty, value);
	}
	#endif

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

	#if BROWSER
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endif

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

		#if BROWSER
		// Do not create DOM/JS synchronously during tab switch — queue host ensure.
		if (_webViewAdapter == null)
		{
			QueueBrowserHostEnsure();
			return;
		}
		#endif

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

		#if BROWSER
		if (_webViewAdapter == null)
		{
			QueueBrowserHostEnsure();
			return text;
		}
		#endif

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

	#if BROWSER
	public Task WaitForNativeHost()
	{
		return _webViewReadyCompletion.Task;
	}

	/// <summary>
	/// Captures the currently visible web surface as a PNG.
	/// </summary>
	public async Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		await WaitForNativeHost().ConfigureAwait(true);

		if (_webViewAdapter == null)
		{
			return NativeSurfaceSnapshot.Failed("WebView adapter is not available.");
		}

		return await _webViewAdapter.CaptureSnapshotAsync(options).ConfigureAwait(true);
	}
	#endif

	#if !BROWSER
	/// <inheritdoc />
	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		return EnsureAdapterInitialized();
	}

	/// <inheritdoc />
	protected override void DestroyNativeControlCore(IPlatformHandle control)
	{
		// Keep adapter alive across temporary host teardown; Dispose owns lifetime.
		// NestedNativeHost still runs Avalonia's DestroyNativeControlCore for attachment cleanup.
	}

	/// <inheritdoc />
	protected override IPausableNativeSurface GetSurface()
	{
		return _webViewAdapter;
	}
	#endif

	#if BROWSER
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		DetachBrowserOverlay();
		ReleaseAdapter();
	}
	#else
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ReleaseAdapter();
		}

		base.Dispose(disposing);
	}
	#endif

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		#if BROWSER
		// Defer host work so tab switch / layout can finish first on the WASM UI thread.
		QueueBrowserHostEnsure();
		#endif
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		#if BROWSER
		_browserHostQueued = false;
		DetachBrowserOverlay();
		#endif
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = _webViewAdapter?.HandleKeyDown(e.Key, e.KeyModifiers) ?? false;
		base.OnKeyDown(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
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

		#if BROWSER
		if (change.Property == IsPausedProperty)
		{
			_ = ApplyPausedStateAsync(change.GetNewValue<bool>());
		}
		else if (change.Property == IsVisibleProperty)
		{
			UpdateBrowserOverlayVisibility();
		}
		#endif

		base.OnPropertyChanged(change);

		#if BROWSER
		if (change.Property == BoundsProperty)
		{
			var newValue = change.GetNewValue<Rect>();

			if (!IsPaused && (_webViewAdapter?.IsNativeSurfaceVisible != false))
			{
				const float scaling = 1.0f;
				_webViewAdapter?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
			}

			HandleBoundsChanged(newValue.Size);
			UpdateBrowserOverlayBounds();
		}
		#endif
	}

	#if BROWSER
	private async Task ApplyPausedStateAsync(bool paused)
	{
		var operationId = ++_pauseOperationId;

		if (paused)
		{
			await PauseCoreAsync(operationId).ConfigureAwait(true);
		}
		else
		{
			ResumeCore();
		}
	}

	private void DetachBrowserOverlay()
	{
		if (_webViewAdapter is BrowserWebViewAdapter browserAdapter)
		{
			browserAdapter.DetachOverlay();
		}
	}

	private void EnsureBrowserAdapter()
	{
		if (!this.IsAttachedToVisualTree())
		{
			return;
		}

		EnsureAdapterInitialized();

		if (_webViewAdapter is BrowserWebViewAdapter browserAdapter)
		{
			browserAdapter.AttachOverlay();
			UpdateBrowserOverlayBounds();
			UpdateBrowserOverlayVisibility();
		}
	}

	private void HandleBoundsChanged(Size newSize)
	{
		if (_suppressResizeResume || !IsPaused || !ResumeOnResize)
		{
			return;
		}

		const double epsilon = 1.0;
		if ((Math.Abs(newSize.Width - _boundsWhenPaused.Width) > epsilon)
			|| (Math.Abs(newSize.Height - _boundsWhenPaused.Height) > epsilon))
		{
			// Auto-resume on resize is currently disabled.
			//IsPaused = false;
		}
	}

	private async Task PauseCoreAsync(int operationId)
	{
		_suppressResizeResume = true;
		try
		{
			// Browser: no snapshot support; hide the DOM overlay so Avalonia can paint over the slot.
			_ = operationId;
			_placeholderImage.Source = null;
			_placeholderImage.IsVisible = false;
			_fallbackBackground.Background = ResourceService.GetColorAsBrush("Background03");
			_fallbackBackground.IsVisible = true;
			_webViewAdapter?.SetNativeSurfaceVisible(false);
			OnPropertyChanged(nameof(IsNativeSurfaceVisible));
			_boundsWhenPaused = Bounds.Size;
			await Task.CompletedTask.ConfigureAwait(true);
		}
		finally
		{
			_suppressResizeResume = false;
		}
	}

	private void QueueBrowserHostEnsure()
	{
		if (_browserHostQueued)
		{
			return;
		}

		_browserHostQueued = true;
		AvaloniaDispatcher.UIThread.Post(() =>
		{
			_browserHostQueued = false;
			if (!this.IsAttachedToVisualTree())
			{
				return;
			}

			try
			{
				EnsureBrowserAdapter();
			}
			catch (Exception ex)
			{
				Console.WriteLine("WebView browser host ensure failed: " + ex);
			}
		}, AvaloniaDispatcherPriority.Background);
	}

	private void ResumeCore()
	{
		_pauseOperationId++;
		_webViewAdapter?.SetNativeSurfaceVisible(true);
		OnPropertyChanged(nameof(IsNativeSurfaceVisible));
		_placeholderImage.Source = null;
		_placeholderImage.Effect = null;
		_placeholderImage.IsVisible = false;
		_fallbackBackground.IsVisible = false;

		if (Bounds is { Width: > 0, Height: > 0 })
		{
			const float scaling = 1.0f;
			_webViewAdapter?.HandleResize((int) (Bounds.Width * scaling), (int) (Bounds.Height * scaling), scaling);
		}

		UpdateBrowserOverlayBounds();
	}

	private void UpdateBrowserOverlayBounds()
	{
		if (_webViewAdapter is not BrowserWebViewAdapter browserAdapter)
		{
			return;
		}

		if (!this.IsAttachedToVisualTree() || (Bounds.Width <= 0) || (Bounds.Height <= 0))
		{
			return;
		}

		try
		{
			var topLeft = this.PointToScreen(new Point(0, 0));
			var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
			// PointToScreen returns physical pixels; CSS overlay uses CSS pixels.
			var x = topLeft.X / scale;
			var y = topLeft.Y / scale;
			var width = Bounds.Width;
			var height = Bounds.Height;
			var next = new Rect(x, y, width, height);

			// Avoid redundant JS + any chance of layout thrash.
			const double epsilon = 0.5;
			if ((Math.Abs(next.X - _lastOverlayBounds.X) < epsilon)
				&& (Math.Abs(next.Y - _lastOverlayBounds.Y) < epsilon)
				&& (Math.Abs(next.Width - _lastOverlayBounds.Width) < epsilon)
				&& (Math.Abs(next.Height - _lastOverlayBounds.Height) < epsilon))
			{
				return;
			}

			_lastOverlayBounds = next;
			browserAdapter.SetOverlayBounds(x, y, width, height);
		}
		catch
		{
			// Visual may not be ready for screen transform yet.
		}
	}

	private void UpdateBrowserOverlayVisibility()
	{
		if (_webViewAdapter is not BrowserWebViewAdapter)
		{
			return;
		}

		// IsVisible on the Avalonia control; pause uses SetNativeSurfaceVisible separately.
		if (!IsVisible || IsPaused)
		{
			_webViewAdapter.SetNativeSurfaceVisible(false);
		}
		else
		{
			_webViewAdapter.SetNativeSurfaceVisible(true);
		}
	}
	#endif

	private IPlatformHandle EnsureAdapterInitialized()
	{
		if (_webViewAdapter != null)
		{
			return _webViewAdapter.PlatformHandle;
		}

		_webViewAdapter = AppBootstrap.GetInstance<IWebViewAdapter>();

		_webViewAdapter.Initialize(Profile);
		_webViewAdapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged += WebViewAdapterOnPropertyChanged;

		#if BROWSER
		_webViewReadyCompletion.TrySetResult();
		#endif

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

		#if BROWSER
		// If IsPaused was set before the host was ready, apply once ready.
		if (IsPaused)
		{
			_ = ApplyPausedStateAsync(true);
		}
		#endif

		return _webViewAdapter.PlatformHandle;
	}

	private void RefreshCookies()
	{
		if (_webViewAdapter == null)
		{
			return;
		}

		_webViewAdapter
			.GetCookiesAsync()
			.ContinueWith(x =>
			{
				if (x.IsFaulted || x.IsCanceled)
				{
					return;
				}

				CornerstoneApplication
					.CornerstoneDispatcher
					.Dispatch(() => Cookies.Load(x.Result));
			});
	}

	private void ReleaseAdapter()
	{
		if (_webViewAdapter is null)
		{
			return;
		}

		#if !BROWSER
		ResetNativeHostReady();
		#else
		_webViewReadyCompletion = new TaskCompletionSource();
		#endif

		_webViewAdapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
		_webViewAdapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
		_webViewAdapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
		_webViewAdapter.PropertyChanged -= WebViewAdapterOnPropertyChanged;

		DisposableExtensions.TryDispose(_webViewAdapter);
		_webViewAdapter = null;
	}

	private void WebViewAdapterOnNavigationCompleted(object sender, WebViewNavigationEventArgs e)
	{
		RefreshCookies();

		CornerstoneApplication.CornerstoneDispatcher
			.Dispatch(() =>
			{
				IsNavigating = false;
				OnPropertyChanged(nameof(Uri));
				#if !BROWSER
				// Page finished loading — refresh freeze-frame underlay for snappy pause later.
				if (!IsPaused)
				{
					RequestWarmUnderlay();
				}
				#endif
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
				CornerstoneApplication.CornerstoneDispatcher.Dispatch(() =>
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