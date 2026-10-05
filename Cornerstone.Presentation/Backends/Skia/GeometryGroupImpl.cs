using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using SkiaSharp;

namespace Cornerstone.Presentation.Backends.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Cornerstone.Presentation.Media.GeometryGroup"/>.
    /// </summary>
    internal class GeometryGroupImpl : GeometryImpl
    {
        public GeometryGroupImpl(FillRule fillRule, IReadOnlyList<IGeometryImpl> children)
        {
            var fillType = fillRule == FillRule.NonZero ? SKPathFillType.Winding : SKPathFillType.EvenOdd;
            var count = children.Count;
            
            var stroke = new SKPath
            {
                FillType = fillType
            };
            
            bool requiresFillPass = false;
            for (var i = 0; i < count; ++i)
            {
                if (children[i] is GeometryImpl geo)
                {
                    if (geo.StrokePath != null)
                        stroke.AddPath(geo.StrokePath);
                    if (!ReferenceEquals(geo.StrokePath, geo.FillPath))
                        requiresFillPass = true;
                }
            }
            
            StrokePath = stroke;
            
            if (requiresFillPass)
            {
                var fill = new SKPath
                {
                    FillType = fillType
                };

                for (var i = 0; i < count; ++i)
                {
                    if (children[i] is GeometryImpl { FillPath: { } fillPath })
                        fill.AddPath(fillPath);
                }

                FillPath = fill;
            }
            else
                FillPath = stroke;

            Bounds = stroke.TightBounds.ToCornerstoneRect();
        }

        public override Rect Bounds { get; }
        public override SKPath StrokePath { get; }
        public override SKPath FillPath { get; }
    }
}
