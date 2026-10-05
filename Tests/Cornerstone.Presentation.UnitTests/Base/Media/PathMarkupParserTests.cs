#region References

using System.Globalization;
using System.IO;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class PathMarkupParserTests
{
	#region Methods

	[PresentationTestMethod]
	public void CloseFigureShouldMoveCurrentPointToCreateFigurePoint()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("M10,10L100,100Z m10,10");

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			CornerstoneTest.AreEqual(2, pathGeometry.Figures.Count);

			var figure = pathGeometry.Figures[0];

			CornerstoneTest.AreEqual(new Point(10, 10), figure.StartPoint);

			CornerstoneTest.AreEqual(true, figure.IsClosed);

			CornerstoneTest.IsNotNull(figure.Segments);
			CornerstoneTest.AreEqual(new Point(100, 100), ((LineSegment) figure.Segments[0]).Point);

			figure = pathGeometry.Figures[1];

			CornerstoneTest.AreEqual(new Point(20, 20), figure.StartPoint);
		}
	}

	[PresentationTestMethod]
	[DataRow("M5.5.5 5.5.5 5.5.5", "M 5.5, 0.5 L 5.5, 0.5 L 5.5, 0.5")]
	[DataRow("F1 M24,14 A2,2,0,1,1,20,14 A2,2,0,1,1,24,14 z", "F1 M 24, 14 A 2, 2 0 1 1 20, 14 A 2, 2 0 1 1 24, 14Z")]
	[DataRow("F1M16,12C16,14.209 14.209,16 12,16 9.791,16 8,14.209 8,12 8,11.817 8.03,11.644 8.054,11.467L6.585,10 4,10 "
		+ "4,6.414 2.5,7.914 0,5.414 0,3.586 3.586,0 4.414,0 7.414,3 7.586,3 9,1.586 11.914,4.5 10.414,6 "
		+ "12.461,8.046C14.45,8.278,16,9.949,16,12",
		"F1 M 16, 12 C 16, 14.209 14.209, 16 12, 16 C 9.791, 16 8, 14.209 8, 12 C 8, 11.817 8.03, 11.644 8.054, 11.467 L 6.585, 10 "
		+ "L 4, 10 L 4, 6.414 L 2.5, 7.914 L 0, 5.414 L 0, 3.586 L 3.586, 0 L 4.414, 0 L 7.414, 3 L 7.586, 3 L 9, 1.586 L "
		+ "11.914, 4.5 L 10.414, 6 L 12.461, 8.046 C 14.45, 8.278 16, 9.949 16, 12")]
	public void ParsedGeometryToStringShouldFormatValue(string pathData, string formattedPathData)
	{
		var target = PathGeometry.Parse(pathData);

		var output = target.ToString();

		CornerstoneTest.AreEqual(formattedPathData, output);
	}

	[PresentationTestMethod]
	[DataRow("M 5.5, 5 L 5.5, 5 L 5.5, 5")]
	[DataRow("F1 M 9.0771, 11 C 9.1161, 10.701 9.1801, 10.352 9.3031, 10 L 9.0001, 10 L 9.0001, 6.166 L 3.0001, 9.767 L 3.0001, 10 "
		+ "L 9.99999999997669E-05, 10 L 9.99999999997669E-05, 0 L 3.0001, 0 L 3.0001, 0.234 L 9.0001, 3.834 L 9.0001, 0 "
		+ "L 12.0001, 0 L 12.0001, 8.062 C 12.1861, 8.043 12.3821, 8.031 12.5941, 8.031 C 15.3481, 8.031 15.7961, 9.826 "
		+ "15.9201, 11 L 16.0001, 16 L 9.0001, 16 L 9.0001, 12.562 L 9.0001, 11Z")]
	[DataRow("F1 M 24, 14 A 2, 2 0 1 1 20, 14 A 2, 2 0 1 1 24, 14Z")]
	[DataRow("M 0, 0 L 10, 10Z")]
	[DataRow("M 50, 50 L 100, 100 L 150, 50")]
	[DataRow("M 50, 50 L -10, -10 L 10, 50")]
	[DataRow("M 50, 50 L 100, 100 L 150, 50Z M 50, 50 L 70, 70 L 120, 50Z")]
	[DataRow("M 80, 200 A 100, 50 45 1 0 100, 50")]
	[DataRow("F1 M 16, 12 C 16, 14.209 14.209, 16 12, 16 C 9.791, 16 8, 14.209 8, 12 C 8, 11.817 8.03, 11.644 8.054, 11.467 L 6.585, 10 "
		+ "L 4, 10 L 4, 6.414 L 2.5, 7.914 L 0, 5.414 L 0, 3.586 L 3.586, 0 L 4.414, 0 L 7.414, 3 L 7.586, 3 L 9, 1.586 L "
		+ "11.914, 4.5 L 10.414, 6 L 12.461, 8.046 C 14.45, 8.278 16, 9.949 16, 12")]
	public void ParsedGeometryToStringShouldProduceValidValue(string pathData)
	{
		var target = PathGeometry.Parse(pathData);

		var output = target.ToString();

		CornerstoneTest.AreEqual(pathData, output);
	}

	[PresentationTestMethod]
	public void ParsesClose()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("M0 0L10 10z");

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			CornerstoneTest.IsTrue(figure.IsClosed);
		}
	}

	[PresentationTestMethod]
	public void ParsesFillModeBeforeMove()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("F 1M0,0");

			CornerstoneTest.AreEqual(FillRule.NonZero, pathGeometry.FillRule);
		}
	}

	[PresentationTestMethod]
	[DataRow("M0 0 10 10 20 20")]
	[DataRow("M0,0 10,10 20,20")]
	[DataRow("M0,0,10,10,20,20")]
	public void ParsesImplicitLineCommandAfterMove(string pathData)
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse(pathData);

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			CornerstoneTest.IsNotNull(figure.Segments);
			var segment = figure.Segments[0];

			CornerstoneTest.IsType<LineSegment>(segment);

			var lineSegment = (LineSegment) segment;

			CornerstoneTest.AreEqual(new Point(10, 10), lineSegment.Point);

			segment = figure.Segments[1];

			CornerstoneTest.IsType<LineSegment>(segment);

			lineSegment = (LineSegment) segment;

			CornerstoneTest.AreEqual(new Point(20, 20), lineSegment.Point);
		}
	}

	[PresentationTestMethod]
	[DataRow("m0 0 10 10 20 20")]
	[DataRow("m0,0 10,10 20,20")]
	[DataRow("m0,0,10,10,20,20")]
	public void ParsesImplicitLineCommandAfterRelativeMove(string pathData)
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse(pathData);

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			CornerstoneTest.IsNotNull(figure.Segments);
			var segment = figure.Segments[0];

			CornerstoneTest.IsType<LineSegment>(segment);

			var lineSegment = (LineSegment) segment;

			CornerstoneTest.AreEqual(new Point(10, 10), lineSegment.Point);

			segment = figure.Segments[1];

			CornerstoneTest.IsType<LineSegment>(segment);

			lineSegment = (LineSegment) segment;

			CornerstoneTest.AreEqual(new Point(30, 30), lineSegment.Point);
		}
	}

	[PresentationTestMethod]
	public void ParsesLine()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("M0 0L10 10");

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			CornerstoneTest.IsNotNull(figure.Segments);
			var segment = figure.Segments[0];

			CornerstoneTest.IsType<LineSegment>(segment);

			var lineSegment = (LineSegment) segment;

			CornerstoneTest.AreEqual(new Point(10, 10), lineSegment.Point);
		}
	}

	[PresentationTestMethod]
	public void ParsesMove()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("M10 10");

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			CornerstoneTest.AreEqual(new Point(10, 10), figure.StartPoint);
		}
	}

	[PresentationTestMethod]
	public void ParsesScientificNotationDouble()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("M -1.01725E-005 -1.01725e-005");

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			CornerstoneTest.AreEqual(new Point(
				double.Parse("-1.01725E-005", NumberStyles.Float, CultureInfo.InvariantCulture),
				double.Parse("-1.01725E-005", NumberStyles.Float, CultureInfo.InvariantCulture)), figure.StartPoint);
		}
	}

	[PresentationTestMethod]
	[DataRow("M0 0L10 10")]
	[DataRow("M0 0L10 10z")]
	[DataRow("M0 0L10 10 \n ")]
	[DataRow("M0 0L10 10z \n ")]
	[DataRow("M0 0L10 10 ")]
	[DataRow("M0 0L10 10z ")]
	public void ShouldAlwaysEndFigure(string pathData)
	{
		var context = new StubGeometryContext();

		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse(pathData);
		}

		context.Calls.VerifyCalledAtLeastOnce("EndFigure");
	}

	[PresentationTestMethod]
	public void ShouldHandleStartPointAfterEmptyFigure()
	{
		var pathGeometry = new PathGeometry();
		using var context = new PathGeometryContext(pathGeometry);
		using var parser = new PathMarkupParser(context);
		parser.Parse("M50,50z l -5,-5");

		CornerstoneTest.IsNotNull(pathGeometry.Figures);
		CornerstoneTest.AreEqual(2, pathGeometry.Figures.Count);

		var firstFigure = pathGeometry.Figures[0];

		CornerstoneTest.AreEqual(new Point(50, 50), firstFigure.StartPoint);

		var secondFigure = pathGeometry.Figures[1];

		CornerstoneTest.AreEqual(new Point(50, 50), secondFigure.StartPoint);
	}

	[PresentationTestMethod]
	[DataRow("M5.5.5 5.5.5 5.5.5")]
	[DataRow("F1M9.0771,11C9.1161,10.701,9.1801,10.352,9.3031,10L9.0001,10 9.0001,6.166 3.0001,9.767 3.0001,10 "
		+ "9.99999999997669E-05,10 9.99999999997669E-05,0 3.0001,0 3.0001,0.234 9.0001,3.834 9.0001,0 "
		+ "12.0001,0 12.0001,8.062C12.1861,8.043 12.3821,8.031 12.5941,8.031 15.3481,8.031 15.7961,9.826 "
		+ "15.9201,11L16.0001,16 9.0001,16 9.0001,12.562 9.0001,11z")] // issue #1708
	[DataRow("         M0 0")]
	[DataRow("F1 M24,14 A2,2,0,1,1,20,14 A2,2,0,1,1,24,14 z")] // issue #1107
	[DataRow("M0 0L10 10z")]
	[DataRow("M50 50 L100 100 L150 50")]
	[DataRow("M50 50L100 100L150 50")]
	[DataRow("M50,50 L100,100 L150,50")]
	[DataRow("M50 50 L-10 -10 L10 50")]
	[DataRow("M50 50L-10-10L10 50")]
	[DataRow("M50 50 L100 100 L150 50zM50 50 L70 70 L120 50z")]
	[DataRow("M 50 50 L 100 100 L 150 50")]
	[DataRow("M50 50 L100 100 L150 50 H200 V100Z")]
	[DataRow("M 80 200 A 100 50 45 1 0 100 50")]
	[DataRow(
		"F1 M 16.6309 18.6563C 17.1309 8.15625 29.8809 14.1563 29.8809 14.1563C 30.8809 11.1563 34.1308 11.4063" +
		" 34.1308 11.4063C 33.5 12 34.6309 13.1563 34.6309 13.1563C 32.1309 13.1562 31.1309 14.9062 31.1309 14.9" +
		"062C 41.1309 23.9062 32.6309 27.9063 32.6309 27.9062C 24.6309 24.9063 21.1309 22.1562 16.6309 18.6563 Z" +
		" M 16.6309 19.9063C 21.6309 24.1563 25.1309 26.1562 31.6309 28.6562C 31.6309 28.6562 26.3809 39.1562 18" +
		".3809 36.1563C 18.3809 36.1563 18 38 16.3809 36.9063C 15 36 16.3809 34.9063 16.3809 34.9063C 16.3809 34" +
		".9063 10.1309 30.9062 16.6309 19.9063 Z ")]
	[DataRow(
		"F1M16,12C16,14.209 14.209,16 12,16 9.791,16 8,14.209 8,12 8,11.817 8.03,11.644 8.054,11.467L6.585,10 4,10 " +
		"4,6.414 2.5,7.914 0,5.414 0,3.586 3.586,0 4.414,0 7.414,3 7.586,3 9,1.586 11.914,4.5 10.414,6 " +
		"12.461,8.046C14.45,8.278,16,9.949,16,12")]
	public void ShouldParse(string pathData)
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse(pathData);

			CornerstoneTest.IsTrue(true);
		}
	}

	[PresentationTestMethod]
	public void ShouldParseFlagsWithoutSeparator()
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			parser.Parse("a.898.898 0 01.27.188");

			CornerstoneTest.IsNotNull(pathGeometry.Figures);
			var figure = pathGeometry.Figures[0];

			var segments = figure.Segments;

			CornerstoneTest.IsNotNull(segments);

			CornerstoneTest.AreEqual(1, segments.Count);

			var arcSegment = segments[0];

			CornerstoneTest.IsType<ArcSegment>(arcSegment);
		}
	}

	[PresentationTestMethod]
	[DataRow("0 0")]
	[DataRow("j")]
	public void ThrowsInvalidDataExceptionOnNoneDefinedCommand(string pathData)
	{
		var pathGeometry = new PathGeometry();
		using (var context = new PathGeometryContext(pathGeometry))
		using (var parser = new PathMarkupParser(context))
		{
			Assert.Throws<InvalidDataException>(() => parser.Parse(pathData));
		}
	}

	#endregion
}