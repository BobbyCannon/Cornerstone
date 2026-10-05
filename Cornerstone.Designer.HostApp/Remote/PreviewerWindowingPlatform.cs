using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.DesignerSupport.Remote
{
    class PreviewerWindowingPlatform : IWindowingPlatform
    {
        static readonly IKeyboardDevice Keyboard = new KeyboardDevice();
        private static ICornerstoneRemoteTransportConnection s_transport;
        private static DetachableTransportConnection s_lastWindowTransport;
        private static PreviewerWindowImpl s_lastWindow;
        public static List<object> PreFlightMessages = new List<object>();

        public ITrayIconImpl CreateTrayIcon() => null;

        public IWindowImpl CreateWindow() => new WindowStub();
        public ITopLevelImpl CreateEmbeddableTopLevel() => CreateEmbeddableWindow();

        public IWindowImpl CreateEmbeddableWindow()
        {
            if (s_lastWindow != null)
            {
                s_lastWindowTransport.Dispose();
                try
                {
                    s_lastWindow.Dispose();
                }
                catch
                {
                    //Ignore
                }
            }
            s_lastWindow =
                new PreviewerWindowImpl(s_lastWindowTransport = new DetachableTransportConnection(s_transport));
            foreach (var pf in PreFlightMessages)
                s_lastWindowTransport.FireOnMessage(s_lastWindowTransport, pf);
            return s_lastWindow;
        }

        public void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder)
            => zOrder.Clear();

        public static void Initialize(ICornerstoneRemoteTransportConnection transport)
        {
            s_transport = transport;
            var instance = new PreviewerWindowingPlatform();
            PresentationLocator.CurrentMutable
                .Bind<ICursorFactory>().ToSingleton<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(Keyboard)
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IRenderLoop>().ToConstant(RenderLoop.FromTimer(new UiThreadRenderTimer(120)))
                .Bind<IWindowingPlatform>().ToConstant(instance)
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoaderStub>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

        }
    }
}
