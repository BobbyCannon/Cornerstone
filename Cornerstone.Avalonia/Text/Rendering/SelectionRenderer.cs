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
		if (vm == null)
		{
			return;
		}

		var selection = vm.Caret.Selection;
		if (selection.Length <= 0)
		{
			return;
		}

		var offset = _renderer.Offset;
		var topY = offset.Y;
		var bottomY = offset.Y + _renderer.Bounds.Height;
		var startOffset = Math.Min(selection.StartOffset, selection.EndOffset);
		var endOffset = Math.Max(selection.StartOffset, selection.EndOffset);
		var firstLine = vm.Lines.GetLineFromOffset(startOffset);
		var lastLine = vm.Lines.GetLineFromOffset(Math.Max(endOffset - 1, startOffset));

		if ((firstLine == null) || (lastLine == null))
		{
			return;
		}

		// Walk logical lines; each line emits one rect per soft-wrapped visual row using
		// WrappedStartOffsets + ViewMetrics.GetAdvance (same authority as caret / hit-test).
		for (var lineNumber = firstLine.LineNumber; lineNumber <= lastLine.LineNumber; lineNumber++)
		{
			if (!vm.Lines.TryGetLine(lineNumber, out var line))
			{
				continue;
			}

			if (line.VisualLayout.Bottom < topY)
			{
				continue;
			}

			if (line.VisualLayout.Top > bottomY)
			{
				break;
			}

			foreach (var documentRect in line.GetSelectionRects(startOffset, endOffset))
			{
				var screenRect = new Rect(
					documentRect.X - offset.X,
					documentRect.Y - offset.Y,
					documentRect.Width,
					documentRect.Height
				);

				drawingContext.FillRectangle(BackgroundBrush, screenRect);
			}
		}
	}

	#endregion
}
