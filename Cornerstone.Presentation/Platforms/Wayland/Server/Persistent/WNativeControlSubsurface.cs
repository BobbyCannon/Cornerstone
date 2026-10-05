using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Wayland.Server.Interop;
using Cornerstone.Presentation.Wayland.Server.Transient;
using NWayland.Protocols.Viewporter;
using NWayland.Protocols.Wayland;
using static Cornerstone.Presentation.Wayland.Server.Interop.UnsafeNativeMethods;

namespace Cornerstone.Presentation.Wayland.Server.Persistent;

/// <summary>
/// Persistent native-host island: a <c>wl_surface</c> with a <c>wl_subsurface</c>
/// role under the toplevel Skia surface. Default content is a solid SHM buffer;
/// Linux WebView attaches BGRA frames. Invert places the island below the parent.
/// </summary>
internal sealed class WNativeControlSubsurface : IPersistentWaylandObject, IWNativeControlSubsurface
{
    private readonly WaylandWorker _worker;
    private readonly WXdgTopLevel _parent;
    private WaylandGlobals _globals;
    private WlSurface _wlSurface;
    private WlSubsurface _subsurface;
    private WpViewport _viewport;
    private bool _behind;
    private bool _usesExternalBuffers;
    private byte[] _externalBgra;
    private bool _mapped;
    private int _x;
    private int _y;
    private int _destWidth = 1;
    private int _destHeight = 1;
    private int _pixelWidth = 1;
    private int _pixelHeight = 1;
    private double _scale = 1;

    public WNativeControlSubsurface(WaylandWorker worker, WXdgTopLevel parent)
    {
        _worker = worker;
        _parent = parent;
        worker.PostOob(() => worker.RegisterPersistentObject(this));
    }

    public void Disconnect()
    {
        _worker.UnregisterPersistentObject(this);
    }

    public void SetUsesExternalBuffers(bool value)
    {
        _usesExternalBuffers = value;
    }

    public void AttachBgra(byte[] pixels, int width, int height)
    {
        _externalBgra = pixels;
        _pixelWidth = Math.Max(1, width);
        _pixelHeight = Math.Max(1, height);
        _usesExternalBuffers = true;
        if (_wlSurface == null)
            return;
        ApplyViewport();
        AttachBgraBuffer(pixels, _pixelWidth, _pixelHeight);
        _mapped = true;
        CommitParent();
    }

    public void SetBehindComposition(bool behind)
    {
        _behind = behind;
        ApplyZOrder();
        CommitParent();
    }

    public void Hide()
    {
        if (_wlSurface == null)
            return;
        _mapped = false;
        _wlSurface.Attach(null, 0, 0);
        _wlSurface.Commit();
        CommitParent();
    }

    public void ShowInBounds(int x, int y, int width, int height, double scale)
    {
        _x = x;
        _y = y;
        _destWidth = Math.Max(1, width);
        _destHeight = Math.Max(1, height);
        _scale = scale > 0 ? scale : 1;
        if (!_usesExternalBuffers)
        {
            _pixelWidth = Math.Max(1, (int)Math.Round(_destWidth * _scale));
            _pixelHeight = Math.Max(1, (int)Math.Round(_destHeight * _scale));
        }
        if (_wlSurface == null || _subsurface == null)
            return;
        _subsurface.SetPosition(_x, _y);
        ApplyZOrder();
        ApplyViewport();
        if (_usesExternalBuffers)
        {
            if (_externalBgra != null)
                AttachBgraBuffer(_externalBgra, _pixelWidth, _pixelHeight);
        }
        else
            AttachSolidBuffer(_pixelWidth, _pixelHeight);
        _mapped = true;
        CommitParent();
    }

