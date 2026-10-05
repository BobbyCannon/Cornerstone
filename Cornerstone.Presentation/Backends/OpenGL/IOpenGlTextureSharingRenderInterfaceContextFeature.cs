using System.Collections.Generic;
using Cornerstone.Presentation.Rendering.Composition;

namespace Cornerstone.Presentation.OpenGL
{
    public interface IOpenGlTextureSharingRenderInterfaceContextFeature
    {
        bool CanCreateSharedContext { get; }
        IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null);
        ICompositionImportableOpenGlSharedTexture CreateSharedTextureForComposition(IGlContext context, PixelSize size);
    }

    public interface ICompositionImportableOpenGlSharedTexture : ICompositionImportableSharedGpuContextImage
    {
        int TextureId { get; }
        int InternalFormat { get; }
        PixelSize Size { get; }
    }
}
