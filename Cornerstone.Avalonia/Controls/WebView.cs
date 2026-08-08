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
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;
using AvaloniaDispatcherPriority = Avalonia.Threading.DispatcherPriority;
using Cornerstone.Avalonia.Converters;
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
/// Cross-platform web view. When <see cref="IsPaused" /> is true, the native surface is hidden
/// and replaced with a snapshot image so Avalonia content can paint over this region.
/// <para>
/// Desktop/mobile use <see cref="NativeControlHost" /> for the platform web engine.
/// Browser (WASM) uses a DOM overlay instead — Avalonia's NativeControlHost path has locked up the UI.
/// </para>
/// </summary>
public class WebView : Grid, IWebView, IDisposable
{
	#region Constants

	public const string DefaultProfileName = "Default";

	#endregion

	#region Fields

	public static readonly StyledProperty<double> BlurRadiusProperty;
	public static readonly StyledProperty<bool> BlurWhenPausedProperty;
	public static readonly StyledProperty<string> ContentProperty;
	public static readonly StyledProperty<bool> IsNavigatingProperty;
	public static readonly StyledProperty<bool> IsPausedProperty;
	public static readonly StyledProperty<bool> ResumeOnResizeProperty;
	public static readonly StyledProperty<Uri> UriProperty;

	private Size _boundsWhenPaused;
	private readonly Border _fallbackBackground;
	#if !BROWSER
	private readonly WebViewNativeHost _nativeHost;
	private Bitmap _placeholderBitmap;
	#endif
	private readonly Image _placeholderImage;
	private int _pauseOperationId;
	private PropertyChangedEventHandler _propertyChangedHandler;
	private bool _suppressNavigationPropertyHandler;
	private bool _suppressResizeResume;
	private IWebViewAdapter _webViewAdapter;
	private TaskCompletionSource _webViewReadyCompletion;
	#if BROWSER
	private Rect _lastOverlayBounds;
	private bool _browserHostQueued;
	#endif

	#endregion

	#region Constructors

	public WebView()
	{
		_webViewReadyCompletion = new();

		Cookies = [];
		Profile = DefaultProfileName;

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

		// Image under the native host in Avalonia Z-order. Native surface paints above Avalonia
		// until SetNativeSurfaceVisible(false) hides it; then the snapshot is visible.
		Children.Add(_fallbackBackground);
		Children.Add(_placeholderImage);

		#if !BROWSER
		_nativeHost = new WebViewNativeHost(this)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Children.Add(_nativeHost);
		#endif
	}

