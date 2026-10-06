using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Runtime;
using Key = Cornerstone.Presentation.Input.Key;
using Cornerstone.Presentation.Controls.Web;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Platforms.Desktop;

/// <summary>
/// Linux WebKit2GTK hosted in NativeControlHost. X11: child XID reparent.
/// Wayland: hidden GtkWindow pixbuf copied into the invert SHM subsurface
/// (same client cannot parent a second wl_display's surface).
/// </summary>
internal sealed class LinuxWebViewAdapter : CornerstoneObject, IWebViewAdapter, IDisposable
{
	private bool _disposed;
	private readonly IntPtr _gtkWindow;
	private readonly IntPtr _webView;
	private readonly LinuxWebKit.LoadChangedHandler _loadChanged;
	private readonly DispatcherTimer _pump;
	private IPlatformHandle _platformHandle;
	private INativePixelBufferHost _pixelHost;
	private InputElement _inputHost;
	private LinuxWebViewTextInputMethodClient _imeClient;
	private Uri _uri;
	private int _pixelWidth = 1;
	private int _pixelHeight = 1;

	[DependencyInjectionConstructor]
	public LinuxWebViewAdapter()
	{
		if (!LinuxWebKit.TryInitialize(out var error))
			throw new WebViewUnavailableForLinuxException(error);

		_gtkWindow = LinuxWebKit.CreateHiddenHostWindow();
		_webView = LinuxWebKit.CreateWebView();
		if (_gtkWindow == IntPtr.Zero || _webView == IntPtr.Zero)
			throw new WebViewUnavailableForLinuxException(
				"WebKitGTK loaded but webkit_web_view_new failed. Check libwebkit2gtk and libgtk-3 packages.");

		LinuxWebKit.DisableHardwareAcceleration(_webView);
		LinuxWebKit.gtk_widget_set_can_focus(_webView, true);
		LinuxWebKit.gtk_container_add(_gtkWindow, _webView);
		LinuxWebKit.gtk_widget_set_size_request(_webView, 1, 1);
		LinuxWebKit.gtk_widget_set_size_request(_gtkWindow, 1, 1);
		LinuxWebKit.MapHiddenHostWindow(_gtkWindow);
		LinuxWebKit.gtk_widget_realize(_webView);
		LinuxWebKit.Pump();

		_loadChanged = OnLoadChanged;
		LinuxWebKit.ConnectLoadChanged(_webView, _loadChanged);

		_uri = new Uri("about:blank");
		_platformHandle = CreateX11Handle();

		_pump = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(16)
		};
		_pump.Tick += OnPumpTick;
		_pump.Start();
	}

	public bool CanGoBack => LinuxWebKit.CanGoBack(_webView);

	public bool CanGoForward => LinuxWebKit.CanGoForward(_webView);

	public string Content => GetContent();

	public byte[] Favicon => null;

	public bool IsNativeSurfaceVisible { get; private set; } = true;

	public IPlatformHandle PlatformHandle => _platformHandle;

	public string Title => LinuxWebKit.GetTitle(_webView);

	public Uri Uri
	{
		get => _uri;
		set => Navigate(value);
	}

	public IPlatformHandle AttachToHost(IPlatformHandle parent, InputElement inputHost)
	{
		AttachInput(inputHost);
		return AttachNativeParent(parent);
	}

	public void DetachFromHost()
	{
		DetachInput();
	}

	public IPlatformHandle AttachNativeParent(IPlatformHandle parent)
	{
		if (parent is INativePixelBufferHost pixels)
		{
			_pixelHost = pixels;
			_pixelHost.SetUsesExternalBuffers(true);
			_platformHandle = parent;
			BlitToSubsurface();
			return parent;
		}

		_platformHandle = CreateX11Handle() ?? parent;
		return _platformHandle;
	}

	public void AttachInput(InputElement host)
	{
		if (ReferenceEquals(_inputHost, host))
			return;
		DetachInput();
		_inputHost = host;
		if (host == null)
			return;
		host.Focusable = true;
		_imeClient = new LinuxWebViewTextInputMethodClient(host);
		host.AddHandler(InputElement.PointerPressedEvent, OnHostPointerPressed, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.PointerReleasedEvent, OnHostPointerReleased, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.PointerMovedEvent, OnHostPointerMoved, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.PointerWheelChangedEvent, OnHostPointerWheel, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.KeyDownEvent, OnHostKeyDown, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.KeyUpEvent, OnHostKeyUp, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.TextInputEvent, OnHostTextInput, RoutingStrategies.Bubble, true);
		host.AddHandler(InputElement.TextInputMethodClientRequestedEvent, OnImeClientRequested, RoutingStrategies.Bubble,
			true);
	}

	public Task ClearBrowsingDataAsync() => Task.CompletedTask;

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
			return;
		_disposed = true;
		_pump.Stop();
		_pump.Tick -= OnPumpTick;
		DetachInput();
		if (_gtkWindow != IntPtr.Zero)
			LinuxWebKit.gtk_widget_destroy(_gtkWindow);
	}

	public IEnumerable<string> GetAvailableProfiles() => [];

	public string GetContent() => string.Empty;

	public Task<IEnumerable<WebViewCookie>> GetCookiesAsync() =>
		Task.FromResult((IEnumerable<WebViewCookie>) []);

	public bool GoBack() => LinuxWebKit.GoBack(_webView);

	public bool GoForward() => LinuxWebKit.GoForward(_webView);

	public bool HandleKeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.F5)
		{
			Reload();
			return true;
		}

		if (key == Key.BrowserBack && CanGoBack)
			return GoBack();
		if (key == Key.BrowserForward && CanGoForward)
			return GoForward();
		return false;
	}

	public void HandleResize(int width, int height, float zoom)
	{
		_pixelWidth = Math.Max(1, width);
		_pixelHeight = Math.Max(1, height);
		LinuxWebKit.ResizeHiddenHostWindow(_gtkWindow, _webView, _pixelWidth, _pixelHeight);
		PresentFromGtk();
	}

	public void Initialize(string profileName)
	{
	}

	public Task<string> InvokeScriptAsync(string script) => Task.FromResult(string.Empty);

	public void Navigate(Uri uri)
	{
		if (uri == null)
			return;
		_uri = uri;
		LinuxWebKit.LoadUri(_webView, uri.ToString());
		NotifyComputedPropertyChanged(nameof(Uri));
		NavigationStarted?.Invoke(this, new WebViewNavigationEventArgs { Uri = _uri });
	}

	public string NavigateToString(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		_uri = new Uri("about:blank");
		LinuxWebKit.LoadHtml(_webView, text, "about:blank");
		NotifyComputedPropertyChanged(nameof(Uri));
		return text;
	}

	public void Reload() => LinuxWebKit.Reload(_webView);

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		if (visible)
			LinuxWebKit.gtk_widget_show(_webView);
		else
			LinuxWebKit.gtk_widget_hide(_webView);
	}

	public void Stop() => LinuxWebKit.Stop(_webView);

	void IWebViewAdapter.Initialize(string profileName)
	{
	}

	#pragma warning disable CS0067
	public event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;
	public event EventHandler<WebViewNavigationEventArgs> NavigationStarted;
	public event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;
	#pragma warning restore CS0067

	private void OnLoadChanged(IntPtr webView, int loadEvent, IntPtr userData)
	{
		if (loadEvent == LinuxWebKit.LoadFinished)
		{
			var uri = LinuxWebKit.GetUri(_webView);
			if (!string.IsNullOrEmpty(uri) && Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
				_uri = parsed;
			NavigationCompleted?.Invoke(this, new WebViewNavigationEventArgs { Uri = _uri });
			NotifyComputedPropertyChanged(nameof(Title));
			NotifyComputedPropertyChanged(nameof(CanGoBack));
			NotifyComputedPropertyChanged(nameof(CanGoForward));
			PresentFromGtk();
		}
	}

	private void OnPumpTick(object sender, EventArgs e)
	{
		PresentFromGtk();
	}

	private void PresentFromGtk()
	{
		if (_webView == IntPtr.Zero)
			return;
		LinuxWebKit.ForcePaint(_webView);
		LinuxWebKit.Pump();
		if (_pixelHost != null)
			BlitToSubsurface();
	}

	private void BlitToSubsurface()
	{
		if (_pixelHost == null || _webView == IntPtr.Zero)
			return;
		var dest = LinuxWebKit.CaptureWidgetBgra(_webView, _pixelWidth, _pixelHeight, out var width, out var height);
		if (dest == null || dest.Length < width * height * 4)
			return;
		_pixelHost.AttachBgra(dest, width, height);
	}

	private void DetachInput()
	{
		if (_inputHost == null)
			return;
		_inputHost.RemoveHandler(InputElement.PointerPressedEvent, OnHostPointerPressed);
		_inputHost.RemoveHandler(InputElement.PointerReleasedEvent, OnHostPointerReleased);
		_inputHost.RemoveHandler(InputElement.PointerMovedEvent, OnHostPointerMoved);
		_inputHost.RemoveHandler(InputElement.PointerWheelChangedEvent, OnHostPointerWheel);
		_inputHost.RemoveHandler(InputElement.KeyDownEvent, OnHostKeyDown);
		_inputHost.RemoveHandler(InputElement.KeyUpEvent, OnHostKeyUp);
		_inputHost.RemoveHandler(InputElement.TextInputEvent, OnHostTextInput);
		_inputHost.RemoveHandler(InputElement.TextInputMethodClientRequestedEvent, OnImeClientRequested);
		_inputHost = null;
		_imeClient = null;
	}

	private void OnHostPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (_pixelHost == null)
			return;
		var (x, y, state) = HostPointer(e);
		var button = GtkButton(e);
		if (button == 0)
			return;
		LinuxWebKit.gtk_widget_grab_focus(_webView);
		_inputHost?.Focus();
		LinuxWebKit.SendButton(_webView, x, y, button, true, state);
		e.Handled = true;
		PresentFromGtk();
	}

	private void OnHostPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		if (_pixelHost == null)
			return;
		var (x, y, state) = HostPointer(e);
		var button = e.InitialPressMouseButton switch
		{
			MouseButton.Left => 1u,
			MouseButton.Middle => 2u,
			MouseButton.Right => 3u,
			_ => GtkButton(e)
		};
		if (button == 0)
			button = 1;
		LinuxWebKit.SendButton(_webView, x, y, button, false, state);
		e.Handled = true;
		PresentFromGtk();
	}

	private void OnHostPointerMoved(object sender, PointerEventArgs e)
	{
		if (_pixelHost == null)
			return;
		var (x, y, state) = HostPointer(e);
		LinuxWebKit.SendMotion(_webView, x, y, state);
	}

	private void OnHostPointerWheel(object sender, PointerWheelEventArgs e)
	{
		if (_pixelHost == null)
			return;
		var (x, y, state) = HostPointer(e);
		// Cornerstone 1.0 is one mouse notch. GDK_SCROLL_SMOOTH 1.0 is one notch;
		// WebKit then converts that to line pixels. Do not scale here.
		LinuxWebKit.SendScroll(_webView, x, y, -e.Delta.X, -e.Delta.Y, state);
		e.Handled = true;
		PresentFromGtk();
	}

	private void OnHostKeyDown(object sender, KeyEventArgs e)
	{
		if (_pixelHost == null)
			return;
		if (HandleKeyDown(e.Key, e.KeyModifiers))
		{
			e.Handled = true;
			return;
		}

		var keyval = ToGdkKeyval(e);
		if (keyval == 0)
			return;
		LinuxWebKit.SendKey(_webView, keyval, true, GtkState(e.KeyModifiers));
		e.Handled = true;
		PresentFromGtk();
	}

	private void OnHostKeyUp(object sender, KeyEventArgs e)
	{
		if (_pixelHost == null)
			return;
		var keyval = ToGdkKeyval(e);
		if (keyval == 0)
			return;
		LinuxWebKit.SendKey(_webView, keyval, false, GtkState(e.KeyModifiers));
		e.Handled = true;
		PresentFromGtk();
	}

	private void OnHostTextInput(object sender, TextInputEventArgs e)
	{
		if (_pixelHost == null || string.IsNullOrEmpty(e.Text))
			return;
		foreach (var ch in e.Text)
		{
			uint keyval;
			if (ch == '\b')
				keyval = 0xff08;
			else if (ch == '\r')
				keyval = 0xff0d;
			else if (ch == '\t')
				keyval = 0xff09;
			else if (ch == '\x1b')
				keyval = 0xff1b;
			else if (ch == '\x7f')
				keyval = 0xffff;
			else if (char.IsControl(ch))
				keyval = 0;
			else
				keyval = LinuxWebKit.gdk_unicode_to_keyval(ch);
			if (keyval == 0)
				continue;
			LinuxWebKit.SendKey(_webView, keyval, true, 0);
			LinuxWebKit.SendKey(_webView, keyval, false, 0);
		}

		e.Handled = true;
		PresentFromGtk();
	}

	private void OnImeClientRequested(object sender, TextInputMethodClientRequestedEventArgs e)
	{
		e.Client = _imeClient;
	}

	private (double x, double y, uint state) HostPointer(PointerEventArgs e)
	{
		var pos = e.GetPosition(_inputHost);
		var scale = TopLevel.GetTopLevel(_inputHost)?.RenderScaling ?? 1;
		var x = pos.X * scale;
		var y = pos.Y * scale;
		var state = GtkState(e.KeyModifiers);
		var props = e.GetCurrentPoint(_inputHost).Properties;
		if (props.IsLeftButtonPressed)
			state |= 1u << 8;
		if (props.IsMiddleButtonPressed)
			state |= 1u << 9;
		if (props.IsRightButtonPressed)
			state |= 1u << 10;
		return (x, y, state);
	}

	private static uint GtkButton(PointerEventArgs e)
	{
		var props = e.GetCurrentPoint(null).Properties;
		return props.PointerUpdateKind switch
		{
			PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => 1,
			PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => 2,
			PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => 3,
			_ when props.IsLeftButtonPressed => 1,
			_ when props.IsMiddleButtonPressed => 2,
			_ when props.IsRightButtonPressed => 3,
			_ => 0
		};
	}

	private static uint GtkState(KeyModifiers modifiers)
	{
		uint state = 0;
		if ((modifiers & KeyModifiers.Shift) != 0)
			state |= 1;
		if ((modifiers & KeyModifiers.Control) != 0)
			state |= 4;
		if ((modifiers & KeyModifiers.Alt) != 0)
			state |= 8;
		return state;
	}

	private static uint ToGdkKeyval(KeyEventArgs e)
	{
		// Named keys first. Wayland KeySymbol for Back is "\b" (length 1);
		// gdk_unicode_to_keyval(8) is not GDK_KEY_BackSpace (0xff08).
		switch (e.Key)
		{
			case Key.Return:
				return 0xff0d;
			case Key.Back:
				return 0xff08;
			case Key.Tab:
				return 0xff09;
			case Key.Escape:
				return 0xff1b;
			case Key.Delete:
				return 0xffff;
			case Key.Left:
				return 0xff51;
			case Key.Up:
				return 0xff52;
			case Key.Right:
				return 0xff53;
			case Key.Down:
				return 0xff54;
			case Key.Home:
				return 0xff50;
			case Key.End:
				return 0xff57;
			case Key.PageUp:
				return 0xff55;
			case Key.PageDown:
				return 0xff56;
			case Key.Space:
				return 0x20;
		}

		if (string.IsNullOrEmpty(e.KeySymbol) || e.KeySymbol.Length != 1)
			return 0;
		var ch = e.KeySymbol[0];
		if (char.IsControl(ch))
			return 0;
		return LinuxWebKit.gdk_unicode_to_keyval(ch);
	}

	private IPlatformHandle CreateX11Handle()
	{
		var window = LinuxWebKit.gtk_widget_get_window(_webView);
		if (window == IntPtr.Zero)
			return null;
		var xid = LinuxWebKit.gdk_x11_window_get_xid(window);
		if (xid == IntPtr.Zero)
			return null;
		return new PlatformHandle(xid, "XID");
	}

	private sealed class LinuxWebViewTextInputMethodClient : TextInputMethodClient
	{
		private readonly InputElement _host;

		public LinuxWebViewTextInputMethodClient(InputElement host)
		{
			_host = host;
		}

		public override Visual TextViewVisual => _host;

		public override bool SupportsPreedit => false;

		public override bool SupportsSurroundingText => false;

		public override string SurroundingText => string.Empty;

		public override Rect CursorRectangle => new(0, 0, 1, 1);

		public override TextSelection Selection
		{
			get => default;
			set { }
		}
	}
}
