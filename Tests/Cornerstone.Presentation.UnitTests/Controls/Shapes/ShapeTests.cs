#region References

using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Shapes;

[TestClass]
public class ShapeTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void NoStrokeProducesNoPen()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var pen = RenderAndGetPen(new TestShape
		{
			Stroke = null,
			StrokeThickness = 4
		});

		CornerstoneTest.IsNull(pen);
	}

	[PresentationTestMethod]
	public void StrokeDashArrayAndOffsetAreAppliedToPen()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var pen = RenderAndGetPen(new TestShape
		{
			Stroke = Brushes.Black,
			StrokeThickness = 4,
			StrokeDashArray = new OldPresentationList<double>(1, 2, 3),
			StrokeDashOffset = 1.5
		});

		CornerstoneTest.IsNotNull(pen);
		CornerstoneTest.IsNotNull(pen.DashStyle);
		CornerstoneTest.AreEqual(3, pen.DashStyle!.Dashes!.Count);
		CornerstoneTest.AreEqual(1, pen.DashStyle.Dashes[0]);
		CornerstoneTest.AreEqual(2, pen.DashStyle.Dashes[1]);
		CornerstoneTest.AreEqual(3, pen.DashStyle.Dashes[2]);
		CornerstoneTest.AreEqual(1.5, pen.DashStyle.Offset);
	}

	[PresentationTestMethod]
	public void StrokeLineCapAndJoinAreAppliedToPen()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var pen = RenderAndGetPen(new TestShape
		{
			Stroke = Brushes.Black,
			StrokeThickness = 4,
			StrokeLineCap = PenLineCap.Round,
			StrokeJoin = PenLineJoin.Bevel
		});

		CornerstoneTest.IsNotNull(pen);
		CornerstoneTest.AreEqual(PenLineCap.Round, pen.LineCap);
		CornerstoneTest.AreEqual(PenLineJoin.Bevel, pen.LineJoin);
	}

	[PresentationTestMethod]
	public void StrokeMiterLimitDefaultIsAppliedToPen()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var pen = RenderAndGetPen(new TestShape
		{
			StrokeThickness = 4,
			Stroke = Brushes.Black
		});

		CornerstoneTest.IsNotNull(pen);
		CornerstoneTest.AreEqual(10, pen.MiterLimit);
	}

	[PresentationTestMethod]
	public void StrokeMiterLimitUpdateRefreshesPen()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var shape = new TestShape
		{
			StrokeThickness = 4,
			Stroke = Brushes.Black
		};

		RenderAndGetPen(shape);
		shape.StrokeMiterLimit = 2;
		var pen = RenderAndGetPen(shape);

		CornerstoneTest.IsNotNull(pen);
		CornerstoneTest.AreEqual(2, pen.MiterLimit);
	}

	[PresentationTestMethod]
	public void StrokeThicknessIsAppliedToPen()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var pen = RenderAndGetPen(new TestShape
		{
			StrokeThickness = 6,
			Stroke = Brushes.Black
		});

		CornerstoneTest.IsNotNull(pen);
		CornerstoneTest.AreEqual(6, pen.Thickness);
	}

	private static IPen RenderAndGetPen(Shape shape)
	{
		using var context = new RecordingDrawingContext();
		shape.Render(context);
		return context.LastPen;
	}

	#endregion

	#region Classes

	private class RecordingDrawingContext : DrawingContext
	{
		#region Properties

		public IPen LastPen { get; private set; }

		#endregion

		#region Methods

		public override void Custom(ICustomDrawOperation custom)
		{
		}

		public override void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
		{
		}

		public void Reset()
		{
			LastPen = null;
		}

		protected override void DisposeCore()
		{
		}

		protected override void DrawEllipseCore(IBrush brush, IPen pen, Rect rect)
		{
		}

		protected override void DrawGeometryCore(IBrush brush, IPen pen, IGeometryImpl geometry)
		{
			LastPen = pen;
		}

		protected override void DrawLineCore(IPen pen, Point p1, Point p2)
		{
		}

		protected override void DrawRectangleCore(IBrush brush, IPen pen, RoundedRect rrect, BoxShadows boxShadows = default)
		{
		}

		protected override void PopClipCore()
		{
		}

		protected override void PopEffectCore()
		{
		}

		protected override void PopGeometryClipCore()
		{
		}

		protected override void PopOpacityCore()
		{
		}

		protected override void PopOpacityMaskCore()
		{
		}

		protected override void PopRenderOptionsCore()
		{
		}

		protected override void PopTextOptionsCore()
		{
		}

		protected override void PopTransformCore()
		{
		}

		protected override void PushClipCore(RoundedRect rect)
		{
		}

		protected override void PushClipCore(Rect rect)
		{
		}

		protected override void PushEffectCore(IEffect effect, Rect bounds)
		{
		}

		protected override void PushGeometryClipCore(Geometry clip)
		{
		}

		protected override void PushOpacityCore(double opacity)
		{
		}

		protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
		{
		}

		protected override void PushRenderOptionsCore(RenderOptions renderOptions)
		{
		}

		protected override void PushTextOptionsCore(TextOptions textOptions)
		{
		}

		protected override void PushTransformCore(Matrix matrix)
		{
		}

		internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect)
		{
		}

		#endregion
	}

	private class TestShape : Shape
	{
		#region Methods

		protected override Geometry CreateDefiningGeometry()
		{
			return new RectangleGeometry(new Rect(0, 0, 20, 20));
		}

		#endregion
	}

	#endregion
}