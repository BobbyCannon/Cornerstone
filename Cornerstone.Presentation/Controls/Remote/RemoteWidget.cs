using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Remote.Protocol.Viewport;
using Cornerstone.Presentation.Threading;
using PixelFormat = Cornerstone.Presentation.Platform.PixelFormat;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.Remote
{
    internal class RemoteWidget : Control
    {
        public enum SizingMode
        {
            Local,
            Remote
        }

        private readonly ICornerstoneRemoteTransportConnection _connection;
        private FrameMessage? _lastFrame;
        private WriteableBitmap? _bitmap;
        public RemoteWidget(ICornerstoneRemoteTransportConnection connection)
        {
            Mode = SizingMode.Local;

            _connection = connection;
            _connection.OnMessage += (t, msg) => Dispatcher.UIThread.Post(OnMessage, msg);
            _connection.Send(new ClientSupportedPixelFormatsMessage
            {
                Formats = new[]
                {
                    Cornerstone.Presentation.Remote.Protocol.Viewport.PixelFormat.Bgra8888,
                    Cornerstone.Presentation.Remote.Protocol.Viewport.PixelFormat.Rgba8888,
                }
            });
        }

        public SizingMode Mode { get; set; }

        private void OnMessage(object? msg)
        {
            if (msg is FrameMessage frame)
            {
                _connection.Send(new FrameReceivedMessage
                {
                    SequenceId = frame.SequenceId
                });
                _lastFrame = frame;
                InvalidateVisual();
            }
            
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            if (Mode == SizingMode.Local)
            {
                _connection.Send(new ClientViewportAllocatedMessage
                {
                    Width = finalRect.Width,
                    Height = finalRect.Height,
                    DpiX = 10 * 96,
                    DpiY = 10 * 96 //TODO: Somehow detect the actual DPI
                });
            }

            base.ArrangeCore(finalRect);
        }

        public sealed override void Render(DrawingContext context)
        {
            if (_lastFrame != null && _lastFrame.Width != 0 && _lastFrame.Height != 0)
            {
                var fmt = new PixelFormat((PixelFormatEnum) _lastFrame.Format);
                if (_bitmap == null || _bitmap.PixelSize.Width != _lastFrame.Width ||
                    _bitmap.PixelSize.Height != _lastFrame.Height)
                {
                    _bitmap?.Dispose();
                    _bitmap = new WriteableBitmap(new PixelSize(_lastFrame.Width, _lastFrame.Height),
                        new Vector(96, 96), fmt);
                }
                using (var l = _bitmap.Lock())
                {
                    var lineLen = (fmt == PixelFormat.Rgb565 ? 2 : 4) * _lastFrame.Width;
                    for (var y = 0; y < _lastFrame.Height; y++)
                        Marshal.Copy(_lastFrame.Data, y * _lastFrame.Stride,
                            new IntPtr(l.Address.ToInt64() + l.RowBytes * y), lineLen);
                }
                context.DrawImage(_bitmap, new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                    new Rect(Bounds.Size));
            }
            base.Render(context);
        }
    }
}
