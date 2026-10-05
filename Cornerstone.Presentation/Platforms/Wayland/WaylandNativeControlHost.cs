using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Wayland.Server.Persistent;

namespace Cornerstone.Presentation.Wayland;

internal sealed class WaylandNativeControlHost : INativeControlHostImpl
{
    public const string HandleDescriptor = "WL_SURFACE";

    private readonly WindowImpl _window;

    public WaylandNativeControlHost(WindowImpl window)
    {
        _window = window;
    }

    public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
    {
        return CreateSubsurfaceHandle();
    }

    public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
    {
        var holder = CreateSubsurfaceHandle();
        try
        {
            var child = create(holder);
            var use = child as WaylandNativeControlHandle ?? holder;
            if (!ReferenceEquals(use, holder) && child is WaylandNativeControlHandle)
                holder.Destroy();
            return new Attachment(use) { AttachedTo = this };
        }
        catch
        {
            holder.Destroy();
            throw;
        }
    }

    public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
    {
        if (handle is not WaylandNativeControlHandle holder)
            throw new ArgumentException(handle.HandleDescriptor + " is not compatible with the current window",
                nameof(handle));
        return new Attachment(holder) { AttachedTo = this };
    }

    public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == HandleDescriptor;

    private WaylandNativeControlHandle CreateSubsurfaceHandle()
    {
        var top = _window.SurfaceProxy as WXdgTopLevelProxy
            ?? throw new InvalidOperationException("Wayland toplevel is not created.");
        var parent = (WXdgTopLevel)top.ProxyTarget;
        var impl = new WNativeControlSubsurface(_window.Client.Worker, parent);
        var proxy = new WNativeControlSubsurfaceProxy(impl, _window.Client.Marshaller);
        proxy.SetBehindComposition(NativeAirspace.BehindComposition);
        return new WaylandNativeControlHandle(proxy);
    }

    internal sealed class WaylandNativeControlHandle : INativeControlHostDestroyableControlHandle, INativePixelBufferHost
    {
        public WaylandNativeControlHandle(WNativeControlSubsurfaceProxy proxy)
        {
            Proxy = proxy;
            Handle = new IntPtr(1);
        }

        public WNativeControlSubsurfaceProxy Proxy { get; }
        public IntPtr Handle { get; }
        public string HandleDescriptor => WaylandNativeControlHost.HandleDescriptor;

        public void Destroy() => Proxy.Disconnect();

        public void SetUsesExternalBuffers(bool value) => Proxy.SetUsesExternalBuffers(value);

        public void AttachBgra(byte[] pixels, int width, int height) => Proxy.AttachBgra(pixels, width, height);
    }

    private sealed class Attachment : INativeControlHostControlTopLevelAttachment
    {
        private WaylandNativeControlHandle _holder;
        private WaylandNativeControlHost _attachedTo;

        public Attachment(WaylandNativeControlHandle holder)
        {
            _holder = holder;
        }

        public void Dispose()
        {
            _holder?.Destroy();
            _holder = null;
            _attachedTo = null;
        }

        [MemberNotNull(nameof(_holder))]
        private void CheckDisposed()
        {
            if (_holder == null)
                throw new ObjectDisposedException(nameof(Attachment));
        }

        public INativeControlHostImpl AttachedTo
        {
            get => _attachedTo;
            set
            {
                CheckDisposed();
                _attachedTo = (WaylandNativeControlHost)value;
                if (_attachedTo == null)
                    _holder.Proxy.Hide();
            }
        }

        public bool IsCompatibleWith(INativeControlHostImpl host) => host is WaylandNativeControlHost;

        public void HideWithSize(Size size)
        {
            if (_attachedTo == null || _holder == null)
                return;
            _holder.Proxy.Hide();
        }

        public void ShowInBounds(Rect bounds)
        {
            CheckDisposed();
            if (_attachedTo == null)
                throw new InvalidOperationException("The control isn't currently attached to a toplevel");
            // wl_subsurface.set_position is parent surface-local, not buffer pixels.
            var x = (int)Math.Round(bounds.X);
            var y = (int)Math.Round(bounds.Y);
            var w = Math.Max(1, (int)Math.Round(bounds.Width));
            var h = Math.Max(1, (int)Math.Round(bounds.Height));
            _holder.Proxy.ShowInBounds(x, y, w, h, _attachedTo._window.RenderScaling);
        }
    }
}
