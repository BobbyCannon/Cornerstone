#region References

using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Text.Margins;

/// <summary>
/// Margin showing line numbers.
/// </summary>
public class LineNumberMargin : Margin
{
	#region Fields

	private readonly TextEditor _editor;

	#endregion

	#region Constructors

	public LineNumberMargin(TextEditor editor)
	{
		_editor = editor;

		Cursor = GetRightArrowCursor();
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext drawingContext)
	{
		// this is necessary so hit-testing works properly and events get tunneled to the TextView.
		drawingContext.FillRectangle(Brushes.Transparent, Bounds);

		var renderer = _editor.Renderer;
		var topY = renderer.Offset.Y;
		var bottomY = renderer.Offset.Y + Bounds.Bottom;

		foreach (var line in renderer.ViewModel.Document.Lines)
		{
			if (line.VisualLayout.Bottom < topY)
			{
				continue;
			}

			if (line.VisualLayout.Top > bottomY)
			{
				break;
			}

			var lineText = line.LineNumber.ToString();
			var textLayout = renderer.GetTextLayout(lineText);
			var textLeft = Bounds.Width - textLayout.Width - (renderer.TextMetrics.CharacterWidth / 2);
			textLayout.Draw(drawingContext, new(textLeft, line.VisualLayout.Top - topY));
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var maxLineNumberLength = _editor.ViewModel.Document.Lines.LastOrDefault().LineNumber.ToString().Length;
		var text = _editor.Renderer.GetTextLayout(new string('9', maxLineNumberLength));
		var width = text.Width + _editor.Renderer.TextMetrics.CharacterWidth;
		return new Size(width, availableSize.Height);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		var viewModel = _editor.ViewModel;
		if ((viewModel == null)
			|| !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			base.OnPointerPressed(e);
			return;
		}

		// Get position relative to the top-left of the visible area
		var localPoint = e.GetPosition(this);

		// Convert to document-space coordinates
		var documentY = localPoint.Y + _editor.Renderer.Offset.Y;
		var lineIndex = Math.Min(
			_editor.ViewModel.Document.Lines.Count - 1,
			(int) Math.Floor(documentY / _editor.Renderer.TextMetrics.CharacterHeight)
		);

		var line = _editor.ViewModel.Document.Lines[lineIndex];
		_editor.ViewModel.Caret.Move(line.StartOffset);

		base.OnPointerPressed(e);
	}

	#endregion
}