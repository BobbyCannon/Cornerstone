#region References

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed partial class CircularProgress : CornerstoneContentControl
{
	#region Fields

	private double _radius;

	#endregion

	#region Properties

	public static FuncValueConverter<CircularProgress, double> GetStrokeBorderThickness => new(x => x.StrokeThickness);

	[StyledProperty]
	public partial bool IsIndeterminate { get; set; }

	[StyledProperty(DefaultValue = 100)]
	public partial double Maximum { get; set; }

	[StyledProperty]
	public partial double Minimum { get; set; }

	/// <summary>
	/// Gets the overall percentage complete of the progress
	/// </summary>
	[DirectProperty]
	public double Percentage { get; private set; }

	[StyledProperty(DefaultValue = "{1:0}%")]
	public partial string ProgressTextFormat { get; set; }

	[StyledProperty]
	public partial bool ShowProgressText { get; set; }

	[StyledProperty]
	public partial IBrush Stroke { get; set; }

	[StyledProperty(DefaultValue = PenLineCap.Round)]
	public partial PenLineCap StrokeLineCap { get; set; }

	[StyledProperty(DefaultValue = 8)]
	public partial int StrokeThickness { get; set; }

	[StyledProperty]
	public partial double SweepAngle { get; set; }

	[StyledProperty]
	public partial double Value { get; set; }

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