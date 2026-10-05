#nullable enable

#region References

using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;

public class ClassWithNullableProperties
{
	#region Properties

	public Orientation? Orientation { get; set; }
	public Thickness? Thickness { get; set; }

	#endregion
}

[TestClass]
public class NullableConverterTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void NullableTypesShouldStillBeConvertedProperly()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = @"<ClassWithNullableProperties 
xmlns='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters'
    Thickness = '5' Orientation='Vertical'
></ClassWithNullableProperties>";
			var data = (ClassWithNullableProperties) CornerstoneRuntimeXamlLoader.Load(xaml, typeof(ClassWithNullableProperties).Assembly);
			CornerstoneTest.AreEqual(new Thickness(5), data.Thickness);
			CornerstoneTest.AreEqual(Orientation.Vertical, data.Orientation);
		}
	}

	#endregion
}