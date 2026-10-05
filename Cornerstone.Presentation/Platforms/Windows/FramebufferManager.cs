using System;
using System.ComponentModel;
using System.Threading;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Internal;
using Cornerstone.Presentation.Platforms.Windows.Interop;

namespace Cornerstone.Presentation.Platforms.Windows
{
    internal class FramebufferManager : IFramebufferPlatformSurface, IDisposable
    {
        private const int _bytesPerPixel = 4;
        private static readonly PixelFormat s_format = PixelFormat.Bgra8888;

        internal const uint PresentMessage = (uint)UnmanagedMethods.WindowsMessage.WM_APP + 0x51;

        private readonly IntPtr _hwnd;
        private readonly bool _layeredPresent;
        private readonly object _lock;
        private readonly Action _onDisposeAction;

        private FramebufferData? _back;
        private FramebufferData? _front;
        private bool _presentPosted;

        public FramebufferManager(IntPtr hwnd, bool layeredPresent = false)
        {
            _hwnd = hwnd;
            _layeredPresent = layeredPresent;
            _lock = new object();
            _onDisposeAction = DrawAndUnlock;
        }

        public ILockedFramebuffer Lock()
        {
            Monitor.Enter(_lock);

            LockedFramebuffer? fb = null;

            try
            {
                UnmanagedMethods.GetClientRect(_hwnd, out var rc);
                var width = Math.Max(1, rc.right - rc.left);
                var height = Math.Max(1, rc.bottom - rc.top);

                if (_back is null || _back.Value.Size.Width != width || _back.Value.Size.Height != height)
                {
                    _back?.Dispose();
                    _back = AllocateFramebufferData(width, height, _layeredPresent);
                }

                var framebufferData = _back.Value;

                return fb = new LockedFramebuffer(
                    framebufferData.Bits, framebufferData.Size, framebufferData.RowBytes,
                    GetCurrentDpi(), s_format, AlphaFormat.Premul, _onDisposeAction);
            }
            finally
            {
                if (fb is null)
                    Monitor.Exit(_lock);
            }
        }

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);

        public void Dispose()
        {
            lock (_lock)
            {
                _back?.Dispose();
                _back = null;
                _front?.Dispose();
                _front = null;
            }
        }

        internal void PresentLayeredOnUiThread(Action<Span<byte>, PixelSize, int> stamp)
        {
            FramebufferData? frame;
            lock (_lock)
            {
                _presentPosted = false;
                frame = _front;
                if (stamp != null && frame is { } data && data.Bits != IntPtr.Zero
                    && data.Size.Width > 0 && data.Size.Height > 0)
                {
                    var length = (long)data.RowBytes * data.Size.Height;
                    if (length > 0 && length <= int.MaxValue)
                    {
                        unsafe
                        {
                            stamp(new Span<byte>((void*)data.Bits, (int)length), data.Size, data.RowBytes);
                        }
                    }
                }
            }

            if (frame is { } present && present.HBitmap != IntPtr.Zero)
                UpdateLayeredWindow(_hwnd, present);
        }

