#region References

using System.Collections.Generic;
using System.Text;

#endregion

namespace Cornerstone.Presentation.Theme.Theming;

/// <summary>
/// Emits a WPF resource dictionary from the same palette that feeds Theme.Light / Theme.Dark.
/// </summary>
public static class ThemeWpfWriter
{
	#region Methods

	public static string Write()
	{
		var builder = new StringBuilder();
		builder.AppendLine("<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
		builder.AppendLine("\t\txmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
		builder.AppendLine("\t<!-- Generated from ThemeColorPalette. Do not edit by hand. -->");
		AppendRamp(builder, "Light", ThemeColorPalette.ThemeColorsForLight);
		AppendRamp(builder, "Dark", ThemeColorPalette.ThemeColorsForDark);
		builder.AppendLine("</ResourceDictionary>");
		return builder.ToString();
	}

	private static void AppendRamp(StringBuilder builder, string prefix, IReadOnlyList<ThemeColorDetails> ramp)
	{
		for (var i = 0; i < ramp.Count; i++)
		{
			var index = i.ToString("00");
			AppendColor(builder, prefix + ".Background" + index, ramp[i].Color);
			AppendColor(builder, prefix + ".Foreground" + index, ramp[i].Foreground);
		}

		var border = ramp.Count > 6 ? ramp[6].Color : ramp[ramp.Count - 1].Color;
		AppendColor(builder, prefix + ".Border", border);
		AppendColor(builder, prefix + ".Selection", "#660055FF");
		foreach (var accent in ThemeColorPalette.ThemeColors)
		{
			AppendColor(builder, prefix + ".Theme" + accent.ThemeColor, accent.Color.Color);
		}

		AppendBrushes(builder, prefix, ramp);
	}

	private static void AppendBrushes(StringBuilder builder, string prefix, IReadOnlyList<ThemeColorDetails> ramp)
	{
		for (var i = 0; i < ramp.Count; i++)
		{
			var index = i.ToString("00");
			AppendBrush(builder, prefix, "Background" + index);
			AppendBrush(builder, prefix, "Foreground" + index);
		}

		AppendBrush(builder, prefix, "Border");
		AppendBrush(builder, prefix, "Selection");
		foreach (var accent in ThemeColorPalette.ThemeColors)
		{
			AppendBrush(builder, prefix, "Theme" + accent.ThemeColor);
		}
	}

	private static void AppendColor(StringBuilder builder, string key, string color)
	{
		builder.Append("\t<Color x:Key=\"");
		builder.Append(key);
		builder.Append("\">");
		builder.Append(color);
		builder.AppendLine("</Color>");
	}

	private static void AppendBrush(StringBuilder builder, string prefix, string name)
	{
		builder.Append("\t<SolidColorBrush x:Key=\"");
		builder.Append(prefix);
		builder.Append('.');
		builder.Append(name);
		builder.Append("Brush\" Color=\"{StaticResource ");
		builder.Append(prefix);
		builder.Append('.');
		builder.Append(name);
		builder.AppendLine("}\" />");
	}

	#endregion
}
