#region References

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed class CircularProgress : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<bool> IsIndeterminateProperty;
	public static readonly StyledProperty<double> MaximumProperty;
	public static readonly StyledProperty<double> MinimumProperty;
	public static readonly DirectProperty<CircularProgress, double> PercentageProperty;
	public static readonly StyledProperty<string> ProgressTextFormatProperty;
	public static readonly StyledProperty<bool> ShowProgressTextProperty;
	public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty;
	public static readonly StyledProperty<IBrush> StrokeProperty;
	public static readonly StyledProperty<int> StrokeThicknessProperty;
	public static readonly StyledProperty<double> SweepAngleProperty;
	public static readonly StyledProperty<double> ValueProperty;

	private double _percentage;
	private double _radius;

	#endregion

	#region Constructors

	static CircularProgress()
	{
		IsIndeterminateProperty = AvaloniaProperty.Register<CircularProgress, bool>(nameof(IsIndeterminate));
		MaximumProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(Maximum), 100);
		MinimumProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(Minimum));
		PercentageProperty = AvaloniaProperty.RegisterDirect<CircularProgress, double>(nameof(Percentage), x => x.Percentage);
		ProgressTextFormatProperty = AvaloniaProperty.Register<CircularProgress, string>(nameof(ProgressTextFormat), "{1:0}%");
		ShowProgressTextProperty = AvaloniaProperty.Register<CircularProgress, bool>(nameof(ShowProgressText));
		StrokeLineCapProperty = AvaloniaProperty.Register<CircularProgress, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Round);
		StrokeProperty = AvaloniaProperty.Register<CircularProgress, IBrush>(nameof(Stroke));
		StrokeThicknessProperty = AvaloniaProperty.Register<CircularProgress, int>(nameof(StrokeThickness), 8);
		SweepAngleProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(SweepAngle));
		ValueProperty = AvaloniaProperty.Register<CircularProgress, double>(nameof(Value));
	}

	#endregion

	#region Properties

	public static FuncValueConverter<CircularProgress, double> GetStrokeBorderThickness => new(x => x.StrokeThickness);

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

	/// <summary>
	/// Gets the overall percentage complete of the progress
	/// </summary>
	public double Percentage
	{
		get => _percentage;
		private set => SetAndRaise(PercentageProperty, ref _percentage, value);
	}

	public string ProgressTextFormat
	{
		get => GetValue(ProgressTextFormatProperty);
		set => SetValue(ProgressTextFormatProperty, value);
	}

	public bool ShowProgressText
	{
		get => GetValue(ShowProgressTextProperty);
		set => SetValue(ShowProgressTextProperty, value);
	}

	public IBrush Stroke
	{
		get => GetValue(StrokeProperty);
		set => SetValue(StrokeProperty, value);
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

	public double Value
	{
		get => GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
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
		Percentage = percentage * 100;
	}

	#endregion
}