using System.IO;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    // OSX doesn't have a concept of *window* icon. 
    // Icons in the title bar are only shown if there is 
    // an opened file (on disk) associated with the current window
    // see https://stackoverflow.com/a/7038671/2231814
    class IconLoader : IPlatformIconLoader
    {
        class IconStub : IWindowIconImpl
        {
            private readonly IBitmapImpl _bitmap;

            public IconStub(IBitmapImpl bitmap)
            {
                _bitmap = bitmap;
            }

            public void Save(Stream outputStream)
            {
                _bitmap.Save(outputStream, PngBitmapEncoderOptions.Default);
            }
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new IconStub(
                PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>().LoadBitmap(fileName));
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new IconStub(
                PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>().LoadBitmap(stream));
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, PngBitmapEncoderOptions.Default);
            ms.Seek(0, SeekOrigin.Begin);
            return LoadIcon(ms);
        }
    }
}
