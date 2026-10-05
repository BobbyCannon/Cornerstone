#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataStreamEffectTests
{
	#region Methods

	[PresentationTestMethod]
	public void EffectInflatesChildBoundsByPadding()
	{
		using var stream = new RenderDataStream();
		stream.PushEffect(new ImmutableBlurEffect(5), new Rect(0, 0, 100, 100));
		stream.DrawRectangle(null, null, null, new RoundedRect(new Rect(0, 0, 100, 100)), default);
		stream.Pop();

		var padding = new ImmutableBlurEffect(5).GetEffectOutputPadding();
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100).Inflate(padding), stream.CalculateBounds());
	}

	[PresentationTestMethod]
	public void EmptyEffectScopeHasNullBounds()
	{
		using var stream = new RenderDataStream();
		stream.PushEffect(new ImmutableBlurEffect(5), new Rect(0, 0, 100, 100));
		stream.Pop();
		CornerstoneTest.IsNull(stream.CalculateBounds());
	}

	#endregion
}