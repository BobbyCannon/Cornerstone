#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.Rendering.Composition.Transport;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataStreamSerializationTests
{
	#region Methods

	[PresentationTestMethod]
	public void RoundTripOfEmptyStreamProducesEmptyStream()
	{
		using var source = new RenderDataStream();
		using var result = RoundTrip(source);

		CornerstoneTest.IsNull(result.CalculateBounds());
	}

	[PresentationTestMethod]
	public void RoundTripPreservesBounds()
	{
		using var source = new RenderDataStream();
		source.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		source.PushTransform(Matrix.CreateTranslation(40, 40));
		source.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);
		source.Pop();

		using var result = RoundTrip(source);

		CornerstoneTest.AreEqual(source.CalculateBounds(), result.CalculateBounds());
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 50), result.CalculateBounds());
	}

	[PresentationTestMethod]
	public void RoundTripPreservesHitTesting()
	{
		using var source = new RenderDataStream();
		source.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
		source.DrawRectangle(Brushes.Black, null, null,
			new RoundedRect(new Rect(0, 0, 100, 100)), default);
		source.Pop();

		using var result = RoundTrip(source);

		CornerstoneTest.IsTrue(result.HitTest(new Point(5, 5)));
		CornerstoneTest.IsFalse(result.HitTest(new Point(50, 50)));
	}

	[PresentationTestMethod]
	public void RoundTripPreservesResourceReferences()
	{
		var brush = Brushes.Black;
		using var source = new RenderDataStream();
		source.DrawRectangle(brush, null, null,
			new RoundedRect(new Rect(0, 0, 10, 10)), default);

		using var result = RoundTrip(source);

		var context = new StubDrawingContextImpl();
		result.Replay(context);
		context.Calls.VerifyCalled("DrawRectangle", 1);
	}

	[PresentationTestMethod]
	public void RoundTripSpanningMultipleStreamSegments()
	{
		using var source = new RenderDataStream();
		for (var i = 0; i < 50; i++)
		{
			source.DrawRectangle(Brushes.Black, null, null,
				new RoundedRect(new Rect(i, i, 1, 1)), default);
		}

		using var result = RoundTrip(source);

		CornerstoneTest.AreEqual(source.CalculateBounds(), result.CalculateBounds());
	}

	private static RenderDataStream RoundTrip(RenderDataStream source)
	{
		var data = new BatchStreamData();
		var memoryPool = new BatchStreamMemoryPool(false, 64, _ => { });
		var objectPool = new BatchStreamObjectPool<object>(false, 8, _ => { });

		using (var writer = new BatchStreamWriter(data, memoryPool, objectPool))
		{
			source.SerializeTo(writer);
		}

		var result = new RenderDataStream();
		using (var reader = new BatchStreamReader(data, memoryPool, objectPool))
		{
			result.DeserializeFrom(reader);
		}

		return result;
	}

	#endregion
}