using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms.Remote;

namespace Cornerstone.Presentation.Platforms
{
    public static class AppBuilderDesktopExtensions
    {
        /// <summary>
        /// When args contain --remote tcp-bson://host:port/, replace Win32/macOS/X11
        /// windowing with a remote framebuffer so a WPF or Visual Studio client can
        /// display the running app. No-op when the switch is absent. Call after
        /// UsePlatformDetect so Skia and HarfBuzz stay in place.
        /// </summary>
        [RequiresUnreferencedCode("Bson uses reflection")]
        public static AppBuilder UseRemoteDisplay(this AppBuilder builder, string[] args)
        {
            if (!TryGetRemoteDisplayUri(args, out var uri))
            {
                return builder;
            }

            return builder.UseWindowingSubsystem(() => RemoteDisplayPlatform.Initialize(uri), "Remote");
        }

        internal static bool TryGetRemoteDisplayUri(string[] args, out Uri uri)
        {
            uri = null;
            if (args == null)
            {
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg == "--remote")
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("--remote requires a tcp-bson URI.");
                    }

                    uri = new Uri(args[i + 1], UriKind.Absolute);
                    return true;
                }

                const string prefix = "--remote=";
                if (arg != null && arg.StartsWith(prefix, StringComparison.Ordinal))
                {
                    uri = new Uri(arg.Substring(prefix.Length), UriKind.Absolute);
                    return true;
                }
            }

            return false;
        }

        public static AppBuilder UsePlatformDetect(this AppBuilder builder)
        {
            LoadHarfBuzz(builder);

            // Helpers stay in separate methods so the CLR does not load platform
            // dependencies until the referenced method is jitted.
            if (OperatingSystem.IsWindows())
            {
                LoadWin32(builder);
                LoadSkia(builder);
            }
            else if (OperatingSystem.IsMacOS())
            {
                LoadMacOS(builder);
                LoadSkia(builder);
            }
            else if (OperatingSystem.IsLinux())
            {
                LoadX11(builder);
                LoadSkia(builder);
                LoadWaylandWithFallback(builder);
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Platform)?.Log(builder,
                    "UsePlatformDetect supports Windows, macOS, and Linux in this fork");
            }

            return builder;
        }

        static void LoadMacOS(AppBuilder builder)
             => builder.UseMacOS();

        static void LoadWin32(AppBuilder builder)
             => builder.UseWin32();

        static void LoadX11(AppBuilder builder)
             => builder.UseX11();

        static void LoadWaylandWithFallback(AppBuilder builder)
             => builder.UseWaylandWithFallback();

        static void LoadSkia(AppBuilder builder)
             => builder.UseSkia();

        static void LoadHarfBuzz(AppBuilder builder)
             => builder.UseHarfBuzz();
    }
}
