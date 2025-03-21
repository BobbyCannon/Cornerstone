#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Folding;

[DoNotNotify]
internal sealed class FoldingMarginMarker : Control
{
	#region Constants

	private const double MarginSizeFactor = 0.7;

	#endregion

	#region Fields

	internal FoldingSection FoldingSection;
	internal VisualLine VisualLine;

	private bool _isExpanded;

	#endregion

	#region Properties

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded != value)
			{
				_isExpanded = value;
				InvalidateVisual();
			}
			if (FoldingSection != null)
			{
				FoldingSection.IsFolded = !value;
			}
		}
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext drawingContext)
	{
		var margin = (FoldingMargin) Parent;
		var activePen = new Pen(margin.SelectedFoldingMarkerBrush,
			lineCap: PenLineCap.Square);
		var inactivePen = new Pen(margin.FoldingMarkerBrush,
			lineCap: PenLineCap.Square);
		var pixelSize = PixelSnapHelpers.GetPixelSize(this);
		var rect = new Rect(pixelSize.Width / 2,
			pixelSize.Height / 2,
			Bounds.Width - pixelSize.Width,
			Bounds.Height - pixelSize.Height);

		drawingContext.FillRectangle(
			IsPointerOver ? margin.SelectedFoldingMarkerBackgroundBrush : margin.FoldingMarkerBackgroundBrush,
			rect);

		drawingContext.DrawRectangle(
			IsPointerOver ? activePen : inactivePen,
			rect);

		var middleX = rect.X + (rect.Width / 2);
		var middleY = rect.Y + (rect.Height / 2);
		var space = PixelSnapHelpers.Round(rect.Width / 8, pixelSize.Width) + pixelSize.Width;
		drawingContext.DrawLine(activePen,
			new Point(rect.X + space, middleY),
			new Point(rect.Right - space, middleY));
		if (!_isExpanded)
		{
			drawingContext.DrawLine(activePen,
				new Point(middleX, rect.Y + space),
				new Point(middleX, rect.Bottom - space));
		}
	}

	protected override Size MeasureCore(Size availableSize)
	{
		var size = MarginSizeFactor * FoldingMargin.SizeFactor * GetValue(TextBlock.FontSizeProperty);
		size = PixelSnapHelpers.RoundToOdd(size, PixelSnapHelpers.GetPixelSize(this).Width);
		return new Size(size, size);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		Cursor = Cursor.Default;
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		if (!e.Handled)
		{
			IsExpanded = !IsExpanded;
			e.Handled = true;
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == IsPointerOverProperty)
		{
			InvalidateVisual();
		}
	}

	#endregion
}