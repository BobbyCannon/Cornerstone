using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls.Embedding.Offscreen;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Remote.Protocol.Input;
using Cornerstone.Presentation.Remote.Protocol.Viewport;
using Cornerstone.Presentation.Threading;
using Key = Cornerstone.Presentation.Input.Key;
using PhysicalKey = Cornerstone.Presentation.Input.PhysicalKey;
using ProtocolPixelFormat = Cornerstone.Presentation.Remote.Protocol.Viewport.PixelFormat;
using ProtocolMouseButton = Cornerstone.Presentation.Remote.Protocol.Input.MouseButton;

namespace Cornerstone.Presentation.Controls.Remote.Server
{
    [Unstable]
    internal partial class RemoteServerTopLevelImpl : OffscreenTopLevelImplBase, IFramebufferPlatformSurface, ITopLevelImpl
    {
        private readonly ICornerstoneRemoteTransportConnection _transport;
        private readonly object _lock = new();
        private readonly Action _sendLastFrameIfNeeded;
        private readonly Action _renderAndSendFrameIfNeeded;
        private Framebuffer _framebuffer = Framebuffer.Empty;
        private const int MaxFramesAwaitingAck = 2;
        private long _lastSentFrame = -1;
        private long _lastReceivedFrame = -1;
        private long _nextFrameNumber = 1;
        private int _framesAwaitingAck;
        private bool _frameWriteActive;
        private byte[] _sendPixels;
        private string _lastFrameDiagnostic;
        private ClientViewportAllocatedMessage? _pendingAllocation;
        private ProtocolPixelFormat? _format;
        private PointerMovedEventMessage _pendingPointerMove;
        private bool _pointerMovePosted;

        public RemoteServerTopLevelImpl(ICornerstoneRemoteTransportConnection transport)
        {
            _sendLastFrameIfNeeded = SendLastFrameIfNeeded;
            _renderAndSendFrameIfNeeded = RenderAndSendFrameIfNeeded;
            _lastFrameDiagnostic = null;
            _pendingPointerMove = null;
            _pointerMovePosted = false;
            _framesAwaitingAck = 0;
            _frameWriteActive = false;
            _sendPixels = null;

            _transport = transport;
            _transport.OnMessage += OnMessage;

            KeyboardDevice = PresentationLocator.Current.GetRequiredService<IKeyboardDevice>();
        }

        /// <summary>
        /// Press/release/keys at Send so they beat Normal timers. Moves are coalesced
        /// to one dispatch so they do not starve render.
        /// </summary>
        private static void PostRemoteInput(Action action)
        {
            Dispatcher.UIThread.Post(action, DispatcherPriority.Send);
        }

        private void FlushPendingPointerMove()
        {
            PointerMovedEventMessage move;
            lock (_lock)
            {
                move = _pendingPointerMove;
                _pendingPointerMove = null;
                _pointerMovePosted = false;
            }

            if ((move == null) || (InputRoot == null))
            {
                return;
            }

            Input?.Invoke(new RawPointerEventArgs(
                MouseDevice,
                0,
                InputRoot,
                RawPointerEventType.Move,
                new Point(move.X, move.Y),
                GetCornerstoneRawInputModifiers(move.Modifiers)));
        }

        private void QueuePointerMove(PointerMovedEventMessage pointer)
        {
            _pendingPointerMove = pointer;
            if (_pointerMovePosted)
            {
                return;
            }

            _pointerMovePosted = true;
            PostRemoteInput(FlushPendingPointerMove);
        }

        private static RawPointerEventType GetCornerstoneEventType(ProtocolMouseButton button, bool pressed)
        {
            switch (button)
            {
                case ProtocolMouseButton.Left:
                    return pressed ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp;

                case ProtocolMouseButton.Middle:
                    return pressed ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp;

                case ProtocolMouseButton.Right:
                    return pressed ? RawPointerEventType.RightButtonDown : RawPointerEventType.RightButtonUp;

                default:
                    return RawPointerEventType.Move;
            }
        }

