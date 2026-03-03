#region References

using System;
using System.Collections.Generic;
using System.Drawing;
using Cornerstone.Avalonia.Extensions;

#endregion

namespace Cornerstone.Avalonia.Themes;

public static class ThemeColorGenerator
{
	#region Methods

	public static List<Color> GenerateColorShades(Color baseColor, int shadeCount = 10, float minFactor = 0.5f)
	{
		var shades = new List<Color>();
		var halfCount = shadeCount / 2; // Split shades into lighter and darker halves
		var step = (1.0f - minFactor) / halfCount; // Step size for interpolation

		// Generate lighter shades (from minFactor towards white to base color)
		for (var i = 0; i < halfCount; i++)
		{
			var factor = minFactor + (step * i); // From minFactor (lighter) to 1.0 (base color)
			var r = (int) (baseColor.R + ((255 - baseColor.R) * (1.0f - factor)));
			var g = (int) (baseColor.G + ((255 - baseColor.G) * (1.0f - factor)));
			var b = (int) (baseColor.B + ((255 - baseColor.B) * (1.0f - factor)));
			shades.Add(Color.FromArgb(baseColor.A, r, g, b));
		}

		// Add the base color (middle)
		shades.Add(baseColor);

		// Generate darker shades (from base color to minFactor towards black)
		for (var i = 1; i < (halfCount + (shadeCount % 2)); i++) // Adjust for odd shadeCount
		{
			var factor = 1.0f - (step * i); // From 1.0 (base color) to minFactor (darker)
			var r = (int) (baseColor.R * factor);
			var g = (int) (baseColor.G * factor);
			var b = (int) (baseColor.B * factor);
			shades.Add(Color.FromArgb(baseColor.A, r, g, b));
		}

		shades.Reverse();
		return shades;
	}

	public static List<Color> GenerateColorShades(Color baseColor, int shadeCount = 10, float minFactor = 0.5f, bool lighterToOriginal = false)
	{
		var shades = new List<Color>();
		var step = lighterToOriginal
			? (minFactor - 1.0f) / (shadeCount - 1) // For lighter to original
			: (1.0f - minFactor) / (shadeCount - 1); // For darker to original

		for (var i = 0; i < shadeCount; i++)
		{
			var factor = lighterToOriginal
				? 1.0f + (step * i) // From 1.0 (original) to minFactor (lighter)
				: minFactor + (step * i); // From minFactor (darker) to 1.0 (original)

			// For lighter shades, interpolate towards white (255, 255, 255)
			if (lighterToOriginal)
			{
				var r = (int) (baseColor.R + ((255 - baseColor.R) * (1.0f - factor)));
				var g = (int) (baseColor.G + ((255 - baseColor.G) * (1.0f - factor)));
				var b = (int) (baseColor.B + ((255 - baseColor.B) * (1.0f - factor)));
				shades.Add(Color.FromArgb(baseColor.A, r, g, b));
			}
			else
			{
				var r = (int) (baseColor.R * factor);
				var g = (int) (baseColor.G * factor);
				var b = (int) (baseColor.B * factor);
				shades.Add(Color.FromArgb(baseColor.A, r, g, b));
			}
		}

		return shades;
	}

	public static List<(string bg, string fg)> GenerateColorTheme(string baseHexColor, double luminance = 0.179)
	{
		var baseColor = ColorTranslator.FromHtml(baseHexColor);
		var shades = GenerateColorShades(baseColor, 10, 0.5f);
		var colors = new List<(string, string)>();

		for (var i = 9; i >= 0; i--)
		{
			var shade = shades[i];
			var hex = shade.ToHtmlString();
			var foreground = GetBestForegroundColor(shade, luminance);
			var fgHex = foreground.ToHtmlString();
			colors.Add((hex, fgHex));
		}

		return colors;
	}

	/// <summary>
	/// Determines the best foreground color (black or white) based on the background color's luminance.
	/// </summary>
	/// <param name="backgroundColor"> The background color. </param>
	/// <param name="luminance"> The luminance threshold. </param>
	/// <returns> Color.Black or Color.White based on better contrast. </returns>
	public static Color GetBestForegroundColor(Color backgroundColor, double luminance)
	{
		// Calculate relative luminance using WCAG formula
		var relativeLuminance = CalculateRelativeLuminance(backgroundColor);

		// If luminance is high (light background), use black; if low (dark background), use white
		//return luminance > 0.179 ? Color.Black : Color.White;
		return relativeLuminance > luminance ? Color.Black : Color.White;
	}

	public static (float hue, float saturation, float lightness) RgbToHsl(int r, int g, int b)
	{
		float red = r / 255f, green = g / 255f, blue = b / 255f;
		var max = Math.Max(red, Math.Max(green, blue));
		var min = Math.Min(red, Math.Min(green, blue));
		var delta = max - min;

		float hue = 0;
		if (delta != 0)
		{
			if (max == red)
			{
				hue = ((green - blue) / delta) % 6;
			}
			else if (max == green)
			{
				hue = ((blue - red) / delta) + 2;
			}
			else
			{
				hue = ((red - green) / delta) + 4;
			}
			hue *= 60;
			if (hue < 0)
			{
				hue += 360;
			}
		}

		var lightness = (max + min) / 2;
		var saturation = delta == 0 ? 0 : delta / (1 - Math.Abs((2 * lightness) - 1));
		return (hue, saturation, lightness);
	}

	/// <summary>
	/// Calculates the relative luminance of a color based on WCAG 2.0 formula.
	/// </summary>
	/// <param name="color"> The color to calculate luminance for. </param>
	/// <returns> Relative luminance value between 0 and 1. </returns>
	private static double CalculateRelativeLuminance(Color color)
	{
		// Convert RGB to linear values
		var r = LinearizeColorComponent(color.R / 255.0);
		var g = LinearizeColorComponent(color.G / 255.0);
		var b = LinearizeColorComponent(color.B / 255.0);

		// Calculate luminance
		return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
	}

	/// <summary>
	/// Linearizes a single color component as per WCAG formula.
	/// </summary>
	/// <param name="component"> Color component value (0 to 1). </param>
	/// <returns> Linearized component value. </returns>
	private static double LinearizeColorComponent(double component)
	{
		// Apply sRGB gamma correction
		return component <= 0.03928 ? component / 12.92 : Math.Pow((component + 0.055) / 1.055, 2.4);
	}

	#endregion
}