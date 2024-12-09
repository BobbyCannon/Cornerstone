#region References

using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia;

public static class ThemeColorPalette
{
	#region Constructors

	static ThemeColorPalette()
	{
		ThemeColorsForDark =
		[
			new("Theme00", "#000000", "#FFFFFF"),
			new("Theme01", "#121212", "#E8E8E8"),
			new("Theme02", "#1D1D1D", "#D1D1D1"),
			new("Theme03", "#272727", "#BABABA"),
			new("Theme04", "#333333", "#A4A4A4"),
			new("Theme05", "#3E3E3E", "#8F8F8F"),
			new("Theme06", "#4A4A4A", "#7A7A7A"),
			new("Theme07", "#575757", "#656565"),
			new("Theme08", "#636363", "#525252"),
			new("Theme09", "#707070", "#3f3f3f")
		];

		ThemeColorsForLight =
		[
			new("Theme00", "#FFFFFF", "#000000"),
			new("Theme01", "#F0F0F0", "#191919"),
			new("Theme02", "#E1E1E1", "#2A2A2A"),
			new("Theme03", "#D2D2D2", "#3D3D3D"),
			new("Theme04", "#C3C3C3", "#515151"),
			new("Theme05", "#B5B5B5", "#666666"),
			new("Theme06", "#A7A7A7", "#7B7B7B"),
			new("Theme07", "#999999", "#929292"),
			new("Theme08", "#8B8B8B", "#A9A9A9"),
			new("Theme09", "#7E7E7E", "#C0C0C0")
		];

		ThemeForegroundColors =
		[
			new("ThemeForeground00", "#000000", "#FFFFFF"),
			new("ThemeForeground01", "#000000", "#FFFFFF"),
			new("ThemeForeground02", "#000000", "#FFFFFF"),
			new("ThemeForeground03", "#000000", "#FFFFFF"),
			new("ThemeForeground04", "#FFFFFF", "#000000"),
			new("ThemeForeground05", "#FFFFFF", "#000000"),
			new("ThemeForeground06", "#FFFFFF", "#000000"),
			new("ThemeForeground07", "#FFFFFF", "#000000"),
			new("ThemeForeground08", "#FFFFFF", "#000000"),
			new("ThemeForeground09", "#FFFFFF", "#000000")
		];

		AmberColors = ToTheme("#FFF8E1", "#FFECB3", "#FFE082", "#FFD54F", "#FFCA28", "#FFC107", "#FFB300", "#FFA000", "#FF8F00", "#FF6F00");
		BlueColors = ToTheme("#E3F2FD", "#BBDEFB", "#90CAF9", "#64B5F6", "#42A5F5", "#2196F3", "#1E88E5", "#1976D2", "#1565C0", "#0D47A1");
		BlueGrayColors = ToTheme("#ECEFF1", "#CFD8DC", "#B0BEC5", "#90A4AE", "#78909C", "#607D8B", "#546E7A", "#455A64", "#37474F", "#263238");
		BrownColors = ToTheme("#EFEBE9", "#D7CCC8", "#BCAAA4", "#A1887F", "#8D6E63", "#795548", "#6D4C41", "#5D4037", "#4E342E", "#3E2723");
		DeepOrangeColors = ToTheme("#FBE9E7", "#FFCCBC", "#FFAB91", "#FF8A65", "#FF7043", "#FF5722", "#F4511E", "#E64A19", "#D84315", "#BF360C");
		DeepPurpleColors = ToTheme("#EDE7F6", "#D1C4E9", "#B39DDB", "#9575CD", "#7E57C2", "#673AB7", "#5E35B1", "#512DA8", "#4527A0", "#311B92");
		GrayColors = ToTheme("#FAFAFA", "#BDBDBD", "#A7A7A7", "#929292", "#7E7E7E", "#6A6A6A", "#565656", "#444444", "#323232", "#212121");
		GreenColors = ToTheme("#E8F5E9", "#C8E6C9", "#A5D6A7", "#81C784", "#66BB6A", "#4CAF50", "#43A047", "#388E3C", "#2E7D32", "#1B5E20");
		IndigoColors = ToTheme("#E8EAF6", "#C5CAE9", "#9FA8DA", "#7986CB", "#5C6BC0", "#3F51B5", "#3949AB", "#303F9F", "#283593", "#1A237E");
		OrangeColors = ToTheme("#FFF3E0", "#FFE0B2", "#FFCC80", "#FFB74D", "#FFA726", "#FF9800", "#FB8C00", "#F57C00", "#EF6C00", "#E65100");
		PinkColors = ToTheme("#FCE4EC", "#F8BBD0", "#F48FB1", "#F06292", "#EC407A", "#E91E63", "#D81B60", "#C2185B", "#AD1457", "#880E4F");
		PurpleColors = ToTheme("#F3E5F5", "#E1BEE7", "#CE93D8", "#BA68C8", "#AB47BC", "#9C27B0", "#8E24AA", "#7B1FA2", "#6A1B9A", "#4A148C");
		RedColors = ToTheme("#FFEBEE", "#FFCDD2", "#EF9A9A", "#E57373", "#EF5350", "#F44336", "#E53935", "#D32F2F", "#C62828", "#B71C1C");
		TealColors = ToTheme("#E0F2F1", "#B2DFDB", "#80CBC4", "#4DB6AC", "#26A69A", "#009688", "#00897B", "#00796B", "#00695C", "#004D40");

		AmberDetails = new ThemeColorPaletteDetails { Name = "Amber", Colors = AmberColors, ThemeColor = ThemeColor.Amber };
		BlueDetails = new ThemeColorPaletteDetails { Name = "Blue", Colors = BlueColors, ThemeColor = ThemeColor.Blue };
		BlueGrayDetails = new ThemeColorPaletteDetails { Name = "BlueGray", Colors = BlueGrayColors, ThemeColor = ThemeColor.BlueGray };
		BrownDetails = new ThemeColorPaletteDetails { Name = "Brown", Colors = BrownColors, ThemeColor = ThemeColor.Brown };
		DeepOrangeDetails = new ThemeColorPaletteDetails { Name = "DeepOrange", Colors = DeepOrangeColors, ThemeColor = ThemeColor.DeepOrange };
		DeepPurpleDetails = new ThemeColorPaletteDetails { Name = "DeepPurple", Colors = DeepPurpleColors, ThemeColor = ThemeColor.DeepPurple };
		GrayDetails = new ThemeColorPaletteDetails { Name = "Gray", Colors = GrayColors, ThemeColor = ThemeColor.Gray };
		GreenDetails = new ThemeColorPaletteDetails { Name = "Green", Colors = GreenColors, ThemeColor = ThemeColor.Green };
		IndigoDetails = new ThemeColorPaletteDetails { Name = "Indigo", Colors = IndigoColors, ThemeColor = ThemeColor.Indigo };
		OrangeDetails = new ThemeColorPaletteDetails { Name = "Orange", Colors = OrangeColors, ThemeColor = ThemeColor.Orange };
		PinkDetails = new ThemeColorPaletteDetails { Name = "Pink", Colors = PinkColors, ThemeColor = ThemeColor.Pink };
		PurpleDetails = new ThemeColorPaletteDetails { Name = "Purple", Colors = PurpleColors, ThemeColor = ThemeColor.Purple };
		RedDetails = new ThemeColorPaletteDetails { Name = "Red", Colors = RedColors, ThemeColor = ThemeColor.Red };
		TealDetails = new ThemeColorPaletteDetails { Name = "Teal", Colors = TealColors, ThemeColor = ThemeColor.Teal };

		ThemeColorPaletteDetails[] details =
		[
			AmberDetails, BlueDetails, BlueGrayDetails, BrownDetails, DeepOrangeDetails, DeepPurpleDetails, GrayDetails,
			GreenDetails, IndigoDetails, OrangeDetails, PinkDetails, PurpleDetails, RedDetails, TealDetails
		];

		foreach (var d in details)
		{
			d.Order = d.ThemeColor.GetEnumDetails().DisplayOrder;
		}

		BasicColors =
		[
			new ThemeColorDetails("Black", "#000000", "#FFFFFF"),
			new ThemeColorDetails("White", "#FFFFFF", "#000000"),
			new ThemeColorDetails("Red", RedColors[6].Background, "#000000"),
			new ThemeColorDetails("Green", GreenColors[6].Background, "#000000"),
			new ThemeColorDetails("Blue", BlueColors[6].Background, "#000000")
		];
		ThemeColors = details.OrderBy(x => x.Order).ToArray();
		ThemeColorNames = ThemeColors.Select(x => x.Name).ToArray();
	}

