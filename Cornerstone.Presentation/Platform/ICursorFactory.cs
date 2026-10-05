using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Metadata;

#nullable enable

namespace Cornerstone.Presentation.Platform
{
    [PrivateApi]
    public interface ICursorFactory
    {
        ICursorImpl GetCursor(StandardCursorType cursorType);
        ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot);
    }
}
