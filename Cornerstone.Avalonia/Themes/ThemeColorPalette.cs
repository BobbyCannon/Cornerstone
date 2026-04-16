#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Avalonia.Themes;

public static class ThemeColorPalette
{
	#region Constructors

	static ThemeColorPalette()
	{
		ThemeColorsForDark =
		[
			new("Theme00", "#000000", "#FFFFFF"),
			new("Theme01", "#090909", "#F4F4F4"),
			new("Theme02", "#131313", "#E8E8E8"),
			new("Theme03", "#1C1C1C", "#DDDDDD"),
			new("Theme04", "#262626", "#D2D2D2"),
			new("Theme05", "#2F2F2F", "#C6C6C6"),
			new("Theme06", "#393939", "#BBBBBB"),
			new("Theme07", "#424242", "#B0B0B0"),
			new("Theme08", "#4C4C4C", "#A4A4A4"),
			new("Theme09", "#555555", "#999999")
		];

		ThemeColorsForLight =
		[
			new("Theme00", "#FFFFFF", "#000000"),
			new("Theme01", "#F4F4F4", "#090909"),
			new("Theme02", "#E8E8E8", "#131313"),
			new("Theme03", "#DDDDDD", "#1C1C1C"),
			new("Theme04", "#D2D2D2", "#262626"),
			new("Theme05", "#C6C6C6", "#2F2F2F"),
			new("Theme06", "#BBBBBB", "#393939"),
			new("Theme07", "#B0B0B0", "#424242"),
			new("Theme08", "#A4A4A4", "#4C4C4C"),
			new("Theme09", "#999999", "#555555")
		];

		var amberColors = ToTheme("#FF6F00", 0.33);
		var blueColors = ToTheme("#1E88E5", 0.24);
		var blueGrayColors = ToTheme("#546E7A", 0.27);
		var brownColors = ToTheme("#6D4C41", 0.179);
		var deepOrangeColors = ToTheme("#F4511E", 0.26);
		var deepPurpleColors = ToTheme("#5E35B1", 0.179);
		var grayColors = ToTheme("#565656", 0.179);
		var greenColors = ToTheme("#43A047", 0.27);
		var indigoColors = ToTheme("#3949AB", 0.179);
		var orangeColors = ToTheme("#FB8C00", 0.40);
		var pinkColors = ToTheme("#D81B60", 0.179);
		var purpleColors = ToTheme("#8E24AA", 0.179);
		var redColors = ToTheme("#E53935", 0.22);
		var tealColors = ToTheme("#00897B", 0.20);

		Amber = new ThemeColorPaletteDetails(ThemeColor.Amber, amberColors);
		Blue = new ThemeColorPaletteDetails(ThemeColor.Blue, blueColors);
		BlueGray = new ThemeColorPaletteDetails(ThemeColor.BlueGray, blueGrayColors);
		Brown = new ThemeColorPaletteDetails(ThemeColor.Brown, brownColors);
		DeepOrange = new ThemeColorPaletteDetails(ThemeColor.DeepOrange, deepOrangeColors);
		DeepPurple = new ThemeColorPaletteDetails(ThemeColor.DeepPurple, deepPurpleColors);
		Gray = new ThemeColorPaletteDetails(ThemeColor.Gray, grayColors);
		Green = new ThemeColorPaletteDetails(ThemeColor.Green, greenColors);
		Indigo = new ThemeColorPaletteDetails(ThemeColor.Indigo, indigoColors);
		Orange = new ThemeColorPaletteDetails(ThemeColor.Orange, orangeColors);
		Pink = new ThemeColorPaletteDetails(ThemeColor.Pink, pinkColors);
		Purple = new ThemeColorPaletteDetails(ThemeColor.Purple, purpleColors);
		Red = new ThemeColorPaletteDetails(ThemeColor.Red, redColors);
		Teal = new ThemeColorPaletteDetails(ThemeColor.Teal, tealColors);

		ThemeColorPaletteDetails[] details =
		[
			Amber, Blue, BlueGray, Brown, DeepOrange, DeepPurple, Gray,
			Green, Indigo, Orange, Pink, Purple, Red, Teal
		];

		BasicColors = details
			.Select(x => x.Color)
			.Append(new ThemeColorDetails("Black", "#000000", "#FFFFFF"))
			.Append(new ThemeColorDetails("White", "#FFFFFF", "#000000"))
			.ToArray();

		ThemeColors = details.OrderBy(x => x.Order).ToArray();
		ThemeColorNames = ThemeColors.Select(x => x.Name).ToArray();
	}

	#endregion

	#region Properties

	public static ThemeColorPaletteDetails Amber { get; }

	public static ThemeColorDetails[] BasicColors { get; }

	public static ThemeColorPaletteDetails Blue { get; }

	public static ThemeColorPaletteDetails BlueGray { get; }

	public static ThemeColorPaletteDetails Brown { get; }

	public static ThemeColorPaletteDetails DeepOrange { get; }

	public static ThemeColorPaletteDetails DeepPurple { get; }

	public static ThemeColorPaletteDetails Gray { get; }

	public static ThemeColorPaletteDetails Green { get; }

	public static ThemeColorPaletteDetails Indigo { get; }

	public static ThemeColorPaletteDetails Orange { get; }

	public static ThemeColorPaletteDetails Pink { get; }

	public static ThemeColorPaletteDetails Purple { get; }

	public static ThemeColorPaletteDetails Red { get; }

	public static ThemeColorPaletteDetails Teal { get; }

	public static string[] ThemeColorNames { get; }

	public static ThemeColorPaletteDetails[] ThemeColors { get; }

	public static List<ThemeColorDetails> ThemeColorsForDark { get; }

	public static List<ThemeColorDetails> ThemeColorsForLight { get; }

	#endregion

	#region Methods

	private static ThemeColorDetails[] ToTheme(string color, double luminance)
	{
		var colors = ThemeColorGenerator.GenerateColorTheme(color, luminance);
		var items = colors.Select((value, i) => new ThemeColorDetails($"ThemeColor0{i}", value.bg, value.fg)).ToArray();
		return items;
	}

	#endregion
}