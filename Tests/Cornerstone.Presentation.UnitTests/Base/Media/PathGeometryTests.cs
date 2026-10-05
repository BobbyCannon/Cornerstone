#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class PathGeometryTests
{
	#region Methods

	[PresentationTestMethod]
	public void PathGeometryTriggersInvalidationOnFiguresAdd()
	{
		var segment = new PolyLineSegment
		{
			Points = [new Point(1, 1), new Point(2, 2)]
		};

		var figure = new PathFigure
		{
			Segments = [segment],
			IsClosed = false,
			IsFilled = false
		};

		var target = new PathGeometry();

		var changed = false;

		target.Changed += (_, _) => { changed = true; };

		target.Figures?.Add(figure);
		CornerstoneTest.IsTrue(changed);
	}

	#endregion
}