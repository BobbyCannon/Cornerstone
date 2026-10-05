using System.Collections.Generic;

namespace Cornerstone.Presentation.OpenGL;

public interface IPlatformGraphicsOpenGlContextFactory
{
    IGlContext CreateContext(IEnumerable<GlVersion>? versions);
}
