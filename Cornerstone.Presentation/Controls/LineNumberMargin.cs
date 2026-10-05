#region References

using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Text;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Margin showing line numbers.
/// </summary>
public class LineNumberMargin<T> : Margin
	where T : TextEditorViewModel, new()
{
	#region Fields

	private readonly TextEditor<T> _editor;
	private int _visualLineSelectionAnchorStart;
	private int _visualLineSelectionAnchorEnd;
	private bool _isGutterSelecting;

	#endregion

	#region Constructors

	public LineNumberMargin(TextEditor<T> editor)
	{
		_editor = editor;

		Cursor = GetRightArrowCursor();
		Focusable = false;
		IsHitTestVisible = true;
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

		// Cornerstone rejects NaN/Infinity from MeasureOverride. When the parent
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
			var textLayout = renderer.GetLineNumberLayout(line.LineNumber);
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
		if (_isGutterSelecting && e.Properties.IsLeftButtonPressed)
		{
			_editor.ViewModel?.SelectVisualLineRange(
				_visualLineSelectionAnchorStart,
				_visualLineSelectionAnchorEnd,
				GetDocumentY(e));
			e.Handled = true;
		}

		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		var viewModel = _editor.ViewModel;
		var properties = e.GetCurrentPoint(this).Properties;
		if ((viewModel == null)
			|| (!properties.IsLeftButtonPressed
				&& (properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)))
		{
			base.OnPointerPressed(e);
			return;
		}

		var documentY = GetDocumentY(e);
		if (!viewModel.TryGetVisualLineRange(documentY, out var start, out var end)
			|| !viewModel.SelectVisualLine(documentY))
		{
			base.OnPointerPressed(e);
			return;
		}

		_visualLineSelectionAnchorStart = start;
		_visualLineSelectionAnchorEnd = end;
		_isGutterSelecting = true;
		e.Handled = true;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		_isGutterSelecting = false;
		base.OnPointerReleased(e);
	}

	private double GetDocumentY(PointerEventArgs e)
	{
		var offsetY = _editor.Renderer?.Offset.Y ?? 0;
		return e.GetPosition(this).Y + offsetY;
	}

	#endregion
}