	static WebView()
	{
		BlurRadiusProperty = AvaloniaProperty.Register<WebView, double>(nameof(BlurRadius), 30);
		BlurWhenPausedProperty = AvaloniaProperty.Register<WebView, bool>(nameof(BlurWhenPaused));
		ContentProperty = AvaloniaProperty.Register<WebView, string>(nameof(Content));
		IsNavigatingProperty = AvaloniaProperty.Register<WebView, bool>(nameof(IsNavigating));
		IsPausedProperty = AvaloniaProperty.Register<WebView, bool>(nameof(IsPaused));
		ResumeOnResizeProperty = AvaloniaProperty.Register<WebView, bool>(nameof(ResumeOnResize), true);
		UriProperty = AvaloniaProperty.Register<WebView, Uri>(nameof(Uri));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gaussian blur radius applied to the placeholder when <see cref="BlurWhenPaused" /> is true. Default 30.
	/// </summary>
	public double BlurRadius
	{
		get => GetValue(BlurRadiusProperty);
		set => SetValue(BlurRadiusProperty, value);
	}

	/// <summary>
	/// When true and paused, applies <see cref="BlurEffect" /> to the full-resolution snapshot.
	/// </summary>
	public bool BlurWhenPaused
	{
		get => GetValue(BlurWhenPausedProperty);
		set => SetValue(BlurWhenPausedProperty, value);
	}

	public bool CanGoBack => _webViewAdapter?.CanGoBack ?? false;

	public bool CanGoForward => _webViewAdapter?.CanGoForward ?? false;

	public string Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public PresentationList<WebViewCookie> Cookies { get; }

	public byte[] Favicon => _webViewAdapter?.Favicon;

	/// <summary>
	/// Whether the native web surface is currently painting.
	/// </summary>
	public bool IsNativeSurfaceVisible => _webViewAdapter?.IsNativeSurfaceVisible ?? true;

	public bool IsNavigating
	{
		get => GetValue(IsNavigatingProperty);
		set => SetValue(IsNavigatingProperty, value);
	}

	/// <summary>
	/// When true, freezes the page as a snapshot image and hides the native web surface so
	/// Avalonia controls can appear over this region. When false, restores the live WebView.
	/// On Browser, pause hides the DOM overlay (no snapshot).
	/// </summary>
	public bool IsPaused
	{
		get => GetValue(IsPausedProperty);
		set => SetValue(IsPausedProperty, value);
	}

	public string Profile { get; set; }

	/// <summary>
	/// When true (default), a significant size change while paused restores the live WebView.
	/// </summary>
	public bool ResumeOnResize
	{
		get => GetValue(ResumeOnResizeProperty);
		set => SetValue(ResumeOnResizeProperty, value);
	}

	public string Title => _webViewAdapter?.Title;

	public Uri Uri
	{
		get => GetValue(UriProperty);
		set => SetValue(UriProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Captures the currently visible web surface as a PNG.
	/// </summary>
	public async Task<WebViewSnapshot> CaptureSnapshotAsync(WebViewSnapshotOptions options = null)
	{
		await WaitForNativeHost().ConfigureAwait(true);

		if (_webViewAdapter == null)
		{
			return WebViewSnapshot.Failed("WebView adapter is not available.");
		}

		return await _webViewAdapter.CaptureSnapshotAsync(options).ConfigureAwait(true);
	}

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

	public Task WaitForNativeHost()
	{
		return _webViewReadyCompletion.Task;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		ClearPlaceholderBitmap();
		#if BROWSER
		DetachBrowserOverlay();
		#endif

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
		_webViewAdapter = null;
	}

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

		if (change.Property == IsPausedProperty)
		{
			_ = ApplyPausedStateAsync(change.GetNewValue<bool>());
		}
		else if ((change.Property == BlurWhenPausedProperty) || (change.Property == BlurRadiusProperty))
		{
			if (IsPaused)
			{
				UpdatePlaceholderBlur();
			}
		}
		else if (change.Property == IsVisibleProperty)
		{
			#if BROWSER
			UpdateBrowserOverlayVisibility();
			#endif
		}

		base.OnPropertyChanged(change);

		if (change.Property == BoundsProperty)
		{
			var newValue = change.GetNewValue<Rect>();

			// Do not resize/show the native HWND while paused — Avalonia + adapter would re-open airspace.
			if (!IsPaused && (_webViewAdapter?.IsNativeSurfaceVisible != false))
			{
				const float scaling = 1.0f;
				_webViewAdapter?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
			}

			HandleBoundsChanged(newValue.Size);
			#if BROWSER
			UpdateBrowserOverlayBounds();
			#endif
		}
	}

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

	private void ClearPlaceholderBitmap()
	{
		_placeholderImage.Source = null;
		#if !BROWSER
		_placeholderBitmap?.Dispose();
		_placeholderBitmap = null;
		#endif
	}

	#if !BROWSER
	private IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		return EnsureAdapterInitialized();
	}
	#endif

	#if BROWSER
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
		_webViewReadyCompletion.TrySetResult();

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

		// If IsPaused was set before the host was ready, apply once ready.
		if (IsPaused)
		{
			_ = ApplyPausedStateAsync(true);
		}

		return _webViewAdapter.PlatformHandle;
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
			// Note: tbd - should we unpause on resize? Not now, maybe later.
			//IsPaused = false;
		}
	}

	private async Task PauseCoreAsync(int operationId)
	{
		_suppressResizeResume = true;
		try
		{
			#if BROWSER
			// Browser: no snapshot support; hide the DOM overlay so Avalonia can paint over the slot.
			_ = operationId;
			ClearPlaceholderBitmap();
			_placeholderImage.IsVisible = false;
			_fallbackBackground.Background = ResourceService.GetColorAsBrush("Background03");
			_fallbackBackground.IsVisible = true;
			_webViewAdapter?.SetNativeSurfaceVisible(false);
			OnPropertyChanged(nameof(IsNativeSurfaceVisible));
			_boundsWhenPaused = Bounds.Size;
			await Task.CompletedTask.ConfigureAwait(true);
			#else
			// Always capture full resolution; blur is applied via Effect on the image.
			var snapshot = await CaptureSnapshotAsync(WebViewSnapshotOptions.Default()).ConfigureAwait(true);
			if (operationId != _pauseOperationId)
			{
				return;
			}

			ClearPlaceholderBitmap();

			if (snapshot is { Success: true, PngBytes: { Length: > 0 } })
			{
				_placeholderBitmap = ImageConverters.BytesToBitmap(snapshot.PngBytes);
				_placeholderImage.Source = _placeholderBitmap;
				_placeholderImage.Opacity = 1.0;
				UpdatePlaceholderBlur();
				_placeholderImage.IsVisible = true;
				_fallbackBackground.IsVisible = false;
			}
			else
			{
				_placeholderImage.Effect = null;
				_placeholderImage.IsVisible = false;
				_fallbackBackground.Background = ResourceService.GetColorAsBrush("Background03");
				_fallbackBackground.IsVisible = true;
			}

			// Avalonia NativeControlHost re-shows the HWND on layout via ShowInBounds unless the
			// host is not effectively visible (then it calls HideWithSize). Hiding only the
			// platform control is not enough on Windows.
			_nativeHost.IsVisible = false;
			_webViewAdapter?.SetNativeSurfaceVisible(false);
			OnPropertyChanged(nameof(IsNativeSurfaceVisible));
			_boundsWhenPaused = Bounds.Size;
			#endif
		}
		finally
		{
			_suppressResizeResume = false;
		}
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

	private void ResumeCore()
	{
		_pauseOperationId++;
		#if !BROWSER
		_nativeHost.IsVisible = true;
		#endif
		_webViewAdapter?.SetNativeSurfaceVisible(true);
		OnPropertyChanged(nameof(IsNativeSurfaceVisible));
		ClearPlaceholderBitmap();
		_placeholderImage.Effect = null;
		_placeholderImage.IsVisible = false;
		_fallbackBackground.IsVisible = false;

		// Re-apply size after the native host is shown again.
		if (Bounds is { Width: > 0, Height: > 0 })
		{
			const float scaling = 1.0f;
			_webViewAdapter?.HandleResize((int) (Bounds.Width * scaling), (int) (Bounds.Height * scaling), scaling);
		}

		#if BROWSER
		UpdateBrowserOverlayBounds();
		#endif
	}

	#if BROWSER
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

	private void UpdatePlaceholderBlur()
	{
		if (BlurWhenPaused && (BlurRadius > 0))
		{
			_placeholderImage.Effect = new BlurEffect { Radius = BlurRadius };
		}
		else
		{
			_placeholderImage.Effect = null;
		}
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

	#if !BROWSER
	#region Classes

	/// <summary>
	/// Thin native host so <see cref="WebView" /> can own both the snapshot image and the native surface.
	/// </summary>
	private sealed class WebViewNativeHost : NativeControlHost
	{
		#region Fields

		private readonly WebView _owner;

		#endregion

		#region Constructors

		public WebViewNativeHost(WebView owner)
		{
			_owner = owner;
		}

		#endregion

		#region Methods

		protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
		{
			return _owner.CreateNativeControlCore(parent);
		}

		protected override void DestroyNativeControlCore(IPlatformHandle control)
		{
			// Keep adapter alive across temporary host teardown; WebView.Dispose owns lifetime.
			base.DestroyNativeControlCore(control);
		}

		#endregion
	}

	#endregion
	#endif
}
