#region References

using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Avalonia.Text.Models;

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
		var vm = _editor.ViewModel;
		if (vm == null)
		{
			return;
		}

		// this is necessary so hit-testing works properly and events get tunneled to the TextView.
		drawingContext.FillRectangle(Brushes.Transparent, Bounds);

		var renderer = _editor.Renderer;
		var topY = renderer.Offset.Y;
		var bottomY = renderer.Offset.Y + renderer.Bounds.Bottom;

		foreach (var line in vm.Lines)
		{
			if (line.VisualLayout.Bottom <= topY)
			{
				continue;
			}

			if (line.VisualLayout.Bottom >= bottomY)
			{
				break;
			}

			var lineText = line.LineNumber.ToString();
			using var textLayout = renderer.GetTextLayout(lineText);
			var textLeft = Bounds.Width - textLayout.Width - (vm.ViewMetrics.CharacterWidth / 2);
			textLayout.Draw(drawingContext, new(textLeft, line.VisualLayout.Top - topY));
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var vm = _editor.ViewModel;
		if (vm == null)
		{
			return default;
		}

		var maxLineNumberLength = vm.Lines.LastOrDefault().LineNumber.ToString().Length;
		using var text = _editor.Renderer.GetTextLayout(new string('9', maxLineNumberLength));
		var width = text.Width + vm.ViewMetrics.CharacterWidth;
		return new Size(width, availableSize.Height);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		var vm = _editor.ViewModel;
		if (vm == null)
		{
			return;
		}

		if (_editor.ViewModel.Caret.Selection.IsSelectingUsingMouse
			&& e.Properties.IsLeftButtonPressed)
		{
			var (line, offset) = GetLine(e);
			if ((line != null) && (_editor.ViewModel.Caret.Selection.EndOffset != offset))
			{
				_editor.ViewModel.Caret.Selection.Update(offset);
				_editor.ViewModel.Caret.Move(offset);
			}
		}

		base.OnPointerMoved(e);
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

		var (line, offset) = GetLine(e);
		_editor.ViewModel.Caret.Selection.StartMouseSelection();
		_editor.ViewModel.Caret.Selection.Update(line.StartOffset, offset);
		_editor.ViewModel.Caret.Move(line.StartOffset);
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			_editor.ViewModel.Caret.Selection.StopMouseSelection();
			InvalidateVisual();
		}
		base.OnPointerReleased(e);
	}

	private (Line, int) GetLine(PointerEventArgs e)
	{
		// Get position relative to the top-left of the visible area
		var localPoint = e.GetPosition(this);

		// Convert to document-space coordinates
		var documentX = int.MaxValue;
		var documentY = localPoint.Y + _editor.Renderer.Offset.Y;

		if (_editor.ViewModel.Lines.TryGetLineForOffset(documentX, documentY, out var line))
		{
			var offset = line.GetNearestOffsetAtVisual(documentX, documentY, false);
			return (line, offset);
		}

		return (null, 0);
	}

	#endregion
}