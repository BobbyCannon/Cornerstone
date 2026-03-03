#region References

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Cornerstone.Avalonia.Text.Models;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

internal class CurrentLineRenderer : IRenderer
{
	#region Fields

	public static readonly Color DefaultBackground = Color.FromArgb(0xFF, 0, 0, 0);

	private readonly TextRenderer _renderer;

	#endregion

	#region Constructors

	public CurrentLineRenderer(TextRenderer textRenderer)
	{
		BackgroundBrush = new ImmutableSolidColorBrush(DefaultBackground);

		_renderer = textRenderer;
	}

	#endregion

	#region Properties

	public IBrush BackgroundBrush { get; set; }

	public Line CurrentLine { get; private set; }

	#endregion

	#region Methods

	public void Draw(TextRenderer renderer, DrawingContext drawingContext)
	{
		//if (!_renderer.Settings.HighlightCurrentLine)
		//{
		//	return;
		//}

		var line = _renderer.ViewModel.Caret.Line;
		if (line == null)
		{
			return;
		}

		CurrentLine = line;

		var bounds = new Rect(
			-_renderer.Margin.Left,
			line.VisualLayout.Y - renderer.Offset.Y,
			renderer.Bounds.Width + _renderer.Margin.Left + _renderer.Margin.Right,
			line.VisualLayout.Height
		);

		drawingContext.FillRectangle(BackgroundBrush, bounds);
	}

	#endregion
}