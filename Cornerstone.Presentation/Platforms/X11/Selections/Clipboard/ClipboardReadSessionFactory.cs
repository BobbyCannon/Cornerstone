using System;
using static Cornerstone.Presentation.X11.XLib;

namespace Cornerstone.Presentation.X11.Selections.Clipboard;

internal static class ClipboardReadSessionFactory
{
    public static SelectionReadSession CreateSession(X11Platform platform, IntPtr selection)
    {
        var window = new EventStreamWindow(platform);
        XSelectInput(platform.Display, window.Handle, new IntPtr((int)XEventMask.PropertyChangeMask));

        return new SelectionReadSession(
            platform.Display,
            window.Handle,
            selection,
            window,
            platform.Info.Atoms);
    }
}
