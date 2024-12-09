#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Cornerstone.Extensions;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia;

[DoNotNotify]
public abstract class Theme : Styles
{
	#region Fields

	public static readonly AttachedProperty<ThemeColor> ColorProperty = AvaloniaProperty.RegisterAttached<Theme, Control, ThemeColor>("Color");
	public static readonly IMultiValueConverter ColorsMatch;

	#endregion

	#region Constructors

	static Theme()
	{
		ColorsMatch = new FuncMultiValueConverter<ThemeColor, bool>(x =>
		{
			var a = x.ToArray();
			var first = a.FirstOrDefault();
			return a.All(c => c == first);
		});

		var colors = EnumExtensions
			.GetAllEnumDetailsExcept([ThemeColor.Default, ThemeColor.Current])
			.OrderBy(x => x.Value.DisplayOrder)
			.Select(x => x.Key)
			.ToArray();

		Colors = colors;
		ColorsWithCurrent = ArrayExtensions.CombineArrays([ThemeColor.Current], colors);
	}

	#endregion

	#region Properties

	public static ThemeColor[] Colors { get; }

	public static ThemeColor[] ColorsWithCurrent { get; }

	#endregion

	#region Methods

	public static ThemeColor GetColor(Control element)
	{
		return element.GetValue(ColorProperty);
	}

	public static CornerstoneTheme GetCornerstoneAvaloniaTheme()
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
		if ((index <= 2) || (index >= Colors.Length))
		{
			return Colors[2];
		}

		return Colors[index];
	}

	public static void SetColor(Control element, ThemeColor value)
	{
		element.SetValue(ColorProperty, value);
	}

	public static void SetThemeColor(ThemeColor color)
	{
		var theme = GetCornerstoneAvaloniaTheme();
		if (theme is not null)
		{
			theme.ThemeColor = color;
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