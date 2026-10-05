#region References

using System.Collections.ObjectModel;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Shapes;

[TestClass]
public class PolygonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void FillRuleOnPolygonIsAppliedToDefiningGeometry()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Polygon
		{
			Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
			FillRule = FillRule.NonZero
		};

		target.Measure(Size.Infinity);

		var geometry = CornerstoneTest.IsType<PolylineGeometry>(target.DefiningGeometry);
		CornerstoneTest.AreEqual(FillRule.NonZero, geometry.FillRule);
	}

	[PresentationTestMethod]
	public void PolygonEqualsClosedPolylineBounds()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var polyline = new Polyline
		{
			Points = new Points
			{
				new Point(0, 0),
				new Point(10, 0),
				new Point(10, 10),
				new Point(0, 10),
				new Point(0, 0)
			},
			FillRule = FillRule.NonZero
		};

		var polygon = new Polygon
		{
			Points = new Points { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10) },
			FillRule = FillRule.NonZero
		};

		polyline.Measure(Size.Infinity);
		polygon.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(polygon.DefiningGeometry!.Bounds, polyline.DefiningGeometry!.Bounds);
	}

	[PresentationTestMethod]
	public void PolygonWillUpdateGeometryOnShapesCollectionContentChange()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var points = new ObservableCollection<Point>();

		var target = new Polygon { Points = points };
		target.Measure(new Size());
		CornerstoneTest.IsTrue(target.IsMeasureValid);

		var root = new TestRoot(target);

		points.Add(new Point());

		CornerstoneTest.IsFalse(target.IsMeasureValid);

		root.Child = null;
	}

	#endregion
}