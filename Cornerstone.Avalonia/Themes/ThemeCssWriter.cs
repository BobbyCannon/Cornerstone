#region References

using System.Collections.Generic;
using System.Text;
using Cornerstone.Avalonia;

#endregion

namespace Cornerstone.Avalonia.Themes;

/// <summary>
/// Emits CSS custom properties from the same palette that feeds Theme.Light / Theme.Dark.
/// </summary>
public static class ThemeCssWriter
{
	#region Constants

	public const string SelectionColor = "rgba(0, 85, 255, 0.4)";

	#endregion

	#region Methods

	public static string Write()
	{
		var builder = new StringBuilder();
		builder.AppendLine("/* Generated from ThemeColorPalette. Do not edit by hand. */");
		builder.AppendLine();
		AppendThemeBlock(builder, ":root, [data-theme=\"light\"]", ThemeColorPalette.ThemeColorsForLight);
		builder.AppendLine();
		AppendThemeBlock(builder, "[data-theme=\"dark\"]", ThemeColorPalette.ThemeColorsForDark);
		builder.AppendLine();
		builder.AppendLine("@media (prefers-color-scheme: dark) {");
		AppendThemeBlock(builder, "\t:root:not([data-theme])", ThemeColorPalette.ThemeColorsForDark, "\t");
		builder.AppendLine("}");
		builder.AppendLine();
		builder.AppendLine(":root {");
		AppendVariable(builder, "", "ControlFontSize", CornerstoneTheme.GetControlFontSize(ThemeDensity.Normal).ToString("0") + "px");
		AppendVariable(builder, "", "ControlFontSizeSmall", CornerstoneTheme.GetControlFontSizeSmall(ThemeDensity.Normal).ToString("0") + "px");
		AppendVariable(builder, "", "ControlCornerRadius", "4px");
		AppendVariable(builder, "", "ControlBorderThickness", "1px");
		AppendVariable(builder, "", "Theme-Accent", "var(--Theme-Blue)");
		builder.AppendLine("}");
		builder.AppendLine();
		AppendDensity(builder, "compact", ThemeDensity.Compact);
		AppendDensity(builder, "normal", ThemeDensity.Normal);
		AppendDensity(builder, "large", ThemeDensity.Large);
		foreach (var accent in ThemeColorPalette.ThemeColors)
		{
			builder.Append(":root[data-theme-color=\"");
			builder.Append(accent.ThemeColor);
			builder.AppendLine("\"] {");
			builder.Append("\t--Theme-Accent: var(--Theme-");
			builder.Append(accent.ThemeColor);
			builder.AppendLine(");");
			builder.AppendLine("}");
			builder.AppendLine();
		}

		return builder.ToString();
	}

	private static void AppendDensity(StringBuilder builder, string name, ThemeDensity density)
	{
		builder.Append(":root[data-density=\"");
		builder.Append(name);
		builder.AppendLine("\"] {");
		builder.Append("\t--ControlFontSize: ");
		builder.Append(CornerstoneTheme.GetControlFontSize(density).ToString("0"));
		builder.AppendLine("px;");
		builder.Append("\t--ControlFontSizeSmall: ");
		builder.Append(CornerstoneTheme.GetControlFontSizeSmall(density).ToString("0"));
		builder.AppendLine("px;");
		builder.AppendLine("}");
		builder.AppendLine();
	}

	private static void AppendThemeBlock(StringBuilder builder, string selector, IReadOnlyList<ThemeColorDetails> ramp, string indent = "")
	{
		builder.Append(indent);
		builder.Append(selector);
		builder.AppendLine(" {");
		for (var i = 0; i < ramp.Count; i++)
		{
			var index = i.ToString("00");
			AppendVariable(builder, indent, "Background" + index, ramp[i].Color);
			AppendVariable(builder, indent, "Foreground" + index, ramp[i].Foreground);
		}

		AppendVariable(builder, indent, "BorderBrush", ramp.Count > 6 ? ramp[6].Color : ramp[^1].Color);
		AppendVariable(builder, indent, "SelectionColor", SelectionColor);

		foreach (var accent in ThemeColorPalette.ThemeColors)
		{
			AppendVariable(builder, indent, "Theme-" + accent.ThemeColor, accent.Color.Color);
		}

		builder.Append(indent);
		builder.AppendLine("}");
	}

	private static void AppendVariable(StringBuilder builder, string indent, string name, string value)
	{
		builder.Append(indent);
		builder.Append("\t--");
		builder.Append(name);
		builder.Append(": ");
		builder.Append(value);
		builder.AppendLine(";");
	}

	#endregion
}
