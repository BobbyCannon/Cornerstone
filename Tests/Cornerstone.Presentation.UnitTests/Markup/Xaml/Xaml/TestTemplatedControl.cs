#nullable enable

#region References

using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class TestTemplatedControl : TemplatedControl
{
	#region Fields

	public static readonly StyledProperty<object> TestDataProperty =
		PresentationProperty.Register<TestTemplatedControl, object>(nameof(TestData));

	#endregion

	#region Properties

	public object TestData
	{
		get => GetValue(TestDataProperty);
		set => SetValue(TestDataProperty, value);
	}

	#endregion
}