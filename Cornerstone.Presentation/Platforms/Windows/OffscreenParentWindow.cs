using System;

namespace Cornerstone.Presentation.Platforms.Windows
{
    internal class OffscreenParentWindow
    {
        private static SimpleWindow s_simpleWindow = new(null);
        public static IntPtr Handle { get; } = s_simpleWindow.Handle;
    }
}
