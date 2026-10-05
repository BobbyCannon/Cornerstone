#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class SliderTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DefaultOrientationShouldBeHorizontal()
	{
		var slider = new Slider();
		CornerstoneTest.AreEqual(Orientation.Horizontal, slider.Orientation);
	}

	[PresentationTestMethod]
	public void ShouldSetHorizontalClass()
	{
		var slider = new Slider
		{
			Orientation = Orientation.Horizontal
		};

		CornerstoneTest.Contains(":horizontal".Equals, slider.Classes);
	}

	[PresentationTestMethod]
	public void ShouldSetVerticalClass()
	{
		var slider = new Slider
		{
			Orientation = Orientation.Vertical
		};

		CornerstoneTest.Contains(":vertical".Equals, slider.Classes);
	}

	#endregion
}