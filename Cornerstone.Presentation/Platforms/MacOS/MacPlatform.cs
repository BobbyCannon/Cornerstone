using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;
using MicroCom.Runtime;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal sealed class MacPlatform : IWindowingPlatform, IDisposable
    {
        private readonly ICornerstoneNativeFactory _factory;
        private MacOSPlatformOptions? _options;
        private IPlatformGraphics? _platformGraphics;
        private int _isDisposed;

        [DllImport("libCornerstoneNative")]
        static extern IntPtr CreateCornerstoneNative();

        [DllImport("libCornerstoneNative")]
        static extern void CsnSetNativeBehindComposition(int enabled);

        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();
        internal static Compositor Compositor { get; private set; } = null!;

        public static MacPlatform Initialize(IntPtr factory, MacOSPlatformOptions options)
        {
            var factoryProxy = MicroComRuntime.CreateProxyFor<ICornerstoneNativeFactory>(factory, true);

            PresentationLocator.CurrentMutable.Bind<ICornerstoneNativeFactory>().ToConstant(factoryProxy);
            
            var result = new MacPlatform(factoryProxy);
            
            result.DoInitialize(options);

            return result;
        }

        delegate IntPtr CreateCornerstoneNativeDelegate();

        public static MacPlatform Initialize(MacOSPlatformOptions options)
        {
            if (options.NativeLibraryPath != null)
            {
                var lib = NativeLibrary.Load(options.NativeLibraryPath);
                if (!NativeLibrary.TryGetExport(lib, "CreateCornerstoneNative", out var proc))
                {
                    throw new InvalidOperationException(
                        "Unable to get \"CreateCornerstoneNative\" export from native library");
                }
                var d = Marshal.GetDelegateForFunctionPointer<CreateCornerstoneNativeDelegate>(proc);

                return Initialize(d(), options);
            }
            else
                return Initialize(CreateCornerstoneNative(), options);
        }

        public void SetupApplicationMenuExporter()
        {
            var exporter = new MacMenuExporter(_factory);
        }

        public void SetupApplicationDockMenuExporter()
        {
            _ = new MacMenuExporter(_factory, MacMenuExporter.MenuTarget.Dock);
        }

        public void SetupApplicationName()
        {
            if (!string.IsNullOrWhiteSpace(Application.Current!.Name))
            {
                _factory.MacOptions.SetApplicationTitle(Application.Current.Name);
            }
        }

        private MacPlatform(ICornerstoneNativeFactory factory)
        {
            _factory = factory;
        }

        class GCHandleDeallocator : NativeCallbackBase, ICsnGCHandleDeallocatorCallback
        {
            public void FreeGCHandle(IntPtr handle)
            {
                GCHandle.FromIntPtr(handle).Free();
            }
        }

        void DoInitialize(MacOSPlatformOptions options)
        {
            _options = options;

            var applicationPlatform = new MacApplicationPlatform(this);

            NativeAirspace.BehindComposition = options.NativeBehindComposition;
            try
            {
                CsnSetNativeBehindComposition(options.NativeBehindComposition ? 1 : 0);
            }
            catch (EntryPointNotFoundException)
            {
                // Older libCornerstoneNative without native-behind export; hosting stays child-on-top.
            }

            if (_factory.MacOptions != null)
                _factory.MacOptions.SetDisableAppDelegate(options.DisableAppDelegate ? 1 : 0);

            _factory.Initialize(new GCHandleDeallocator(), applicationPlatform, new CsnDispatcher());

            if (_factory.MacOptions != null)
            {
                _factory.MacOptions.SetShowInDock(options.ShowInDock ? 1 : 0);
                _factory.MacOptions.SetDisableSetProcessName(options.DisableSetProcessName ? 1 : 0);
            }

            var clipboardImpl = new ClipboardImpl(_factory.CreateClipboard());
            var clipboard = new Clipboard(clipboardImpl);

            Dispatcher.InitializeUIThreadDispatcher(new DispatcherImpl(_factory.CreatePlatformThreadingInterface()));
            PresentationLocator.CurrentMutable
                .Bind<ICursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IScreenImpl>().ToConstant(new ScreenImpl(_factory.CreateScreens))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IPlatformSettings>().ToConstant(new NativePlatformSettings(_factory.CreatePlatformSettings()))
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IClipboardImpl>().ToConstant(clipboardImpl)
                .Bind<IClipboard>().ToConstant(clipboard)
                .Bind<IRenderLoop>().ToConstant(RenderLoop.FromTimer(new ThreadProxyRenderTimer(new MacRenderTimer(_factory.CreatePlatformRenderTimer()))))
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new MacOSMountedVolumeInfoProvider())
                .Bind<IPlatformDragSource>().ToConstant(new MacDragSource(_factory))
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(applicationPlatform)
                .Bind<INativeApplicationCommands>().ToConstant(new MacOSNativeMenuCommands(_factory.CreateApplicationCommands()))
                .Bind<IActivatableLifetime>().ToSingleton<MacOSActivatableLifetime>()
                .Bind<IStorageProviderFactory>().ToConstant(new StorageProviderApi(_factory.CreateStorageProvider(), options.AppSandboxEnabled));

            var hotkeys = new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt);
            hotkeys.MoveCursorToTheStartOfLine.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers));
            hotkeys.MoveCursorToTheStartOfLineWithSelection.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
            hotkeys.MoveCursorToTheEndOfLine.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers));
            hotkeys.MoveCursorToTheEndOfLineWithSelection.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));

            PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(hotkeys);

            PresentationLocator.CurrentMutable.Bind<KeyGestureFormatInfo>().ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>()
                    {
                        { Key.Back , "⌫" }, { Key.Down , "↓" }, { Key.End , "↘" }, { Key.Escape , "⎋" },
                        { Key.Home , "↖" }, { Key.Left , "←" }, { Key.Return , "↩" }, { Key.PageDown , "⇟" },
                        { Key.PageUp , "⇞" }, { Key.Right , "→" }, { Key.Space , "␣" }, { Key.Tab , "⇥" },
                        { Key.Up , "↑" }
                    }, ctrl: "⌃", meta: "⌘", shift: "⇧", alt: "⌥"));

            foreach (var mode in _options.RenderingMode)
            {
                if (mode == MacRenderingMode.OpenGl)
                {
                    try
                    {
                        _platformGraphics = new MacGlPlatformGraphics(_factory.ObtainGlDisplay(), _factory);
                        break;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                else if (mode == MacRenderingMode.Metal)
                {
                    try
                    {
                        var metal = new MetalPlatformGraphics(_factory);
                        metal.CreateContext().Dispose();
                        _platformGraphics = metal;
                        break;
                    }
                    catch
                    {
                        // Ignored
                    }
                }
                else if (mode == MacRenderingMode.Software)
                    break;
            }

            if (_platformGraphics != null)
                PresentationLocator.CurrentMutable
                    .Bind<IPlatformGraphics>().ToConstant(_platformGraphics);
            

            Compositor = new Compositor(_platformGraphics, true);
            PresentationLocator.CurrentMutable.Bind<Compositor>().ToConstant(Compositor);

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0)
                return;

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _factory.Dispose();
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            return new TrayIconImpl(_factory);
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl(_factory, _options ?? new MacOSPlatformOptions());
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }
        
        public ITopLevelImpl CreateEmbeddableTopLevel()
        {
            return new EmbeddableTopLevelImpl(_factory);
        }

        public void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder)
        {
            for (var i = 0; i < windows.Length; i++)
            {
                zOrder[i] = (windows[i] as WindowImpl)?.ZOrder?.ToInt64() ?? 0;
            }
        }
    }
}
