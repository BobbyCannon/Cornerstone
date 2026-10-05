#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class RoundedRectTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(0, 0, false)]
	[DataRow(100, 0, false)]
	[DataRow(100, 100, false)]
	[DataRow(0, 100, false)]
	[DataRow(10, 10, false)]
	[DataRow(90, 10, true)]
	[DataRow(90, 90, false)]
	[DataRow(10, 90, true)]
	[DataRow(17, 17, false)]
	[DataRow(83, 17, true)]
	[DataRow(83, 83, true)]
	[DataRow(17, 83, true)]
	[DataRow(50, 50, true)]
	public void ContainsExclusiveShouldReturnExpectedResultForPoint(double x, double y, bool expectedResult)
	{
		var rrect = new RoundedRect(new Rect(0, 0, 100, 100), new CornerRadius(60, 10, 50, 30));

		CornerstoneTest.AreEqual(expectedResult, rrect.ContainsExclusive(new Point(x, y)));
	}

	#endregion
}