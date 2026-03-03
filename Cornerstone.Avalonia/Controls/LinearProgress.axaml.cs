#region References

using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

#endregion

namespace Cornerstone.Avalonia.Controls;

[PseudoClasses(":vertical", ":horizontal", ":indeterminate")]
public class LinearProgress : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<TimeSpan> InDeterminateDurationProperty;
	public static readonly StyledProperty<bool> IsIndeterminateProperty;
	public static readonly StyledProperty<double> MaximumProperty;
	public static readonly StyledProperty<double> MinimumProperty;
	public static readonly StyledProperty<Orientation> OrientationProperty;
	public static readonly StyledProperty<double> PercentageProperty;
	public static readonly StyledProperty<string> ProgressTextFormatProperty;
	public static readonly StyledProperty<bool> ShowProgressTextProperty;
	public static readonly StyledProperty<IBrush> StrokeProperty;
	public static readonly StyledProperty<double> ValueProperty;
	private Animation _animation;
	private CancellationTokenSource _animationCancellationTokenSource;
	private Border _progressBar;

	#endregion

	#region Constructors

	public LinearProgress()
	{
		UpdatePseudoClasses(IsIndeterminate, Orientation);
	}

	static LinearProgress()
	{
		InDeterminateDurationProperty = AvaloniaProperty.Register<LinearProgress, TimeSpan>(nameof(InDeterminateDuration), TimeSpan.FromSeconds(1.5));
		IsIndeterminateProperty = AvaloniaProperty.Register<LinearProgress, bool>(nameof(IsIndeterminate));
		MaximumProperty = AvaloniaProperty.Register<LinearProgress, double>(nameof(Maximum), 100);
		MinimumProperty = AvaloniaProperty.Register<LinearProgress, double>(nameof(Minimum));
		OrientationProperty = AvaloniaProperty.Register<LinearProgress, Orientation>(nameof(Orientation));
		PercentageProperty = AvaloniaProperty.Register<LinearProgress, double>(nameof(Percentage));
		ProgressTextFormatProperty = AvaloniaProperty.Register<LinearProgress, string>(nameof(ProgressTextFormat), "{1:0}%");
		ShowProgressTextProperty = AvaloniaProperty.Register<LinearProgress, bool>(nameof(ShowProgressText));
		StrokeProperty = AvaloniaProperty.Register<LinearProgress, IBrush>(nameof(Stroke));
		ValueProperty = AvaloniaProperty.Register<LinearProgress, double>(nameof(Value));
	}

	#endregion

	#region Properties

	public TimeSpan InDeterminateDuration
	{
		get => GetValue(InDeterminateDurationProperty);
		set => SetValue(InDeterminateDurationProperty, value);
	}

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

	public Orientation Orientation
	{
		get => GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	public double Percentage
	{
		get => GetValue(PercentageProperty);
		set => SetValue(PercentageProperty, value);
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

	public double Value
	{
		get => GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_progressBar = e.NameScope.Find<Border>("ProgressBar");
		UpdatePercentage();
		UpdateIndeterminateAnimation();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		LayoutUpdated += OnLayoutUpdated;
		base.OnAttachedToVisualTree(e);
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ControlAutomationPeer(this);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		LayoutUpdated -= OnLayoutUpdated;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == ValueProperty)
			|| (change.Property == MinimumProperty)
			|| (change.Property == MaximumProperty))
		{
			UpdatePercentage();
		}

		if ((change.Property == IsIndeterminateProperty)
			|| (change.Property == IsVisibleProperty)
			|| (change.Property == BoundsProperty))
		{
			UpdateIndeterminateAnimation();
		}

		if (change.Property == IsIndeterminateProperty)
		{
			UpdatePseudoClasses(change.GetNewValue<bool>(), null);
		}
		else if (change.Property == OrientationProperty)
		{
			UpdatePseudoClasses(null, change.GetNewValue<Orientation>());
		}
	}

	private void OnLayoutUpdated(object sender, EventArgs e)
	{
		if (_animation == null)
		{
			UpdateIndeterminateAnimation();
		}
	}

	private void StopAnimation()
	{
		if (_animationCancellationTokenSource != null)
		{
			_animationCancellationTokenSource.Cancel();
			_animationCancellationTokenSource.Dispose();
			_animationCancellationTokenSource = null;
		}
		if (_progressBar != null)
		{
			_progressBar.RenderTransform = new TranslateTransform { X = 0 };
		}
		_animation = null;
	}

	private void UpdateIndeterminateAnimation()
	{
		if (_progressBar == null)
		{
			return;
		}

		// Get control and bar widths
		var controlWidth = Bounds.Width;
		var barWidth = _progressBar.Bounds.Width;

		// Skip if widths are not yet available
		if ((controlWidth <= 0) || (barWidth <= 0))
		{
			return;
		}

		StopAnimation();

		if (IsIndeterminate && IsVisible)
		{
			_progressBar.RenderTransform = new TranslateTransform { X = 0 };

			// Create new animation
			_animation = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(Math.Max(1000.0, InDeterminateDuration.TotalMilliseconds)),
				IterationCount = IterationCount.Infinite,
				PlaybackDirection = PlaybackDirection.Normal,
				Easing = new ExponentialEaseInOut()
			};

			// Start keyframe: right edge at control's left edge
			var startFrame = new KeyFrame
			{
				Cue = new Cue(0.0)
			};
			startFrame.Setters.Add(new Setter
			{
				Property = TranslateTransform.XProperty,
				Value = 0.0 - barWidth
			});

			// End keyframe: right edge at control's right edge
			var endFrame = new KeyFrame
			{
				Cue = new Cue(1.0)
			};
			endFrame.Setters.Add(new Setter
			{
				Property = TranslateTransform.XProperty,
				Value = controlWidth
			});

			_animation.Children.Add(startFrame);
			_animation.Children.Add(endFrame);

			// Start the animation
			_animationCancellationTokenSource = new CancellationTokenSource();
			_animation.RunAsync(_progressBar, _animationCancellationTokenSource.Token);
		}
		else
		{
			// Reset transform for determinate state
			_progressBar.RenderTransform = new TranslateTransform { X = 0 };
		}
	}

	private void UpdatePercentage()
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
			percentage = total > 0 ? value / total : 0;
		}

		Percentage = percentage * 100;
	}

	private void UpdatePseudoClasses(bool? isIndeterminate, Orientation? o)
	{
		if (isIndeterminate.HasValue)
		{
			PseudoClasses.Set(":indeterminate", isIndeterminate.Value);
		}

		if (!o.HasValue)
		{
			return;
		}
		PseudoClasses.Set(":vertical", o == Orientation.Vertical);
		PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
	}

	#endregion
}