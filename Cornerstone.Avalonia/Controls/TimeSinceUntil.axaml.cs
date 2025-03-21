#region References

using System;
using Avalonia;
using Avalonia.Layout;
using Cornerstone.Data.Times;
using Cornerstone.Runtime;
using Cornerstone.Text;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed class TimeSinceUntil : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<HorizontalAlignment> ElapsedHorizontalAlignmentProperty;
	public static readonly StyledProperty<string> ElapsedLabelProperty;
	public static readonly StyledProperty<string> ElapsedTextProperty;
	public static readonly StyledProperty<DateTimeOffset> EndProperty;
	public static readonly StyledProperty<double> PercentProperty;
	public static readonly StyledProperty<int> ProgressBarHeightProperty;
	public static readonly StyledProperty<HorizontalAlignment> RemainingHorizontalAlignmentProperty;
	public static readonly StyledProperty<string> RemainingLabelProperty;
	public static readonly StyledProperty<string> RemainingTextProperty;
	public static readonly StyledProperty<bool> ShowPercentProperty;
	public static readonly StyledProperty<DateTimeOffset> StartProperty;
	public static readonly StyledProperty<HorizontalAlignment> TitleHorizontalAlignmentProperty;
	public static readonly StyledProperty<string> TitleProperty;
	private IDateTimeProvider _dateTimeProvider;
	private readonly HumanizeSettings _humanizeSettings;

	#endregion

	#region Constructors

	public TimeSinceUntil()
	{
		_dateTimeProvider = DateTimeProvider.RealTime;
		_humanizeSettings = new HumanizeSettings
		{
			MaxUnit = TimeUnit.Year,
			MinUnit = TimeUnit.Day
		};
	}

	static TimeSinceUntil()
	{
		ElapsedHorizontalAlignmentProperty = AvaloniaProperty.Register<TimeSinceUntil, HorizontalAlignment>(nameof(ElapsedHorizontalAlignment), HorizontalAlignment.Left);
		RemainingHorizontalAlignmentProperty = AvaloniaProperty.Register<TimeSinceUntil, HorizontalAlignment>(nameof(RemainingHorizontalAlignment), HorizontalAlignment.Right);
		TitleHorizontalAlignmentProperty = AvaloniaProperty.Register<TimeSinceUntil, HorizontalAlignment>(nameof(TitleHorizontalAlignment), HorizontalAlignment.Center);
		ElapsedLabelProperty = AvaloniaProperty.Register<TimeSinceUntil, string>(nameof(ElapsedLabel));
		ElapsedTextProperty = AvaloniaProperty.Register<TimeSinceUntil, string>(nameof(ElapsedText));
		EndProperty = AvaloniaProperty.Register<TimeSinceUntil, DateTimeOffset>(nameof(End));
		PercentProperty = AvaloniaProperty.Register<TimeSinceUntil, double>(nameof(Percent));
		ProgressBarHeightProperty = AvaloniaProperty.Register<TimeSinceUntil, int>(nameof(ProgressBarHeight), 10);
		ShowPercentProperty = AvaloniaProperty.Register<TimeSinceUntil, bool>(nameof(ShowPercent));
		StartProperty = AvaloniaProperty.Register<TimeSinceUntil, DateTimeOffset>(nameof(Start));
		TitleProperty = AvaloniaProperty.Register<TimeSinceUntil, string>(nameof(Title));
		RemainingLabelProperty = AvaloniaProperty.Register<TimeSinceUntil, string>(nameof(RemainingLabel));
		RemainingTextProperty = AvaloniaProperty.Register<TimeSinceUntil, string>(nameof(RemainingText));
	}

	#endregion

	#region Properties

	public TimeSpan Elapsed
	{
		get
		{
			var currentTime = _dateTimeProvider.Now;
			var start = Start == DateTimeOffset.MinValue ? _dateTimeProvider.Now : Start;
			return start >= currentTime ? TimeSpan.Zero : currentTime - start;
		}
	}

	public HorizontalAlignment ElapsedHorizontalAlignment
	{
		get => GetValue(ElapsedHorizontalAlignmentProperty);
		set => SetValue(ElapsedHorizontalAlignmentProperty, value);
	}

	public string ElapsedLabel
	{
		get => GetValue(ElapsedLabelProperty);
		set => SetValue(ElapsedLabelProperty, value);
	}

	public string ElapsedText
	{
		get => GetValue(ElapsedTextProperty);
		private set => SetValue(ElapsedTextProperty, value);
	}

	public DateTimeOffset End
	{
		get => GetValue(EndProperty);
		set => SetValue(EndProperty, value);
	}

	public double Percent
	{
		get => GetValue(PercentProperty);
		set => SetValue(PercentProperty, value);
	}

	public int ProgressBarHeight
	{
		get => GetValue(ProgressBarHeightProperty);
		set => SetValue(ProgressBarHeightProperty, value);
	}

	public HorizontalAlignment RemainingHorizontalAlignment
	{
		get => GetValue(RemainingHorizontalAlignmentProperty);
		set => SetValue(RemainingHorizontalAlignmentProperty, value);
	}

	public string RemainingLabel
	{
		get => GetValue(RemainingLabelProperty);
		set => SetValue(RemainingLabelProperty, value);
	}

	public string RemainingText
	{
		get => GetValue(RemainingTextProperty);
		private set => SetValue(RemainingTextProperty, value);
	}

	public bool ShowPercent
	{
		get => GetValue(ShowPercentProperty);
		set => SetValue(ShowPercentProperty, value);
	}

	public DateTimeOffset Start
	{
		get => GetValue(StartProperty);
		set => SetValue(StartProperty, value);
	}

	public string Title
	{
		get => GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public HorizontalAlignment TitleHorizontalAlignment
	{
		get => GetValue(TitleHorizontalAlignmentProperty);
		set => SetValue(TitleHorizontalAlignmentProperty, value);
	}

	public TimeSpan TotalTime
	{
		get
		{
			var start = Start == DateTimeOffset.MinValue ? _dateTimeProvider.Now : Start;
			var end = End == DateTimeOffset.MinValue ? _dateTimeProvider.Now : End;
			var response = end - start;
			return response > TimeSpan.Zero ? response : TimeSpan.Zero;
		}
	}

	#endregion

	#region Methods

	public void Refresh(IDateTimeProvider dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;

		Refresh();
	}

	public void Refresh()
	{
		var elapsed = Elapsed;
		var totalTime = TotalTime;

		Percent = (elapsed == TimeSpan.Zero) || (totalTime == TimeSpan.Zero)
			? 0
			: elapsed / totalTime;

		ElapsedText = elapsed.Humanize(_humanizeSettings);
		RemainingText = (totalTime - elapsed).Humanize(_humanizeSettings);

		OnPropertyChanged(nameof(Elapsed));
		OnPropertyChanged(nameof(TotalTime));
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if ((e.Property == StartProperty) ||
			(e.Property == EndProperty))
		{
			Refresh();
		}
	}

	#endregion
}