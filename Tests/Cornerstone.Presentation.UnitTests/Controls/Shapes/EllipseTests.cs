#region References

using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Shapes;

[TestClass]
public class EllipseTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangeSetsRenderedGeometryProperties()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Ellipse();

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		var geometry = CornerstoneTest.IsType<EllipseGeometry>(target.RenderedGeometry);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), geometry.Rect);
	}

	[PresentationTestMethod]
	public void MeasureDoesNotSetRenderedGeometryRect()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Ellipse();

		target.Measure(new Size(100, 100));

		var geometry = CornerstoneTest.IsType<EllipseGeometry>(target.RenderedGeometry);
		CornerstoneTest.AreEqual(default, geometry.Rect);
	}

	[PresentationTestMethod]
	public void RearrangingUpdatesRenderedGeometryRect()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Ellipse();

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		var geometry = CornerstoneTest.IsType<EllipseGeometry>(target.RenderedGeometry);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), geometry.Rect);

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(0, 0, 200, 200));

		geometry = CornerstoneTest.IsType<EllipseGeometry>(target.RenderedGeometry);
		CornerstoneTest.AreEqual(new Rect(0, 0, 200, 200), geometry.Rect);
	}

	#endregion
}