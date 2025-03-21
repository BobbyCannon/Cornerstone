#region References

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Renders a ruler at a certain column.
/// </summary>
internal sealed class ColumnRulerRenderer : IBackgroundRenderer
{
	#region Fields

	public static readonly Color DefaultForeground = Colors.LightGray;
	private IEnumerable<int> _columns;
	private IPen _pen;
	private readonly TextView _textView;

	#endregion

	#region Constructors

	public ColumnRulerRenderer(TextView textView)
	{
		_pen = new ImmutablePen(new ImmutableSolidColorBrush(DefaultForeground));
		_textView = textView ?? throw new ArgumentNullException(nameof(textView));
		_textView.BackgroundRenderers.Add(this);
	}

	#endregion

	#region Properties

	public KnownLayer Layer => KnownLayer.Background;

	#endregion

	#region Methods

	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		if (_columns == null)
		{
			return;
		}

		foreach (var column in _columns)
		{
			var offset = textView.WideSpaceWidth * column;
			var pixelSize = PixelSnapHelpers.GetPixelSize(textView);
			var markerXPos = PixelSnapHelpers.PixelAlign(offset, pixelSize.Width);
			markerXPos -= textView.ScrollOffset.X;
			var start = new Point(markerXPos, 0);
			var end = new Point(markerXPos, Math.Max(textView.DocumentHeight, textView.Bounds.Height));

			drawingContext.DrawLine(
				_pen,
				start.SnapToDevicePixels(textView),
				end.SnapToDevicePixels(textView));
		}
	}

	public void SetRuler(IEnumerable<int> columns, IPen pen)
	{
		_columns = columns;
		_pen = pen;
		_textView.InvalidateLayer(Layer);
	}

	#endregion
}