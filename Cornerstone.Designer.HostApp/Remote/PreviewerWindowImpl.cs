using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Remote.Server;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Remote.Protocol.Viewport;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.DesignerSupport.Remote
{
    class PreviewerWindowImpl : RemoteServerTopLevelImpl, IWindowImpl
    {
        private readonly ICornerstoneRemoteTransportConnection _transport;

        public PreviewerWindowImpl(ICornerstoneRemoteTransportConnection transport) : base(transport)
        {
            _transport = transport;
            ClientSize = new Size(1, 1);
        }

        public void Show(bool activate, bool isDialog)
        {
            // The resize render runs before StartRendering, so it often paints nothing.
            // Ask again after Show returns and the render loop is running.
            Dispatcher.UIThread.Post(RenderAndSendFrameIfNeeded);
        }

        public void Hide()
        {
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
        }

        public override double DesktopScaling => 1.0;
        public PixelPoint Position { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public Func<WindowCloseReason, bool> Closing { get; set; }
        public WindowState WindowState { get; set; }
        public bool WindowStateGetterIsUsable => false;
        public Action<WindowState> WindowStateChanged { get; set; }
        public Size MaxAutoSizeHint { get; } = new Size(4096, 4096);

        protected override void OnMessage(ICornerstoneRemoteTransportConnection transport, object obj)
        {
            // Viewport allocation carries the monitor DPI, not the preview zoom.
            // Applying it snaps RenderScaling back to 100% after ClientRenderInfo.
            if (obj is ClientViewportAllocatedMessage)
            {
                return;
            }
            base.OnMessage(transport, obj);
        }
        
        public void Resize(Size clientSize, WindowResizeReason reason)
        {
            // Don't let it clientSize be unconstrained or risk running Out Of Memory 
            clientSize = new Size(
                Math.Min(clientSize.Width, MaxAutoSizeHint.Width),
                Math.Min(clientSize.Height, MaxAutoSizeHint.Height)
            );

            _transport.Send(new RequestViewportResizeMessage
            {
                Width = Math.Ceiling(clientSize.Width * RenderScaling),
                Height = Math.Ceiling(clientSize.Height * RenderScaling)
            });
            ClientSize = clientSize;
            RenderAndSendFrameIfNeeded();
        }

        public void Move(PixelPoint point)
        {
            
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public Action GotInputWhenDisabled { get; set; }        
        
        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public PlatformRequestedDrawnDecoration RequestedDrawnDecorations { get; }
        public Thickness ExtendedMargins { get; } = new Thickness();

        public bool IsClientAreaExtendedToDecorations { get; }

        public Thickness OffScreenMargin { get; } = new Thickness();

        public bool NeedsManagedDecorations => false;

        public override object TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
            {
                return new NoopStorageProvider();
            }

            if (featureType == typeof(IScreenImpl))
            {
                return new ScreenStub();
            }

            return base.TryGetFeature(featureType);
        }
        
        public void Activate()
        {
        }
        
        public void SetTitle(string title)
        {
        }

        public void SetWindowDecorations(WindowDecorations enabled)
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        public void ShowTaskbarIcon(bool value)
        {
        }

        public void CanResize(bool value)
        {
        }

        public void SetCanMinimize(bool value)
        {
        }

        public void SetCanMaximize(bool value)
        {
        }

        public void SetTopmost(bool value)
        {
        }

        public void SetParent(IWindowImpl parent)
        {
        }

        public void SetEnabled(bool enable)
        {
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {            
        }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {            
        }
    }
}
