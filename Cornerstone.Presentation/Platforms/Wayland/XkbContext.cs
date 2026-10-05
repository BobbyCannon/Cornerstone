using System;
using static Cornerstone.Presentation.Wayland.Server.Interop.XkbCommonNativeMethods;

namespace Cornerstone.Presentation.Wayland;

/// <summary>
/// Wraps an xkb_context, shared by XkbCommonKeymap and XkbComposeTable.
/// </summary>
sealed class XkbContext : IDisposable
{
    public IntPtr Handle { get; private set; }

    public XkbContext()
    {
        Handle = xkb_context_new(XKB_CONTEXT_NO_FLAGS);
        if (Handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create xkb context");
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            xkb_context_unref(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
