#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Styling;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation.Theme.Theming;

/// <summary>
/// Combo and preview lists for theme pickers (same order as Theme.Colors / ThemeDensities).
/// </summary>
public static class ThemeChoices
{
	#region Constructors

	static ThemeChoices()
	{
		var colors = SourceReflector
			.GetEnumDetails<ThemeColor>()
			.OrderBy(x => x.DisplayOrder)
			.Select(x => (ThemeColor) x.Value)
			.ToArray();

		ThemeColors = colors;
		Colors = [.. colors.Except([ThemeColor.None, ThemeColor.Current])];
		Densities =
		[
			ThemeDensity.Compact,
			ThemeDensity.Normal,
			ThemeDensity.Large
		];
		Modes =
		[
			ThemeMode.Dark,
			ThemeMode.Light,
			ThemeMode.Default
		];
		Variants = [ThemeVariant.Dark, ThemeVariant.Light, ThemeVariant.Default];
	}

	#endregion

	#region Properties

	public static ThemeColor[] Colors { get; }

	public static ThemeDensity[] Densities { get; }

	public static ThemeMode[] Modes { get; }

	public static ThemeColor[] ThemeColors { get; }

	public static ThemeVariant[] Variants { get; }

	#endregion
}