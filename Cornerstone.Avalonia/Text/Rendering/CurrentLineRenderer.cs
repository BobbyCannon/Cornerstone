#region References

using Avalonia;
using Avalonia.Media;
using Cornerstone.Avalonia.Text.Models;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

internal class CurrentLineRenderer : IRenderer
{
	#region Fields

	private readonly TextRenderer _renderer;

	#endregion

	#region Constructors

	public CurrentLineRenderer(TextRenderer textRenderer)
	{
		_renderer = textRenderer;
	}

	#endregion

	#region Properties

	public Line CurrentLine { get; private set; }

	#endregion

	#region Methods

	public void Draw(TextRenderer renderer, DrawingContext drawingContext)
	{
		var vm = _renderer.ViewModel;
		var line = vm?.Caret.Line;
		if ((line == null) || !vm.HighlightCurrentLine)
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

		drawingContext.FillRectangle(_renderer.CurrentLineBackground, bounds);
	}

	#endregion
}