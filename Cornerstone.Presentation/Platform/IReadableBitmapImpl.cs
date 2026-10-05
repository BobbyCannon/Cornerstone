using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform;

[PrivateApi]
public interface IReadableBitmapImpl : IBitmapImpl
{
    PixelFormat? Format { get; }
    AlphaFormat? AlphaFormat { get; }
    ILockedFramebuffer Lock();
}
