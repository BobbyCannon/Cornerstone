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
public class RenderDataStreamBoundsTests
{
	#region Methods

	[PresentationTestMethod]
	public void BitmapBoundsAreTheDestinationRect()
	{
		using var bitmap = RefCountable.Create(new StubBitmapImpl());

		using var stream = new RenderDataStream();
		stream.DrawBitmap(bitmap, 1, new Rect(0, 0, 10, 10), new Rect(5, 5, 20, 20));

		CornerstoneTest.AreEqual(new Rect(5, 5, 20, 20), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void BoundsAreTheUnionOfAllDraws()
	{
		using var stream = new RenderDataStream();
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(20, 20, 10, 10)), default);

		CornerstoneTest.AreEqual(new Rect(0, 0, 30, 30), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void BoundsHandlesDeeplyNestedScopes()
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

		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void ClipDoesNotRestrictBounds()
	{
		using var stream = new RenderDataStream();
		stream.PushClip(new RoundedRect(new Rect(0, 0, 5, 5)));
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);
		stream.Pop();

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void CustomOperationBoundsAreUsed()
	{
		var operation = new StubCustomDrawOperation();
		operation.Bounds = new Rect(1, 2, 3, 4);

		using var stream = new RenderDataStream();
		stream.DrawCustom(operation);

		CornerstoneTest.AreEqual(new Rect(1, 2, 3, 4), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void EmptyPushScopeContributesNothing()
	{
		using var stream = new RenderDataStream();
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.PushTransform(Matrix.CreateTranslation(1000, 1000));
		stream.Pop();

		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void EmptyStreamHasNullBounds()
	{
		using var stream = new RenderDataStream();
		CornerstoneTest.IsNull(stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void FilledRectangleBoundsAreTheRectangle()
	{
		using var stream = new RenderDataStream();
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);

		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void LineBoundsCoverTheSegment()
	{
		var pen = new ImmutablePen(Brushes.Black, 2);

		using var stream = new RenderDataStream();
		stream.DrawLine(pen, pen, new Point(0, 0), new Point(100, 0));

		var bounds = CornerstoneTest.IsNotNull(stream.CalculateBounds());
		CornerstoneTest.IsTrue(bounds.Contains(new Point(50, 0)));
	}

	[PresentationTestMethod]
	public void NestedTransformsComposeForBounds()
	{
		using var stream = new RenderDataStream();
		stream.PushTransform(Matrix.CreateTranslation(20, 20));
		stream.PushTransform(Matrix.CreateScale(2, 2));
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.Pop();
		stream.Pop();

		CornerstoneTest.AreEqual(new Rect(20, 20, 20, 20), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void OpacityPushIsTransparentToBounds()
	{
		using var stream = new RenderDataStream();
		stream.PushOpacity(0.5);
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.Pop();

		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void StrokedEllipseBoundsAreInflatedByThickness()
	{
		var pen = new ImmutablePen(Brushes.Black, 4);

		using var stream = new RenderDataStream();
		stream.DrawEllipse(null, pen, pen, new Rect(0, 0, 10, 10));

		CornerstoneTest.AreEqual(new Rect(-4, -4, 18, 18), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void StrokedRectangleBoundsAreInflatedByHalfThickness()
	{
		var pen = new ImmutablePen(Brushes.Black, 4);

		using var stream = new RenderDataStream();
		stream.DrawRectangle(null, pen, pen,
			new RoundedRect(new Rect(10, 10, 20, 20)), default);

		CornerstoneTest.AreEqual(new Rect(8, 8, 24, 24), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void TransformIsAppliedToBounds()
	{
		using var stream = new RenderDataStream();
		stream.PushTransform(Matrix.CreateTranslation(50, 50));
		stream.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		stream.Pop();

		CornerstoneTest.AreEqual(new Rect(50, 50, 10, 10), stream.CalculateBounds());
	}

	#endregion
}