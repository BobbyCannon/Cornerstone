using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Backends.Skia;
[PrivateApi]
public interface IGlSkiaSpecificOptionsFeature
{
    public bool UseNativeSkiaGrGlInterface { get; }
}