#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class LayoutTransformControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BoundsOnRotate0degreesArecorrect()
	{
		TransformRootBoundsTest(
			new Size(100, 25),
			new RotateTransform { Angle = 0 },
			new Rect(0, 0, 100, 25));
	}

	[PresentationTestMethod]
	public void BoundsOnRotate180degreesArecorrect()
	{
		TransformRootBoundsTest(
			new Size(100, 25),
			new RotateTransform { Angle = 180 },
			new Rect(100, 25, 100, 25));
	}

	[PresentationTestMethod]
	public void BoundsOnRotate90degreesArecorrect()
	{
		TransformRootBoundsTest(
			new Size(100, 25),
			new RotateTransform { Angle = 90 },
			new Rect(25, 0, 100, 25));
	}

	[PresentationTestMethod]
	public void BoundsOnRotateminus90degreesArecorrect()
	{
		TransformRootBoundsTest(
			new Size(100, 25),
			new RotateTransform { Angle = -90 },
			new Rect(0, 100, 100, 25));
	}

	[PresentationTestMethod]
	public void BoundsOnScalex05Arecorrect()
	{
		var scale = 0.5;

		TransformRootBoundsTest(
			new Size(100, 50),
			new ScaleTransform { ScaleX = scale, ScaleY = scale },
			new Rect(0, 0, 100, 50));
	}

	[PresentationTestMethod]
	public void BoundsOnScalex2Arecorrect()
	{
		double scale = 2;

		TransformRootBoundsTest(
			new Size(100, 50),
			new ScaleTransform { ScaleX = scale, ScaleY = scale },
			new Rect(0, 0, 100, 50));
	}

	[PresentationTestMethod]
	public void BoundsOnTransformAppliedThenRemovedAreCorrect()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var control = CreateWithChildAndMeasureAndTransform(
			100,
			25,
			new RotateTransform { Angle = 90 });

		CornerstoneTest.AreEqual(new Size(25, 100), control.DesiredSize);

		control.LayoutTransform = null;
		control.Measure(Size.Infinity);
		control.Arrange(new Rect(control.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 25), control.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureOnRotate0degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 25),
			new RotateTransform { Angle = 0 },
			new Size(100, 25));
	}

	[PresentationTestMethod]
	public void MeasureOnRotate180degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 25),
			new RotateTransform { Angle = 180 },
			new Size(100, 25));
	}

	[PresentationTestMethod]
	public void MeasureOnRotate90degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 25),
			new RotateTransform { Angle = 90 },
			new Size(25, 100));
	}

	[PresentationTestMethod]
	public void MeasureOnRotateminus90degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 25),
			new RotateTransform { Angle = -90 },
			new Size(25, 100));
	}

	[PresentationTestMethod]
	public void MeasureOnScalex05IsCorrect()
	{
		var scale = 0.5;

		TransformMeasureSizeTest(
			new Size(100, 50),
			new ScaleTransform { ScaleX = scale, ScaleY = scale },
			new Size(50, 25));
	}

	[PresentationTestMethod]
	public void MeasureOnScalex2IsCorrect()
	{
		double scale = 2;

		TransformMeasureSizeTest(
			new Size(100, 50),
			new ScaleTransform { ScaleX = scale, ScaleY = scale },
			new Size(200, 100));
	}

	[PresentationTestMethod]
	public void MeasureOnSkew0degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 100),
			new SkewTransform { AngleX = 0, AngleY = 0 },
			new Size(100, 100));
	}

	[PresentationTestMethod]
	public void MeasureOnSkewXaxis45degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 100),
			new SkewTransform { AngleX = 45 },
			new Size(200, 100));
	}

	[PresentationTestMethod]
	public void MeasureOnSkewXaxisminus45degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 100),
			new SkewTransform { AngleX = -45 },
			new Size(200, 100));
	}

	[PresentationTestMethod]
	public void MeasureOnSkewYaxis45degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 100),
			new SkewTransform { AngleY = 45 },
			new Size(100, 200));
	}

	[PresentationTestMethod]
	public void MeasureOnSkewYaxisminus45degreesIsCorrect()
	{
		TransformMeasureSizeTest(
			new Size(100, 100),
			new SkewTransform { AngleY = -45 },
			new Size(100, 200));
	}

	[PresentationTestMethod]
	public void ShouldApplyTransformOnAttachToVisualTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var transform = new SkewTransform { AngleX = -45, AngleY = -45 };

			var lt = CreateWithChildAndMeasureAndTransform(
				100,
				100,
				transform);

			transform.AngleX = 45;
			transform.AngleY = 45;

			var window = new Window { Content = lt };
			window.Show();

			CornerstoneTest.IsNotNull(lt.TransformRoot);
			CornerstoneTest.IsNotNull(lt.TransformRoot.RenderTransform);
			var actual = lt.TransformRoot.RenderTransform.Value;
			var expected = Matrix.CreateSkew(Matrix.ToRadians(45), Matrix.ToRadians(45));
			CornerstoneTest.AreEqual(expected.M11, actual.M11, 3);
			CornerstoneTest.AreEqual(expected.M12, actual.M12, 3);
			CornerstoneTest.AreEqual(expected.M21, actual.M21, 3);
			CornerstoneTest.AreEqual(expected.M22, actual.M22, 3);
			CornerstoneTest.AreEqual(expected.M31, actual.M31, 3);
			CornerstoneTest.AreEqual(expected.M32, actual.M32, 3);
		}
	}

	[PresentationTestMethod]
	public void ShouldGenerateRotateTransform90degrees()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(
			100,
			25,
			new RotateTransform { Angle = 90 });

		CornerstoneTest.IsNotNull(lt.TransformRoot);
		CornerstoneTest.IsNotNull(lt.TransformRoot.RenderTransform);

		var m = lt.TransformRoot.RenderTransform.Value;

		var res = Matrix.CreateRotation(Matrix.ToRadians(90));

		CornerstoneTest.AreEqual(m.M11, res.M11, 3);
		CornerstoneTest.AreEqual(m.M12, res.M12, 3);
		CornerstoneTest.AreEqual(m.M21, res.M21, 3);
		CornerstoneTest.AreEqual(m.M22, res.M22, 3);
		CornerstoneTest.AreEqual(m.M31, res.M31, 3);
		CornerstoneTest.AreEqual(m.M32, res.M32, 3);
	}

	[PresentationTestMethod]
	public void ShouldGenerateRotateTransformminus90degrees()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(
			100,
			25,
			new RotateTransform { Angle = -90 });

		CornerstoneTest.IsNotNull(lt.TransformRoot);
		CornerstoneTest.IsNotNull(lt.TransformRoot.RenderTransform);

		var m = lt.TransformRoot.RenderTransform.Value;

		var res = Matrix.CreateRotation(Matrix.ToRadians(-90));

		CornerstoneTest.AreEqual(m.M11, res.M11, 3);
		CornerstoneTest.AreEqual(m.M12, res.M12, 3);
		CornerstoneTest.AreEqual(m.M21, res.M21, 3);
		CornerstoneTest.AreEqual(m.M22, res.M22, 3);
		CornerstoneTest.AreEqual(m.M31, res.M31, 3);
		CornerstoneTest.AreEqual(m.M32, res.M32, 3);
	}

	[PresentationTestMethod]
	public void ShouldGenerateScaleTransformx2()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(
			100,
			50,
			new ScaleTransform { ScaleX = 2, ScaleY = 2 });

		CornerstoneTest.IsNotNull(lt.TransformRoot);
		CornerstoneTest.IsNotNull(lt.TransformRoot.RenderTransform);

		var m = lt.TransformRoot.RenderTransform.Value;
		var res = Matrix.CreateScale(2, 2);

		CornerstoneTest.AreEqual(m.M11, res.M11, 3);
		CornerstoneTest.AreEqual(m.M12, res.M12, 3);
		CornerstoneTest.AreEqual(m.M21, res.M21, 3);
		CornerstoneTest.AreEqual(m.M22, res.M22, 3);
		CornerstoneTest.AreEqual(m.M31, res.M31, 3);
		CornerstoneTest.AreEqual(m.M32, res.M32, 3);
	}

	[PresentationTestMethod]
	public void ShouldGenerateSkewTransform45degrees()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(
			100,
			100,
			new SkewTransform { AngleX = 45, AngleY = 45 });

		CornerstoneTest.IsNotNull(lt.TransformRoot);
		CornerstoneTest.IsNotNull(lt.TransformRoot.RenderTransform);

		var m = lt.TransformRoot.RenderTransform.Value;

		var res = Matrix.CreateSkew(Matrix.ToRadians(45), Matrix.ToRadians(45));

		CornerstoneTest.AreEqual(m.M11, res.M11, 3);
		CornerstoneTest.AreEqual(m.M12, res.M12, 3);
		CornerstoneTest.AreEqual(m.M21, res.M21, 3);
		CornerstoneTest.AreEqual(m.M22, res.M22, 3);
		CornerstoneTest.AreEqual(m.M31, res.M31, 3);
		CornerstoneTest.AreEqual(m.M32, res.M32, 3);
	}

	[PresentationTestMethod]
	public void ShouldGenerateSkewTransformminus45degrees()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(
			100,
			100,
			new SkewTransform { AngleX = -45, AngleY = -45 });

		CornerstoneTest.IsNotNull(lt.TransformRoot);
		CornerstoneTest.IsNotNull(lt.TransformRoot.RenderTransform);

		var m = lt.TransformRoot.RenderTransform.Value;

		var res = Matrix.CreateSkew(Matrix.ToRadians(-45), Matrix.ToRadians(-45));

		CornerstoneTest.AreEqual(m.M11, res.M11, 3);
		CornerstoneTest.AreEqual(m.M12, res.M12, 3);
		CornerstoneTest.AreEqual(m.M21, res.M21, 3);
		CornerstoneTest.AreEqual(m.M22, res.M22, 3);
		CornerstoneTest.AreEqual(m.M31, res.M31, 3);
		CornerstoneTest.AreEqual(m.M32, res.M32, 3);
	}

	private static LayoutTransformControl CreateWithChildAndMeasureAndTransform(
		double width,
		double height,
		Transform transform)
	{
		var lt = new LayoutTransformControl
		{
			LayoutTransform = transform
		};

		lt.Child = new Rectangle { Width = width, Height = height };

		lt.Measure(Size.Infinity);
		lt.Arrange(new Rect(lt.DesiredSize));

		return lt;
	}

	private static void TransformMeasureSizeTest(Size size, Transform transform, Size expectedSize)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(
			size.Width,
			size.Height,
			transform);

		var outSize = lt.DesiredSize;

		CornerstoneTest.AreEqual(outSize.Width, expectedSize.Width);
		CornerstoneTest.AreEqual(outSize.Height, expectedSize.Height);
	}

	private static void TransformRootBoundsTest(Size size, Transform transform, Rect expectedBounds)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var lt = CreateWithChildAndMeasureAndTransform(size.Width, size.Height, transform);

		CornerstoneTest.IsNotNull(lt.TransformRoot);
		var outBounds = lt.TransformRoot.Bounds;

		CornerstoneTest.AreEqual(outBounds.X, expectedBounds.X);
		CornerstoneTest.AreEqual(outBounds.Y, expectedBounds.Y);
		CornerstoneTest.AreEqual(outBounds.Width, expectedBounds.Width);
		CornerstoneTest.AreEqual(outBounds.Height, expectedBounds.Height);
	}

	#endregion
}