        private static RawInputModifiers GetCornerstoneRawInputModifiers(InputModifiers[]? modifiers)
        {
            var result = RawInputModifiers.None;

            if (modifiers == null)
            {
                return result;
            }

            foreach(var modifier in modifiers)
            {
                switch (modifier)
                {
                    case InputModifiers.Control:
                        result |= RawInputModifiers.Control;
                        break;

                    case InputModifiers.Alt:
                        result |= RawInputModifiers.Alt;
                        break;

                    case InputModifiers.Shift:
                        result |= RawInputModifiers.Shift;
                        break;

                    case InputModifiers.Windows:
                        result |= RawInputModifiers.Meta;
                        break;

                    case InputModifiers.LeftMouseButton:
                        result |= RawInputModifiers.LeftMouseButton;
                        break;

                    case InputModifiers.MiddleMouseButton:
                        result |= RawInputModifiers.MiddleMouseButton;
                        break;

                    case InputModifiers.RightMouseButton:
                        result |= RawInputModifiers.RightMouseButton;
                        break;
                }
            }

            return result;
        }

        protected virtual void OnMessage(ICornerstoneRemoteTransportConnection transport, object obj)
        {
            lock (_lock)
            {
                switch (obj)
                {
                    case FrameReceivedMessage lastFrame:
                        _lastReceivedFrame = Math.Max(lastFrame.SequenceId, _lastReceivedFrame);
                        if (_framesAwaitingAck > 0)
                            _framesAwaitingAck--;
                        Dispatcher.UIThread.Post(_sendLastFrameIfNeeded);
                        break;

                    case ClientRenderInfoMessage renderInfo:
                        Dispatcher.UIThread.Post(() =>
                        {
                            RenderScaling = renderInfo.DpiX / 96.0;
                            RenderAndSendFrameIfNeeded();
                        });
                        break;

                    case ClientSupportedPixelFormatsMessage supportedFormats:
                        _format = TryGetValidPixelFormat(supportedFormats.Formats);
                        Dispatcher.UIThread.Post(_renderAndSendFrameIfNeeded);
                        break;

                    case MeasureViewportMessage measure:
                        Dispatcher.UIThread.Post(() =>
                        {
                            var m = Measure(new Size(measure.Width, measure.Height));
                            _transport.Send(new MeasureViewportMessage
                            {
                                Width = m.Width,
                                Height = m.Height
                            });
                        });
                        break;

                    case ClientViewportAllocatedMessage allocated:
                        if (_pendingAllocation == null)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                ClientViewportAllocatedMessage allocation;
                                lock (_lock)
                                {
                                    allocation = _pendingAllocation!;
                                    _pendingAllocation = null;
                                }

                                RenderScaling = allocation.DpiX / 96.0;
                                ClientSize = new Size(allocation.Width, allocation.Height);
                                RenderAndSendFrameIfNeeded();
                            });
                        }

                        _pendingAllocation = allocated;
                        break;

                    case PointerMovedEventMessage pointer:
                        QueuePointerMove(pointer);
                        break;

                    case PointerPressedEventMessage pressed:
                        PostRemoteInput(() =>
                        {
                            FlushPendingPointerMove();
                            Input?.Invoke(new RawPointerEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                GetCornerstoneEventType(pressed.Button, true),
                                new Point(pressed.X, pressed.Y),
                                GetCornerstoneRawInputModifiers(pressed.Modifiers)));
                        });
                        break;

