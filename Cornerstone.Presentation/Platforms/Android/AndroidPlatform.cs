using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Android.Platform;
using Cornerstone.Presentation.Android.Platform.Input;
using Cornerstone.Presentation.Android.Platform.Vulkan;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.OpenGL.Egl;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Vulkan;

namespace Cornerstone.Presentation
{
    public static class AndroidApplicationExtensions
    {
        public static AppBuilder UseAndroid(this AppBuilder builder)
        {
            return builder
                .UseAndroidRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() => AndroidPlatform.Initialize(), "Android")
                .UseHarfBuzz()
                .UseSkia();
        }
    }

    /// <summary>
    /// Represents the rendering mode for platform graphics.
    /// </summary>
    public enum AndroidRenderingMode
    {
        /// <summary>
        /// Cornerstone is rendered into a framebuffer.
        /// </summary>
        Software = 1,

        /// <summary>
        /// Enables android EGL rendering.
        /// </summary>
        Egl = 2,

        /// <summary>
        /// Enables Vulkan rendering
        /// </summary>
        Vulkan = 3
    }

    public sealed class AndroidPlatformOptions
    {
        /// <summary>
        /// Gets or sets Cornerstone rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="AndroidRenderingMode.Egl"/>, <see cref="AndroidRenderingMode.Software"/>.
        /// </summary>
        /// <remarks>
        /// If application should work on as wide range of devices as possible, at least add <see cref="AndroidRenderingMode.Software"/> as a fallback value.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<AndroidRenderingMode> RenderingMode { get; set; } = new[]
        {
            AndroidRenderingMode.Egl, AndroidRenderingMode.Software
        };

        /// <summary>
        /// Non-opaque TextureView with native views below and hole punch.
        /// Default on. Set false for SurfaceView child-on-top. See Presentation/NativeLayering.md.
        /// </summary>
        public bool NativeBehindComposition { get; set; } = true;
    }
}

namespace Cornerstone.Presentation.Android
{
    class AndroidPlatform
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public static AndroidPlatformOptions? Options { get; private set; }

        internal static Compositor? Compositor { get; private set; }
        internal static ChoreographerTimer? Timer { get; private set; }

        public static void Initialize()
        {
            Options = PresentationLocator.Current.GetService<AndroidPlatformOptions>() ?? new AndroidPlatformOptions();
            NativeAirspace.BehindComposition = Options.NativeBehindComposition;

            Dispatcher.InitializeUIThreadDispatcher(new AndroidDispatcherImpl());
            Timer = new ChoreographerTimer();
            PresentationLocator.CurrentMutable
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToSingleton<AndroidPlatformSettings>()
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
                .Bind<IRenderLoop>().ToConstant(RenderLoop.FromTimer(Timer))
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<KeyGestureFormatInfo>().ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>() { }))
                .Bind<IActivatableLifetime>().ToConstant(new AndroidActivatableLifetime());

            var graphics = InitializeGraphics(Options);
            if (graphics is not null)
            {
                PresentationLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(graphics);
            }

            Compositor = new Compositor(graphics);
            PresentationLocator.CurrentMutable.Bind<Compositor>().ToConstant(Compositor);
        }
        
        private static IPlatformGraphics? InitializeGraphics(AndroidPlatformOptions opts)
        {
            if (opts.RenderingMode is null || !opts.RenderingMode.Any())
            {
                throw new InvalidOperationException($"{nameof(AndroidPlatformOptions)}.{nameof(AndroidPlatformOptions.RenderingMode)} must not be empty or null");
            }

            foreach (var renderingMode in opts.RenderingMode)
            {
                if (renderingMode == AndroidRenderingMode.Software)
                {
                    return null;
                }

                if (renderingMode == AndroidRenderingMode.Egl)
                {
                    if (EglPlatformGraphics.TryCreate() is { } egl)
                    {
                        return egl;
                    }
                }

                if (renderingMode == AndroidRenderingMode.Vulkan)
                {
                    var vulkan = VulkanSupport.TryInitialize(PresentationLocator.Current.GetService<VulkanOptions>() ?? new());
                    if (vulkan != null)
                        return vulkan;
                }
            }

            throw new InvalidOperationException($"{nameof(AndroidPlatformOptions)}.{nameof(AndroidPlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
        }
    }
}
