using System;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;

namespace Cornerstone.Presentation.OpenGL.Surfaces
{
    [PrivateApi]
    public interface IGlPlatformSurfaceRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
    {
        IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo);
    }
}
