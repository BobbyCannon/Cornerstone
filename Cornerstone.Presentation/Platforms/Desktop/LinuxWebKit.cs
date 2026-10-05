using System;
using System.Runtime.InteropServices;

namespace Cornerstone.Presentation.Platforms.Desktop;

/// <summary>
/// P/Invoke for GTK3 + WebKit2GTK 4.1/4.0 (Linux WebView). GTK is forced to the X11
/// backend so XID embedding works; Wayland invert copies pixbufs from a hidden
/// GtkWindow into the Cornerstone subsurface.
/// GtkOffscreenWindow is not used: WebKitGTK 2.48 crashes there
/// (gdk_window_get_origin GDK_IS_WINDOW / ToplevelWindow.isInMonitor).
/// </summary>
internal static unsafe class LinuxWebKit
{
	private const string Gtk = "libgtk-3.so.0";
	private const string Gdk = "libgdk-3.so.0";
	private const string GObject = "libgobject-2.0.so.0";
	private const string GdkPixbuf = "libgdk_pixbuf-2.0.so.0";
	private const string Cairo = "libcairo.so.2";
	private const string GLib = "libglib-2.0.so.0";
	private const int CairoFormatArgb32 = 0;

	private static IntPtr _webkit;

	internal const int LoadStarted = 0;
	internal const int LoadFinished = 3;
	internal const int HardwareAccelerationNever = 2;
	internal const int GtkWindowToplevel = 0;
	internal const int GdkWindowTypeHintUtility = 5;
	internal const int HiddenHostX = -32000;
	internal const int HiddenHostY = -32000;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void LoadChangedHandler(IntPtr webView, int loadEvent, IntPtr userData);

	internal static bool TryInitialize() => TryInitialize(out _);

	internal static bool TryInitialize(out string error)
	{
		error = null;
		if (_webkit != IntPtr.Zero)
			return true;
		if (!OperatingSystem.IsLinux())
		{
			error = "WebKitGTK is only used on Linux.";
			return false;
		}
		if (!NativeLibrary.TryLoad("libwebkit2gtk-4.1.so.0", out _webkit)
			&& !NativeLibrary.TryLoad("libwebkit2gtk-4.0.so.0", out _webkit))
		{
			error =
				"This app needs the system WebKitGTK engine to show web pages on Linux.\n\n" +
				"Steam OS (Desktop Mode). Root is read-only; this does not survive a Steam OS update:\n" +
				"  sudo steamos-readonly disable\n" +
				"  sudo pacman-key --init\n" +
				"  sudo pacman-key --populate archlinux holo\n" +
				"  sudo pacman -S webkit2gtk-4.1\n" +
				"  sudo steamos-readonly enable\n\n" +
				"Debian / Ubuntu:\n" +
				"  sudo apt install libwebkit2gtk-4.1-0\n\n" +
				"Fedora:\n" +
				"  sudo dnf install webkit2gtk4.1\n\n" +
				"Arch:\n" +
				"  sudo pacman -S webkit2gtk-4.1\n\n" +
				"If 4.1 is not in your repos, install 4.0 instead (libwebkit2gtk-4.0-37 or webkit2gtk3). " +
				"You also need GTK 3 (libgtk-3-0).";
			return false;
		}
		try
		{
			using var backends = new Utf8("x11");
			gdk_set_allowed_backends(backends.Pointer);
		}
		catch
		{
			// Older GDK without gdk_set_allowed_backends.
		}

		// Child WebKit processes inherit these. X11 is required for the hidden host
		// window (Wayland cannot place a GtkWindow off-screen). Compositing on a
		// hidden X window often screenshots black.
		Environment.SetEnvironmentVariable("GDK_BACKEND", "x11");
		Environment.SetEnvironmentVariable("WEBKIT_DISABLE_COMPOSITING_MODE", "1");
		Environment.SetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
		Environment.SetEnvironmentVariable("WAYLAND_DISPLAY",
			"/proc/fake-display-to-prevent-wayland-initialization-by-gtk3");
		if (gtk_init_check(0, IntPtr.Zero))
			return true;

		_webkit = IntPtr.Zero;
		error =
			"WebKitGTK was found but GTK could not start.\n\n" +
			"Install GTK 3 (libgtk-3-0). On a Wayland-only session also install XWayland " +
			"(xwayland / xorg-x11-server-Xwayland) so GTK can use the X11 backend.";
		return false;
	}

