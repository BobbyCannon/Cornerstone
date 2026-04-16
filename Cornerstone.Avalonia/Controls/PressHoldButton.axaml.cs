#region References

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornerstone.Avalonia.Resources;
using Key = Avalonia.Input.Key;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

#endregion

namespace Cornerstone.Avalonia.Controls;

[TemplatePart("PART_Indicator", typeof(Border), IsRequired = true)]
[PseudoClasses(":vertical", ":horizontal")]
public sealed class PressHoldButton : Button
{
	#region Fields

	public static readonly StyledProperty<IBrush> HoldBackgroundProperty;
	public static readonly StyledProperty<object> HoldCommandParameterProperty;
	public static readonly StyledProperty<ICommand> HoldCommandProperty;
	public static readonly StyledProperty<TimeSpan> HoldDurationProperty;
	public static readonly StyledProperty<Orientation> OrientationProperty;
	public static readonly StyledProperty<double> ProgressProperty;

	private Border _indicator;
	private bool _isPressed;
	private DateTime _pressStartTime;
	private DispatcherTimer _timer;

	#endregion

	#region Constructors

	public PressHoldButton()
	{
		UpdatePseudoClasses(Orientation);
	}

	static PressHoldButton()
	{
		HoldBackgroundProperty = AvaloniaProperty.Register<PressHoldButton, IBrush>(nameof(HoldBackground), ResourceService.GetColorAsBrush("Green06"));
		HoldCommandParameterProperty = AvaloniaProperty.Register<PressHoldButton, object>(nameof(HoldCommandParameter));
		HoldCommandProperty = AvaloniaProperty.Register<PressHoldButton, ICommand>(nameof(HoldCommand));
		HoldDurationProperty = AvaloniaProperty.Register<PressHoldButton, TimeSpan>(nameof(HoldDuration), TimeSpan.FromSeconds(1));
		OrientationProperty = AvaloniaProperty.Register<PressHoldButton, Orientation>(nameof(Orientation));
		ProgressProperty = AvaloniaProperty.Register<PressHoldButton, double>(nameof(Progress));
	}

	#endregion

	#region Properties

	public IBrush HoldBackground
	{
		get => GetValue(HoldBackgroundProperty);
		set => SetValue(HoldBackgroundProperty, value);
	}

	public ICommand HoldCommand
	{
		get => GetValue(HoldCommandProperty);
		set => SetValue(HoldCommandProperty, value);
	}

	public object HoldCommandParameter
	{
		get => GetValue(HoldCommandParameterProperty);
		set => SetValue(HoldCommandParameterProperty, value);
	}

	public TimeSpan HoldDuration
	{
		get => GetValue(HoldDurationProperty);
		set => SetValue(HoldDurationProperty, value);
	}

	public Orientation Orientation
	{
		get => GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	public double Progress
	{
		get => GetValue(ProgressProperty);
		set => SetValue(ProgressProperty, value);
	}

	#endregion

	#region Methods

	protected override Size ArrangeOverride(Size finalSize)
	{
		var result = base.ArrangeOverride(finalSize);
		UpdateIndicator();
		return result;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		// dispose any previous track size listener
		_indicator = e.NameScope.Get<Border>("PART_Indicator");

		UpdateIndicator();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16), IsEnabled = false };
		_timer.Tick += OnTimerTick;
		base.OnAttachedToVisualTree(e);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ButtonAutomationPeer(this);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		_timer?.Stop();
		_timer = null;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Space)
		{
			StartHold();
		}
		base.OnKeyDown(e);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Space)
		{
			StopHold();
		}
		base.OnKeyUp(e);
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		StopHold();
		base.OnPointerCaptureLost(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			StartHold();
		}
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		StopHold();
		base.OnPointerReleased(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == OrientationProperty)
		{
			UpdatePseudoClasses(change.GetNewValue<Orientation>());
		}
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		// todo: pass through throttle?
		UpdateIndicator();
	}

	private async Task RunFlashAnimationAsync()
	{
		var originalBackground = _indicator.Background;
		var originalOpacity = _indicator.Opacity;
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.08),
			IterationCount = new IterationCount(2),
			PlaybackDirection = PlaybackDirection.Alternate,
			FillMode = FillMode.Forward
		};

		animation.Children.Add(new KeyFrame
		{
			Setters = { new Setter(OpacityProperty, 1.0) },
			Cue = new Cue(0.0)
		});
		animation.Children.Add(new KeyFrame
		{
			Setters = { new Setter(Border.BackgroundProperty, Foreground ?? new SolidColorBrush(Colors.White)) },
			Cue = new Cue(1.0)
		});

		// Run the animation
		await animation.RunAsync(_indicator);

		// Restore original state after animation
		_indicator.Background = originalBackground;
		_indicator.Opacity = originalOpacity;

		// v12: Hack to fix render issue...
		InvalidateMeasure();
	}

	private void StartHold()
	{
		if (_isPressed)
		{
			return;
		}

		_isPressed = true;
		_pressStartTime = DateTime.UtcNow;
		_timer?.Start();

		UpdateIndicator();
	}

	private void StopHold()
	{
		_isPressed = false;
		UpdateIndicator();
	}

	private async void UpdateIndicator()
	{
		var timer = _timer;
		if (timer is not { IsEnabled: true })
		{
			return;
		}

		var elapsed = DateTime.UtcNow - _pressStartTime;
		var progress = elapsed.TotalMilliseconds / HoldDuration.TotalMilliseconds;

		if (!_isPressed || (progress >= 1.0))
		{
			timer.Stop();

			if (progress >= 1.0)
			{
				Progress = 1.0;

				await RunFlashAnimationAsync();

				if (HoldCommand?.CanExecute(HoldCommandParameter) == true)
				{
					HoldCommand.Execute(HoldCommandParameter);
				}
			}

			_isPressed = false;

			progress = 0;
		}

		Progress = progress;
	}

	private void UpdatePseudoClasses(Orientation? o)
	{
		if (!o.HasValue)
		{
			return;
		}
		PseudoClasses.Set(":vertical", o == Orientation.Vertical);
		PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
	}

	#endregion
}