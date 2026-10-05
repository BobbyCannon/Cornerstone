#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Controls.Text.Models;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Rendering;

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

		var offset = _renderer.Offset;
		var topY = offset.Y;
		var bottomY = offset.Y + _renderer.Bounds.Height;

		foreach (var caret in vm.Carets.All)
		{
			if (caret.Selection.Length <= 0)
			{
				continue;
			}

			foreach (var documentRect in GetPaintedDocumentRects(vm, caret, topY, bottomY))
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

	/// <summary>
	/// Document-space selection rectangles for every caret, ignoring viewport clip.
	/// </summary>
	internal static List<Rect> CollectDocumentRects(TextEditorViewModel viewModel)
	{
		var rects = new List<Rect>();
		if (viewModel == null)
		{
			return rects;
		}

		foreach (var caret in viewModel.Carets.All)
		{
			if (caret.Selection.Length <= 0)
			{
				continue;
			}

			rects.AddRange(GetDocumentRects(viewModel, caret, double.NegativeInfinity, double.PositiveInfinity));
		}

		return rects;
	}

	private IEnumerable<Rect> GetPaintedDocumentRects(TextEditorViewModel vm, Caret caret, double topY, double bottomY)
	{
		var startOffset = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var endOffset = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var firstLine = vm.Lines.GetLineFromOffset(startOffset);
		var lastLine = vm.Lines.GetLineFromOffset(Math.Max(endOffset - 1, startOffset));

		if ((firstLine == null) || (lastLine == null))
		{
			yield break;
		}

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
				yield break;
			}

			foreach (var documentRect in _renderer.GetPaintedSelectionRects(line, startOffset, endOffset))
			{
				yield return documentRect;
			}
		}
	}

	private static IEnumerable<Rect> GetDocumentRects(TextEditorViewModel vm, Caret caret, double topY, double bottomY)
	{
		var startOffset = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var endOffset = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var firstLine = vm.Lines.GetLineFromOffset(startOffset);
		var lastLine = vm.Lines.GetLineFromOffset(Math.Max(endOffset - 1, startOffset));

		if ((firstLine == null) || (lastLine == null))
		{
			yield break;
		}

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
				yield break;
			}

			foreach (var documentRect in line.GetSelectionRects(startOffset, endOffset))
			{
				yield return documentRect;
			}
		}
	}

	#endregion
}