    public void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        _globals = globals;
        if (_parent.WlSurface == null)
            return;
        _wlSurface = globals.WlCompositor.CreateSurface(null);
        if (globals.Viewporter != null)
            _viewport = globals.Viewporter.GetViewport(_wlSurface);
        _subsurface = globals.WlSubcompositor.GetSubsurface(
            _wlSurface, _parent.WlSurface, new SubsurfaceListener(), connection.Queue);
        _subsurface.SetDesync();
        ApplyZOrder();
        if (_mapped)
        {
            _subsurface.SetPosition(_x, _y);
            ApplyViewport();
            if (_usesExternalBuffers && _externalBgra != null)
                AttachBgraBuffer(_externalBgra, _pixelWidth, _pixelHeight);
            else if (!_usesExternalBuffers)
                AttachSolidBuffer(_pixelWidth, _pixelHeight);
        }
        CommitParent();
    }

    public void OnDisconnected()
    {
        if (_viewport != null)
        {
            _viewport.Destroy();
            _viewport.Dispose();
            _viewport = null;
        }
        if (_subsurface != null)
        {
            _subsurface.Destroy();
            _subsurface.Dispose();
            _subsurface = null;
        }
        if (_wlSurface != null)
        {
            _wlSurface.Destroy();
            _wlSurface.Dispose();
            _wlSurface = null;
        }
        _globals = null;
    }

    private void ApplyViewport()
    {
        if (_wlSurface == null)
            return;
        if (_viewport != null)
            _viewport.SetDestination(_destWidth, _destHeight);
        else
            _wlSurface.SetBufferScale(Math.Max(1, (int)Math.Ceiling(_scale)));
    }

    private void ApplyZOrder()
    {
        if (_subsurface == null || _parent.WlSurface == null)
            return;
        if (_behind)
            _subsurface.PlaceBelow(_parent.WlSurface);
        else
            _subsurface.PlaceAbove(_parent.WlSurface);
    }

    private void CommitParent()
    {
        _parent.WlSurface?.Commit();
    }

    private unsafe void AttachSolidBuffer(int width, int height)
    {
        var bufferLen = width * height * 4;
        var filled = new byte[bufferLen];
        for (var i = 0; i < bufferLen; i += 4)
        {
            filled[i] = 0x40;
            filled[i + 1] = 0x80;
            filled[i + 2] = 0xFF;
            filled[i + 3] = 0xFF;
        }
        AttachBgraBuffer(filled, width, height);
    }

    private unsafe void AttachBgraBuffer(byte[] pixels, int width, int height)
    {
        if (_wlSurface == null || _globals == null || pixels == null)
            return;
        var bufferLen = width * height * 4;
        if (pixels.Length < bufferLen)
            return;
        var fd = memfd_create("cornerstone-wayland-native-host", MFD_CLOEXEC);
        IntPtr map;
        if (fd == -1
            || ftruncate(fd, bufferLen) != 0
            || new IntPtr(-1) == (map = mmap(IntPtr.Zero, bufferLen, PROT_READ | PROT_WRITE, MAP_SHARED, fd, IntPtr.Zero)))
        {
            if (fd != -1)
                close(fd);
            return;
        }

        try
        {
            Marshal.Copy(pixels, 0, map, bufferLen);
        }
        finally
        {
            munmap(map, bufferLen);
        }

        var pool = _globals.WlShm.CreatePool(fd, bufferLen);
        try
        {
            var buffer = pool.CreateBuffer(0, width, height, width * 4, WlShm.FormatEnum.Argb8888, new BufferListener());
            _wlSurface.Attach(buffer, 0, 0);
            _wlSurface.DamageBuffer(0, 0, width, height);
            _wlSurface.Commit();
        }
        finally
        {
            pool.Destroy();
            close(fd);
        }
    }

    private sealed class SubsurfaceListener : WlSubsurface.Listener
    {
    }

    private sealed class BufferListener : WlBuffer.Listener
    {
        protected override void Release(WlBuffer eventSender)
        {
            eventSender.Destroy();
            base.Release(eventSender);
        }
    }
}
