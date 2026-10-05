using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platforms.Windows.Interop;

namespace Cornerstone.Presentation.Platforms.Windows;

/// <summary>
/// Layered composition window: WS_EX_LAYERED child covering the frame client.
/// Present with UpdateLayeredWindow so alpha-0 hole pixels click through to native siblings.
/// Hit-testable Cornerstone coverage that paints nothing is stamped to alpha 1 first.
/// </summary>
internal sealed class Win32NativeAirspaceOverlay : IDisposable
{
    private static readonly UnmanagedMethods.WndProc s_wndProcDelegate = OverlayWndProc;
    private static readonly ConcurrentDictionary<IntPtr, Win32NativeAirspaceOverlay> s_instances = new();
    private static readonly string s_className;
    private static readonly uint s_classAtom;

    private readonly WindowImpl _owner;
    private readonly IntPtr _ownerHwnd;
    private bool _disposed;
    private bool _hasLayeredPresent;
    private bool _skiaPresented;

    static Win32NativeAirspaceOverlay()
    {
        s_className = "CornerstoneNativeAirspaceOverlay-" + Guid.NewGuid();
        var wndClassEx = new UnmanagedMethods.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
            style = (int)(UnmanagedMethods.ClassStyles.CS_HREDRAW | UnmanagedMethods.ClassStyles.CS_VREDRAW |
                          UnmanagedMethods.ClassStyles.CS_DBLCLKS),
            lpfnWndProc = s_wndProcDelegate,
            hInstance = UnmanagedMethods.GetModuleHandle(null),
            hCursor = IntPtr.Zero,
            hbrBackground = IntPtr.Zero,
            lpszClassName = s_className
        };

