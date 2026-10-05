#nullable enable

#region References

using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ShapeTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanSpecifyDashStyleInXAML()
	{
		var xaml = @"
<Pen xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Pen.DashStyle>
	    <DashStyle Offset='0' Dashes='1,3'/>
    </Pen.DashStyle>
</Pen>";

		var target = CornerstoneRuntimeXamlLoader.Parse<Pen>(xaml);

		CornerstoneTest.IsNotNull(target);
	}

	#endregion
}