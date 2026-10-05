using System;
using Cornerstone.Presentation.X11.Dispatching;

namespace Cornerstone.Presentation.Wayland;

/// <summary>
/// GLib (GMainLoop) based UI-thread dispatcher for the Wayland backend, enabled via
/// <see cref="WaylandPlatformOptions.UseGLibMainLoop"/>. It lets Cornerstone share a GLib main loop with GLib/GTK
/// based libraries on the UI thread. Unlike X11 it attaches no platform event source of its own: the Wayland
/// connection is owned and pumped by the worker thread, which posts input/events back via the dispatcher, so the
/// base class' signaling/timer/background machinery is all the UI thread needs.
/// </summary>
internal sealed class WaylandGlibDispatcher : GlibDispatcherImplBase
{
    public WaylandGlibDispatcher(Action<Exception>? externalExceptionLogger)
        : base(externalExceptionLogger)
    {
    }
}