	internal static void Pump()
	{
		while (gtk_events_pending())
			gtk_main_iteration_do(false);
	}

	internal static IntPtr CreateWebView()
	{
		var proc = NativeLibrary.GetExport(_webkit, "webkit_web_view_new");
		return ((delegate* unmanaged[Cdecl]<IntPtr>)proc)();
	}

	internal static void DisableHardwareAcceleration(IntPtr view)
	{
		var settings = CallPtr(view, "webkit_web_view_get_settings");
		if (settings == IntPtr.Zero)
			return;
		try
		{
			var proc = NativeLibrary.GetExport(_webkit, "webkit_settings_set_hardware_acceleration_policy");
			((delegate* unmanaged[Cdecl]<IntPtr, int, void>)proc)(settings, HardwareAccelerationNever);
		}
		catch
		{
			// Older WebKit without hardware-acceleration-policy.
		}
	}

	internal static IntPtr CreateHiddenHostWindow()
	{
		var window = gtk_window_new(GtkWindowToplevel);
		if (window == IntPtr.Zero)
			return IntPtr.Zero;
		gtk_window_set_decorated(window, false);
		gtk_window_set_skip_taskbar_hint(window, true);
		gtk_window_set_skip_pager_hint(window, true);
		gtk_window_set_accept_focus(window, false);
		gtk_window_set_focus_on_map(window, false);
		gtk_window_set_keep_below(window, true);
		gtk_window_set_type_hint(window, GdkWindowTypeHintUtility);
		gtk_window_move(window, HiddenHostX, HiddenHostY);
		return window;
	}

	internal static void MapHiddenHostWindow(IntPtr window)
	{
		gtk_widget_realize(window);
		var gdk = gtk_widget_get_window(window);
		if (gdk != IntPtr.Zero)
			gdk_window_set_override_redirect(gdk, true);
		gtk_window_move(window, HiddenHostX, HiddenHostY);
		gtk_widget_show_all(window);
		gtk_window_move(window, HiddenHostX, HiddenHostY);
	}

	internal static void ResizeHiddenHostWindow(IntPtr window, IntPtr webView, int width, int height)
	{
		gtk_widget_set_size_request(webView, width, height);
		gtk_widget_set_size_request(window, width, height);
		gtk_window_resize(window, width, height);
		var allocation = new GtkAllocation { Width = width, Height = height };
		gtk_widget_size_allocate(window, ref allocation);
		gtk_widget_size_allocate(webView, ref allocation);
		gtk_window_move(window, HiddenHostX, HiddenHostY);
	}

	internal static void ForcePaint(IntPtr widget)
	{
		if (widget == IntPtr.Zero)
			return;
		gtk_widget_queue_draw(widget);
		var gdk = gtk_widget_get_window(widget);
		if (gdk == IntPtr.Zero)
			return;
		gdk_window_invalidate_rect(gdk, IntPtr.Zero, true);
		gdk_window_process_updates(gdk, true);
	}

