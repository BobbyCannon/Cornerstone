using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android
{
    internal class CursorFactory : ICursorFactory
    {
        public ICursorImpl CreateCursor(Cornerstone.Presentation.Media.Imaging.Bitmap cursor, PixelPoint hotSpot) => CursorImpl.ZeroCursor;

        public ICursorImpl GetCursor(StandardCursorType cursorType) => CursorImpl.ZeroCursor;

        private sealed class CursorImpl : ICursorImpl
        {
            public static CursorImpl ZeroCursor { get; } = new CursorImpl();

            private CursorImpl() { }

            public void Dispose() { }
        }
    }
}
