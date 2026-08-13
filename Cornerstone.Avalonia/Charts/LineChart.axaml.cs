#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
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

	/// <summary>
	/// Chronological samples captured when <see cref="Data" /> is assigned or raises
	/// <see cref="ISeriesDataProvider.DataChanged" />. Render uses this snapshot so a
	/// pre-filled provider (values written before bind) still paints, and so ring-buffer
	/// mutation after the fact cannot leave the control stuck on zeros.
	/// </summary>
	private double[] _plotSamples = [];

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

	static LineChart()
	{
		// Data must AffectsRender: callers often replace the whole series with a pre-filled
		// provider (AddRange before bind). DataChanged already fired with no subscribers.
		AffectsRender<LineChart>(
			DataProperty,
			ForegroundProperty,
			StrokeProperty,
			ShowLabelsProperty,
			OverLabelsProperty,
			SteppedProperty,
			RelativeScaleProperty,
			RelativeMinimumPaddingProperty,
			ScaleMaximumProperty
		);

		AffectsMeasure<LineChart>(
			DataProperty,
			FontFamilyProperty,
			FontSizeProperty,
			FontStyleProperty,
			FontWeightProperty
		);
	}

	#endregion

	#region Properties

	[StyledProperty(DefaultValue = false)]
	public partial bool OverLabels { get; set; }

	[StyledProperty]
	public partial ISeriesDataProvider Data { get; set; }

	/// <summary>
	/// When true, Y min is (data min − |data min| × <see cref="RelativeMinimumPadding" />)
	/// and Y max is data max so close values spread vertically. When false, Y min is 0.
	/// </summary>
	[StyledProperty(DefaultValue = false)]
	public partial bool RelativeScale { get; set; }

	/// <summary>
	/// Fraction of the smallest sample subtracted from that min when <see cref="RelativeScale" />
	/// is true (default 0.1 = 10%).
	/// </summary>
	[StyledProperty(DefaultValue = 0.1)]
	public partial double RelativeMinimumPadding { get; set; }

	/// <summary>
	/// When &gt; 0 and <see cref="RelativeScale" /> is false, Y max is at least this value
	/// (e.g. 100 for a fixed 0–100% axis). Raised if any sample exceeds it.
	/// </summary>
	[StyledProperty(DefaultValue = 0d)]
	public partial double ScaleMaximum { get; set; }

	[StyledProperty(DefaultValue = true)]
	public partial bool ShowLabels { get; set; }

	[StyledProperty(DefaultValue = false)]
	public partial bool ShowLabelForMaximum { get; set; }

	[StyledProperty(DefaultValue = true)]
	public partial bool Stepped { get; set; }

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
		var cornerRadius = (float)CornerstoneExtensions.GetBestSingle(CornerRadius);
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
		var samples = _plotSamples;
		// First paint can race Data assignment before OnPropertyChanged snapshots; pull live once.
		if ((samples.Length == 0) && (Data is { Length: > 0 } live))
		{
			samples = CaptureSamples(live);
			_plotSamples = samples;
		}

		// This prevents the chart fill/line from ever drawing into the top-left label area.
		var topMargin = ShowLabels && !OverLabels ? _contextHelper.SpriteHeight + 10 : 0d;

		if (samples.Length > 0)
		{
			var fillGeometry = new StreamGeometry();
			var lineGeometry = new StreamGeometry();

			using (var ctxFill = fillGeometry.Open())
			using (var ctxLine = lineGeometry.Open())
			{
				var offsetX = Padding.Left + backgroundArea.Left;
				var offsetY = Padding.Top + backgroundArea.Top + topMargin;
				var gWidth = Math.Max(0, backgroundArea.Width - Padding.Left - Padding.Right);
				var gHeight = Math.Max(0, backgroundArea.Height - Padding.Top - Padding.Bottom - topMargin);

				// Stepped: each sample owns an equal-width bin so the last day/value still
				// gets a horizontal segment (point mode only places a vertical at the right edge).
				// Linear: vertices at i/(n-1) so the polyline spans the full plot width.
				var binCount = samples.Length;
				var xStep = Stepped
					? (binCount > 0 ? gWidth / binCount : 0)
					: (binCount > 1 ? gWidth / (binCount - 1) : 0);
				var lastPoint = new Point(offsetX, offsetY + gHeight);

				ctxFill.BeginFigure(lastPoint, true);

				var dataMin = samples[0];
				var dataMax = samples[0];
				lastValue = samples[0];
				for (var i = 1; i < samples.Length; i++)
				{
					lastValue = samples[i];
					if (lastValue > dataMax)
					{
						dataMax = lastValue;
					}

					if (lastValue < dataMin)
					{
						dataMin = lastValue;
					}
				}

				ResolveVerticalScale(
					dataMin,
					dataMax,
					RelativeScale,
					RelativeMinimumPadding,
					ScaleMaximum,
					out minValue,
					out maxValue);

				var range = maxValue - minValue;
				if (!(range > 0) || double.IsNaN(range) || double.IsInfinity(range))
				{
					range = 1;
				}

				var plotBottom = gHeight + offsetY;
				var lastX = offsetX;

				for (var i = 0; i < samples.Length; i++)
				{
					lastValue = samples[i];

					var y = (gHeight - (((lastValue - minValue) / range) * gHeight)) + offsetY;
					if (double.IsNaN(y) || double.IsInfinity(y))
					{
						y = plotBottom;
					}

					if (Stepped)
					{
						// Bin [xLeft, xRight]: vertical join at left, then horizontal to right.
						var xLeft = (i * xStep) + offsetX;
						var xRight = ((i + 1) * xStep) + offsetX;
						lastX = xRight;

						if (i == 0)
						{
							ctxFill.LineTo(new Point(xLeft, y));
							ctxFill.LineTo(new Point(xRight, y));
							ctxLine.BeginFigure(new Point(xLeft, y), false);
							ctxLine.LineTo(new Point(xRight, y));
						}
						else
						{
							// lastPoint is the previous bin's right edge at previous Y
							ctxFill.LineTo(new Point(xLeft, lastPoint.Y));
							ctxFill.LineTo(new Point(xLeft, y));
							ctxFill.LineTo(new Point(xRight, y));
							ctxLine.LineTo(new Point(xLeft, lastPoint.Y));
							ctxLine.LineTo(new Point(xLeft, y));
							ctxLine.LineTo(new Point(xRight, y));
						}

						lastPoint = new Point(xRight, y);
					}
					else
					{
						var x = (i * xStep) + offsetX;
						lastX = x;
						var nextPoint = new Point(x, y);
						ctxFill.LineTo(nextPoint);

						if (i == 0)
						{
							ctxLine.BeginFigure(nextPoint, false);
						}
						else
						{
							ctxLine.LineTo(nextPoint);
						}

						lastPoint = nextPoint;
					}
				}

				ctxFill.LineTo(new Point(lastX, plotBottom));
				ctxFill.LineTo(new Point(offsetX, plotBottom));
				ctxFill.EndFigure(true);
				ctxLine.EndFigure(false);
			}

			var fillBrush = _fill ??= CornerstoneExtensions.WithOpacity(Stroke, 0.15);
			context.DrawGeometry(fillBrush, null, fillGeometry);

			_linePen.Brush = Stroke;
			context.DrawGeometry(null, _linePen, lineGeometry);
		}

		if (ShowLabels)
		{
			var visualX = 10d;
			var visualY = 8d;
			_contextHelper.Draw(context, Title, ref visualX, ref visualY);
			_contextHelper.Draw(context, ": ", ref visualX, ref visualY);
			if (ShowLabelForMaximum)
			{
				_contextHelper.Draw(context, "MAX: ", ref visualX, ref visualY);
				_contextHelper.Draw(context, maxValue, ref visualX, ref visualY);
				_contextHelper.Draw(context, " VAL: ", ref visualX, ref visualY);
			}
			if (ValueFormatter != null)
			{
				_contextHelper.Draw(context, ValueFormatter.Invoke(lastValue), ref visualX, ref visualY);
			}
			else
			{
				_contextHelper.Draw(context, lastValue, ref visualX, ref visualY);
			}
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

			CapturePlotSamples();
			RequestPlotRedraw();
		}

		if (change.Property == StrokeProperty)
		{
			// Drop cached fill so WithOpacity rebuilds from the new stroke (theme color).
			_fill = null;
			InvalidateVisual();
		}

		base.OnPropertyChanged(change);
	}

	private void OnDataChanged(object sender, EventArgs e)
	{
		// Prefer SeriesDataProvider.CopyFrom / AddRange so bulk model→view updates
		// raise DataChanged once (single invalidate) instead of per-sample Add spam.
		//
		// Model series may mutate Off Dispatcher (AppDispatcher demos). Capture from the
		// event source without touching Avalonia styled properties, then repaint on UI.
		var source = sender as ISeriesDataProvider;
		var samples = CaptureSamples(source);

		if (Dispatcher.UIThread.CheckAccess())
		{
			ApplyPlotSamples(samples);
			return;
		}

		Dispatcher.UIThread.Post(() => ApplyPlotSamples(samples), DispatcherPriority.Render);
	}

	private void ApplyPlotSamples(double[] samples)
	{
		_plotSamples = samples ?? [];
		RequestPlotRedraw();
	}

	private void CapturePlotSamples()
	{
		// Data is a styled property — UI thread only (property change path).
		_plotSamples = CaptureSamples(Data);
	}

	private static double[] CaptureSamples(ISeriesDataProvider data)
	{
		if (data is null || (data.Length <= 0))
		{
			return [];
		}

		var samples = new double[data.Length];
		for (var i = 0; i < samples.Length; i++)
		{
			samples[i] = data[i];
		}

		return samples;
	}

	private void RequestPlotRedraw()
	{
		// Fixed Height charts often keep the same measure; InvalidateMeasure alone may not
		// repaint. Always invalidate visual when samples change.
		InvalidateVisual();
		InvalidateMeasure();
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		base.OnSizeChanged(e);
		// First layout often has non-zero bounds only after measure; repaint with captured samples.
		if ((e.NewSize.Width > 0) && (e.NewSize.Height > 0))
		{
			InvalidateVisual();
		}
	}

	/// <summary>
	/// Resolves Y-axis min/max for plotting.
	/// Relative: min = dataMin − |dataMin| × padding, max = dataMax.
	/// Absolute (default): min = 0, max = max(1, dataMax, scaleMaximum when scaleMaximum &gt; 0).
	/// </summary>
	internal static void ResolveVerticalScale(
		double dataMin,
		double dataMax,
		bool relativeScale,
		double relativeMinimumPadding,
		out double minValue,
		out double maxValue)
	{
		ResolveVerticalScale(dataMin, dataMax, relativeScale, relativeMinimumPadding, 0, out minValue, out maxValue);
	}

	/// <summary>
	/// Resolves Y-axis min/max for plotting.
	/// When <paramref name="scaleMaximum" /> &gt; 0 and not relative, Y max is at least that floor.
	/// </summary>
	internal static void ResolveVerticalScale(
		double dataMin,
		double dataMax,
		bool relativeScale,
		double relativeMinimumPadding,
		double scaleMaximum,
		out double minValue,
		out double maxValue)
	{
		if (relativeScale)
		{
			var padding = relativeMinimumPadding;
			if (double.IsNaN(padding) || (padding < 0))
			{
				padding = 0;
			}

			minValue = dataMin - (Math.Abs(dataMin) * padding);
			maxValue = dataMax;
			if (!(maxValue > minValue))
			{
				// Flat series: keep a non-zero range so division is safe and the line sits mid-plot.
				var pad = Math.Max(1, Math.Abs(dataMax) * 0.1);
				minValue = dataMin - pad;
				maxValue = dataMax + pad;
			}

			return;
		}

		minValue = 0;
		maxValue = Math.Max(1, dataMax);
		if ((scaleMaximum > 0) && !double.IsNaN(scaleMaximum) && !double.IsInfinity(scaleMaximum))
		{
			maxValue = Math.Max(maxValue, scaleMaximum);
		}
	}

	#endregion
}