	#endregion

	#region Properties

	public static SpeedyList<ThemeColorDetails> AmberColors { get; }

	public static ThemeColorPaletteDetails AmberDetails { get; }

	public static ThemeColorDetails[] BasicColors { get; }

	public static SpeedyList<ThemeColorDetails> BlueColors { get; }

	public static ThemeColorPaletteDetails BlueDetails { get; }

	public static SpeedyList<ThemeColorDetails> BlueGrayColors { get; }

	public static ThemeColorPaletteDetails BlueGrayDetails { get; }

	public static SpeedyList<ThemeColorDetails> BrownColors { get; }

	public static ThemeColorPaletteDetails BrownDetails { get; }

	public static SpeedyList<ThemeColorDetails> DeepOrangeColors { get; }

	public static ThemeColorPaletteDetails DeepOrangeDetails { get; }

	public static SpeedyList<ThemeColorDetails> DeepPurpleColors { get; }

	public static ThemeColorPaletteDetails DeepPurpleDetails { get; }

	public static SpeedyList<ThemeColorDetails> GrayColors { get; }

	public static ThemeColorPaletteDetails GrayDetails { get; }

	public static SpeedyList<ThemeColorDetails> GreenColors { get; }

	public static ThemeColorPaletteDetails GreenDetails { get; }

