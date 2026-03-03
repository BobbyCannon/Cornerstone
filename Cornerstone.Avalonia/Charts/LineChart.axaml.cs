#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Avalonia.Drawing;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Avalonia.Charts;

public partial class LineChart : CornerstoneTemplatedControl
{
	#region Fields

	private readonly DrawingContextHelper _contextHelper;
	private IBrush _fill;
	private readonly Pen _linePen;

	#endregion

	#region Constructors

	public LineChart()
	{
		_linePen = new(null, 2);
		_contextHelper = new DrawingContextHelper(this);

		if (Design.IsDesignMode)
		{
			Data ??= new SeriesDataProvider();
		}
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial ISeriesDataProvider Data { get; set; }

	[StyledProperty]
	public partial IBrush Stroke { get; set; }

	[StyledProperty]
	public partial string Title { get; set; }

	public Func<double, string> ValueFormatter
	{
		get;
		set
		{
			if (field != value)
			{
				field = value;
				InvalidateVisual();
			}
		}
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext context)
	{
		using var start = ProfilerExtensions.Start(Profiler, "Render");
		var borderThickness = CornerstoneExtensions.GetBestSingle(BorderThickness);
		var cornerRadius = (float) CornerstoneExtensions.GetBestSingle(CornerRadius);
		var backgroundArea = new Rect(Bounds.Size);

		if ((BorderBrush != null) && (borderThickness > 0))
		{
			backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
			var roundedRect = new RoundedRect(backgroundArea, CornerRadius.TopLeft, CornerRadius.TopRight, CornerRadius.BottomRight, CornerRadius.BottomLeft);
			context.DrawRectangle(Background, new Pen(BorderBrush, borderThickness), roundedRect);
			backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
		}
		else
		{
			context.DrawRectangle(Background, null, backgroundArea);
		}

		var clippedRect = new RoundedRect(backgroundArea, cornerRadius);
		using var _ = context.PushClip(clippedRect);

		var maxValue = 0d;
		var minValue = 0d;
		var lastValue = 0d;
		var data = Data;

		if (data is not null)
		{
			var fillGeometry = new StreamGeometry();
			var lineGeometry = new StreamGeometry();

			using (var ctxFill = fillGeometry.Open())
			using (var ctxLine = lineGeometry.Open())
			{
				var offsetX = Padding.Left + backgroundArea.Left;
				var offsetY = Padding.Top + backgroundArea.Top;
				var gWidth = backgroundArea.Width - Padding.Left - Padding.Right;
				var gHeight = backgroundArea.Height - Padding.Top - Padding.Bottom;
				var xStep = gWidth / (data.Length - 1);

				ctxFill.BeginFigure(new Point(offsetX, offsetY + gHeight), true);

				for (var i = 0; i < data.Length; i++)
				{
					lastValue = data[i];
					if (lastValue > maxValue)
					{
						maxValue = lastValue;
					}
				}

				for (var i = 0; i < data.Length; i++)
				{
					lastValue = data[i];

					var x = (i * xStep) + offsetX;
					var y = (gHeight - (((lastValue - minValue) / (maxValue - minValue)) * gHeight)) + offsetY;

					ctxFill.LineTo(new Point(x, y));

					if (i == 0)
					{
						ctxLine.BeginFigure(new Point(x, y), false);
					}
					else
					{
						ctxLine.LineTo(new Point(x, y));
					}
				}

				var lastX = ((Data.Length - 1) * xStep) + offsetX;
				ctxFill.LineTo(new Point(lastX, gHeight + offsetY));
				ctxFill.LineTo(new Point(offsetX, gHeight + offsetY));
				ctxFill.EndFigure(true);
				ctxLine.EndFigure(false);
			}

			var fillBrush = _fill ??= CornerstoneExtensions.WithOpacity(Stroke, 0.15);
			context.DrawGeometry(fillBrush, null, fillGeometry);

			_linePen.Brush = Stroke;
			context.DrawGeometry(null, _linePen, lineGeometry);
		}

		var visualX = 10d;
		var visualY = 8d;
		_contextHelper.Draw(context, Title, ref visualX, ref visualY);
		_contextHelper.Draw(context, ":", ref visualX, ref visualY);
		_contextHelper.Draw(context, " MAX: ", ref visualX, ref visualY);
		_contextHelper.Draw(context, maxValue, ref visualX, ref visualY);
		_contextHelper.Draw(context, " VAL: ", ref visualX, ref visualY);

		if (ValueFormatter != null)
		{
			_contextHelper.Draw(context, ValueFormatter.Invoke(lastValue), ref visualX, ref visualY);
		}
		else
		{
			_contextHelper.Draw(context, lastValue, ref visualX, ref visualY);
		}

		base.Render(context);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == DataProperty)
		{
			if (change.OldValue is ISeriesDataProvider oldValue)
			{
				oldValue.DataChanged -= OnDataChanged;
			}
			if (change.NewValue is ISeriesDataProvider newValue)
			{
				newValue.DataChanged += OnDataChanged;
			}
		}

		if (change.Property == StrokeProperty)
		{
			_fill = null;
		}

		base.OnPropertyChanged(change);
	}

	private void OnDataChanged(object sender, EventArgs e)
	{
		InvalidateVisual();
	}

	#endregion
}