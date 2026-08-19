#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Themes;

public abstract class Theme : Styles
{
	#region Fields

	public static readonly AttachedProperty<ThemeColor> ColorProperty;
	public static readonly IMultiValueConverter ColorsMatch;

	#endregion

	#region Constructors

	static Theme()
	{
		ColorProperty = AvaloniaProperty.RegisterAttached<Theme, Control, ThemeColor>("Color");
		ColorsMatch = new FuncMultiValueConverter<ThemeColor, bool>(x =>
		{
			var a = x.ToArray();
			var first = a.FirstOrDefault();
			return a.All(c => c == first);
		});

		var colors = SourceReflector
			.GetEnumDetails<ThemeColor>()
			.OrderBy(x => x.DisplayOrder)
			.Select(x => (ThemeColor) x.Value)
			.ToArray();

		Colors = [.. colors.Except([ThemeColor.None, ThemeColor.Current])];
		ThemeColors = colors;
		ThemeDensities =
		[
			ThemeDensity.Compact,
			ThemeDensity.Normal,
			ThemeDensity.Large
		];
		ThemeModes =
		[
			ThemeMode.Dark,
			ThemeMode.Light,
			ThemeMode.Default
		];
		ThemeVariants = [ThemeVariant.Dark, ThemeVariant.Light, ThemeVariant.Default];
	}

	#endregion

	#region Properties

	public static ThemeColor[] Colors { get; }

	public static ThemeColor[] ThemeColors { get; }

	/// <summary>
	/// Compact / Normal / Large for preview pickers and settings UIs.
	/// </summary>
	public static ThemeDensity[] ThemeDensities { get; }

	/// <summary>
	/// Dark / Light / Default for app ThemeMode pickers.
	/// </summary>
	public static ThemeMode[] ThemeModes { get; }

	public static ThemeVariant[] ThemeVariants { get; }

	#endregion

	#region Methods

	public static ThemeColor GetColor(Control element)
	{
		return element.GetValue(ColorProperty);
	}

	public static CornerstoneTheme GetCornerstoneTheme()
	{
		var current = Application.Current;
		if (current is null)
		{
			return null;
		}

		foreach (var style in current.Styles)
		{
			if (style is CornerstoneTheme theme)
			{
				return theme;
			}
		}

		return null;
	}

	public static ThemeColor GetNextThemeColor(ThemeColor current)
	{
		var index = Array.IndexOf(Colors, current) + 1;

		// Stay in range, skip first two values (Default, Current)
		if ((index <= 2) || (index >= Colors.Length))
		{
			// Start at the first color.
			return Colors[2];
		}

		return Colors[index];
	}

	public static ThemeColor? GetThemeColor()
	{
		var theme = GetCornerstoneTheme();
		return theme?.ThemeColor;
	}

	/// <summary>
	/// Mid accent for the current ThemeColor (Blue when unset / None / Current).
	/// Used for markdown links and other “current theme” chrome.
	/// </summary>
	public static IBrush GetAccentBrush()
	{
		var selected = GetThemeColor() ?? ThemeColor.Blue;
		if ((selected == ThemeColor.None) || (selected == ThemeColor.Current))
		{
			selected = ThemeColor.Blue;
		}

		foreach (var details in ThemeColorPalette.ThemeColors)
		{
			if (details.ThemeColor == selected)
			{
				return details.Color.Brush;
			}
		}

		return ThemeColorPalette.Blue.Color.Brush;
	}

	public static event EventHandler AccentChanged;

	internal static void RaiseAccentChanged()
	{
		AccentChanged?.Invoke(null, EventArgs.Empty);
	}

	public static ThemeDensity GetThemeDensity()
	{
		var theme = GetCornerstoneTheme();
		return theme?.ThemeDensity ?? ThemeDensity.Normal;
	}

	public static ThemeVariant GetThemeVariant()
	{
		var app = Application.Current;
		return app is not null
			? app.RequestedThemeVariant
			: ThemeVariant.Default;
	}

	public static void SetColor(Control element, ThemeColor value)
	{
		element.SetValue(ColorProperty, value);
	}

	public static void SetThemeColor(ThemeColor? color)
	{
		var theme = GetCornerstoneTheme();
		if (theme is not null && (color != null))
		{
			theme.ThemeColor = (ThemeColor) color;
		}
	}

	/// <summary>
	/// Apply density app-wide (same scope model as <see cref="SetThemeColor" />; not subtree-scoped).
	/// </summary>
	public static void SetThemeDensity(ThemeDensity density)
	{
		CornerstoneTheme.SelectThemeDensity(density);
	}

	public static void SetThemeVariant(ThemeVariant themeVariant)
	{
		var app = Application.Current;
		if (app is not null)
		{
			app.RequestedThemeVariant = themeVariant;
		}
	}

	public static void SetThemeVariant(bool useDarkMode)
	{
		var app = Application.Current;
		if (app is not null)
		{
			app.RequestedThemeVariant = useDarkMode
				? ThemeVariant.Dark
				: ThemeVariant.Light;
		}
	}

	#endregion
}