	internal static byte[] CaptureWidgetBgra(IntPtr widget, int fallbackWidth, int fallbackHeight, out int width,
		out int height)
	{
		width = fallbackWidth;
		height = fallbackHeight;
		if (widget == IntPtr.Zero || fallbackWidth <= 0 || fallbackHeight <= 0)
			return null;
		var allocatedWidth = gtk_widget_get_allocated_width(widget);
		var allocatedHeight = gtk_widget_get_allocated_height(widget);
		if (allocatedWidth > 0)
			width = allocatedWidth;
		if (allocatedHeight > 0)
			height = allocatedHeight;
		var surface = cairo_image_surface_create(CairoFormatArgb32, width, height);
		if (surface == IntPtr.Zero)
			return CaptureWindowPixbufBgra(widget, width, height);
		var cr = cairo_create(surface);
		if (cr == IntPtr.Zero)
		{
			cairo_surface_destroy(surface);
			return CaptureWindowPixbufBgra(widget, width, height);
		}

		try
		{
			gtk_widget_draw(widget, cr);
			cairo_surface_flush(surface);
			var src = cairo_image_surface_get_data(surface);
			if (src == IntPtr.Zero)
				return CaptureWindowPixbufBgra(widget, width, height);
			var stride = cairo_image_surface_get_stride(surface);
			var dest = new byte[width * height * 4];
			if (stride == width * 4)
				Marshal.Copy(src, dest, 0, dest.Length);
			else
			{
				for (var y = 0; y < height; y++)
					Marshal.Copy(src + y * stride, dest, y * width * 4, width * 4);
			}

			if (!HasOpaquePixel(dest, width, height))
			{
				var fallback = CaptureWindowPixbufBgra(widget, width, height);
				if (fallback != null)
					return fallback;
			}

			return dest;
		}
		finally
		{
			cairo_destroy(cr);
			cairo_surface_destroy(surface);
		}
	}

	private static bool HasOpaquePixel(byte[] bgra, int width, int height)
	{
		if (bgra == null || width <= 0 || height <= 0)
			return false;
		var stepX = Math.Max(1, width / 8);
		var stepY = Math.Max(1, height / 8);
		for (var y = 0; y < height; y += stepY)
		{
			for (var x = 0; x < width; x += stepX)
			{
				if (bgra[(y * width + x) * 4 + 3] != 0)
					return true;
			}
		}

		return false;
	}

	private static byte[] CaptureWindowPixbufBgra(IntPtr widget, int width, int height)
	{
		var gdk = gtk_widget_get_window(widget);
		if (gdk == IntPtr.Zero)
			return null;
		var gw = gdk_window_get_width(gdk);
		var gh = gdk_window_get_height(gdk);
		if (gw > 0)
			width = Math.Min(width, gw);
		if (gh > 0)
			height = Math.Min(height, gh);
		if (width <= 0 || height <= 0)
			return null;
		var pixbuf = gdk_pixbuf_get_from_window(gdk, 0, 0, width, height);
		if (pixbuf == IntPtr.Zero)
			return null;
		try
		{
			var pw = gdk_pixbuf_get_width(pixbuf);
			var ph = gdk_pixbuf_get_height(pixbuf);
			var stride = gdk_pixbuf_get_rowstride(pixbuf);
			var channels = gdk_pixbuf_get_n_channels(pixbuf);
			var src = gdk_pixbuf_get_pixels(pixbuf);
			if (pw <= 0 || ph <= 0 || src == IntPtr.Zero)
				return null;
			var dest = new byte[pw * ph * 4];
			for (var y = 0; y < ph; y++)
			{
				var row = src + y * stride;
				for (var x = 0; x < pw; x++)
				{
					var i = (y * pw + x) * 4;
					dest[i] = Marshal.ReadByte(row, x * channels + 2);
					dest[i + 1] = Marshal.ReadByte(row, x * channels + 1);
					dest[i + 2] = Marshal.ReadByte(row, x * channels);
					dest[i + 3] = channels > 3 ? Marshal.ReadByte(row, x * channels + 3) : (byte)255;
				}
			}

			return dest;
		}
		finally
		{
			g_object_unref(pixbuf);
		}
	}

	internal static void LoadUri(IntPtr view, string uri)
	{
		var proc = NativeLibrary.GetExport(_webkit, "webkit_web_view_load_uri");
		using var utf = new Utf8(uri);
		((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)proc)(view, utf.Pointer);
	}

	internal static void LoadHtml(IntPtr view, string html, string baseUri)
	{
		var proc = NativeLibrary.GetExport(_webkit, "webkit_web_view_load_html");
		using var htmlUtf = new Utf8(html);
		using var baseUtf = new Utf8(baseUri);
		((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)proc)(view, htmlUtf.Pointer, baseUtf.Pointer);
	}

	internal static void Reload(IntPtr view) => CallVoid(view, "webkit_web_view_reload");

