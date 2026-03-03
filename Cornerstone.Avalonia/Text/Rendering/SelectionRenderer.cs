#region References

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

internal class SelectionRenderer : IRenderer
{
	#region Fields

	public static readonly Color DefaultBackground = Color.FromArgb(0x66, 0, 0x55, 0xFF);

	private readonly TextRenderer _renderer;

	#endregion

	#region Constructors

	public SelectionRenderer(TextRenderer textRenderer)
	{
		BackgroundBrush = new ImmutableSolidColorBrush(DefaultBackground);

		_renderer = textRenderer;
	}

	#endregion

	#region Properties

	public IBrush BackgroundBrush { get; set; }

	#endregion

	#region Methods

	public void Draw(TextRenderer renderer, DrawingContext drawingContext)
	{
		var vm = _renderer.ViewModel;
		var selection = vm.Selection;

		if (selection.Length <= 0)
		{
			return;
		}

		var offset = _renderer.Offset;
		var topY = offset.Y;
		var bottomY = offset.Y + _renderer.Bounds.Height;
		var startOffset = Math.Min(selection.StartOffset, selection.EndOffset);
		var endOffset = Math.Max(selection.StartOffset, selection.EndOffset);
		var firstLine = vm.Document.Lines.GetLineForOffset(startOffset);
		var lastLine = vm.Document.Lines.GetLineForOffset(endOffset);

		for (var lineNumber = firstLine.LineNumber; lineNumber <= lastLine.LineNumber; lineNumber++)
		{
			var line = vm.Document.Lines[lineNumber - 1];
			if (line.VisualLayout.Bottom < topY)
			{
				continue;
			}
			if (line.VisualLayout.Top > bottomY)
			{
				break;
			}

			// get the selection of this line
			var lineSelStart = Math.Max(startOffset, line.StartOffset);
			var lineSelEnd = Math.Min(endOffset, line.StartOffset + line.Length);

			if (lineSelStart >= lineSelEnd)
			{
				continue;
			}

			var localStart = lineSelStart - line.StartOffset;
			var localLength = lineSelEnd - lineSelStart;
			var textLayout = _renderer.GetTextLayout(line.ToString());

			// Get bounding rectangles for the selected range in **this line**
			var rects = textLayout.HitTestTextRange(localStart, localLength);

			// Draw each rectangle (usually 1 per line, but can be >1 with tabs/wrap)
			foreach (var rect in rects)
			{
				// rect is in local line coordinates → translate to screen
				var screenRect = new Rect(
					rect.X - offset.X,
					(rect.Y + line.VisualLayout.Top) - offset.Y, // ← important: use precomputed line top
					rect.Width,
					rect.Height
				);

				// Optional: inset a tiny bit so it doesn't touch line edges too harshly
				// screenRect = screenRect.Deflate(new Thickness(0, 1, 0, 1));

				drawingContext.FillRectangle(BackgroundBrush, screenRect);
			}
		}
	}

	#endregion
}