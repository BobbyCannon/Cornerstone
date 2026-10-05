using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Platforms.Windows.Interop;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Platforms.Windows
{
    class EmbeddedWindowImpl : WindowImpl
    {
        public EmbeddedWindowImpl()
        {
            _windowProperties = new WindowProperties
            {
                ShowInTaskbar = false,
                IsResizable = false,
                IsMinimizable = false,
                IsMaximizable = false,
                Decorations = WindowDecorations.None
            };
        }

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            var hWnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_CHILD,
                0,
                0,
                640,
                480,
                OffscreenParentWindow.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return hWnd;
        }

    }
}
