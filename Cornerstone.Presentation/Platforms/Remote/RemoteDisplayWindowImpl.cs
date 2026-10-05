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

namespace Cornerstone.Presentation.Platforms.Remote
{
	internal sealed class RemoteDisplayWindowImpl : RemoteServerTopLevelImpl, IWindowImpl
	{
		private readonly ICornerstoneRemoteTransportConnection _transport;

		public RemoteDisplayWindowImpl(ICornerstoneRemoteTransportConnection transport)
			: base(transport)
		{
			_transport = transport;
			ClientSize = new Size(960, 640);
		}

		public override double DesktopScaling => RenderScaling;

		public PixelPoint Position { get; set; }

		public Action<PixelPoint> PositionChanged { get; set; }

		public Action Deactivated { get; set; }

		public Action Activated { get; set; }

		public Func<WindowCloseReason, bool> Closing { get; set; }

		public WindowState WindowState { get; set; }

		public bool WindowStateGetterIsUsable => false;

		public Action<WindowState> WindowStateChanged { get; set; }

		public Size MaxAutoSizeHint { get; } = new Size(4096, 4096);

		public Action GotInputWhenDisabled { get; set; }

		public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

		public PlatformRequestedDrawnDecoration RequestedDrawnDecorations { get; }

		public Thickness ExtendedMargins { get; } = new Thickness();

		public bool IsClientAreaExtendedToDecorations { get; }

		public Thickness OffScreenMargin { get; } = new Thickness();

		public bool NeedsManagedDecorations => false;

		public void Show(bool activate, bool isDialog)
		{
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

		public void Resize(Size clientSize, WindowResizeReason reason)
		{
			clientSize = new Size(
				Math.Min(clientSize.Width, MaxAutoSizeHint.Width),
				Math.Min(clientSize.Height, MaxAutoSizeHint.Height));
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

		public override object TryGetFeature(Type featureType)
		{
			if (featureType == typeof(IStorageProvider))
			{
				return new NoopStorageProvider();
			}

			if (featureType == typeof(IScreenImpl))
			{
				return PresentationLocator.Current.GetService<IScreenImpl>() ?? base.TryGetFeature(featureType);
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
