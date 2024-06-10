#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Color = Avalonia.Media.Color;

#endregion

namespace Cornerstone.Avalonia.Controls;

public static class ColorExtensions
{
	#region Fields

	public static readonly Dictionary<ConsoleColor, Color> ConsoleColorMap =
		new()
		{
			{ ConsoleColor.Black, Colors.Black },
			{ ConsoleColor.Blue, Color.FromArgb(0xFF, 0x01, 0x24, 0x56) },
			{ ConsoleColor.Cyan, Colors.Cyan },
			{ ConsoleColor.DarkBlue, Color.FromRgb(0x01, 0x24, 0x56) },
			{ ConsoleColor.DarkCyan, Colors.DarkCyan },
			{ ConsoleColor.DarkGray, Colors.DarkGray },
			{ ConsoleColor.DarkGreen, Colors.DarkGreen },
			{ ConsoleColor.DarkMagenta, Colors.DarkMagenta },
			{ ConsoleColor.DarkRed, Colors.DarkRed },
			{ ConsoleColor.DarkYellow, Color.FromRgb(170, 170, 0) },
			{ ConsoleColor.Gray, Colors.Gray },
			{ ConsoleColor.Green, Color.FromRgb(0, 0xFF, 0) },
			{ ConsoleColor.Magenta, Colors.Magenta },
			{ ConsoleColor.Red, Colors.Red },
			{ ConsoleColor.White, Colors.White },
			{ ConsoleColor.Yellow, Colors.Yellow }
		};

	#endregion

	#region Methods

	public static Color AdjustBrightness(this Color color, double correctionFactor, bool excludeAlpha = true)
	{
		var alpha = (double) color.A;
		var red = (double) color.R;
		var green = (double) color.G;
		var blue = (double) color.B;

		if (correctionFactor < 0)
		{
			correctionFactor = 1 + correctionFactor;

			if (!excludeAlpha)
			{
				alpha *= correctionFactor;
			}

			red *= correctionFactor;
			green *= correctionFactor;
			blue *= correctionFactor;
		}
		else
		{
			if (!excludeAlpha)
			{
				alpha = ((255 - alpha) * correctionFactor) + alpha;
			}

			red = ((255 - red) * correctionFactor) + red;
			green = ((255 - green) * correctionFactor) + green;
			blue = ((255 - blue) * correctionFactor) + blue;
		}

		return Color.FromArgb((byte) alpha, (byte) red, (byte) green, (byte) blue);
	}

	public static Color FromHtmlString(this string value)
	{
		return Color.TryParse(value, out var response)
			? response
			: default;
	}

	public static SolidColorBrush ToBrush(this Color color)
	{
		return new SolidColorBrush(color);
	}

	public static Color ToColor(this ConsoleColor color)
	{
		return ConsoleColorMap[color];
	}

	public static ConsoleColor ToConsoleColor(this Color color)
	{
		if (ConsoleColorMap.ContainsValue(color))
		{
			return ConsoleColorMap.First(x => x.Value == color).Key;
		}

		// Return the color closest to the color requested.
		return ConsoleColorMap
			.Select(x => new
			{
				Color = x,
				Delta = Math.Abs(color.R - x.Value.R) +
					Math.Abs(color.B - x.Value.B) +
					Math.Abs(color.G - x.Value.G)
			})
			.OrderBy(x => x.Delta)
			.First()
			.Color
			.Key;
	}

	public static string ToHtmlString(this Color color)
	{
		return color.A < 255
			? $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255.0:0.###})"
			: $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	#endregion
}