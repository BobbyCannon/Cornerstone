using System;
using Cornerstone.Presentation.Media;
using CoreGraphics;
using UIKit;

namespace Cornerstone.Presentation.iOS
{
    static class Extensions
    {

        public static Size ToCornerstone(this CGSize size) => new Size(size.Width, size.Height);

        public static Point ToCornerstone(this CGPoint point) => new Point(point.X, point.Y);

        static float ColorComponent(byte c) => (float) c / 255;

        public static UIColor ToUiColor(this Color color) => new UIColor(
            ColorComponent(color.R),
            ColorComponent(color.G),
            ColorComponent(color.B),
            ColorComponent(color.A));
    }
}
