using Cornerstone.Presentation.Platform.Surfaces;

namespace Cornerstone.Presentation.OpenGL.Surfaces
{
    public interface IGlPlatformSurface : IPlatformRenderSurface
    {
        IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context);
    }
}
