#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataStreamTests
{
	#region Methods

	[PresentationTestMethod]
	public void DisposeResourcesDisposesOwnedResources()
	{
		var bitmap = RefCountable.Create(new StubBitmapImpl());
		var glyphRun = RefCountable.Create(new StubGlyphRunImpl());
		var operation = new StubCustomDrawOperation();

		using (var stream = new RenderDataStream())
		{
			stream.DrawBitmap(bitmap.Clone(), 1, new Rect(0, 0, 1, 1), new Rect(0, 0, 1, 1));
			stream.DrawGlyphRun(null, glyphRun.Clone());
			stream.DrawCustom(operation);

			CornerstoneTest.AreEqual(2, bitmap.RefCount);
			CornerstoneTest.AreEqual(2, glyphRun.RefCount);

			stream.DisposeResources();
		}

		CornerstoneTest.AreEqual(1, bitmap.RefCount);
		CornerstoneTest.AreEqual(1, glyphRun.RefCount);
		operation.Calls.VerifyCalled("Dispose", 1);

		bitmap.Dispose();
		glyphRun.Dispose();
	}

	[PresentationTestMethod]
	public void ReplayAppliesAndRestoresTransform()
	{
		var context = new StubDrawingContextImpl();
		context.Transform = Matrix.Identity;
		var matrix = Matrix.CreateTranslation(5, 7);

		using var stream = new RenderDataStream();
		stream.PushTransform(matrix);
		stream.Pop();

		stream.Replay(context);

		CornerstoneTest.AreEqual(Matrix.Identity, context.Transform);
	}

	[PresentationTestMethod]
	public void ReplayForwardsCustomOperation()
	{
		var operation = new StubCustomDrawOperation();
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.DrawCustom(operation);
		stream.Replay(context);

		operation.Calls.VerifyCalled("Render", 1);
	}

	[PresentationTestMethod]
	public void ReplayForwardsLine()
	{
		var pen = new Pen();
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.DrawLine(pen, pen, new Point(1, 2), new Point(3, 4));
		stream.Replay(context);

		context.Calls.VerifyLastPrefix("DrawLine", pen, new Point(1, 2), new Point(3, 4));
	}

	[PresentationTestMethod]
	public void ReplayForwardsRectangleWithBoxShadows()
	{
		var brush = Brushes.Black;
		var pen = new Pen();
		var rect = new RoundedRect(new Rect(0, 0, 10, 20));
		var shadows = new BoxShadows(
			new BoxShadow { Blur = 1 },
			new[] { new BoxShadow { Blur = 2 } });
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.DrawRectangle(brush, pen, pen, rect, shadows);
		stream.Replay(context);

		context.Calls.VerifyCalled("DrawRectangle", 1);
		CornerstoneTest.AreEqual(2, ((BoxShadows) context.Calls.Arguments("DrawRectangle")[0][3]).Count);
	}

	[PresentationTestMethod]
	public void ReplayForwardsRenderOptions()
	{
		var options = new RenderOptions { EdgeMode = EdgeMode.Aliased };
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.PushRenderOptions(options);
		stream.Pop();
		stream.Replay(context);

		context.Calls.VerifyCalled("PushRenderOptions", 1);
		context.Calls.VerifyCalled("PopRenderOptions", 1);
	}

	[PresentationTestMethod]
	public void ReplayForwardsTextOptions()
	{
		var options = new TextOptions { TextRenderingMode = TextRenderingMode.Antialias };
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.PushTextOptions(options);
		stream.Pop();
		stream.Replay(context);

		context.Calls.VerifyCalled("PushTextOptions", 1);
		context.Calls.VerifyCalled("PopTextOptions", 1);
	}

	[PresentationTestMethod]
	public void ReplayHandlesDeeplyNestedScopes()
	{
		var context = new StubDrawingContextImpl();

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

		stream.Replay(context);

		context.Calls.VerifyCalled("DrawRectangle", 1);
	}

	[PresentationTestMethod]
	public void ReplayPopDispatchesToMatchingPopInLifoOrder()
	{
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
		stream.PushOpacity(0.5);
		stream.Pop();
		stream.Pop();
		stream.Replay(context);

		CornerstoneTest.AreEqual(new[] { "PushClip", "PushOpacity", "PopOpacity", "PopClip" }, context.Calls.Names());
	}

	[PresentationTestMethod]
	public void ReplaySkipsNullGeometryClip()
	{
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.PushGeometryClip(null);
		stream.Pop();
		stream.Replay(context);

		CornerstoneTest.Empty(context.Calls.Names());
	}

	[PresentationTestMethod]
	public void ReplaySkipsOpacityOnePush()
	{
		var context = new StubDrawingContextImpl();

		using var stream = new RenderDataStream();
		stream.PushOpacity(1);
		stream.Pop();
		stream.Replay(context);

		CornerstoneTest.Empty(context.Calls.Names());
	}

	[PresentationTestMethod]
	public void ReplayWalksNestedPushesInOrder()
	{
		var context = new StubDrawingContextImpl();
		var geometry = new StubGeometryImpl();

		using var stream = new RenderDataStream();
		stream.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
		stream.PushGeometryClip(geometry);
		stream.Pop();
		stream.Pop();
		stream.Replay(context);

		CornerstoneTest.AreEqual(new[] { "PushClip", "PushGeometryClip", "PopGeometryClip", "PopClip" }, context.Calls.Names());
	}

	#endregion
}