#nullable enable

#region References

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;

[TestClass]
public class PointsListTypeConverterTests : XamlTestBase
{
	#region Constructors

	static PointsListTypeConverterTests()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	[DataRow("1,2 3,4")]
	[DataRow("1 2 3 4")]
	[DataRow("1 2,3 4")]
	[DataRow("1,2,3,4")]
	public void ShouldParsePointsinXaml(string input)
	{
		var xaml = $"<Polygon xmlns='https://github.com/BobbyCannon/Cornerstone' Points='{input}' />";
		var polygon = (Polygon) CornerstoneRuntimeXamlLoader.Load(xaml);

		var points = polygon.Points;

		CornerstoneTest.AreEqual(2, points.Count);
		CornerstoneTest.AreEqual(new Point(1, 2), points[0]);
		CornerstoneTest.AreEqual(new Point(3, 4), points[1]);
	}

	[PresentationTestMethod]
	[DataRow("1,2 3,4")]
	[DataRow("1 2 3 4")]
	[DataRow("1 2,3 4")]
	[DataRow("1,2,3,4")]
	public void TypeConverterShouldParse(string input)
	{
		var conv = new PointsListTypeConverter();

		var points = (IList<Point>) conv.ConvertFrom(input)!;

		CornerstoneTest.AreEqual(2, points.Count);
		CornerstoneTest.AreEqual(new Point(1, 2), points[0]);
		CornerstoneTest.AreEqual(new Point(3, 4), points[1]);
	}

	#endregion
}