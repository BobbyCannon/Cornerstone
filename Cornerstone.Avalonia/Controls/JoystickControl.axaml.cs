#region References

using Avalonia;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed class JoystickControl : CornerstoneTemplatedControl
{
	#region Fields

	public static readonly StyledProperty<IBrush> DotProperty;
	public static readonly StyledProperty<float> HorizontalPercentProperty;
	public static readonly StyledProperty<IBrush> StrokeProperty;
	public static readonly StyledProperty<string> TitleProperty;
	public static readonly StyledProperty<float> VerticalPercentProperty;

	#endregion

	#region Constructors

	static JoystickControl()
	{
		DotProperty = AvaloniaProperty.Register<JoystickControl, IBrush>(nameof(Dot), Brushes.Red);
		HorizontalPercentProperty = AvaloniaProperty.Register<JoystickControl, float>(nameof(HorizontalPercent));
		VerticalPercentProperty = AvaloniaProperty.Register<JoystickControl, float>(nameof(VerticalPercent));
		StrokeProperty = AvaloniaProperty.Register<JoystickControl, IBrush>(nameof(Stroke), Brushes.Red);
		TitleProperty = AvaloniaProperty.Register<JoystickControl, string>(nameof(Title), "Joystick");
	}

	#endregion

	#region Properties

	public IBrush Dot
	{
		get => GetValue(DotProperty);
		set => SetValue(DotProperty, value);
	}

	public float HorizontalPercent
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

	public float VerticalPercent
	{
		get => GetValue(VerticalPercentProperty);
		set => SetValue(VerticalPercentProperty, value);
	}

	#endregion
}