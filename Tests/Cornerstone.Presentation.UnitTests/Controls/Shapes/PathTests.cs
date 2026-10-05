#region References

using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Shapes;

[TestClass]
public class PathTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangeReservesAllOfArrangeRect()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		RectangleGeometry geometry;
		var target = new Path
		{
			Data = geometry = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
			Stretch = Stretch.Uniform
		};

		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(0, 0, 400, 400));

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 200), geometry.Rect);
		CornerstoneTest.IsNotNull(target.RenderedGeometry);
		CornerstoneTest.IsNotNull(target.RenderedGeometry.Transform);
		CornerstoneTest.AreEqual(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);
		CornerstoneTest.AreEqual(new Rect(0, 0, 400, 400), target.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(Stretch.None, 1, 1)]
	[DataRow(Stretch.Fill, 5, 2.5)]
	[DataRow(Stretch.Uniform, 2.5, 2.5)]
	[DataRow(Stretch.UniformToFill, 5, 5)]
	public void ArrangeUpdatesRenderedGeometryTransform(Stretch stretch, double expectedScaleX, double expectedScaleY)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Path
		{
			Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
			Stretch = stretch
		};

		target.Measure(new Size(500, 500));
		target.Arrange(new Rect(0, 0, 500, 500));

		CornerstoneTest.IsNotNull(target.RenderedGeometry);

		if ((expectedScaleX == 1) && (expectedScaleY == 1))
		{
			CornerstoneTest.IsNull(target.RenderedGeometry.Transform);
		}
		else
		{
			CornerstoneTest.IsNotNull(target.RenderedGeometry.Transform);
			CornerstoneTest.AreEqual(Matrix.CreateScale(expectedScaleX, expectedScaleY), target.RenderedGeometry.Transform.Value);
		}
	}

	[PresentationTestMethod]
	public void ArrangeWithoutMeasureUpdatesRenderedGeometryTransform()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Path
		{
			Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) },
			Stretch = Stretch.Fill
		};

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(0, 0, 200, 200));
		CornerstoneTest.IsNotNull(target.RenderedGeometry);
		CornerstoneTest.IsNotNull(target.RenderedGeometry.Transform);
		CornerstoneTest.AreEqual(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);

		target.Arrange(new Rect(0, 0, 300, 300));
		CornerstoneTest.AreEqual(Matrix.CreateScale(3, 3), target.RenderedGeometry.Transform.Value);
	}

	[PresentationTestMethod]
	[DataRow(Stretch.None, 100, 200)]
	[DataRow(Stretch.Fill, 500, 500)]
	[DataRow(Stretch.Uniform, 250, 500)]
	[DataRow(Stretch.UniformToFill, 500, 500)]
	public void CalculatesCorrectDesiredSizeForFiniteBounds(Stretch stretch, double expectedWidth, double expectedHeight)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Path
		{
			Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
			Stretch = stretch
		};

		target.Measure(new Size(500, 500));

		CornerstoneTest.AreEqual(new Size(expectedWidth, expectedHeight), target.DesiredSize);
	}

	[PresentationTestMethod]
	[DataRow(Stretch.None)]
	[DataRow(Stretch.Fill)]
	[DataRow(Stretch.Uniform)]
	[DataRow(Stretch.UniformToFill)]
	public void CalculatesCorrectDesiredSizeForInfiniteBounds(Stretch stretch)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Path
		{
			Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
			Stretch = stretch
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(100, 200), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureDoesNotUpdateRenderedGeometryTransform()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Path
		{
			Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) },
			Stretch = Stretch.Fill
		};

		target.Measure(new Size(500, 500));

		CornerstoneTest.IsNotNull(target.RenderedGeometry);
		CornerstoneTest.IsNull(target.RenderedGeometry.Transform);
	}

	[PresentationTestMethod]
	public void MeasureWithoutArrangeDoesNotClearRenderedGeometryTransform()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Path
		{
			Data = new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) },
			Stretch = Stretch.Fill
		};

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(0, 0, 200, 200));

		CornerstoneTest.IsNotNull(target.RenderedGeometry);
		CornerstoneTest.IsNotNull(target.RenderedGeometry.Transform);
		CornerstoneTest.AreEqual(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);

		target.Measure(new Size(300, 300));

		CornerstoneTest.AreEqual(Matrix.CreateScale(2, 2), target.RenderedGeometry.Transform.Value);
	}

	[PresentationTestMethod]
	public void PathWithNullDataDoesNotThrowOnMeasure()
	{
		var target = new Path();

		target.Measure(Size.Infinity);
	}

	[PresentationTestMethod]
	public void SubscribesToGeometryChanges()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
		var target = new Path { Data = geometry };

		var root = new TestRoot(target);

		target.Measure(Size.Infinity);
		CornerstoneTest.IsTrue(target.IsMeasureValid);

		geometry.Rect = new Rect(0, 0, 20, 20);

		CornerstoneTest.IsFalse(target.IsMeasureValid);

		root.Child = null;
	}

	#endregion
}