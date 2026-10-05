using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform;

[Unstable, PrivateApi]
public interface IPlatformRenderInterfaceRegion : IDisposable
{
    void AddRect(LtrbPixelRect rect);
    void ExcludeRect(LtrbPixelRect rect);
    void ExcludeRoundedRect(LtrbPixelRect rect, float radiusTopLeft, float radiusTopRight, float radiusBottomRight, float radiusBottomLeft);
    void Reset();
    bool IsEmpty { get; }
    LtrbPixelRect Bounds { get; }
    IList<LtrbPixelRect> Rects { get; }
    bool Intersects(LtrbRect rect);
    bool Contains(Point pt);
}