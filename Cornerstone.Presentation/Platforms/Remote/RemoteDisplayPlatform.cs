using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Platforms.Remote
{
	internal sealed class RemoteDisplayPlatform : IWindowingPlatform
	{
		private static ICornerstoneRemoteTransportConnection s_transport;
		private static Compositor s_compositor;

		public ITrayIconImpl CreateTrayIcon() => null;

		public IWindowImpl CreateWindow() => new RemoteDisplayWindowImpl(s_transport);

		public ITopLevelImpl CreateEmbeddableTopLevel() => CreateWindow();

		public IWindowImpl CreateEmbeddableWindow() => CreateWindow();

		public void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder) => zOrder.Clear();

		[RequiresUnreferencedCode("Bson uses reflection")]
		public static void Initialize(Uri transportUri)
		{
			if (transportUri == null)
			{
				throw new ArgumentNullException(nameof(transportUri));
			}

			if (!string.Equals(transportUri.Scheme, "tcp-bson", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Remote display requires a tcp-bson URI.", nameof(transportUri));
			}

			var host = IPAddress.Parse(transportUri.Host);
			var port = transportUri.Port;
			var transport = new BsonTcpTransport().Connect(host, port).GetAwaiter().GetResult();
			transport.Start();
			s_transport = transport;

			Dispatcher.InitializeUIThreadDispatcher(new ManagedDispatcherImpl(null));

			var clipboardImpl = new HeadlessClipboardImplStub();
			var clipboard = new Clipboard(clipboardImpl);
			var renderTimer = new UiThreadRenderTimer(60);
			var renderLoop = RenderLoop.FromTimer(renderTimer);

			PresentationLocator.CurrentMutable
				.Bind<IClipboardImpl>().ToConstant(clipboardImpl)
				.Bind<IClipboard>().ToConstant(clipboard)
				.Bind<ICursorFactory>().ToSingleton<HeadlessCursorFactoryStub>()
				.Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
				.Bind<IPlatformIconLoader>().ToSingleton<HeadlessIconLoaderStub>()
				.Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
				.Bind<IScreenImpl>().ToSingleton<HeadlessScreensStub>()
				.Bind<IRenderLoop>().ToConstant(renderLoop)
				.Bind<IWindowingPlatform>().ToConstant(new RemoteDisplayPlatform())
				.Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
				.Bind<KeyGestureFormatInfo>().ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>()));

			s_compositor = new Compositor(null);
			PresentationLocator.CurrentMutable.Bind<Compositor>().ToConstant(s_compositor);
		}
	}
}
