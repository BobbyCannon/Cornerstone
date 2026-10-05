using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a
    /// <see cref="Cornerstone.Presentation.Media.Imaging.RenderTargetBitmap"/>.
    /// </summary>
    [Unstable]
    public interface IRenderTargetBitmapImpl : IReadableBitmapImpl
    {
        IDrawingContextImpl CreateDrawingContext();
    }
}
