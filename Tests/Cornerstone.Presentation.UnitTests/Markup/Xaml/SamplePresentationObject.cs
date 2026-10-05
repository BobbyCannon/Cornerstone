#nullable enable
namespace Cornerstone.Presentation.UnitTests.Markup.Xaml;

internal class SamplePresentationObject : PresentationObject
{
	#region Fields

	public static readonly StyledProperty<int> IntProperty =
		PresentationProperty.Register<PresentationObject, int>("IntProp");

	public static readonly StyledProperty<string> StringProperty =
		PresentationProperty.Register<PresentationObject, string>("StrProp", string.Empty);

	#endregion

	#region Properties

	public int Int
	{
		get => GetValue(IntProperty);
		set => SetValue(IntProperty, value);
	}

	public string String
	{
		get => GetValue(StringProperty);
		set => SetValue(StringProperty, value);
	}

	#endregion
}