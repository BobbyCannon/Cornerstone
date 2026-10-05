#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataStreamLineHitTestTests
{
	#region Methods

	[PresentationTestMethod]
	public void HitTestShouldBeFalse()
	{
		using var stream = LineStream(new Pen(Brushes.Black, 3), new Point(15, 10), new Point(150, 73));

		var pointsOutside = new List<Point>
		{
			new(14, 8),
			new(14, 8.8),
			new(30, 15.3),
			new(30, 18.7),
			new(151, 71.8),
			new(155, 75)
		};

		foreach (var point in pointsOutside)
		{
			CornerstoneTest.IsFalse(stream.HitTest(point));
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldBeTrue()
	{
		using var stream = LineStream(new Pen(Brushes.Black, 3), new Point(15, 10), new Point(150, 73));

		var pointsInside = new List<Point>
		{
			new(14, 8.9),
			new(15, 10),
			new(30, 15.5),
			new(30, 18.5),
			new(150, 73),
			new(151, 71.9)
		};

		foreach (var point in pointsInside)
		{
			CornerstoneTest.IsTrue(stream.HitTest(point));
		}
	}

	private static RenderDataStream LineStream(IPen pen, Point p1, Point p2)
	{
		var stream = new RenderDataStream();
		stream.DrawLine(pen, pen, p1, p2);
		return stream;
	}

	#endregion
}