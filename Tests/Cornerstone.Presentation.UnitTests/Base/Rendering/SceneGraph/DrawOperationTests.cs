#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class DrawOperationTests : ScopedTestBase
{
	#region Fields

	private readonly CompositorTestServices _services = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void BoundsReflectPushTransformTranslation()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(50, 50)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		CornerstoneTest.AreEqual(new Rect(50, 50, 10, 10), ctx.GetBounds());
	}

	[PresentationTestMethod]
	public void BoundsUnionAcrossMultipleTopLevelDraws()
	{
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(20, 20, 10, 10)));

		CornerstoneTest.AreEqual(new Rect(0, 0, 30, 30), ctx.GetBounds());
	}

	[PresentationTestMethod]
	public void BrushAndPenOnSameDrawAreBothAddRefedOnce()
	{
		var brush = new TrackingBrush();
		var pen = new TrackingPen();
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(brush, pen, new RoundedRect(new Rect(0, 0, 10, 10)));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, brush.AddRefCount);
		CornerstoneTest.AreEqual(1, pen.AddRefCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, brush.ReleaseCount);
		CornerstoneTest.AreEqual(1, pen.ReleaseCount);
	}

	[PresentationTestMethod]
	public void CustomDrawOperationDisposedWhenRenderDataDisposedBeforeCommit()
	{
		var op = new TrackingCustomOp();
		var ctx = new TestContext(_services);
		ctx.Context.Custom(op);
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(0, op.DisposeCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, op.DisposeCount);
	}

	public override void Dispose()
	{
		_services.Dispose();
		base.Dispose();
	}

	[PresentationTestMethod]
	public void EmptyBoundsRemainEmpty()
	{
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(Brushes.Black, null, default);

		CornerstoneTest.IsNull(ctx.GetBounds());
	}

	[PresentationTestMethod]
	public void EmptyPushBetweenRealDrawsDoesNotAffectSiblings()
	{
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(100, 100)))
		{
		}
		ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(40, 40, 10, 10)));

		var rd = ctx.Context.GetRenderResults()!;
		ctx.ForceRender();
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 50), rd.Server.Bounds?.ToRect());
		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
		CornerstoneTest.IsTrue(rd.HitTest(new Point(45, 45)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(25, 25)));
	}

	[PresentationTestMethod]
	public void EmptyPushPopSequenceProducesNoResults()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
		using (ctx.Context.PushOpacity(1))
		{
		}

		CornerstoneTest.IsNull(ctx.Context.GetRenderResults());
	}

	[PresentationTestMethod]
	public void GeometryNodeAddRefsBrushAndPen()
	{
		var brush = new TrackingBrush();
		var pen = new TrackingPen();
		var ctx = new TestContext(_services);
		ctx.Context.DrawGeometry(brush, pen, new StubGeometryImpl());
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, brush.AddRefCount);
		CornerstoneTest.AreEqual(1, pen.AddRefCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, brush.ReleaseCount);
		CornerstoneTest.AreEqual(1, pen.ReleaseCount);
	}

	[PresentationTestMethod]
	public void GeometryNodeHitTestUsesFillContainsWhenBrushSet()
	{
		var geomMock = new StubGeometryImpl();
		geomMock.SetFillContains(p => p == new Point(5, 5));

		var ctx = new TestContext(_services);
		ctx.Context.DrawGeometry(Brushes.Black, null, geomMock);
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void GeometryNodeHitTestUsesStrokeContainsWhenPenSet()
	{
		var pen = new ImmutablePen(Brushes.Black, 1);
		var geomMock = new StubGeometryImpl();
		geomMock.SetStrokeContains((_, p) => p == new Point(5, 5));

		var ctx = new TestContext(_services);
		ctx.Context.DrawGeometry(null, pen, geomMock);
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void HitTestOnGeometryNodeWithZeroTransformDoesNotThrow()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(new Matrix()))
		{
			var geometry = new StubGeometryImpl();

			ctx.Context.DrawGeometry(Brushes.Black, null, geometry);
		}

		CornerstoneTest.IsFalse(ctx.Context.GetRenderResults()!.HitTest(default(Point)));
	}

	[PresentationTestMethod]
	public void HitTestRectangleNodeWithTransformHits()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		CornerstoneTest.IsTrue(ctx.Context.GetRenderResults()!.HitTest(new Point(25, 25)));
	}

	[PresentationTestMethod]
	public void HitTestThroughNestedTransformsComposesOuterToInner()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
		using (ctx.Context.PushTransform(Matrix.CreateScale(2, 2)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		var rd = ctx.Context.GetRenderResults()!;
		CornerstoneTest.IsTrue(rd.HitTest(new Point(30, 30)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(45, 45)));
	}

	[PresentationTestMethod]
	public void HitTestThroughPushClipRestrictsToClipRegion()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushClip(new Rect(0, 0, 10, 10)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 100, 100)));
		}

		var rd = ctx.Context.GetRenderResults()!;
		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void HitTestThroughPushTransformMapsCoordinates()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(50, 50)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		var rd = ctx.Context.GetRenderResults()!;
		CornerstoneTest.IsTrue(rd.HitTest(new Point(55, 55)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	public void IdentityPushTransformDoesNotWrapChildren()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.Identity))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		var rd = ctx.Context.GetRenderResults()!;
		ctx.ForceRender();
		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), rd.Server.Bounds?.ToRect());
		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ImageNodeReleasesReferenceToBitmapOnDispose(bool disposeBeforeCommit)
	{
		var bitmap = RefCountable.Create(new StubBitmapImpl());

		var ctx = new TestContext(_services);
		ctx.Context.DrawBitmap(bitmap, 1, new Rect(1, 1, 1, 1), new Rect(1, 1, 1, 1));
		var renderData = ctx.Context.GetRenderResults()!;
		CornerstoneTest.AreEqual(2, bitmap.RefCount);
		if (disposeBeforeCommit)
		{
			renderData.Dispose();
			CornerstoneTest.AreEqual(1, bitmap.RefCount);
			ctx.ForceRender();
			CornerstoneTest.AreEqual(1, bitmap.RefCount);
		}
		else
		{
			ctx.ForceRender();
			CornerstoneTest.AreEqual(2, bitmap.RefCount);

			// Refs ownership is transferred to server-side render data 
			renderData.Dispose();
			CornerstoneTest.AreEqual(2, bitmap.RefCount);

			ctx.ForceRender();
			CornerstoneTest.AreEqual(1, bitmap.RefCount);
		}
	}

	[PresentationTestMethod]
	public void ImmutableBrushIsNotRegisteredAsServerResource()
	{
		var brush = new ImmutableTrackingBrush();
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(0, brush.AddRefCount);

		rd.Dispose();
	}

	[PresentationTestMethod]
	public void ImmutablePenIsNotRegisteredAsServerResource()
	{
		var pen = new ImmutableTrackingPen();
		var ctx = new TestContext(_services);
		ctx.Context.DrawLine(pen, new Point(0, 0), new Point(10, 10));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(0, pen.AddRefCount);

		rd.Dispose();
	}

	[PresentationTestMethod]
	public void NoOpInnerPushInsideRealOuterPushIsStripped()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
		{
			using (ctx.Context.PushOpacity(1))
			{
			}
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		var rd = ctx.Context.GetRenderResults()!;
		ctx.ForceRender();
		CornerstoneTest.AreEqual(new Rect(20, 20, 10, 10), rd.Server.Bounds?.ToRect());
		CornerstoneTest.IsTrue(rd.HitTest(new Point(25, 25)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	public void NonImmutableBrushIsAddRefedOnceAndReleasedOnDispose()
	{
		var brush = new TrackingBrush();
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, brush.AddRefCount);
		CornerstoneTest.AreEqual(0, brush.ReleaseCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, brush.AddRefCount);
		CornerstoneTest.AreEqual(1, brush.ReleaseCount);
	}

	[PresentationTestMethod]
	public void NonImmutableBrushUsedMultipleTimesIsAddRefedOnce()
	{
		var brush = new TrackingBrush();
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(20, 20, 10, 10)));
		ctx.Context.DrawEllipse(brush, null, new Rect(40, 40, 10, 10));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, brush.AddRefCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, brush.ReleaseCount);
	}

	[PresentationTestMethod]
	public void NonImmutablePenIsAddRefedOnceAndReleasedOnDispose()
	{
		var pen = new TrackingPen();
		var ctx = new TestContext(_services);
		ctx.Context.DrawLine(pen, new Point(0, 0), new Point(10, 10));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, pen.AddRefCount);
		CornerstoneTest.AreEqual(0, pen.ReleaseCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, pen.ReleaseCount);
	}

	[PresentationTestMethod]
	public void OpacityOnePushDoesNotWrapChildren()
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushOpacity(1))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}

		var rd = ctx.Context.GetRenderResults()!;
		ctx.ForceRender();
		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), rd.Server.Bounds?.ToRect());
		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	public void PushOpacityMaskBrushIsAddRefedOnceAndReleasedOnDispose()
	{
		var brush = new TrackingBrush();
		var ctx = new TestContext(_services);
		using (ctx.Context.PushOpacityMask(brush, new Rect(0, 0, 100, 100)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, brush.AddRefCount);
		CornerstoneTest.AreEqual(0, brush.ReleaseCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, brush.ReleaseCount);
	}

	[PresentationTestMethod]
	public void PushOpacityMaskHitTestRecursesIntoChildren()
	{
		var brush = new TrackingBrush();
		var ctx = new TestContext(_services);
		using (ctx.Context.PushOpacityMask(brush, new Rect(0, 0, 100, 100)))
		{
			ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		}
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.IsTrue(rd.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(rd.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	[DataRow(10, 10, 10, 10, 1, 1, 1, 9, 9, 12, 12)]
	[DataRow(10, 10, 10, 10, 1, 1, 2, 9, 9, 12, 12)]
	[DataRow(10, 10, 10, 10, 1.5, 1.5, 1, 14, 14, 17, 17)]
	public void RectangleBoundsAreSnappedToPixels(
		double x,
		double y,
		double width,
		double height,
		double scaleX,
		double scaleY,
		double penThickness,
		double expectedX,
		double expectedY,
		double expectedWidth,
		double expectedHeight)
	{
		var ctx = new TestContext(_services);
		using (ctx.Context.PushTransform(Matrix.CreateScale(scaleX, scaleY)))
		{
			ctx.Context.DrawRectangle(null, new ImmutablePen(Brushes.Black, penThickness), new Rect(x, y, width, height));
		}

		var bounds = CornerstoneTest.IsNotNull(ctx.GetBounds());
		CornerstoneTest.AreEqual(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), bounds);
	}

	[PresentationTestMethod]
	public void TwoDistinctBrushesAreAddRefedSeparately()
	{
		var brush1 = new TrackingBrush();
		var brush2 = new TrackingBrush();
		var ctx = new TestContext(_services);
		ctx.Context.DrawRectangle(brush1, null, new RoundedRect(new Rect(0, 0, 10, 10)));
		ctx.Context.DrawRectangle(brush2, null, new RoundedRect(new Rect(20, 20, 10, 10)));
		var rd = ctx.Context.GetRenderResults()!;

		CornerstoneTest.AreEqual(1, brush1.AddRefCount);
		CornerstoneTest.AreEqual(1, brush2.AddRefCount);

		rd.Dispose();
		CornerstoneTest.AreEqual(1, brush1.ReleaseCount);
		CornerstoneTest.AreEqual(1, brush2.ReleaseCount);
	}

	#endregion

	#region Classes

	private sealed class ImmutableTrackingBrush : IImmutableBrush, ICompositionRenderResource<IBrush>
	{
		#region Fields

		public int AddRefCount;

		#endregion

		#region Properties

		public double Opacity => 1;
		public ITransform RelativeTransform { get; set; }
		public ITransform Transform => null;
		public RelativePoint TransformOrigin => default;

		#endregion

		#region Methods

		public void AddRefOnCompositor(Compositor c)
		{
			AddRefCount++;
		}

		public IBrush GetForCompositor(Compositor c)
		{
			return this;
		}

		public void ReleaseOnCompositor(Compositor c)
		{
		}

		#endregion
	}

	private sealed class ImmutableTrackingPen : ImmutablePen, ICompositionRenderResource<IPen>
	{
		#region Fields

		public int AddRefCount;

		#endregion

		#region Constructors

		public ImmutableTrackingPen() : base(Brushes.Black, 1)
		{
		}

		#endregion

		#region Methods

		public void AddRefOnCompositor(Compositor c)
		{
			AddRefCount++;
		}

		public IPen GetForCompositor(Compositor c)
		{
			return this;
		}

		public void ReleaseOnCompositor(Compositor c)
		{
		}

		#endregion
	}

	private class TestContext
	{
		#region Fields

		private readonly Compositor _compositor;

		#endregion

		#region Constructors

		public TestContext(CompositorTestServices services)
		{
			_compositor = services.Compositor;
			Context = new RenderDataDrawingContext(_compositor);
		}

		#endregion

		#region Properties

		public RenderDataDrawingContext Context { get; }

		#endregion

		#region Methods

		public void ForceRender()
		{
			_compositor.Commit();
			_compositor.Server.Render(false);
		}

		public Rect? GetBounds()
		{
			var renderData = Context.GetRenderResults();
			if (renderData == null)
			{
				return null;
			}
			ForceRender();
			return renderData.Server.Bounds?.ToRect();
		}

		#endregion
	}

	private sealed class TrackingBrush : IBrush, ICompositionRenderResource<IBrush>
	{
		#region Fields

		public int AddRefCount;
		public int ReleaseCount;

		#endregion

		#region Properties

		public double Opacity => 1;
		public ITransform RelativeTransform { get; set; }
		public ITransform Transform => null;
		public RelativePoint TransformOrigin => default;

		#endregion

		#region Methods

		public void AddRefOnCompositor(Compositor c)
		{
			AddRefCount++;
		}

		public IBrush GetForCompositor(Compositor c)
		{
			return this;
		}

		public void ReleaseOnCompositor(Compositor c)
		{
			ReleaseCount++;
		}

		#endregion
	}

	private sealed class TrackingCustomOp : ICustomDrawOperation
	{
		#region Fields

		public int DisposeCount;

		#endregion

		#region Properties

		public Rect Bounds => new(0, 0, 10, 10);

		#endregion

		#region Methods

		public void Dispose()
		{
			DisposeCount++;
		}

		public bool Equals(ICustomDrawOperation other)
		{
			return ReferenceEquals(this, other);
		}

		public bool HitTest(Point p)
		{
			return false;
		}

		public void Render(ImmediateDrawingContext context)
		{
		}

		#endregion
	}

	private sealed class TrackingPen : IPen, ICompositionRenderResource<IPen>
	{
		#region Fields

		public int AddRefCount;
		public int ReleaseCount;

		#endregion

		#region Properties

		public IBrush Brush => Brushes.Black;
		public IDashStyle DashStyle => null;
		public PenLineCap LineCap => PenLineCap.Flat;
		public PenLineJoin LineJoin => PenLineJoin.Miter;
		public double MiterLimit => 10;
		public double Thickness => 1;

		#endregion

		#region Methods

		public void AddRefOnCompositor(Compositor c)
		{
			AddRefCount++;
		}

		public IPen GetForCompositor(Compositor c)
		{
			return this;
		}

		public void ReleaseOnCompositor(Compositor c)
		{
			ReleaseCount++;
		}

		#endregion
	}

	#endregion
}