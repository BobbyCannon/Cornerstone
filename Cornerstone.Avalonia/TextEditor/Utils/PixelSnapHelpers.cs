﻿#region References

using System;
using Avalonia;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

/// <summary>
/// Contains static helper methods for aligning stuff on a whole number of pixels.
/// </summary>
public static class PixelSnapHelpers
{
	#region Methods

	/// <summary>
	/// Gets the pixel size on the screen containing visual.
	/// This method does not take transforms on visual into account.
	/// </summary>
	public static Size GetPixelSize(Visual visual)
	{
		if (visual == null)
		{
			throw new ArgumentNullException(nameof(visual));
		}

		// TODO-avedit
		//PresentationSource source = PresentationSource.FromVisual(visual);
		//if (source != null) {
		//	Matrix matrix = source.CompositionTarget.TransformFromDevice;
		//	return new Size(matrix.M11, matrix.M22);
		//} else {
		return new Size(1, 1);
		//}
	}

	/// <summary>
	/// Aligns <paramref name="value" /> on the next middle of a pixel.
	/// </summary>
	/// <param name="value"> The value that should be aligned </param>
	/// <param name="pixelSize"> The size of one pixel </param>
	public static double PixelAlign(double value, double pixelSize)
	{
		// 0 -> 0.5
		// 0.1 -> 0.5
		// 0.5 -> 0.5
		// 0.9 -> 0.5
		// 1 -> 1.5
		return pixelSize * (Math.Round((value / pixelSize) + 0.5, MidpointRounding.AwayFromZero) - 0.5);
	}

	/// <summary>
	/// Aligns the borders of rect on the middles of pixels.
	/// </summary>
	public static Rect PixelAlign(Rect rect, Size pixelSize)
	{
		var x = PixelAlign(rect.X, pixelSize.Width);
		var y = PixelAlign(rect.Y, pixelSize.Height);
		var width = Round(rect.Width, pixelSize.Width);
		var height = Round(rect.Height, pixelSize.Height);
		return new Rect(x, y, width, height);
	}

	/// <summary>
	/// Rounds <paramref name="point" /> to whole number of pixels.
	/// </summary>
	public static Point Round(Point point, Size pixelSize)
	{
		return new Point(Round(point.X, pixelSize.Width), Round(point.Y, pixelSize.Height));
	}

	/// <summary>
	/// Rounds val to whole number of pixels.
	/// </summary>
	public static Rect Round(Rect rect, Size pixelSize)
	{
		return new Rect(Round(rect.X, pixelSize.Width), Round(rect.Y, pixelSize.Height),
			Round(rect.Width, pixelSize.Width), Round(rect.Height, pixelSize.Height));
	}

	/// <summary>
	/// Rounds <paramref name="value" /> to a whole number of pixels.
	/// </summary>
	public static double Round(double value, double pixelSize)
	{
		return pixelSize * Math.Round(value / pixelSize, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Rounds <paramref name="value" /> to an whole odd number of pixels.
	/// </summary>
	public static double RoundToOdd(double value, double pixelSize)
	{
		return Round(value - pixelSize, pixelSize * 2) + pixelSize;
	}

	#endregion
}