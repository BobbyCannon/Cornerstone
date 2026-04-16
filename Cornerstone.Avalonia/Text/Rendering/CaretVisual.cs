#region References

using Avalonia;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

public partial class CaretVisual : CornerstoneControl
{
	#region Fields

	private readonly TextRenderer _renderer;

	#endregion

	#region Constructors

	public CaretVisual(TextRenderer renderer)
	{
		_renderer = renderer;
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext context)
	{
		if (_renderer.ViewModel == null)
		{
			base.Render(context);
			return;
		}

		var caret = _renderer.ViewModel.Caret;
		var line = caret?.Line;

		if ((caret?.ToggleBlink() != true)
			|| (line == null))
		{
			return;
		}

		var caretRect = caret.VisualLayout;
		var renderX = caretRect.X - _renderer.Offset.X;
		var renderY = caretRect.Y - _renderer.Offset.Y;

		var caretWidth = caret.OverstrikeMode ? _renderer.ViewModel.ViewMetrics.CharacterWidth : 1;
		var finalRect = new Rect(renderX, renderY, caretWidth, caret.VisualLayout.Height);
		var caretBrush = _renderer.Foreground;

		if (caret.OverstrikeMode
			&& caretBrush is ISolidColorBrush brush)
		{
			var color = brush.Color;
			caretBrush = new SolidColorBrush(Color.FromArgb(100, color.R, color.G, color.B));
		}

		context.FillRectangle(caretBrush, finalRect);

		base.Render(context);
	}

	#endregion
}