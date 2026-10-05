using System.Collections.Generic;
using Cornerstone.Presentation.Platforms.MacOS;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Presentation
{
    public static class MacPlatformExtensions
    {
        public static AppBuilder UseMacOS(this AppBuilder builder)
        {
            builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() =>
                {
                    var platform = MacPlatform.Initialize(
                        PresentationLocator.Current.GetService<MacOSPlatformOptions>() ??
                        new MacOSPlatformOptions());

                        builder.AfterSetup (x=>
                        {
                            platform.SetupApplicationName();
                            platform.SetupApplicationMenuExporter();
                            platform.SetupApplicationDockMenuExporter();
                        });
                });

            return builder;
        }
    }

    /// <summary>
    /// Represents the rendering mode for platform graphics.
    /// </summary>
    public enum MacRenderingMode
    {
        /// <summary>
        /// Cornerstone would try to use native OpenGL with GPU rendering.
        /// </summary>
        OpenGl = 1,
        /// <summary>
        /// Cornerstone is rendered into a framebuffer.
        /// </summary>
        Software = 2,
        /// <summary>
        /// Cornerstone would try to use Metal with GPU rendering.
        /// </summary>
        Metal = 3
    }

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// macOS backend and front-end options.
    /// </summary>
    public class MacOSPlatformOptions
    {
        /// <summary>
        /// Gets or sets Cornerstone rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="MacRenderingMode.Metal"/>, <see cref="MacRenderingMode.OpenGl"/>, <see cref="MacRenderingMode.Software"/>.
        /// </summary>
        /// <remarks>
        /// If application should work on as wide range of devices as possible,
        /// at least add <see cref="MacRenderingMode.Software"/> as a fallback value.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<MacRenderingMode> RenderingMode { get; set; } = new[]
        {
            MacRenderingMode.Metal,
            MacRenderingMode.OpenGl,
            MacRenderingMode.Software
        };

        /// <summary>
        /// Embeds popups to the window when set to true. The default value is false.
        /// </summary>
        public bool OverlayPopups { get; set; }

        /// <summary>
        /// Path to a custom libCornerstoneNative dylib. The default value is null (load the bundled library).
        /// </summary>
        public string NativeLibraryPath { get; set; }

        /// <summary>
        /// If you distribute your app in App Store - it should be with sandbox enabled.
        /// This parameter enables <see cref="Cornerstone.Presentation.Platform.Storage.IStorageItem.SaveBookmarkAsync"/> and related APIs,
        /// as well as wrapping all storage related calls in secure context. The default value is true.
        /// </summary>
        public bool AppSandboxEnabled { get; set; } = true;

        /// <summary>
        /// Determines whether to show your application in the dock when it runs. The default value is true.
        /// </summary>
        public bool ShowInDock { get; set; } = true;

        /// <summary>
        /// By default, Cornerstone adds items like Quit, Hide to the OSX Application Menu.
        /// You can prevent Cornerstone from adding those items to the OSX Application Menu with this property. The default value is false.
        /// </summary>
        public bool DisableDefaultApplicationMenuItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the native macOS menu bar will be enabled for the application.
        /// </summary>
        public bool DisableNativeMenus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the native macOS should set [NSProcessInfo setProcessName] in runtime.
        /// </summary>
        public bool DisableSetProcessName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Cornerstone can install its own AppDelegate.
        /// Disabling this can be useful in some scenarios like when running as a plugin inside an existing macOS application.
        /// </summary>
        public bool DisableAppDelegate { get; set; }

        /// <summary>
        /// Non-opaque Metal layer with native views as siblings below and hole punch.
        /// Default on. Set false for child-on-top. See Presentation/NativeLayering.md.
        /// </summary>
        public bool NativeBehindComposition { get; set; } = true;
    }
}
