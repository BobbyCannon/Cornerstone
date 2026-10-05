#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataStreamHitTestTests
{
	#region Methods

	[PresentationTestMethod]
	public void BitmapIsHitWithinItsDestinationRect()
	{
		using var bitmap = RefCountable.Create(new StubBitmapImpl());

		using var stream = new RenderDataStream();
		stream.DrawBitmap(bitmap, 1, new Rect(0, 0, 10, 10), new Rect(20, 20, 30, 30));

		CornerstoneTest.IsTrue(stream.HitTest(new Point(25, 25)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	public void ClipRestrictsTheHitRegion()
	{
		using var stream = new RenderDataStream();
		stream.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);
		stream.Pop();

		CornerstoneTest.IsTrue(stream.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void CustomOperationHitTestIsDelegated()
	{
		var operation = new StubCustomDrawOperation();
		operation.HitTestHandler = p => p == new Point(5, 5);

		using var stream = new RenderDataStream();
		stream.DrawCustom(operation);

		CornerstoneTest.IsTrue(stream.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(99, 99)));
	}

	[PresentationTestMethod]
	public void FilledEllipseIsHitAtCenterAndMissedAtCorner()
	{
		using var stream = new RenderDataStream();
		stream.DrawEllipse(Brushes.Black, null, null, new Rect(0, 0, 100, 100));

		CornerstoneTest.IsTrue(stream.HitTest(new Point(50, 50)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(2, 2)));
	}

	[PresentationTestMethod]
	public void FilledRectangleIsHitInsideAndMissedOutside()
	{
		using var stream = new RenderDataStream();
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);

		CornerstoneTest.IsTrue(stream.HitTest(new Point(50, 50)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(150, 150)));
	}

	[PresentationTestMethod]
	public void GeometryClipRestrictsTheHitRegion()
	{
		var geometry = new StubGeometryImpl();
		geometry.SetFillContains(p => p == new Point(5, 5));

		using var stream = new RenderDataStream();
		stream.PushGeometryClip(geometry);
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);
		stream.Pop();

		CornerstoneTest.IsTrue(stream.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void GeometryIsHitViaFillContains()
	{
		var geometry = new StubGeometryImpl();
		geometry.SetFillContains(p => p == new Point(5, 5));

		using var stream = new RenderDataStream();
		stream.DrawGeometry(Brushes.Black, null, null, geometry);

		CornerstoneTest.IsTrue(stream.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void HitTestHandlesDeeplyNestedScopes()
	{
		using var stream = new RenderDataStream();
		for (var i = 0; i < 100; i++)
		{
			stream.PushOpacity(0.5);
		}
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		for (var i = 0; i < 100; i++)
		{
			stream.Pop();
		}

		CornerstoneTest.IsTrue(stream.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void LineIsHitAlongItsLength()
	{
		var pen = new ImmutablePen(Brushes.Black, 4);

		using var stream = new RenderDataStream();
		stream.DrawLine(pen, pen, new Point(0, 0), new Point(100, 0));

		CornerstoneTest.IsTrue(stream.HitTest(new Point(50, 1)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void NestedTransformsCompose()
	{
		using var stream = new RenderDataStream();
		stream.PushTransform(Matrix.CreateTranslation(20, 20));
		stream.PushTransform(Matrix.CreateScale(2, 2));
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.Pop();
		stream.Pop();

		CornerstoneTest.IsTrue(stream.HitTest(new Point(30, 30)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	public void OpacityPushIsTransparentToHitTesting()
	{
		using var stream = new RenderDataStream();
		stream.PushOpacity(0.5);
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);
		stream.Pop();

		CornerstoneTest.IsTrue(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void ScopeStateIsRestoredAfterPop()
	{
		using var stream = new RenderDataStream();
		stream.PushTransform(Matrix.CreateTranslation(1000, 1000));
		stream.Pop();
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);

		CornerstoneTest.IsTrue(stream.HitTest(new Point(5, 5)));
	}

	[PresentationTestMethod]
	public void SingularTransformExcludesItsScope()
	{
		using var stream = new RenderDataStream();
		stream.PushTransform(new Matrix());
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);
		stream.Pop();

		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void StrokedRectangleIsHitOnBorderAndMissedInHollowCenter()
	{
		var pen = new ImmutablePen(Brushes.Black, 4);

		using var stream = new RenderDataStream();
		stream.DrawRectangle(null, pen, pen,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);

		CornerstoneTest.IsTrue(stream.HitTest(new Point(0, 50)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void TransformMapsHitTestCoordinates()
	{
		using var stream = new RenderDataStream();
		stream.PushTransform(Matrix.CreateTranslation(50, 50));
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.Pop();

		CornerstoneTest.IsTrue(stream.HitTest(new Point(55, 55)));
		CornerstoneTest.IsFalse(stream.HitTest(new Point(5, 5)));
	}

	#endregion
}