	internal static void Stop(IntPtr view) => CallVoid(view, "webkit_web_view_stop_loading");

	internal static bool GoBack(IntPtr view) => CallBool(view, "webkit_web_view_go_back");

	internal static bool GoForward(IntPtr view) => CallBool(view, "webkit_web_view_go_forward");

	internal static bool CanGoBack(IntPtr view) => CallBool(view, "webkit_web_view_can_go_back");

	internal static bool CanGoForward(IntPtr view) => CallBool(view, "webkit_web_view_can_go_forward");

	internal static string GetTitle(IntPtr view) => PtrToUtf8(CallPtr(view, "webkit_web_view_get_title"));

	internal static string GetUri(IntPtr view) => PtrToUtf8(CallPtr(view, "webkit_web_view_get_uri"));

	internal static ulong ConnectLoadChanged(IntPtr view, LoadChangedHandler handler)
	{
		var ptr = Marshal.GetFunctionPointerForDelegate(handler);
		using var name = new Utf8("load-changed");
		return g_signal_connect_data(view, name.Pointer, ptr, IntPtr.Zero, IntPtr.Zero, 0);
	}

	private static void CallVoid(IntPtr view, string name)
	{
		var proc = NativeLibrary.GetExport(_webkit, name);
		((delegate* unmanaged[Cdecl]<IntPtr, void>)proc)(view);
	}

	private static bool CallBool(IntPtr view, string name)
	{
		var proc = NativeLibrary.GetExport(_webkit, name);
		return ((delegate* unmanaged[Cdecl]<IntPtr, int>)proc)(view) != 0;
	}

