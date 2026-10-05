using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Platform;
using SkiaSharp;

namespace Cornerstone.Presentation.Backends.Skia;

internal class SkiaRegionImpl : IPlatformRenderInterfaceRegion
{
    private SKRegion? _region = new();
    public SKRegion Region => _region ?? throw new ObjectDisposedException(nameof(SkiaRegionImpl));
    private bool _rectsValid;
    private List<LtrbPixelRect>? _rects;
    public void Dispose()
    {
        _region?.Dispose();
        _region = null;
    }

    public void AddRect(LtrbPixelRect rect)
    {
        _rectsValid = false;
        Region.Op(rect.Left, rect.Top, rect.Right, rect.Bottom, SKRegionOperation.Union);
    }

    public void ExcludeRect(LtrbPixelRect rect)
    {
        _rectsValid = false;
        Region.Op(rect.Left, rect.Top, rect.Right, rect.Bottom, SKRegionOperation.Difference);
    }

    public void ExcludeRoundedRect(LtrbPixelRect rect, float radiusTopLeft, float radiusTopRight,
        float radiusBottomRight, float radiusBottomLeft)
    {
        if (radiusTopLeft <= 0 && radiusTopRight <= 0 && radiusBottomRight <= 0 && radiusBottomLeft <= 0)
        {
            ExcludeRect(rect);
            return;
        }

        _rectsValid = false;
        var skRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        using var round = new SKRoundRect(skRect);
        round.SetRectRadii(skRect, new[]
        {
            new SKPoint(radiusTopLeft, radiusTopLeft),
            new SKPoint(radiusTopRight, radiusTopRight),
            new SKPoint(radiusBottomRight, radiusBottomRight),
            new SKPoint(radiusBottomLeft, radiusBottomLeft)
        });
        using var path = new SKPath();
        path.AddRoundRect(round);
        Region.Op(path, SKRegionOperation.Difference);
    }

    public void Reset()
    {
        _rectsValid = false;
        Region.SetEmpty();
    }

    public bool IsEmpty => Region.IsEmpty;
    public LtrbPixelRect Bounds => Region.Bounds.ToCornerstoneLtrbPixelRect();

    public IList<LtrbPixelRect> Rects
    {
        get
        {
            _rects ??= new();
            if (!_rectsValid)
            {
                _rects.Clear();
                using var iter = Region.CreateRectIterator();
                while (iter.Next(out var rc))
                    _rects.Add(rc.ToCornerstoneLtrbPixelRect());
            }
            return _rects;
        }
    }

    public bool Intersects(LtrbRect rect) => Region.Intersects(
        new SKRectI((int)rect.Left, (int)rect.Top,
            (int)Math.Ceiling(rect.Right), (int)Math.Ceiling(rect.Bottom)));
    
    public bool Contains(Point pt) => Region.Contains((int)pt.X, (int)pt.Y);
}