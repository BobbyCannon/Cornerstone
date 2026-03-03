#region References

using System.Collections.Generic;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Avalonia.Themes;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ThemeConverters
{
	#region Fields

	public static readonly FuncValueConverter<ThemeColor, object, IBrush> GetColorBrushAtIndex;
	public static readonly FuncValueConverter<ThemeColor, object, IBrush> GetColorForegroundBrushAtIndex;
	public static readonly IValueConverter ToThemeIcon;
	public static readonly IValueConverter ToThemeIconInverted;
	public static readonly FuncValueConverter<bool, ThemeVariant> ToThemeVariant;
	private static readonly Dictionary<ThemeVariant, string> ToIconName;
	private static readonly Dictionary<ThemeVariant, string> ToIconNameInverted;

	#endregion

	#region Constructors

	static ThemeConverters()
	{
		GetColorBrushAtIndex = new FuncValueConverter<ThemeColor, object, IBrush>((x, y) => GetColorBrush(x, y, string.Empty));
		GetColorForegroundBrushAtIndex = new FuncValueConverter<ThemeColor, object, IBrush>((x, y) => GetColorBrush(x, y, "Text"));
		ToThemeIcon = new FuncValueConverter<ThemeVariant, StreamGeometry>(GetThemeIconSvg);
		ToThemeIconInverted = new FuncValueConverter<ThemeVariant, StreamGeometry>(GetThemeIconSvgInverted);
		ToThemeVariant = new FuncValueConverter<bool, ThemeVariant>(x => x ? ThemeVariant.Light : ThemeVariant.Dark);

		ToIconName = new()
		{
			{ ThemeVariant.Default, "Icons.Moon.Sun" },
			{ ThemeVariant.Dark, "Icons.Moon" },
			{ ThemeVariant.Light, "Icons.Sun" }
		};
		ToIconNameInverted = new()
		{
			{ ThemeVariant.Default, "Icons.Moon.Sun" },
			{ ThemeVariant.Dark, "Icons.Sun" },
			{ ThemeVariant.Light, "Icons.Moon" }
		};
	}

	#endregion

	#region Methods

	public static IBrush GetColorBrush(ThemeColor value, object parameter, string delimiter)
	{
		var key = $"{value}{delimiter}{parameter}";
		var color = ResourceService.Get<Color>(key);
		return new SolidColorBrush(color);
	}

	private static StreamGeometry GetThemeIconSvg(ThemeVariant x)
	{
		return (x != null) && ToIconName.TryGetValue(x, out var name)
			? ResourceService.GetSvg(name)
			: ResourceService.GetSvg("Icons.Moon.Sun");
	}

	private static StreamGeometry GetThemeIconSvgInverted(ThemeVariant x)
	{
		return (x != null) && ToIconNameInverted.TryGetValue(x, out var name)
			? ResourceService.GetSvg(name)
			: ResourceService.GetSvg("Icons.Moon.Sun");
	}

	#endregion
}