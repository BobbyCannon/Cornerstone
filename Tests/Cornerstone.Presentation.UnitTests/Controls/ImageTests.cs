#region References

using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ImageTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangeShouldReturnCorrectSizeForFillStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.Fill;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 25, 100));

		CornerstoneTest.AreEqual(new Size(25, 100), target.Bounds.Size);
	}

	[PresentationTestMethod]
	public void ArrangeShouldReturnCorrectSizeForNoStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.None;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 100, 400));

		CornerstoneTest.AreEqual(new Size(50, 100), target.Bounds.Size);
	}

	[PresentationTestMethod]
	public void ArrangeShouldReturnCorrectSizeForUniformStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.Uniform;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 25, 100));

		CornerstoneTest.AreEqual(new Size(25, 50), target.Bounds.Size);
	}

	[PresentationTestMethod]
	public void ArrangeShouldReturnCorrectSizeForUniformToFillStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.UniformToFill;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 25, 100));

		CornerstoneTest.AreEqual(new Size(25, 100), target.Bounds.Size);
	}

	[PresentationTestMethod]
	public void DrawingImageSourceInvalidatesMeasure()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var drawing = new GeometryDrawing();
		drawing.Geometry = new RectangleGeometry(new Rect(0, 0, 500, 500));
		var image = new DrawingImage(drawing);
		var target = new Image
		{
			Stretch = Stretch.None,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Source = image
		};

		var root = new TestRoot(target);
		root.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(new Size(500, 500), target.DesiredSize);

		drawing.Geometry = new RectangleGeometry(new Rect(0, 0, 600, 600));

		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(new Size(600, 600), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForFillStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.Fill;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));

		CornerstoneTest.AreEqual(new Size(50, 50), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForInfiniteHeight()
	{
		var bitmap = CreateBitmap(50, 100);
		var image = new Image();
		image.Source = bitmap;

		image.Measure(new Size(200, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(200, 400), image.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForInfiniteWidth()
	{
		var bitmap = CreateBitmap(50, 100);
		var image = new Image();
		image.Source = bitmap;

		image.Measure(new Size(double.PositiveInfinity, 400));

		CornerstoneTest.AreEqual(new Size(200, 400), image.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForInfiniteWidthHeight()
	{
		var bitmap = CreateBitmap(50, 100);
		var image = new Image();
		image.Source = bitmap;

		image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(50, 100), image.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForNoStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.None;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));

		CornerstoneTest.AreEqual(new Size(50, 50), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForUniformStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.Uniform;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));

		CornerstoneTest.AreEqual(new Size(25, 50), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeForUniformToFillStretch()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.Stretch = Stretch.UniformToFill;
		target.Source = bitmap;

		target.Measure(new Size(50, 50));

		CornerstoneTest.AreEqual(new Size(50, 50), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnCorrectSizeWithStretchDirectionDownOnly()
	{
		var bitmap = CreateBitmap(50, 100);
		var target = new Image();
		target.StretchDirection = StretchDirection.DownOnly;
		target.Source = bitmap;

		target.Measure(new Size(150, 150));

		CornerstoneTest.AreEqual(new Size(50, 100), target.DesiredSize);
	}

	private static IBitmap CreateBitmap(int width, int height)
	{
		return new StubBitmap(width, height);
	}

	#endregion
}