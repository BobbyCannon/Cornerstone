#region References

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed class CircularProgress : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<bool> IsIndeterminateProperty = AvaloniaProperty.Register<CircularProgress, bool>(nameof(IsIndeterminate));
	public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(Maximum), 100);
	public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(Minimum));
	public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(Value));
	public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty = AvaloniaProperty.Register<CircularProgress, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Round);
	public static readonly StyledProperty<int> StrokeThicknessProperty = AvaloniaProperty.Register<CircularProgress, int>(nameof(StrokeThickness), 5);
	public static readonly StyledProperty<double> SweepAngleProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(SweepAngle));
	private double _radius;

	#endregion

	#region Properties

	public static FuncValueConverter<CircularProgress, double> GetStrokeBorderThickness => new(x => x.StrokeThickness + x.BorderThickness.Max());

	public bool IsIndeterminate
	{
		get => GetValue(IsIndeterminateProperty);
		set => SetValue(IsIndeterminateProperty, value);
	}

	public double Maximum
	{
		get => GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	public double Minimum
	{
		get => GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public double Value
	{
		get => GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public PenLineCap StrokeLineCap
	{
		get => GetValue(StrokeLineCapProperty);
		set => SetValue(StrokeLineCapProperty, value);
	}

	public int StrokeThickness
	{
		get => GetValue(StrokeThicknessProperty);
		set => SetValue(StrokeThicknessProperty, value);
	}

	public double SweepAngle
	{
		get => GetValue(SweepAngleProperty);
		set => SetValue(SweepAngleProperty, value);
	}

	#endregion

	#region Methods

	protected override Size MeasureOverride(Size availableSize)
	{
		_radius = availableSize.Height / 2;
		_radius -= StrokeThickness;
		RenderArc();
		return new Size(_radius * 2, _radius * 2);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if ((e.Property == IsIndeterminateProperty) ||
			(e.Property == MinimumProperty) ||
			(e.Property == MaximumProperty) ||
			(e.Property == StrokeThicknessProperty) ||
			(e.Property == ValueProperty))
		{
			RenderArc();
		}
	}

	private void RenderArc()
	{
		var total = Maximum - Minimum;
		var value = Value - Minimum;
		double percentage;

		if (value <= 0)
		{
			percentage = 0;
		}
		else if (value > total)
		{
			percentage = 1;
		}
		else
		{
			percentage = value / total;
		}
		SweepAngle = percentage * 360;
	}

	#endregion
}