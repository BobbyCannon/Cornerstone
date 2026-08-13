#region References

using System;
using System.Linq;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class SeriesPresentationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void NormalizeEmptyAndSingle()
	{
		AreEqual(new[] { 0d, 0d }, SeriesPresentation.NormalizeChartPayload(null));
		AreEqual(new[] { 0d, 0d }, SeriesPresentation.NormalizeChartPayload([]));
		AreEqual(new[] { 0d, 42d }, SeriesPresentation.NormalizeChartPayload([42]));
		AreEqual(new[] { 1d, 2d, 3d }, SeriesPresentation.NormalizeChartPayload([1, 2, 3]));
	}

	[TestMethod]
	public void PublishSameLengthReplacesInPlace()
	{
		var series = new SeriesDataProvider(3);
		series.ReplaceAll([1, 2, 3]);
		var assigned = 0;

		SeriesPresentation.Publish([4, 5, 6], series, _ => assigned++);

		AreEqual(0, assigned);
		AreEqual(new[] { 4d, 5d, 6d }, series.ToArray());
	}

	[TestMethod]
	public void PublishNewLengthAssignsPreFilled()
	{
		var current = new SeriesDataProvider(2);
		SeriesDataProvider next = null;

		SeriesPresentation.Publish([1, 2, 3, 4], current, s => next = s);

		IsNotNull(next);
		AreEqual(4, next.Length);
		AreEqual(new[] { 1d, 2d, 3d, 4d }, next.ToArray());
	}

	#endregion
}
