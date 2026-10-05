using static Cornerstone.Presentation.Platforms.Windows.Interop.UnmanagedMethods;

namespace Cornerstone.Presentation.Platforms.Windows
{
    internal static class Win32TypeExtensions
    {
        public static PixelRect ToPixelRect(this global::Windows.Win32.Foundation.RECT rect)
        {
            return new PixelRect(rect.left, rect.top, rect.right - rect.left,
                rect.bottom - rect.top);
        }

        public static PixelRect ToPixelRect(this RECT rect)
        {
            return new PixelRect(rect.left, rect.top, rect.right - rect.left,
                    rect.bottom - rect.top);
        }
    }
}
