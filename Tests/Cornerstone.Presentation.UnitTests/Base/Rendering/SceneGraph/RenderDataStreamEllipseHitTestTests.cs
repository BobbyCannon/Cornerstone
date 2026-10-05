#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataStreamEllipseHitTestTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(50, 50, true)]
	[DataRow(50, 0, true)]
	[DataRow(100, 50, true)]
	[DataRow(50, 100, true)]
	[DataRow(-1, 0, false)]
	[DataRow(101, 0, false)]
	[DataRow(101, 101, false)]
	[DataRow(0, 101, false)]
	public void FillOnlyHitTest(double x, double y, bool inside)
	{
		using var stream = new RenderDataStream();
		stream.DrawEllipse(Brushes.Black, null, null, new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(inside, stream.HitTest(new Point(x, y)));
	}

	[PresentationTestMethod]
	[DataRow(50, 0, true)]
	[DataRow(51, 0, true)]
	[DataRow(100, 50, true)]
	[DataRow(50, 100, true)]
	[DataRow(-1, 50, true)]
	[DataRow(53, 50, false)]
	[DataRow(101, 0, false)]
	[DataRow(101, 101, false)]
	[DataRow(0, 101, false)]
	public void StrokeOnlyHitTest(double x, double y, bool inside)
	{
		var pen = new ImmutablePen(Brushes.Black, 2);

		using var stream = new RenderDataStream();
		stream.DrawEllipse(null, pen, pen, new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(inside, stream.HitTest(new Point(x, y)));
	}

	#endregion
}