#region References

using Avalonia;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

public class ViewMetrics
{
	#region Properties

	public double CharacterHeight { get; set; }

	public double CharacterWidth { get; set; }

	/// <summary>
	/// The total size of the document.
	/// </summary>
	public Size DocumentSize { get; set; }

	/// <summary>
	/// The offset of the view.
	/// </summary>
	public Vector Offset { get; set; }

	/// <summary>
	/// The size of the viewport.
	/// </summary>
	public Size Viewport { get; set; }

	#endregion

	#region Methods

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