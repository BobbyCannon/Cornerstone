#nullable enable

#region References

using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class TestControl : Control
{
	#region Fields

	public static readonly StyledProperty<double> DoubleProperty =
		AttachedPropertyOwner.DoubleProperty.AddOwner<TestControl>();

	#endregion

	#region Properties

	public double Double
	{
		get => GetValue(DoubleProperty);
		set => SetValue(DoubleProperty, value);
	}

	#endregion
}