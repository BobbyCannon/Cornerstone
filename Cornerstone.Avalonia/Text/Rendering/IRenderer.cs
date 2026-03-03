#region References

using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

/// <summary>
/// Custom renderer to draw in the TextRenderer.
/// You can use renderers to draw non-interactive elements without introducing new UIElements.
/// </summary>
public interface IRenderer
{
	#region Methods

	/// <summary>
	/// Causes the background renderer to draw.
	/// </summary>
	void Draw(TextRenderer renderer, DrawingContext drawingContext);

	#endregion
}