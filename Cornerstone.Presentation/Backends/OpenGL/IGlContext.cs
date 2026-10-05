using System;
using System.Collections.Generic;
using Cornerstone.Presentation.OpenGL.Surfaces;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;

namespace Cornerstone.Presentation.OpenGL
{
    public interface IGlContext : IPlatformGraphicsContext
    {
        GlVersion Version { get; }
        GlInterface GlInterface { get; }
        int SampleCount { get; }
        int StencilSize { get; }
        IDisposable MakeCurrent();
        bool IsSharedWith(IGlContext context);
        bool CanCreateSharedContext { get; }
        IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null);
    }

    public interface IGlPlatformSurfaceRenderTargetFactory
    {
        bool CanRenderToSurface(IGlContext context, IPlatformRenderSurface surface);
        IGlPlatformSurfaceRenderTarget CreateRenderTarget(IGlContext context, IPlatformRenderSurface surface);
    }
}