                    case PointerReleasedEventMessage released:
                        PostRemoteInput(() =>
                        {
                            FlushPendingPointerMove();
                            Input?.Invoke(new RawPointerEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                GetCornerstoneEventType(released.Button, false),
                                new Point(released.X, released.Y),
                                GetCornerstoneRawInputModifiers(released.Modifiers)));
                        });
                        break;

                    case ScrollEventMessage scroll:
                        PostRemoteInput(() =>
                        {
                            Input?.Invoke(new RawMouseWheelEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                new Point(scroll.X, scroll.Y),
                                new Vector(scroll.DeltaX, scroll.DeltaY),
                                GetCornerstoneRawInputModifiers(scroll.Modifiers)));
                        });
                        break;

                    case KeyEventMessage key:
                        PostRemoteInput(() =>
                        {
                            Input?.Invoke(new RawKeyEventArgs(
                                KeyboardDevice,
                                0,
                                InputRoot!,
                                key.IsDown ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                                (Key)key.Key,
                                GetCornerstoneRawInputModifiers(key.Modifiers),
                                (PhysicalKey)key.PhysicalKey,
                                key.KeySymbol));
                        });
                        break;

                    case TextInputEventMessage text:
                        PostRemoteInput(() =>
                        {
                            Input?.Invoke(new RawTextInputEventArgs(
                                KeyboardDevice,
                                0,
                                InputRoot!,
                                text.Text));
                        });
                        break;
                }
            }
        }

        private static ProtocolPixelFormat? TryGetValidPixelFormat(ProtocolPixelFormat[]? formats)
        {
            if (formats is not null)
            {
                foreach (var format in formats)
                {
                    if (format is >= 0 and <= ProtocolPixelFormat.MaxValue)
                        return format;
                }
            }

            return null;
        }

        protected virtual Size Measure(Size constraint)
        {
            var l = InputRoot!.RootElement;
            l.Measure(constraint);
            return l.DesiredSize;
        }

        public override IPlatformRenderSurface[] Surfaces => [this];

        private Framebuffer GetOrCreateFramebuffer()
        {
            lock (_lock)
            {
                if (_format is not { } format)
                    _framebuffer = Framebuffer.Empty;
                else if (_framebuffer.Format != format || _framebuffer.ClientSize != ClientSize || _framebuffer.RenderScaling != RenderScaling)
                    _framebuffer = new Framebuffer(format, ClientSize, RenderScaling);

                return _framebuffer;
            }
        }

        private void SendLastFrameIfNeeded()
        {
            if (IsDisposed)
                return;

            FrameMessage message;
            lock (_lock)
            {
                // The socket write owns _sendPixels. A newer paint stays in the framebuffer
                // and goes out when this write finishes. Two unacknowledged frames is enough
                // to keep the pipe full; more means the client is not reading.
                if (_frameWriteActive || (_framesAwaitingAck >= MaxFramesAwaitingAck))
                    return;

                var pixels = _sendPixels;
                message = _framebuffer.CopyRendered(_nextFrameNumber, pixels, out pixels);
                if (message == null)
                    return;

                _sendPixels = pixels;
                _frameWriteActive = true;
                _framesAwaitingAck++;
                _lastSentFrame = message.SequenceId;
                _nextFrameNumber = message.SequenceId + 1;
            }

            WriteFrameDiagnostic("Preview host: frame sent " + message.Width + "x" + message.Height);
            Task send;
            try
            {
                send = _transport.Send(message);
            }
            catch (Exception)
            {
                lock (_lock)
                {
                    _frameWriteActive = false;
                    if (_framesAwaitingAck > 0)
                        _framesAwaitingAck--;
                }

                throw;
            }

            send.ContinueWith(
                CompleteFrameWrite,
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);
        }

        private void CompleteFrameWrite(Task task)
        {
            if (task.IsFaulted)
                task.Exception.Handle(_ => true);

            lock (_lock)
                _frameWriteActive = false;

            if (IsDisposed)
                return;

            Dispatcher.UIThread.Post(_sendLastFrameIfNeeded);
        }

        protected void RenderAndSendFrameIfNeeded()
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                if (_format is null)
                {
                    WriteFrameDiagnostic("Preview host: frame skipped (pixel format is not set)");
                    return;
                }
            }

            var framebuffer = GetOrCreateFramebuffer();

            if (framebuffer.Stride > 0)
                Paint?.Invoke(new Rect(framebuffer.ClientSize));
            else
                WriteFrameDiagnostic("Preview host: frame skipped (framebuffer is empty)");

            SendLastFrameIfNeeded();
        }

        private void WriteFrameDiagnostic(string message)
        {
            if (_lastFrameDiagnostic == message)
                return;

            _lastFrameDiagnostic = message;
            Console.WriteLine(message);
        }

        public override IMouseDevice MouseDevice { get; } = new MouseDevice();

        public IKeyboardDevice KeyboardDevice { get; }
        
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() =>
            new FuncFramebufferRenderTarget(() => GetOrCreateFramebuffer().Lock(_sendLastFrameIfNeeded));
    }
}
