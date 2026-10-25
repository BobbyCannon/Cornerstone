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

	[PresentationTestMethod]
	public void UniformBorderCenterlineShrinksRadiiWithTheRect()
	{
		var outer = new RoundedRect(new Rect(0, 0, 80, 80), new CornerRadius(16, 12, 10, 8));
		var centerline = outer.UniformBorderCenterline(2);

		CornerstoneTest.AreEqual(1d, centerline.Rect.X, 0);
		CornerstoneTest.AreEqual(1d, centerline.Rect.Y, 0);
		CornerstoneTest.AreEqual(78d, centerline.Rect.Width, 0);
		CornerstoneTest.AreEqual(78d, centerline.Rect.Height, 0);
		CornerstoneTest.AreEqual(15d, centerline.RadiiTopLeft.X, 0);
		CornerstoneTest.AreEqual(15d, centerline.RadiiTopLeft.Y, 0);
		CornerstoneTest.AreEqual(11d, centerline.RadiiTopRight.X, 0);
		CornerstoneTest.AreEqual(9d, centerline.RadiiBottomRight.X, 0);
		CornerstoneTest.AreEqual(7d, centerline.RadiiBottomLeft.X, 0);
	}

	[PresentationTestMethod]
	public void UniformBorderCenterlineClampsRadiusAtZero()
	{
		var centerline = new RoundedRect(new Rect(0, 0, 40, 40), 2).UniformBorderCenterline(6);

		CornerstoneTest.AreEqual(0d, centerline.RadiiTopLeft.X, 0);
		CornerstoneTest.AreEqual(0d, centerline.RadiiTopLeft.Y, 0);
	}

	#endregion
}