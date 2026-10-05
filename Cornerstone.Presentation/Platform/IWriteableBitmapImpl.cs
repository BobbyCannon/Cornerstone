using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Cornerstone.Presentation.Media.Imaging.WriteableBitmap"/>.
    /// </summary>
    [PrivateApi]
    public interface IWriteableBitmapImpl : IBitmapImpl, IReadableBitmapImpl
    {
    }
}
