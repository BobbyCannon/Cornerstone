#region References

using System;
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

		if ((caret?.ShouldShow(true) != true)
			|| (line == null))
		{
			return;
		}

		var lineText = line.ToString();
		var textLayout = _renderer.GetTextLayout(lineText);
		var localPosition = caret.Offset - line.StartOffset;
		localPosition = Math.Clamp(localPosition, 0, lineText.Length);

		var caretRect = textLayout.HitTestTextPosition(localPosition);
		if ((localPosition == lineText.Length) && (caretRect.Width <= 0))
		{
			// Place caret right after the last character
			caretRect = new Rect(
				textLayout.WidthIncludingTrailingWhitespace,
				caretRect.Y,
				1,
				caretRect.Height > 0
					? caretRect.Height
					: _renderer.TextMetrics.CharacterHeight
			);
		}

		var documentY = line.VisualLayout.Top + caretRect.Y;
		var renderX = caretRect.X - _renderer.Offset.X;
		var renderY = documentY - _renderer.Offset.Y;

		var caretWidth = caret.OverstrikeMode ? _renderer.TextMetrics.CharacterWidth : 1;
		var finalRect = new Rect(renderX, renderY, caretWidth, caretRect.Height);
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