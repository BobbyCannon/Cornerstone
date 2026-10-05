#nullable enable

#region References

using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class NonControl : PresentationObject
{
	#region Fields

	//getter only presentation property
	public static readonly StyledProperty<string> BarProperty =
		PresentationProperty.Register<NonControl, string>(nameof(Bar));

	public static readonly StyledProperty<Control> ControlProperty =
		PresentationProperty.Register<NonControl, Control>(nameof(Control));

	//No getter or setter presentation property
	public static readonly StyledProperty<int> FooProperty =
		PresentationProperty.Register<NonControl, int>("Foo");

	public static readonly StyledProperty<string> StringProperty =
		PresentationProperty.Register<NonControl, string>(nameof(String));

	#endregion

	#region Properties

	public string Bar => GetValue(BarProperty);

	public Control Control
	{
		get => GetValue(ControlProperty);
		set => SetValue(ControlProperty, value);
	}

	public string String
	{
		get => GetValue(StringProperty);
		set => SetValue(StringProperty, value);
	}

	#endregion
}