using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.OpenGL.Egl;

namespace Cornerstone.Presentation.Platforms.Windows;

internal interface IWindowsSurfaceFactory
{
    bool RequiresNoRedirectionBitmap { get; }

    IPlatformRenderSurface CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info);
}
