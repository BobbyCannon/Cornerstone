using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS;

internal class NativePlatformSettings : DefaultPlatformSettings
{
    private readonly ICsnPlatformSettings _platformSettings;
    private PlatformColorValues? _colorValues;
    private string? _lastLanguage;

    public NativePlatformSettings(ICsnPlatformSettings platformSettings)
    {
        _platformSettings = platformSettings;
        platformSettings.RegisterColorsChange(new ColorsChangeCallback(this));
        platformSettings.RegisterLanguageChange(new LanguageChangeCallback(this));
    }

    public override PlatformColorValues GetColorValues()
        => _colorValues ??= GetUncachedColorValues();

    private PlatformColorValues GetUncachedColorValues()
    {
        var (theme, contrast) = _platformSettings.PlatformTheme switch
        {
            CsnPlatformThemeVariant.Dark => (PlatformThemeVariant.Dark, ColorContrastPreference.NoPreference),
            CsnPlatformThemeVariant.Light => (PlatformThemeVariant.Light, ColorContrastPreference.NoPreference),
            CsnPlatformThemeVariant.HighContrastDark => (PlatformThemeVariant.Dark, ColorContrastPreference.High),
            CsnPlatformThemeVariant.HighContrastLight => (PlatformThemeVariant.Light, ColorContrastPreference.High),
            _ => throw new ArgumentOutOfRangeException()
        };
        var color = _platformSettings.AccentColor;

        if (color > 0)
        {
            return new PlatformColorValues
            {
                ThemeVariant = theme,
                ContrastPreference = contrast,
                AccentColor1 = Color.FromUInt32(color)
            };
        }
        else
        {
            return new PlatformColorValues
            {
                ThemeVariant = theme,
                ContrastPreference = contrast
            };
        }
    }

    public void OnColorValuesChanged()
    {
        var oldColorValues = _colorValues;
        var colorValues = GetUncachedColorValues();

        if (oldColorValues != colorValues)
        {
            _colorValues = colorValues;
            OnColorValuesChanged(colorValues);
        }
    }

    public override string PreferredApplicationLanguage =>
        _lastLanguage ??= QueryPreferredApplicationLanguage();

    private void OnPreferredLanguageChanged()
    {
        var oldLanguage = _lastLanguage;
        _lastLanguage = null;

        if (oldLanguage != PreferredApplicationLanguage)
        {
            OnPreferredApplicationLanguageChanged();
        }
    }

    private string QueryPreferredApplicationLanguage()
    {
        using var language = _platformSettings.PreferredLanguage;
        return language?.String is { Length: > 0 } value ? value : base.PreferredApplicationLanguage;
    }

    private class ColorsChangeCallback : NativeCallbackBase, ICsnActionCallback
    {
        private readonly NativePlatformSettings _settings;

        public ColorsChangeCallback(NativePlatformSettings settings)
        {
            _settings = settings;
        }
        
        public void Run()
        {
            _settings.OnColorValuesChanged();
        }
    }

    private class LanguageChangeCallback : NativeCallbackBase, ICsnActionCallback
    {
        private readonly NativePlatformSettings _settings;

        public LanguageChangeCallback(NativePlatformSettings settings)
        {
            _settings = settings;
        }

        public void Run()
        {
            _settings.OnPreferredLanguageChanged();
        }
    }
}