	public static SpeedyList<ThemeColorDetails> IndigoColors { get; }

	public static ThemeColorPaletteDetails IndigoDetails { get; }

	public static SpeedyList<ThemeColorDetails> OrangeColors { get; }

	public static ThemeColorPaletteDetails OrangeDetails { get; }

	public static SpeedyList<ThemeColorDetails> PinkColors { get; }

	public static ThemeColorPaletteDetails PinkDetails { get; }

	public static SpeedyList<ThemeColorDetails> PurpleColors { get; }

	public static ThemeColorPaletteDetails PurpleDetails { get; }

	public static SpeedyList<ThemeColorDetails> RedColors { get; }

	public static ThemeColorPaletteDetails RedDetails { get; }

	public static SpeedyList<ThemeColorDetails> TealColors { get; }

	public static ThemeColorPaletteDetails TealDetails { get; }

	public static string[] ThemeColorNames { get; }

	public static ThemeColorPaletteDetails[] ThemeColors { get; }

	public static SpeedyList<ThemeColorDetails> ThemeColorsForDark { get; }

	public static SpeedyList<ThemeColorDetails> ThemeColorsForLight { get; }

	public static SpeedyList<ThemeColorDetails> ThemeForegroundColors { get; }

	#endregion

	#region Methods

	private static SpeedyList<ThemeColorDetails> ToTheme(params string[] colors)
	{
		var items = colors.Select((value, i) => new ThemeColorDetails($"ThemeColor0{i}", value, ThemeForegroundColors[i].Background)).ToArray();
		return new SpeedyList<ThemeColorDetails> { items };
	}

	#endregion
}