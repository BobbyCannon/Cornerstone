#region References

using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Avalonia.Text.Models;

#endregion

namespace Cornerstone.Avalonia.Text.Margins;

/// <summary>
/// Margin showing line numbers.
/// </summary>
public class LineNumberMargin<T> : Margin
	where T : TextEditorViewModel, new()
{
	#region Fields

	private readonly TextEditor<T> _editor;

	#endregion

	#region Constructors

	public LineNumberMargin(TextEditor<T> editor)
	{
		_editor = editor;

		Cursor = GetRightArrowCursor();
	}

	#endregion

	#region Methods

	public static Size Measure(TextEditor<T> editor, int maxLineNumber, Size availableSize)
	{
		// Add an extra character for padding
		var maxLineNumberLength = maxLineNumber.ToString().Length + 1;
		var columnWidth = 0.0;

		if (editor.Renderer != null)
		{
			using var singleCharLayout = editor.Renderer.GetTextLayout("9");
			columnWidth = singleCharLayout.Width * maxLineNumberLength;
		}

		// Avalonia rejects NaN/Infinity from MeasureOverride. When the parent
		// height is unconstrained (StackPanel, Auto row, etc.), report content
		// height instead of availableSize.Height (PositiveInfinity).
		var height = availableSize.Height;
		if (!double.IsFinite(height))
		{
			var metrics = editor.ViewModel?.ViewMetrics;
			height = metrics?.DocumentSize.Height ?? 0;
			if ((height <= 0) && (metrics != null) && double.IsFinite(metrics.CharacterHeight))
			{
				height = metrics.CharacterHeight;
			}
		}

		if (!double.IsFinite(columnWidth) || (columnWidth < 0))
		{
			columnWidth = 0;
		}
		if (!double.IsFinite(height) || (height < 0))
		{
			height = 0;
		}

		return new Size(columnWidth, height);
	}

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

		foreach (var line in _editor.Renderer.GetVisualLines())
		{
			var lineText = line.LineNumber.ToString();
			using var textLayout = renderer.GetTextLayout(lineText);
			var textLeft = Bounds.Width - textLayout.Width - (vm.ViewMetrics.CharacterWidth / 2);
			textLayout.Draw(drawingContext, new(textLeft, line.VisualLayout.Top - topY));
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var maxLineNumber = _editor.ViewModel.Lines.Any() ? _editor.ViewModel.Lines.LastOrDefault().LineNumber : 1;
		return Measure(_editor, maxLineNumber, availableSize);
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