	private static IntPtr CallPtr(IntPtr view, string name)
	{
		var proc = NativeLibrary.GetExport(_webkit, name);
		return ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)proc)(view);
	}

	private static string PtrToUtf8(IntPtr pointer) =>
		pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(pointer) ?? string.Empty;

	[DllImport(Gtk)]
	private static extern bool gtk_init_check(int argc, IntPtr argv);

	[DllImport(Gtk)]
	private static extern bool gtk_events_pending();

	[DllImport(Gtk)]
	private static extern bool gtk_main_iteration_do(bool blocking);

	[DllImport(Gtk)]
	internal static extern IntPtr gtk_window_new(int type);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_decorated(IntPtr window, bool decorated);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_skip_taskbar_hint(IntPtr window, bool setting);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_skip_pager_hint(IntPtr window, bool setting);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_accept_focus(IntPtr window, bool setting);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_focus_on_map(IntPtr window, bool setting);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_keep_below(IntPtr window, bool setting);

	[DllImport(Gtk)]
	internal static extern void gtk_window_set_type_hint(IntPtr window, int hint);

	[DllImport(Gtk)]
	internal static extern void gtk_window_move(IntPtr window, int x, int y);

	[DllImport(Gtk)]
	internal static extern void gtk_window_resize(IntPtr window, int width, int height);

	[DllImport(Gtk)]
	internal static extern void gtk_container_add(IntPtr container, IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_show_all(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_realize(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_destroy(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_hide(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_show(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_set_size_request(IntPtr widget, int width, int height);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_set_can_focus(IntPtr widget, bool canFocus);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_queue_draw(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_draw(IntPtr widget, IntPtr cr);

	[DllImport(Gtk)]
	internal static extern int gtk_widget_get_allocated_width(IntPtr widget);

	[DllImport(Gtk)]
	internal static extern int gtk_widget_get_allocated_height(IntPtr widget);

	[DllImport(Gtk)]
	private static extern void gtk_widget_size_allocate(IntPtr widget, ref GtkAllocation allocation);

	[DllImport(Gtk)]
	internal static extern IntPtr gtk_widget_get_window(IntPtr widget);

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_x11_window_get_xid(IntPtr window);

	[DllImport(Gdk)]
	internal static extern void gdk_window_set_override_redirect(IntPtr window, bool overrideRedirect);

	[DllImport(Gdk)]
	internal static extern void gdk_window_invalidate_rect(IntPtr window, IntPtr rect, bool invalidateChildren);

	[DllImport(Gdk)]
	internal static extern void gdk_window_process_updates(IntPtr window, bool updateChildren);

	[DllImport(Gdk)]
	internal static extern int gdk_window_get_width(IntPtr window);

	[DllImport(Gdk)]
	internal static extern int gdk_window_get_height(IntPtr window);

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_pixbuf_get_from_window(IntPtr window, int srcX, int srcY, int width, int height);

	[DllImport(Gdk)]
	private static extern void gdk_set_allowed_backends(IntPtr backends);

	[DllImport(GObject)]
	private static extern ulong g_signal_connect_data(IntPtr instance, IntPtr detailedSignal, IntPtr cHandler,
		IntPtr data, IntPtr destroyData, int flags);

	[DllImport(GdkPixbuf)]
	internal static extern int gdk_pixbuf_get_width(IntPtr pixbuf);

	[DllImport(GdkPixbuf)]
	internal static extern int gdk_pixbuf_get_height(IntPtr pixbuf);

	[DllImport(GdkPixbuf)]
	internal static extern int gdk_pixbuf_get_rowstride(IntPtr pixbuf);

	[DllImport(GdkPixbuf)]
	internal static extern int gdk_pixbuf_get_n_channels(IntPtr pixbuf);

	[DllImport(GdkPixbuf)]
	internal static extern IntPtr gdk_pixbuf_get_pixels(IntPtr pixbuf);

	[DllImport(GObject)]
	internal static extern void g_object_unref(IntPtr obj);

	[DllImport(GObject)]
	internal static extern IntPtr g_object_ref(IntPtr obj);

	internal const int GdkMotionNotify = 3;
	internal const int GdkButtonPress = 4;
	internal const int GdkButtonRelease = 7;
	internal const int GdkKeyPress = 8;
	internal const int GdkKeyRelease = 9;
	internal const int GdkScroll = 31;
	internal const int GdkScrollSmooth = 4;

	[DllImport(Gtk)]
	internal static extern bool gtk_widget_event(IntPtr widget, IntPtr gdkEvent);

	[DllImport(Gtk)]
	internal static extern void gtk_widget_grab_focus(IntPtr widget);

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_event_new(int type);

	[DllImport(Gdk)]
	internal static extern void gdk_event_free(IntPtr gdkEvent);

	[DllImport(Gdk)]
	internal static extern void gdk_event_set_device(IntPtr gdkEvent, IntPtr device);

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_display_get_default();

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_display_get_default_seat(IntPtr display);

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_seat_get_pointer(IntPtr seat);

	[DllImport(Gdk)]
	internal static extern IntPtr gdk_seat_get_keyboard(IntPtr seat);

	[DllImport(Gdk)]
	internal static extern uint gdk_unicode_to_keyval(uint wc);

	[DllImport(Gdk)]
	private static extern IntPtr gdk_keymap_get_for_display(IntPtr display);

	[DllImport(Gdk)]
	private static extern bool gdk_keymap_get_entries_for_keyval(IntPtr keymap, uint keyval, out IntPtr keys,
		out int nKeys);

	[DllImport(GLib)]
	private static extern void g_free(IntPtr mem);

	[DllImport(GLib)]
	private static extern ulong g_get_monotonic_time();

	[DllImport(Cairo)]
	private static extern IntPtr cairo_image_surface_create(int format, int width, int height);

	[DllImport(Cairo)]
	private static extern IntPtr cairo_create(IntPtr surface);

	[DllImport(Cairo)]
	private static extern void cairo_destroy(IntPtr cr);

	[DllImport(Cairo)]
	private static extern void cairo_surface_destroy(IntPtr surface);

	[DllImport(Cairo)]
	private static extern void cairo_surface_flush(IntPtr surface);

	[DllImport(Cairo)]
	private static extern IntPtr cairo_image_surface_get_data(IntPtr surface);

	[DllImport(Cairo)]
	private static extern int cairo_image_surface_get_stride(IntPtr surface);

	internal static IntPtr PointerDevice()
	{
		var seat = DefaultSeat();
		return seat == IntPtr.Zero ? IntPtr.Zero : gdk_seat_get_pointer(seat);
	}

	internal static IntPtr KeyboardDevice()
	{
		var seat = DefaultSeat();
		return seat == IntPtr.Zero ? IntPtr.Zero : gdk_seat_get_keyboard(seat);
	}

	private static IntPtr DefaultSeat()
	{
		var display = gdk_display_get_default();
		return display == IntPtr.Zero ? IntPtr.Zero : gdk_display_get_default_seat(display);
	}

	private static uint EventTime() => (uint)(g_get_monotonic_time() / 1000);

	private static ushort HardwareKeycodeForKeyval(uint keyval)
	{
		var display = gdk_display_get_default();
		if (display == IntPtr.Zero)
			return 0;
		var keymap = gdk_keymap_get_for_display(display);
		if (keymap == IntPtr.Zero)
			return 0;
		if (!gdk_keymap_get_entries_for_keyval(keymap, keyval, out var keys, out var count)
			|| keys == IntPtr.Zero || count <= 0)
			return 0;
		try
		{
			return (ushort)Marshal.PtrToStructure<GdkKeymapKey>(keys).Keycode;
		}
		finally
		{
			g_free(keys);
		}
	}

	internal static void SendButton(IntPtr widget, double x, double y, uint button, bool press, uint state)
	{
		var type = press ? GdkButtonPress : GdkButtonRelease;
		var window = gtk_widget_get_window(widget);
		if (window == IntPtr.Zero)
			return;
		var ev = gdk_event_new(type);
		if (ev == IntPtr.Zero)
			return;
		try
		{
			var data = new GdkEventButton
			{
				Type = type,
				Window = g_object_ref(window),
				SendEvent = 1,
				Time = EventTime(),
				X = x,
				Y = y,
				State = state,
				Button = button,
				Device = PointerDevice(),
				XRoot = x,
				YRoot = y
			};
			Marshal.StructureToPtr(data, ev, false);
			if (data.Device != IntPtr.Zero)
				gdk_event_set_device(ev, data.Device);
			gtk_widget_event(widget, ev);
		}
		finally
		{
			gdk_event_free(ev);
		}
	}

	internal static void SendMotion(IntPtr widget, double x, double y, uint state)
	{
		var window = gtk_widget_get_window(widget);
		if (window == IntPtr.Zero)
			return;
		var ev = gdk_event_new(GdkMotionNotify);
		if (ev == IntPtr.Zero)
			return;
		try
		{
			var data = new GdkEventMotion
			{
				Type = GdkMotionNotify,
				Window = g_object_ref(window),
				SendEvent = 1,
				Time = EventTime(),
				X = x,
				Y = y,
				State = state,
				Device = PointerDevice(),
				XRoot = x,
				YRoot = y
			};
			Marshal.StructureToPtr(data, ev, false);
			if (data.Device != IntPtr.Zero)
				gdk_event_set_device(ev, data.Device);
			gtk_widget_event(widget, ev);
		}
		finally
		{
			gdk_event_free(ev);
		}
	}

	internal static void SendScroll(IntPtr widget, double x, double y, double dx, double dy, uint state)
	{
		var window = gtk_widget_get_window(widget);
		if (window == IntPtr.Zero)
			return;
		var ev = gdk_event_new(GdkScroll);
		if (ev == IntPtr.Zero)
			return;
		try
		{
			var data = new GdkEventScroll
			{
				Type = GdkScroll,
				Window = g_object_ref(window),
				SendEvent = 1,
				Time = EventTime(),
				X = x,
				Y = y,
				State = state,
				Direction = GdkScrollSmooth,
				Device = PointerDevice(),
				XRoot = x,
				YRoot = y,
				DeltaX = dx,
				DeltaY = dy
			};
			Marshal.StructureToPtr(data, ev, false);
			if (data.Device != IntPtr.Zero)
				gdk_event_set_device(ev, data.Device);
			gtk_widget_event(widget, ev);
		}
		finally
		{
			gdk_event_free(ev);
		}
	}

	internal static void SendKey(IntPtr widget, uint keyval, bool press, uint state)
	{
		if (keyval == 0)
			return;
		var type = press ? GdkKeyPress : GdkKeyRelease;
		var window = gtk_widget_get_window(widget);
		if (window == IntPtr.Zero)
			return;
		var ev = gdk_event_new(type);
		if (ev == IntPtr.Zero)
			return;
		try
		{
			var data = new GdkEventKey
			{
				Type = type,
				Window = g_object_ref(window),
				SendEvent = 1,
				Time = EventTime(),
				State = state,
				Keyval = keyval,
				Length = 0,
				String = IntPtr.Zero,
				HardwareKeycode = HardwareKeycodeForKeyval(keyval),
				Group = 0
			};
			Marshal.StructureToPtr(data, ev, false);
			var keyboard = KeyboardDevice();
			if (keyboard != IntPtr.Zero)
				gdk_event_set_device(ev, keyboard);
			gtk_widget_grab_focus(widget);
			gtk_widget_event(widget, ev);
		}
		finally
		{
			gdk_event_free(ev);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct GdkKeymapKey
	{
		public uint Keycode;
		public int Group;
		public int Level;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct GtkAllocation
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct GdkEventKey
	{
		[FieldOffset(0)] public int Type;
		[FieldOffset(8)] public IntPtr Window;
		[FieldOffset(16)] public sbyte SendEvent;
		[FieldOffset(20)] public uint Time;
		[FieldOffset(24)] public uint State;
		[FieldOffset(28)] public uint Keyval;
		[FieldOffset(32)] public int Length;
		[FieldOffset(40)] public IntPtr String;
		[FieldOffset(48)] public ushort HardwareKeycode;
		[FieldOffset(50)] public byte Group;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct GdkEventButton
	{
		[FieldOffset(0)] public int Type;
		[FieldOffset(8)] public IntPtr Window;
		[FieldOffset(16)] public sbyte SendEvent;
		[FieldOffset(20)] public uint Time;
		[FieldOffset(24)] public double X;
		[FieldOffset(32)] public double Y;
		[FieldOffset(40)] public IntPtr Axes;
		[FieldOffset(48)] public uint State;
		[FieldOffset(52)] public uint Button;
		[FieldOffset(56)] public IntPtr Device;
		[FieldOffset(64)] public double XRoot;
		[FieldOffset(72)] public double YRoot;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct GdkEventMotion
	{
		[FieldOffset(0)] public int Type;
		[FieldOffset(8)] public IntPtr Window;
		[FieldOffset(16)] public sbyte SendEvent;
		[FieldOffset(20)] public uint Time;
		[FieldOffset(24)] public double X;
		[FieldOffset(32)] public double Y;
		[FieldOffset(40)] public IntPtr Axes;
		[FieldOffset(48)] public uint State;
		[FieldOffset(56)] public IntPtr Device;
		[FieldOffset(64)] public double XRoot;
		[FieldOffset(72)] public double YRoot;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct GdkEventScroll
	{
		[FieldOffset(0)] public int Type;
		[FieldOffset(8)] public IntPtr Window;
		[FieldOffset(16)] public sbyte SendEvent;
		[FieldOffset(20)] public uint Time;
		[FieldOffset(24)] public double X;
		[FieldOffset(32)] public double Y;
		[FieldOffset(40)] public uint State;
		[FieldOffset(44)] public int Direction;
		[FieldOffset(48)] public IntPtr Device;
		[FieldOffset(56)] public double XRoot;
		[FieldOffset(64)] public double YRoot;
		[FieldOffset(72)] public double DeltaX;
		[FieldOffset(80)] public double DeltaY;
	}

	private sealed class Utf8 : IDisposable
	{
		public Utf8(string value)
		{
			Pointer = Marshal.StringToCoTaskMemUTF8(value ?? string.Empty);
		}

		public IntPtr Pointer { get; private set; }

		public void Dispose()
		{
			if (Pointer == IntPtr.Zero)
				return;
			Marshal.FreeCoTaskMem(Pointer);
			Pointer = IntPtr.Zero;
		}
	}
}
