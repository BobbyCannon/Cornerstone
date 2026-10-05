using System;
using System.IO;
using Microsoft.Win32;

namespace Cornerstone.Presentation.Platform
{
    internal static partial class OperatingSystemTheme
    {
        private static partial bool TryGetPlatformVariant(out PlatformThemeVariant variant)
        {
            if (OperatingSystem.IsWindows() && TryReadWindowsAppsTheme(out variant))
            {
                return true;
            }

            if (OperatingSystem.IsMacOS() && TryReadMacInterfaceStyle(out variant))
            {
                return true;
            }

            if (OperatingSystem.IsLinux() && TryReadLinuxGtkTheme(out variant))
            {
                return true;
            }

            variant = default;
            return false;
        }

        private static bool TryReadWindowsAppsTheme(out PlatformThemeVariant variant)
        {
            variant = PlatformThemeVariant.Light;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int useLightTheme)
                {
                    variant = useLightTheme == 0
                        ? PlatformThemeVariant.Dark
                        : PlatformThemeVariant.Light;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private static bool TryReadMacInterfaceStyle(out PlatformThemeVariant variant)
        {
            variant = PlatformThemeVariant.Light;
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Preferences",
                    ".GlobalPreferences.plist");
                if (!File.Exists(path))
                {
                    return false;
                }

                var text = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path));
                var index = text.IndexOf("AppleInterfaceStyle", StringComparison.Ordinal);
                if (index < 0)
                {
                    return true;
                }

                var length = Math.Min(80, text.Length - index);
                variant = text.Substring(index, length).Contains("Dark", StringComparison.Ordinal)
                    ? PlatformThemeVariant.Dark
                    : PlatformThemeVariant.Light;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryReadLinuxGtkTheme(out PlatformThemeVariant variant)
        {
            variant = PlatformThemeVariant.Light;
            try
            {
                var config = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(config))
                {
                    return false;
                }

                var found = false;
                foreach (var relative in new[]
                {
                    Path.Combine(".config", "gtk-4.0", "settings.ini"),
                    Path.Combine(".config", "gtk-3.0", "settings.ini")
                })
                {
                    var path = Path.Combine(config, relative);
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    found = true;
                    foreach (var line in File.ReadLines(path))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("gtk-application-prefer-dark-theme", StringComparison.Ordinal)
                            && trimmed.Contains("1", StringComparison.Ordinal))
                        {
                            variant = PlatformThemeVariant.Dark;
                            return true;
                        }

                        if (trimmed.StartsWith("gtk-theme-name", StringComparison.OrdinalIgnoreCase)
                            && trimmed.Contains("dark", StringComparison.OrdinalIgnoreCase))
                        {
                            variant = PlatformThemeVariant.Dark;
                            return true;
                        }
                    }
                }

                return found;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
