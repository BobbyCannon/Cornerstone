#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Color = Avalonia.Media.Color;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class ColorExtensions
{
	#region Fields

	public static readonly IReadOnlyDictionary<ConsoleColor, Color> ConsoleColorMap;
	public static readonly IReadOnlyDictionary<string, Color> ControlColorMap;
	private static readonly ConcurrentDictionary<uint, SolidColorBrush> _brushCache;

	#endregion

	#region Constructors

	static ColorExtensions()
	{
		_brushCache = new();

		ConsoleColorMap = new ReadOnlyDictionary<ConsoleColor, Color>(
			new Dictionary<ConsoleColor, Color>
			{
				{ ConsoleColor.Black, Colors.Black },
				{ ConsoleColor.Red, Colors.Red },
				{ ConsoleColor.DarkRed, Colors.DarkRed },
				{ ConsoleColor.Green, Color.FromRgb(0, 0xFF, 0) },
				{ ConsoleColor.DarkGreen, Colors.DarkGreen },
				{ ConsoleColor.Yellow, Colors.Yellow },
				{ ConsoleColor.DarkYellow, Color.FromRgb(128, 128, 0) },
				{ ConsoleColor.Blue, Color.FromArgb(0xFF, 0x01, 0x24, 0xFF) },
				{ ConsoleColor.DarkBlue, Color.FromRgb(0x01, 0x24, 0x56) },
				{ ConsoleColor.Magenta, Colors.Magenta },
				{ ConsoleColor.DarkMagenta, Colors.DarkMagenta },
				{ ConsoleColor.Cyan, Colors.Cyan },
				{ ConsoleColor.DarkCyan, Colors.DarkCyan },
				{ ConsoleColor.White, Colors.White },
				{ ConsoleColor.Gray, Colors.Gray },
				{ ConsoleColor.DarkGray, Colors.DarkGray }
			});

		ControlColorMap = new ReadOnlyDictionary<string, Color>(
			new Dictionary<string, Color>
			{
				// Standard colors 30-37 (foreground) / 40-47 (background)
				["30"] = ConsoleColorMap[ConsoleColor.Black],
				["31"] = ConsoleColorMap[ConsoleColor.DarkRed],
				["32"] = ConsoleColorMap[ConsoleColor.DarkGreen],
				["33"] = ConsoleColorMap[ConsoleColor.DarkYellow],
				["34"] = ConsoleColorMap[ConsoleColor.DarkBlue],
				["35"] = ConsoleColorMap[ConsoleColor.DarkMagenta],
				["36"] = ConsoleColorMap[ConsoleColor.DarkCyan],
				["37"] = ConsoleColorMap[ConsoleColor.DarkGray],

				["40"] = ConsoleColorMap[ConsoleColor.Black],
				["41"] = ConsoleColorMap[ConsoleColor.DarkRed],
				["42"] = ConsoleColorMap[ConsoleColor.DarkGreen],
				["43"] = ConsoleColorMap[ConsoleColor.DarkYellow],
				["44"] = ConsoleColorMap[ConsoleColor.DarkBlue],
				["45"] = ConsoleColorMap[ConsoleColor.DarkMagenta],
				["46"] = ConsoleColorMap[ConsoleColor.DarkCyan],
				["47"] = ConsoleColorMap[ConsoleColor.DarkGray],

				// Bright colors 90-97 (foreground) / 100-107 (background)
				["90"] = ConsoleColorMap[ConsoleColor.Black],
				["91"] = ConsoleColorMap[ConsoleColor.Red],
				["92"] = ConsoleColorMap[ConsoleColor.Green],
				["93"] = ConsoleColorMap[ConsoleColor.Yellow],
				["94"] = ConsoleColorMap[ConsoleColor.Blue],
				["95"] = ConsoleColorMap[ConsoleColor.Magenta],
				["96"] = ConsoleColorMap[ConsoleColor.Cyan],
				["97"] = ConsoleColorMap[ConsoleColor.White],

				["100"] = ConsoleColorMap[ConsoleColor.Black],
				["101"] = ConsoleColorMap[ConsoleColor.Red],
				["102"] = ConsoleColorMap[ConsoleColor.Green],
				["103"] = ConsoleColorMap[ConsoleColor.Yellow],
				["104"] = ConsoleColorMap[ConsoleColor.Blue],
				["105"] = ConsoleColorMap[ConsoleColor.Magenta],
				["106"] = ConsoleColorMap[ConsoleColor.Cyan],
				["107"] = ConsoleColorMap[ConsoleColor.White]
			});

		// Pre-cache common colors to avoid lock contention in hot paths
		foreach (var color in ConsoleColorMap.Values)
		{
			var argb = color.ToUInt32();
			_brushCache[argb] = new SolidColorBrush(color);
		}
	}

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
		return Color.TryParse(value, out var response) ? response : default;
	}

	public static IBrush GetBrush(this ConsoleColor consoleColor)
	{
		var argb = ToColor(consoleColor).ToUInt32();
		return GetBrush(argb);
	}

	public static IBrush GetBrush(this uint argb)
	{
		// Fast path: no locking or heavy synchronization for existing keys
		if (_brushCache.TryGetValue(argb, out var brush))
		{
			return brush;
		}

		// Slow path: use GetOrAdd to handle the race condition of multiple threads creating the same color
		return _brushCache.GetOrAdd(argb, x => new SolidColorBrush(Color.FromUInt32(x)));
	}

	public static Color ToColor(this ConsoleColor color)
	{
		return ConsoleColorMap[color];
	}

	public static ConsoleColor ToConsoleColor(this Color color)
	{
		var bestMatch = ConsoleColor.Black;
		var minDelta = double.MaxValue;

		foreach (var entry in ConsoleColorMap)
		{
			if (entry.Value == color)
			{
				return entry.Key;
			}

			double delta = Math.Abs(color.R - entry.Value.R) +
				Math.Abs(color.B - entry.Value.B) +
				Math.Abs(color.G - entry.Value.G);

			if (delta < minDelta)
			{
				minDelta = delta;
				bestMatch = entry.Key;
			}
		}

		return bestMatch;
	}

	public static string ToHtmlString(this Color color)
	{
		return color.A < 255
			? $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255.0:0.###})"
			: $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	public static string ToHtmlString(this System.Drawing.Color color)
	{
		return color.A < 255
			? $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255.0:0.###})"
			: $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	#endregion
}