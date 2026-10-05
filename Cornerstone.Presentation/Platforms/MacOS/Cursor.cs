using System;
using System.IO;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    class MacCursor : ICursorImpl, IDisposable
    {
        public ICsnCursor? Cursor { get; private set; }
        public IntPtr Handle => IntPtr.Zero;

        public string HandleDescriptor => "<none>";

        public MacCursor(ICsnCursor cursor)
        {
            Cursor = cursor;
        }

        public void Dispose()
        {
            Cursor?.Dispose();
            Cursor = null;
        }
    }

    class CursorFactory : ICursorFactory
    {
        ICsnCursorFactory _native;

        public CursorFactory(ICsnCursorFactory native)
        {
            _native = native;
        }

        public ICursorImpl GetCursor(StandardCursorType cursorType)
        {
            var cursor = _native.GetCursor((CsnStandardCursorType)cursorType);
            return new MacCursor( cursor );
        }

        public unsafe ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot)
        {
            using(var ms = new MemoryStream())
            {
                cursor.Save(ms, PngBitmapEncoderOptions.Default);

                var imageData = ms.ToArray();

                fixed(void* ptr = imageData)
                {
                    var csnCursor = _native.CreateCustomCursor(ptr, new IntPtr(imageData.Length),
                        new CsnPixelSize { Width = hotSpot.X, Height = hotSpot.Y });

                    return new MacCursor(csnCursor);
                }
            }
        }
    }
}
