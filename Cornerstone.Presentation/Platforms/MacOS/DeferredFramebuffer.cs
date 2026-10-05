using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal unsafe class DeferredFramebuffer : ILockedFramebuffer
    {
        private readonly ICsnSoftwareRenderTarget _renderTarget;
        private readonly Action<Action<ICsnTopLevel>> _lockTopLevel;
        
        public DeferredFramebuffer(ICsnSoftwareRenderTarget renderTarget, Action<Action<ICsnTopLevel>> lockTopLevel,
                                   int width, int height, Vector dpi)
        {
            _renderTarget = renderTarget;
            _lockTopLevel = lockTopLevel;
            Address = Marshal.AllocHGlobal(width * height * 4);
            Size = new PixelSize(width, height);
            RowBytes = width * 4;
            Dpi = dpi;
            Format = PixelFormat.Bgra8888;
        }

        public IntPtr Address { get; set; }
        public PixelSize Size { get; set; }
        public int Height { get; set; }
        public int RowBytes { get; set; }
        public Vector Dpi { get; set; }
        public PixelFormat Format { get; set; }
        public AlphaFormat AlphaFormat { get; set; }

        public void Dispose()
        {
            if (Address == IntPtr.Zero)
                return;

            _lockTopLevel(win =>
            {
                var fb = new CsnFramebuffer
                {
                    Data = Address.ToPointer(),
                    Dpi = new CsnVector { X = Dpi.X, Y = Dpi.Y },
                    Width = Size.Width,
                    Height = Size.Height,
                    PixelFormat = (CsnPixelFormat)Format.FormatEnum,
                    Stride = RowBytes
                };

                _renderTarget.SetFrame(&fb);

            });
            
            Marshal.FreeHGlobal(Address);

            Address = IntPtr.Zero;
        }
    }
}
