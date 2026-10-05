using System;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;

namespace Cornerstone.Presentation.Metal;


[PrivateApi]
public interface IMetalDevice : IPlatformGraphicsContext
{
    IntPtr Device { get; }
    IntPtr CommandQueue { get; }
}

[PrivateApi]
public interface IMetalPlatformSurface : IPlatformRenderSurface
{
    IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device);
}

[PrivateApi]
public interface IMetalPlatformSurfaceRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
{
    IMetalPlatformSurfaceRenderingSession BeginRendering();
}

[PrivateApi]
public interface IMetalPlatformSurfaceRenderingSession : IDisposable
{
    IntPtr Texture { get; }
    PixelSize Size { get; }
    double Scaling { get; }
    bool IsYFlipped { get; }
}