        private void DrawAndUnlock()
        {
            try
            {
                if (_back.HasValue)
                {
                    if (_layeredPresent)
                    {
                        (_front, _back) = (_back, _front);
                        if (!_presentPosted)
                        {
                            _presentPosted = true;
                            UnmanagedMethods.PostMessage(_hwnd, PresentMessage, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                    else
                        DrawToWindow(_hwnd, _back.Value);
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        private Vector GetCurrentDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable && Win32Platform.WindowsVersion >= PlatformConstants.Windows8_1)
            {
                var monitor =
                    UnmanagedMethods.MonitorFromWindow(_hwnd, UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                    monitor,
                    UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                    out var dpix,
                    out var dpiy) == 0)
                {
                    return new Vector(dpix, dpiy);
                }
            }

            return new Vector(96, 96);
        }

        private static FramebufferData AllocateFramebufferData(int width, int height, bool layered)
        {
            if (layered)
            {
                var header = new UnmanagedMethods.BITMAPINFOHEADER();
                header.Init();
                header.biWidth = width;
                header.biHeight = -height;
                header.biPlanes = 1;
                header.biBitCount = 32;
                header.biCompression = UnmanagedMethods.BitmapCompressionMode.BI_RGB;
                var hBitmap = UnmanagedMethods.CreateDIBSection(IntPtr.Zero, ref header,
                    UnmanagedMethods.DIBColorTable.DIB_RGB_COLORS, out var bits, IntPtr.Zero, 0);
                if (hBitmap == IntPtr.Zero)
                    throw new Win32Exception();
                return new FramebufferData(bits, hBitmap, width, height);
            }

            var bitmapBlob = new UnmanagedBlob(width * height * _bytesPerPixel);
            return new FramebufferData(bitmapBlob, width, height);
        }

        private static void UpdateLayeredWindow(IntPtr hWnd, FramebufferData framebufferData)
        {
            if (hWnd == IntPtr.Zero || framebufferData.HBitmap == IntPtr.Zero)
                return;

            var size = new UnmanagedMethods.SIZE
            {
                X = framebufferData.Size.Width,
                Y = framebufferData.Size.Height
            };
            var source = new UnmanagedMethods.POINT { X = 0, Y = 0 };
            var blend = new UnmanagedMethods.BLENDFUNCTION
            {
                BlendOp = UnmanagedMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = UnmanagedMethods.AC_SRC_ALPHA
            };

            var memoryDc = UnmanagedMethods.CreateCompatibleDC(IntPtr.Zero);
            var previous = UnmanagedMethods.SelectObject(memoryDc, framebufferData.HBitmap);
            try
            {
                UnmanagedMethods.GdiFlush();
                UnmanagedMethods.UpdateLayeredWindow(hWnd, IntPtr.Zero, IntPtr.Zero, ref size, memoryDc, ref source, 0,
                    ref blend, UnmanagedMethods.ULW_ALPHA);
            }
            finally
            {
                UnmanagedMethods.SelectObject(memoryDc, previous);
                UnmanagedMethods.DeleteDC(memoryDc);
            }
        }

        private static void DrawToDevice(FramebufferData framebufferData, IntPtr hDC, int destX = 0, int destY = 0, int srcX = 0,
            int srcY = 0, int width = -1,
            int height = -1)
        {
            if (width == -1)
                width = framebufferData.Size.Width;
            if (height == -1)
                height = framebufferData.Size.Height;

            var bmpInfo = framebufferData.Header;

            UnmanagedMethods.SetDIBitsToDevice(hDC, destX, destY, (uint)width, (uint)height, srcX, srcY,
                0, (uint)framebufferData.Size.Height, framebufferData.Data.Address, ref bmpInfo, 0);
        }

        private static bool DrawToWindow(IntPtr hWnd, FramebufferData framebufferData, int destX = 0, int destY = 0, int srcX = 0,
            int srcY = 0, int width = -1,
            int height = -1)
        {
            if (framebufferData.Data.IsDisposed)
                throw new ObjectDisposedException("Framebuffer");

            if (hWnd == IntPtr.Zero)
                return false;

            var hDC = UnmanagedMethods.GetDC(hWnd);

            if (hDC == IntPtr.Zero)
                return false;

            try
            {
                DrawToDevice(framebufferData, hDC, destX, destY, srcX, srcY, width, height);
            }
            finally
            {
                UnmanagedMethods.ReleaseDC(hWnd, hDC);
            }

            return true;
        }

        private readonly struct FramebufferData
        {
            public UnmanagedBlob Data { get; }

            public IntPtr Bits { get; }

            public IntPtr HBitmap { get; }

            public PixelSize Size { get; }

            public int RowBytes => Size.Width * _bytesPerPixel;

            public UnmanagedMethods.BITMAPINFOHEADER Header { get; }

            public FramebufferData(UnmanagedBlob data, int width, int height)
            {
                Data = data;
                Bits = data.Address;
                HBitmap = IntPtr.Zero;
                Size = new PixelSize(width, height);

                var header = new UnmanagedMethods.BITMAPINFOHEADER();
                header.Init();

                header.biPlanes = 1;
                header.biBitCount = _bytesPerPixel * 8;
                header.Init();

                header.biWidth = width;
                header.biHeight = -height;

                Header = header;
            }

            public FramebufferData(IntPtr bits, IntPtr hBitmap, int width, int height)
            {
                Data = default;
                Bits = bits;
                HBitmap = hBitmap;
                Size = new PixelSize(width, height);
                Header = default;
            }

            public void Dispose()
            {
                if (HBitmap != IntPtr.Zero)
                    UnmanagedMethods.DeleteObject(HBitmap);
                else
                    Data.Dispose();
            }
        }
    }
}
