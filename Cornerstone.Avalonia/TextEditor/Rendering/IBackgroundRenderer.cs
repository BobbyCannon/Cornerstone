#region References

using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Background renderers draw in the background of a known layer.
/// You can use background renderers to draw non-interactive elements on the TextView
/// without introducing new UIElements.
/// </summary>
public interface IBackgroundRenderer
{
	#region Properties

	/// <summary>
	/// Gets the layer on which this background renderer should draw.
	/// </summary>
	KnownLayer Layer { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Causes the background renderer to draw.
	/// </summary>
	void Draw(TextView textView, DrawingContext drawingContext);

	#endregion
}