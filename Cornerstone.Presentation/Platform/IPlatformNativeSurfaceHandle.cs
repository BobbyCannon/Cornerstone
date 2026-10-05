using System;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [Unstable]
    public interface INativePlatformHandleSurface : IPlatformHandle, IPlatformRenderSurface
    {
        PixelSize Size { get; }
        double Scaling { get; }
    }
}
