using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal static class Helpers
    {
        public static Point ToPoint (this CsnPoint pt)
        {
            return new Point(pt.X, pt.Y);
        }

        public static PixelPoint ToPixelPoint(this CsnPoint pt)
        {
            return new PixelPoint((int)pt.X, (int)pt.Y);
        }

        public static CsnPoint ToCsnPoint (this Point pt)
        {
            return new CsnPoint { X = pt.X, Y = pt.Y };
        }

        public static CsnPoint ToCsnPoint(this PixelPoint pt)
        {
            return new CsnPoint { X = pt.X, Y = pt.Y };
        }

        public static CsnRect ToCsnRect (this Rect rect)
        {
            return new CsnRect() { X = rect.X, Y= rect.Y, Height = rect.Height, Width = rect.Width };
        }

        public static CsnSize ToCsnSize (this Size size)
        {
            return new CsnSize { Height = size.Height, Width = size.Width };
        }

        [return: NotNullIfNotNull(nameof(s))]
        public static ICsnString? ToCsnString(this string? s)
        {
            return s != null ? new CsnString(s) : null;
        }
        
        public static Size ToSize (this CsnSize size)
        {
            return new Size(size.Width, size.Height);
        }

        public static Rect ToRect (this CsnRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static PixelRect ToPixelRect(this CsnRect rect)
        {
            return new PixelRect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
    }
}
