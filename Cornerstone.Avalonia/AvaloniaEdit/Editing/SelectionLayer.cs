#region References

using System;
using Avalonia.Media;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

internal sealed class SelectionLayer : Layer
{
	#region Fields

	private readonly TextArea _textArea;

	#endregion

	#region Constructors

	public SelectionLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Selection)
	{
		IsHitTestVisible = false;

		_textArea = textArea;

		TextViewWeakEventManager.VisualLinesChanged.AddHandler(TextView, ReceiveWeakEvent);
		TextViewWeakEventManager.ScrollOffsetChanged.AddHandler(TextView, ReceiveWeakEvent);
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext drawingContext)
	{
		base.Render(drawingContext);

		var selectionBorder = _textArea.SelectionBorder;

		var geoBuilder = new BackgroundGeometryBuilder
		{
			AlignToWholePixels = true,
			BorderThickness = selectionBorder?.Thickness ?? 0,
			ExtendToFullWidthAtLineEnd = _textArea.Selection.EnableVirtualSpace,
			CornerRadius = _textArea.SelectionCornerRadius
		};

		foreach (var segment in _textArea.Selection.Segments)
		{
			geoBuilder.AddSegment(TextView, segment);
		}

		var geometry = geoBuilder.CreateGeometry();
		if (geometry != null)
		{
			drawingContext.DrawGeometry(_textArea.SelectionBackground, selectionBorder, geometry);
		}
	}

	private void ReceiveWeakEvent(object sender, EventArgs e)
	{
		InvalidateVisual();
	}

	#endregion
}