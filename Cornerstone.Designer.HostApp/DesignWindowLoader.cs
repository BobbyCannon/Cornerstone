using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Embedding.Offscreen;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Theme.Theming;

namespace Cornerstone.Presentation.DesignerSupport
{
    [RequiresUnreferencedCode("Remote designer relies on reflection")]
    public class DesignWindowLoader
    {
        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath)
            => LoadDesignerWindow(xaml, assemblyPath, xamlFileProjectPath, 1.0);

        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath, double renderScaling)
            => LoadDesignerWindow(xaml, assemblyPath, xamlFileProjectPath, renderScaling, null, null, null);

        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath, double renderScaling, string themeVariant)
            => LoadDesignerWindow(xaml, assemblyPath, xamlFileProjectPath, renderScaling, themeVariant, null, null);

        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath, double renderScaling, string themeVariant, string themeColor, string themeDensity)
        {
            Window window;
            using (PlatformManager.DesignerMode())
            {
                var loader = PresentationLocator.Current.GetRequiredService<CornerstoneXamlLoader.IRuntimeXamlLoader>();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));

                Uri baseUri = null;
                if (assemblyPath != null)
                {
                    if (xamlFileProjectPath == null)
                        xamlFileProjectPath = "/Fake.xaml";
                    //Fabricate fake Uri
                    baseUri =
                        new Uri($"csres://{System.IO.Path.GetFileNameWithoutExtension(assemblyPath)}{xamlFileProjectPath}");
                }

                var localAsm = assemblyPath != null ? LoadLocalAssembly(assemblyPath) : null;
                var useCompiledBindings = localAsm?.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => a.Key == "CornerstoneUseCompiledBindingsByDefault"
                        || a.Key == "AvaloniaUseCompiledBindingsByDefault")?.Value;

                var loaded = loader.Load(new RuntimeXamlLoaderDocument(baseUri, stream), new RuntimeXamlLoaderConfiguration
                {
                    LocalAssembly = localAsm,
                    DesignMode = true,
                    UseCompiledBindingsByDefault = bool.TryParse(useCompiledBindings, out var parsedValue) && parsedValue
                });

                var control = Design.CreatePreviewWithControl(loaded);
                window = control as Window ?? new Window { Content = control };

                if (window.PlatformImpl is OffscreenTopLevelImplBase offscreenImpl)
                    offscreenImpl.RenderScaling = renderScaling;

                Design.ApplyDesignModeProperties(window, control);
                ApplyThemeOverride(window, themeVariant, themeColor, themeDensity);

                if (!window.IsSet(Window.SizeToContentProperty))
                {
                    if (double.IsNaN(window.Width))
                    {
                        window.SizeToContent |= SizeToContent.Width;
                    }

                    if (double.IsNaN(window.Height))
                    {
                        window.SizeToContent |= SizeToContent.Height;
                    }
                }
            }
            window.Show();
            return window;
        }

        private static Assembly LoadLocalAssembly(string assemblyPath)
        {
            var fullPath = Path.GetFullPath(assemblyPath);
            try
            {
                return Assembly.LoadFrom(fullPath);
            }
            catch (FileLoadException) when (TryGetLoadedAssembly(fullPath, out var loaded))
            {
                // The designer host already loaded this strong name at another version.
                // Preview the editor text against that copy instead of failing the session.
                Console.WriteLine(
                    "Preview host: assembly version differs for " + loaded.GetName().Name +
                    "; using the copy already loaded by the designer host.");
                return loaded;
            }
        }

        private static bool TryGetLoadedAssembly(string fullPath, out Assembly loaded)
        {
            AssemblyName requested;
            try
            {
                requested = AssemblyName.GetAssemblyName(fullPath);
            }
            catch (Exception)
            {
                loaded = null;
                return false;
            }

            foreach (var candidate in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (candidate.IsDynamic)
                {
                    continue;
                }

                if (SameIdentityIgnoringVersion(candidate.GetName(), requested))
                {
                    loaded = candidate;
                    return true;
                }
            }

            loaded = null;
            return false;
        }

        private static bool SameIdentityIgnoringVersion(AssemblyName loaded, AssemblyName requested)
        {
            if (!string.Equals(loaded.Name, requested.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var loadedToken = loaded.GetPublicKeyToken();
            var requestedToken = requested.GetPublicKeyToken();
            if ((loadedToken == null) || (loadedToken.Length == 0))
            {
                return (requestedToken == null) || (requestedToken.Length == 0);
            }

            return (requestedToken != null) && loadedToken.SequenceEqual(requestedToken);
        }

        private static void ApplyThemeOverride(Window window, string themeVariant, string themeColor, string themeDensity)
        {
            var variant = themeVariant switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ResolveOperatingSystemVariant()
            };

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = variant;
            }

            // Default must be a concrete Light or Dark value. Assigning ThemeVariant.Default
            // clears the theme the window copied from the application and the preview falls back to light.
            window.RequestedThemeVariant = variant;

            var snippet = window.Content as PreviewCodeSnippet;
            if (snippet != null)
            {
                snippet.ThemeVariant = variant;
            }

            ApplyThemeColor(themeColor, snippet);
            ApplyThemeDensity(themeDensity, snippet);
        }

        private static void ApplyThemeColor(string themeColor, PreviewCodeSnippet snippet)
        {
            if (!TryGetThemeColor(themeColor, out var color))
            {
                return;
            }

            var theme = ApplicationTheme.GetCurrent();
            if (theme != null)
            {
                theme.ThemeColor = color;
            }

            if (snippet != null)
            {
                snippet.ThemeColor = color;
            }
        }

        private static void ApplyThemeDensity(string themeDensity, PreviewCodeSnippet snippet)
        {
            if (!TryGetThemeDensity(themeDensity, out var density))
            {
                return;
            }

            var theme = ApplicationTheme.GetCurrent();
            if (theme != null)
            {
                theme.ThemeDensity = density;
            }

            if (snippet != null)
            {
                snippet.ThemeDensity = density;
            }
        }

        private static bool TryGetThemeColor(string themeColor, out ThemeColor color)
        {
            color = ThemeColor.Blue;
            if (string.IsNullOrEmpty(themeColor) || !Enum.TryParse(themeColor, out color))
            {
                return false;
            }

            return color is not (ThemeColor.None or ThemeColor.Current);
        }

        private static bool TryGetThemeDensity(string themeDensity, out ThemeDensity density)
        {
            density = ThemeDensity.Normal;
            if (string.IsNullOrEmpty(themeDensity) || !Enum.TryParse(themeDensity, out density))
            {
                return false;
            }

            return density is ThemeDensity.Compact or ThemeDensity.Normal or ThemeDensity.Large;
        }

        private static ThemeVariant ResolveOperatingSystemVariant()
        {
            var platform = PresentationLocator.Current.GetService<IPlatformSettings>()
                ?.GetColorValues().ThemeVariant;
            if (platform == PlatformThemeVariant.Dark || WindowsAppsUseDarkTheme())
            {
                return ThemeVariant.Dark;
            }

            return ThemeVariant.Light;
        }

        private static bool WindowsAppsUseDarkTheme()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int useLightTheme && useLightTheme == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
