#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class UnicodeRangeSegmentTests
{
	#region Methods

	[DataRow(0)]
	[DataRow(19)]
	[DataRow(26)]
	[DataRow(100)]
	[PresentationTestMethod]
	public void InRangeShouldReturnFalseForValuesOutsideRange(int value)
	{
		var segment = new UnicodeRangeSegment(20, 25);

		CornerstoneTest.AreEqual(false, segment.IsInRange(value));
	}

	[DataRow(20)]
	[DataRow(21)]
	[DataRow(22)]
	[PresentationTestMethod]
	public void InRangeShouldReturnTrueForValuesWithinRange(int value)
	{
		var segment = new UnicodeRangeSegment(20, 22);

		CornerstoneTest.AreEqual(true, segment.IsInRange(value));
	}

	[DataRow("u+00-FF", 0, 255)]
	[DataRow("U+00-FF", 0, 255)]
	[DataRow("U+00-U+FF", 0, 255)]
	[DataRow("U+AB??", 43776, 44031)]
	[PresentationTestMethod]
	public void ShouldParse(string s, int expectedStart, int expectedEnd)
	{
		var segment = UnicodeRangeSegment.Parse(s);

		CornerstoneTest.AreEqual(expectedStart, segment.Start);

		CornerstoneTest.AreEqual(expectedEnd, segment.End);
	}

	#endregion
}