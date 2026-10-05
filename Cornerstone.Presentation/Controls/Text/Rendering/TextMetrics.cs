#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Rendering;

public class ViewMetrics
{
	#region Constants

	/// <summary>
	/// Extra leading relative to font size, matching CornerstoneEdit's paragraph LineHeight.
	/// </summary>
	public const double DefaultLineHeightMultiplier = 1.35;

	#endregion

	#region Properties

	public double CharacterHeight { get; set; }

	public double CharacterWidth { get; set; }

	/// <summary>
	/// The total size of the document.
	/// </summary>
	public Size DocumentSize { get; set; }

	/// <summary>
	/// Scroll position. Source of truth on TextEditorViewModel; the renderer applies this
	/// and must not overwrite it from a collapsed (inactive tab) viewport.
	/// </summary>
	public Vector Offset { get; set; }

	/// <summary>
	/// The size of the viewport.
	/// </summary>
	public Size Viewport { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Clamp Offset to the scrollable range for the given extent and viewport.
	/// </summary>
	public Vector ClampOffset(Size extent, Size viewport)
	{
		var maxX = Math.Max(0, extent.Width - viewport.Width);
		var maxY = Math.Max(0, extent.Height - viewport.Height);
		var x = Offset.X;
		var y = Offset.Y;
		if (x < 0)
		{
			x = 0;
		}
		else if (x > maxX)
		{
			x = maxX;
		}

		if (y < 0)
		{
			y = 0;
		}
		else if (y > maxY)
		{
			y = maxY;
		}

		return new Vector(x, y);
	}

	/// <summary>
	/// Visual row pitch for the given font size (glyphs plus leading).
	/// </summary>
	public static double GetLineHeight(double fontSize)
	{
		return fontSize * DefaultLineHeightMultiplier;
	}

	/// <summary>
	/// Single source of truth for how many pixels a character advances.
	/// </summary>
	public double GetAdvance(char c)
	{
		return c switch
		{
			'\r' or '\n' => 0,
			'\t' => CharacterWidth * 4,
			_ when c <= 0x7F => CharacterWidth, // ASCII
			_ when c <= 0xFFFF => CharacterWidth, // BMP
			_ => CharacterWidth * 2
		};
	}

	#endregion
}