#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GeometryBuilderTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(20.0, 10.0)]
	[DataRow(10.0, 5.0)]
	[DataRow(2.0, 1.0)]
	[DataRow(1.0, 0.0)]
	public void CalculateRoundedCornersRectangleWinUIInnerBorderEdgeBordersLargerThanCornersTest(
		double uniformBorders,
		double uniformCorners)
	{
		var bounds = new Rect(new Size(100, 100));
		var borderThickness = new Thickness(uniformBorders);
		var cornerRadius = new CornerRadius(uniformCorners);

		var points = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(bounds, borderThickness, cornerRadius, BackgroundSizing.InnerBorderEdge);

		CornerstoneTest.AreEqual(new Point(uniformBorders, uniformBorders), points.LeftTop);
		CornerstoneTest.AreEqual(new Point(uniformBorders, uniformBorders), points.TopLeft);
		CornerstoneTest.AreEqual(new Point(100 - uniformBorders, uniformBorders), points.TopRight);
		CornerstoneTest.AreEqual(new Point(100 - uniformBorders, uniformBorders), points.RightTop);
		CornerstoneTest.AreEqual(new Point(100 - uniformBorders, 100 - uniformBorders), points.RightBottom);
		CornerstoneTest.AreEqual(new Point(100 - uniformBorders, 100 - uniformBorders), points.BottomRight);
		CornerstoneTest.AreEqual(new Point(uniformBorders, 100 - uniformBorders), points.BottomLeft);
		CornerstoneTest.AreEqual(new Point(uniformBorders, 100 - uniformBorders), points.LeftBottom);

		CornerstoneTest.IsFalse(points.IsRounded);
	}

	[PresentationTestMethod]
	[DataRow(20.0, 10.0)]
	[DataRow(10.0, 5.0)]
	[DataRow(2.0, 1.0)]
	public void CalculateRoundedCornersRectangleWinUIOuterBorderEdgeBordersLargerThanCornersTest(
		double uniformBorders,
		double uniformCorners)
	{
		var bounds = new Rect(new Size(100, 100));
		var borderThickness = new Thickness(uniformBorders);
		var cornerRadius = new CornerRadius(uniformCorners);

		var points = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(bounds, borderThickness, cornerRadius, BackgroundSizing.OuterBorderEdge);

		CornerstoneTest.AreEqual(new Point(0, uniformBorders), points.LeftTop);
		CornerstoneTest.AreEqual(new Point(uniformBorders, 0), points.TopLeft);
		CornerstoneTest.AreEqual(new Point(100 - uniformBorders, 0), points.TopRight);
		CornerstoneTest.AreEqual(new Point(100, uniformBorders), points.RightTop);
		CornerstoneTest.AreEqual(new Point(100, 100 - uniformBorders), points.RightBottom);
		CornerstoneTest.AreEqual(new Point(100 - uniformBorders, 100), points.BottomRight);
		CornerstoneTest.AreEqual(new Point(uniformBorders, 100), points.BottomLeft);
		CornerstoneTest.AreEqual(new Point(0, 100 - uniformBorders), points.LeftBottom);

		CornerstoneTest.IsTrue(points.IsRounded);
	}

	#endregion
}