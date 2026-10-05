using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Analog stick visualization with dead zone and cardinal highlights.
/// </summary>
public sealed class JoystickControl : TemplatedControl
{
	public static readonly StyledProperty<bool> ButtonDownProperty =
		PresentationProperty.Register<JoystickControl, bool>(nameof(ButtonDown));

	public static readonly StyledProperty<bool> ButtonLeftProperty =
		PresentationProperty.Register<JoystickControl, bool>(nameof(ButtonLeft));

	public static readonly StyledProperty<bool> ButtonRightProperty =
		PresentationProperty.Register<JoystickControl, bool>(nameof(ButtonRight));

	public static readonly StyledProperty<bool> ButtonUpProperty =
		PresentationProperty.Register<JoystickControl, bool>(nameof(ButtonUp));

	public static readonly StyledProperty<double> DeadZonePercentProperty =
		PresentationProperty.Register<JoystickControl, double>(nameof(DeadZonePercent));

	public static readonly StyledProperty<IBrush> DotProperty =
		PresentationProperty.Register<JoystickControl, IBrush>(nameof(Dot), Brushes.Red);

	public static readonly StyledProperty<double> HorizontalPercentProperty =
		PresentationProperty.Register<JoystickControl, double>(nameof(HorizontalPercent));

	public static readonly StyledProperty<IBrush> StrokeProperty =
		PresentationProperty.Register<JoystickControl, IBrush>(nameof(Stroke), Brushes.Red);

	public static readonly StyledProperty<string> TitleProperty =
		PresentationProperty.Register<JoystickControl, string>(nameof(Title), "Joystick");

	public static readonly StyledProperty<double> VerticalPercentProperty =
		PresentationProperty.Register<JoystickControl, double>(nameof(VerticalPercent));

	public bool ButtonDown
	{
		get => GetValue(ButtonDownProperty);
		set => SetValue(ButtonDownProperty, value);
	}

	public bool ButtonLeft
	{
		get => GetValue(ButtonLeftProperty);
		set => SetValue(ButtonLeftProperty, value);
	}

	public bool ButtonRight
	{
		get => GetValue(ButtonRightProperty);
		set => SetValue(ButtonRightProperty, value);
	}

	public bool ButtonUp
	{
		get => GetValue(ButtonUpProperty);
		set => SetValue(ButtonUpProperty, value);
	}

	public double DeadZonePercent
	{
		get => GetValue(DeadZonePercentProperty);
		set => SetValue(DeadZonePercentProperty, value);
	}

	public IBrush Dot
	{
		get => GetValue(DotProperty);
		set => SetValue(DotProperty, value);
	}

	public double HorizontalPercent
	{
		get => GetValue(HorizontalPercentProperty);
		set => SetValue(HorizontalPercentProperty, value);
	}

	public IBrush Stroke
	{
		get => GetValue(StrokeProperty);
		set => SetValue(StrokeProperty, value);
	}

	public string Title
	{
		get => GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public double VerticalPercent
	{
		get => GetValue(VerticalPercentProperty);
		set => SetValue(VerticalPercentProperty, value);
	}
}
