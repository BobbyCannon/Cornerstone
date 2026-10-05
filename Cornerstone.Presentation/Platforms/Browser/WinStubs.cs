using System.Collections.Generic;
using System.IO;
using Cornerstone.Presentation.Platform;

#nullable enable

namespace Cornerstone.Presentation.Browser
{
    internal class IconLoaderStub : IPlatformIconLoader
    {
        private class IconStub : IWindowIconImpl
        {
            public void Save(Stream outputStream)
            {

            }
        }

        public IWindowIconImpl LoadIcon(string fileName) => new IconStub();

        public IWindowIconImpl LoadIcon(Stream stream) => new IconStub();

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconStub();
    }
}