        s_classAtom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);
        if (s_classAtom == 0)
            throw new Win32Exception();
    }

    public Win32NativeAirspaceOverlay(WindowImpl owner)
    {
        _owner = owner;
        _ownerHwnd = owner.Handle.Handle;
        var gcHandle = GCHandle.Alloc(this);
        try
        {
            var exStyle = UnmanagedMethods.WindowStyles.WS_EX_LAYERED;
            // Hidden until the first UpdateLayeredWindow. A visible layered HWND
            // with no bitmap paints opaque white (startup flash).
            var style = UnmanagedMethods.WindowStyles.WS_CHILD
                        | UnmanagedMethods.WindowStyles.WS_CLIPCHILDREN;

            var hwnd = UnmanagedMethods.CreateWindowEx(
                (int)exStyle,
                s_classAtom,
                null,
                (uint)style,
                0,
                0,
                1,
                1,
                _ownerHwnd,
                IntPtr.Zero,
                IntPtr.Zero,
                GCHandle.ToIntPtr(gcHandle));

            if (hwnd == IntPtr.Zero)
                throw new Win32Exception();

            Handle = hwnd;
        }
        finally
        {
            gcHandle.Free();
        }
    }

    public IntPtr Handle { get; private set; }

    public void SyncToOwner(bool ownerVisible)
    {
        if (Handle == IntPtr.Zero)
            return;

        if (!ownerVisible)
        {
            UnmanagedMethods.ShowWindow(Handle, UnmanagedMethods.ShowWindowCommand.Hide);
            return;
        }

        UnmanagedMethods.GetClientRect(_ownerHwnd, out var client);
        var width = Math.Max(1, client.right - client.left);
        var height = Math.Max(1, client.bottom - client.top);

        var flags = UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE;
        if (_hasLayeredPresent)
            flags |= UnmanagedMethods.SetWindowPosFlags.SWP_SHOWWINDOW;
        else
            flags |= UnmanagedMethods.SetWindowPosFlags.SWP_NOREDRAW;

        UnmanagedMethods.SetWindowPos(
            Handle,
            UnmanagedMethods.WindowPosZOrder.HWND_TOP,
            0,
            0,
            width,
            height,
            flags);
    }

    public unsafe void PresentSolidFill(Color color)
    {
        if (_disposed || Handle == IntPtr.Zero || color.A == 0 || _skiaPresented)
            return;

        UnmanagedMethods.GetClientRect(_ownerHwnd, out var client);
        var width = Math.Max(1, client.right - client.left);
        var height = Math.Max(1, client.bottom - client.top);

        var header = new UnmanagedMethods.BITMAPINFOHEADER();
        header.Init();
        header.biWidth = width;
        header.biHeight = -height;
        header.biPlanes = 1;
        header.biBitCount = 32;
        header.biCompression = UnmanagedMethods.BitmapCompressionMode.BI_RGB;

        var hBitmap = UnmanagedMethods.CreateDIBSection(IntPtr.Zero, ref header,
            UnmanagedMethods.DIBColorTable.DIB_RGB_COLORS, out var bits, IntPtr.Zero, 0);
        if (hBitmap == IntPtr.Zero || bits == IntPtr.Zero)
            return;

        try
        {
            var premulA = color.A;
            var premulR = (byte)((color.R * premulA) / 255);
            var premulG = (byte)((color.G * premulA) / 255);
            var premulB = (byte)((color.B * premulA) / 255);
            var pixel = (uint)(premulB | (premulG << 8) | (premulR << 16) | (premulA << 24));
            var count = width * height;
            var dest = (uint*)bits;
            for (var i = 0; i < count; i++)
                dest[i] = pixel;

            var size = new UnmanagedMethods.SIZE { X = width, Y = height };
            var source = new UnmanagedMethods.POINT { X = 0, Y = 0 };
            var blend = new UnmanagedMethods.BLENDFUNCTION
            {
                BlendOp = UnmanagedMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = UnmanagedMethods.AC_SRC_ALPHA
            };
            var memoryDc = UnmanagedMethods.CreateCompatibleDC(IntPtr.Zero);
            var previous = UnmanagedMethods.SelectObject(memoryDc, hBitmap);
            try
            {
                UnmanagedMethods.GdiFlush();
                UnmanagedMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, ref size, memoryDc, ref source, 0,
                    ref blend, UnmanagedMethods.ULW_ALPHA);
            }
            finally
            {
                UnmanagedMethods.SelectObject(memoryDc, previous);
                UnmanagedMethods.DeleteDC(memoryDc);
            }
        }
        finally
        {
            UnmanagedMethods.DeleteObject(hBitmap);
        }

        _hasLayeredPresent = true;
        ShowOverlayIfOwnerVisible();
    }

    public void NotifyLayeredPresented()
    {
        if (_disposed || Handle == IntPtr.Zero)
            return;

        _skiaPresented = true;
        _hasLayeredPresent = true;
        ShowOverlayIfOwnerVisible();
    }

    private void ShowOverlayIfOwnerVisible()
    {
        if (!UnmanagedMethods.IsWindowVisible(_ownerHwnd))
            return;

        UnmanagedMethods.ShowWindow(Handle, UnmanagedMethods.ShowWindowCommand.ShowNoActivate);
        UnmanagedMethods.SetWindowPos(
            Handle,
            UnmanagedMethods.WindowPosZOrder.HWND_TOP,
            0,
            0,
            0,
            0,
            UnmanagedMethods.SetWindowPosFlags.SWP_NOMOVE
            | UnmanagedMethods.SetWindowPosFlags.SWP_NOSIZE
            | UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE
            | UnmanagedMethods.SetWindowPosFlags.SWP_SHOWWINDOW);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (Handle != IntPtr.Zero)
        {
            s_instances.TryRemove(Handle, out _);
            UnmanagedMethods.DestroyWindow(Handle);
            Handle = IntPtr.Zero;
        }
    }

    private static IntPtr OverlayWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        Win32NativeAirspaceOverlay overlay;
        var message = (UnmanagedMethods.WindowsMessage)msg;

        if (message == UnmanagedMethods.WindowsMessage.WM_CREATE)
        {
            var createParams = Marshal.ReadIntPtr(lParam);
            overlay = (Win32NativeAirspaceOverlay)GCHandle.FromIntPtr(createParams).Target;
            s_instances[hWnd] = overlay;
            return IntPtr.Zero;
        }

        if (!s_instances.TryGetValue(hWnd, out overlay))
            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);

        if (message == UnmanagedMethods.WindowsMessage.WM_DESTROY)
        {
            s_instances.TryRemove(hWnd, out _);
            return IntPtr.Zero;
        }

        if (msg == FramebufferManager.PresentMessage)
        {
            overlay._owner.PresentLayeredFramebuffer();
            return IntPtr.Zero;
        }

        if (message == UnmanagedMethods.WindowsMessage.WM_ERASEBKGND)
            return new IntPtr(1);

        if (message == UnmanagedMethods.WindowsMessage.WM_PAINT)
        {
            UnmanagedMethods.BeginPaint(hWnd, out var ps);
            UnmanagedMethods.EndPaint(hWnd, ref ps);
            return IntPtr.Zero;
        }

        if (message == UnmanagedMethods.WindowsMessage.WM_NCHITTEST)
        {
            var hit = (UnmanagedMethods.HitTestValues)overlay._owner.HitTestNativeAirspace(lParam).ToInt64();
            if (hit is UnmanagedMethods.HitTestValues.HTCLIENT or UnmanagedMethods.HitTestValues.HTNOWHERE)
                return new IntPtr((int)UnmanagedMethods.HitTestValues.HTCLIENT);

            // Caption, resize, and sysbuttons belong to the frame, not this child.
            return new IntPtr((int)UnmanagedMethods.HitTestValues.HTTRANSPARENT);
        }

        if (message == UnmanagedMethods.WindowsMessage.WM_MOUSEACTIVATE)
            return new IntPtr((int)UnmanagedMethods.MouseActivate.MA_NOACTIVATE);

        if (message == UnmanagedMethods.WindowsMessage.WM_SETCURSOR)
        {
            UnmanagedMethods.SendMessage(overlay._ownerHwnd, (int)message, wParam, lParam);
            return new IntPtr(1);
        }

        if (ShouldForwardToOwner(message))
            return UnmanagedMethods.SendMessage(overlay._ownerHwnd, (int)message, wParam, lParam);

        return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private static bool ShouldForwardToOwner(UnmanagedMethods.WindowsMessage message)
    {
        if (message is >= UnmanagedMethods.WindowsMessage.WM_MOUSEFIRST
            and <= UnmanagedMethods.WindowsMessage.WM_MOUSELAST)
            return true;

        if (message is UnmanagedMethods.WindowsMessage.WM_MOUSELEAVE
            or UnmanagedMethods.WindowsMessage.WM_MOUSEHOVER)
            return true;

        if (message is >= UnmanagedMethods.WindowsMessage.WM_NCPOINTERUPDATE
            and <= UnmanagedMethods.WindowsMessage.WM_POINTERHWHEEL)
            return true;

        return false;
    }
}
