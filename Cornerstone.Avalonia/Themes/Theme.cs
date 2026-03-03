#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
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
		ThemeVariants = [ThemeVariant.Dark, ThemeVariant.Light, ThemeVariant.Default];
	}

	#endregion

	#region Properties

	public static ThemeColor[] Colors { get; }

	public static ThemeColor[] ThemeColors { get; }

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