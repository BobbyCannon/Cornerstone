using SkiaSharp;

namespace Cornerstone.Presentation.Backends.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Cornerstone.Presentation.Media.RectangleGeometry"/>.
    /// </summary>
    internal class RectangleGeometryImpl : GeometryImpl
    {
        public override Rect Bounds { get; }
        public override SKPath StrokePath { get; }
        public override SKPath? FillPath => StrokePath;

        public RectangleGeometryImpl(Rect rect)
        {
            var path = new SKPath();
            path.AddRect(rect.ToSKRect());

            StrokePath = path;
            Bounds = rect;
        }
    }
}
