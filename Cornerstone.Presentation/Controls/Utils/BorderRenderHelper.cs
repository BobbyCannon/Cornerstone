using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Controls.Utils
{
    /// <summary>
    /// Contains helper methods for rendering a <see cref="Border"/>'s background and border to a given context.
    /// </summary>
    internal class BorderRenderHelper
    {
        private bool _useComplexRendering;
        private bool? _backendSupportsIndividualCorners;
        private Geometry? _backgroundGeometryCache;
        private Geometry? _borderGeometryCache;
        private Size _size;
        private Thickness _borderThickness;
        private CornerRadius _cornerRadius;
        private BackgroundSizing _backgroundSizing;
        private bool _initialized;
        private IPen? _cachedPen;

        private void Update(Size finalSize, Thickness borderThickness, CornerRadius cornerRadius, BackgroundSizing backgroundSizing)
        {
            _backendSupportsIndividualCorners ??= PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>()
                .SupportsIndividualRoundRects;
            _size = finalSize;
            _borderThickness = borderThickness;
            _cornerRadius = cornerRadius;
            _backgroundSizing = backgroundSizing;
            _initialized = true;

            if (borderThickness.IsUniform &&
                (cornerRadius.IsUniform || _backendSupportsIndividualCorners == true) &&
                backgroundSizing == BackgroundSizing.CenterBorder)
            {
                _backgroundGeometryCache = null;
                _borderGeometryCache = null;
                _useComplexRendering = false;
            }
            else
            {
                _useComplexRendering = true;

                var boundRect = new Rect(finalSize);
                var innerRect = boundRect.Deflate(borderThickness);

                if (innerRect.Width != 0 && innerRect.Height != 0)
                {
                    var backgroundOuterKeypoints = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(
                        boundRect,
                        borderThickness,
                        cornerRadius,
                        backgroundSizing);

                    var backgroundGeometry = new StreamGeometry();
                    using (var ctx = backgroundGeometry.Open())
                    {
                        GeometryBuilder.DrawRoundedCornersRectangle(ctx, ref backgroundOuterKeypoints);
                    }

                    _backgroundGeometryCache = backgroundGeometry;
                }
                else
                {
                    _backgroundGeometryCache = null;
                }

                if (boundRect.Width != 0 && boundRect.Height != 0)
                {
                    var borderInnerKeypoints = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(
                        boundRect,
                        borderThickness,
                        cornerRadius,
                        BackgroundSizing.InnerBorderEdge);
                    var borderOuterKeypoints = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(
                        boundRect,
                        borderThickness,
                        cornerRadius,
                        BackgroundSizing.OuterBorderEdge);

                    var borderInnerGeometry = new StreamGeometry();
                    using (var ctx = borderInnerGeometry.Open())
                    {
                        GeometryBuilder.DrawRoundedCornersRectangle(ctx, ref borderInnerKeypoints);
                    }

                    var borderOuterGeometry = new StreamGeometry();
                    using (var ctx = borderOuterGeometry.Open())
                    {
                        GeometryBuilder.DrawRoundedCornersRectangle(ctx, ref borderOuterKeypoints);
                    }

                    _borderGeometryCache = new CombinedGeometry(GeometryCombineMode.Exclude, borderOuterGeometry, borderInnerGeometry);
                }
                else
                {
                    _borderGeometryCache = null;
                }
            }
        }

        public void Render(
            DrawingContext context,
            Size finalSize,
            Thickness borderThickness,
            CornerRadius cornerRadius,
            BackgroundSizing backgroundSizing,
            IBrush? background,
            IBrush? borderBrush,
            BoxShadows boxShadows)
        {
            if (_size != finalSize
                || _borderThickness != borderThickness
                || _cornerRadius != cornerRadius
                || _backgroundSizing != backgroundSizing
                || !_initialized)
            {
                Update(finalSize, borderThickness, cornerRadius, backgroundSizing);
            }

            if (_useComplexRendering)
            {
                if (_backgroundGeometryCache != null)
                {
                    context.DrawGeometry(background, null, _backgroundGeometryCache);
                }

                if (_borderGeometryCache != null)
                {
                    context.DrawGeometry(borderBrush, null, _borderGeometryCache);
                }
            }
            else
            {
                var thickness = _borderThickness.Top;

                Pen.TryModifyOrCreate(ref _cachedPen, borderBrush, thickness);

                // CornerRadius is the outer corner. Shrink the radii with the centerline so the
                // stroke stays on that corner. Leaving the outer radius on the inset rect bows the
                // bend past the rounded clip, and the antialiased edge fades.
                var rrect = new RoundedRect(new Rect(_size), _cornerRadius).UniformBorderCenterline(thickness);

                context.DrawRectangle(background, _cachedPen, rrect, boxShadows);
            }
        }
    }
}
