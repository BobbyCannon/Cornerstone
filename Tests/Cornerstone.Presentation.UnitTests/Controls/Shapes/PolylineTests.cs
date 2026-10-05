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
public class PolylineTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void FillRuleDiffersBetweenEvenOddAndNonZero()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var evenOdd = new Polyline
		{
			Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
			Fill = Brushes.Red,
			FillRule = FillRule.EvenOdd
		};

		var nonZero = new Polyline
		{
			Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
			Fill = Brushes.Red,
			FillRule = FillRule.NonZero
		};

		evenOdd.Measure(Size.Infinity);
		nonZero.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(FillRule.EvenOdd, CornerstoneTest.IsType<PolylineGeometry>(evenOdd.DefiningGeometry).FillRule);
		CornerstoneTest.AreEqual(FillRule.NonZero, CornerstoneTest.IsType<PolylineGeometry>(nonZero.DefiningGeometry).FillRule);
	}

	[PresentationTestMethod]
	public void FillRuleOnPolylineIsAppliedToDefiningGeometry()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Polyline
		{
			Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
			Fill = Brushes.Red,
			FillRule = FillRule.NonZero
		};

		target.Measure(Size.Infinity);

		var geometry = CornerstoneTest.IsType<PolylineGeometry>(target.DefiningGeometry);
		CornerstoneTest.AreEqual(FillRule.NonZero, geometry.FillRule);
		CornerstoneTest.IsTrue(geometry.IsFilled);
	}

	[PresentationTestMethod]
	public void PolylineWillUpdateGeometryOnShapesCollectionContentChange()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var points = new ObservableCollection<Point>();

		var target = new Polyline { Points = points };
		target.Measure(new Size());
		CornerstoneTest.IsTrue(target.IsMeasureValid);

		var root = new TestRoot(target);

		points.Add(new Point());

		CornerstoneTest.IsFalse(target.IsMeasureValid);

		root.Child = null;
	}

	[PresentationTestMethod]
	public void WhenFillIsNullPolylineGeometryIsNotFilled()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Polyline
		{
			Points = new Points { new Point(0, 0), new Point(10, 10), new Point(20, 0) },
			FillRule = FillRule.NonZero,
			Fill = null
		};

		target.Measure(Size.Infinity);
		var geometry = CornerstoneTest.IsType<PolylineGeometry>(target.DefiningGeometry);
		CornerstoneTest.IsFalse(geometry.IsFilled);
	}

	#endregion
}