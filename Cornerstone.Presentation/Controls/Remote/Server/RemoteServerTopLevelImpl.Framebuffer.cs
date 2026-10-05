using System;
using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Remote.Protocol.Viewport;
using PlatformPixelFormat = Cornerstone.Presentation.Platform.PixelFormat;
using ProtocolPixelFormat = Cornerstone.Presentation.Remote.Protocol.Viewport.PixelFormat;

namespace Cornerstone.Presentation.Controls.Remote.Server
{
    internal partial class RemoteServerTopLevelImpl
    {
        private enum FrameStatus
        {
            NotRendered,
            Rendered,
            CopiedToMessage
        }

        private sealed class Framebuffer
        {
            public static Framebuffer Empty { get; } = new(ProtocolPixelFormat.Rgba8888, default, 1.0);

            private readonly double _dpi;
            private readonly PixelSize _frameSize;
            private readonly object _dataLock = new();
            private readonly byte[] _data; // for rendering only
            private readonly byte[] _dataCopy; // for messages only
            private FrameStatus _status = FrameStatus.NotRendered;

            public Framebuffer(ProtocolPixelFormat format, Size clientSize, double renderScaling)
            {
                var frameSize = PixelSize.FromSize(clientSize, renderScaling);
                if (frameSize.Width <= 0 || frameSize.Height <= 0)
                    frameSize = PixelSize.Empty;

                var bpp = format == ProtocolPixelFormat.Rgb565 ? 2 : 4;
                var stride = frameSize.Width * bpp;
                var dataLength = Math.Max(0, stride * frameSize.Height);

                _dpi = renderScaling * 96.0;
                _frameSize = frameSize;
                Format = format;
                ClientSize = clientSize;
                RenderScaling = renderScaling;

                (Stride, _data, _dataCopy) = dataLength > 0 ?
                    (stride, new byte[dataLength], new byte[dataLength]) :
                    (0, Array.Empty<byte>(), Array.Empty<byte>());
            }

            public ProtocolPixelFormat Format { get; }

            public Size ClientSize { get; }

            public double RenderScaling { get; }

            public int Stride { get; }

            public ILockedFramebuffer Lock(Action onUnlocked)
            {
                var handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                Monitor.Enter(_dataLock);

                try
                {
                    return new LockedFramebuffer(
                        handle.AddrOfPinnedObject(),
                        _frameSize,
                        Stride,
                        new Vector(_dpi, _dpi),
                        new PlatformPixelFormat((PixelFormatEnum)Format),
                        Format == ProtocolPixelFormat.Rgb565 ? AlphaFormat.Opaque : AlphaFormat.Premul,
                        () =>
                        {
                            handle.Free();
                            Array.Copy(_data, _dataCopy, _data.Length);
                            _status = FrameStatus.Rendered;
                            Monitor.Exit(_dataLock);
                            onUnlocked();
                        });
                }
                catch
                {
                    handle.Free();
                    Monitor.Exit(_dataLock);
                    throw;
                }
            }

            /// <summary>
            /// Copies the latest rendered pixels into <paramref name="reuse"/> when that array is the right size.
            /// The returned message owns <paramref name="pixels"/> until the caller finishes writing it.
            /// </summary>
            public FrameMessage CopyRendered(long sequenceId, byte[] reuse, out byte[] pixels)
            {
                lock (_dataLock)
                {
                    if ((_status != FrameStatus.Rendered) || (_dataCopy.Length == 0))
                    {
                        pixels = reuse;
                        return null;
                    }

                    if ((reuse == null) || (reuse.Length != _dataCopy.Length))
                        reuse = new byte[_dataCopy.Length];

                    Array.Copy(_dataCopy, 0, reuse, 0, _dataCopy.Length);
                    _status = FrameStatus.CopiedToMessage;
                    pixels = reuse;
                    return new FrameMessage
                    {
                        SequenceId = sequenceId,
                        Data = reuse,
                        Format = Format,
                        Width = _frameSize.Width,
                        Height = _frameSize.Height,
                        Stride = Stride,
                        DpiX = _dpi,
                        DpiY = _dpi
                    };
                }
            }
        }
    }
}
