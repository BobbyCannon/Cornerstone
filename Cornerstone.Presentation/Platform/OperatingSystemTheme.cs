namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Operating-system light or dark preference used when an app does not request a theme variant.
    /// </summary>
    internal static partial class OperatingSystemTheme
    {
        public static PlatformThemeVariant GetVariant()
        {
            return TryGetPlatformVariant(out var variant)
                ? variant
                : PlatformThemeVariant.Light;
        }

        private static partial bool TryGetPlatformVariant(out PlatformThemeVariant variant);
    }
}
