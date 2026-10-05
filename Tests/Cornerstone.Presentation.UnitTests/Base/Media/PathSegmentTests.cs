#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class PathSegmentTests
{
	#region Methods

	[PresentationTestMethod]
	public void PathSegmentTriggersInvalidationOnPropertyChange()
	{
		var targetSegment = new ArcSegment
		{
			Size = new Size(10, 10),
			Point = new Point(5, 5)
		};

		var target = new PathGeometry
		{
			Figures = new PathFigures
			{
				new PathFigure { IsClosed = false, Segments = new PathSegments { targetSegment } }
			}
		};

		var changed = false;

		target.Changed += (s, e) => changed = true;

		targetSegment.Size = new Size(20, 20);

		CornerstoneTest.IsTrue(changed);
	}

	#endregion
}