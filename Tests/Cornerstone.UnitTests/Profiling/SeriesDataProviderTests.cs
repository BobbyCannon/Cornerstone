#region References

using System;
using System.Linq;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class SeriesDataProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddIncrementsVersionAndRaisesOnce()
	{
		var series = new SeriesDataProvider(4);
		var raises = 0;
		series.DataChanged += (_, _) => raises++;

		AreEqual(0ul, series.Version);
		series.Add(1.5);
		AreEqual(1ul, series.Version);
		AreEqual(1, raises);
		AreEqual(1.5, series.GetCurrentValue());
	}

	[TestMethod]
	public void AddRangeAppendsWithSingleNotify()
	{
		var series = new SeriesDataProvider(4);
		var raises = 0;
		series.DataChanged += (_, _) => raises++;

		series.AddRange([1, 2, 3, 4, 5]);

		AreEqual(1ul, series.Version);
		AreEqual(1, raises);

		// Ring of 4 after five adds: 2,3,4,5 (oldest→newest)
		AreEqual(new[] { 2d, 3d, 4d, 5d }, series.ToArray());
		AreEqual(5d, series.GetCurrentValue());
	}

	[TestMethod]
	public void AddRangeEmptyIsNoOp()
	{
		var series = new SeriesDataProvider(4);
		var raises = 0;
		series.DataChanged += (_, _) => raises++;

		series.AddRange([]);

		AreEqual(0ul, series.Version);
		AreEqual(0, raises);
	}

	[TestMethod]
	public void CopyFromAlignsVersionForViewSync()
	{
		var model = new SeriesDataProvider(4);
		var view = new SeriesDataProvider(4);

		model.Add(10);
		model.Add(20);
		model.Add(30);

		// View lags model until CopyFrom — same pattern as dispatch HasModelChanges.
		IsTrue(model.Version != view.Version);

		var raises = 0;
		view.DataChanged += (_, _) => raises++;
		view.CopyFrom(model);

		AreEqual(model.Version, view.Version);
		AreEqual(1, raises);
		AreEqual(model.ToArray(), view.ToArray());
		IsFalse(model.Version != view.Version);

		model.Add(40);
		IsTrue(model.Version != view.Version);
		view.CopyFrom(model);
		AreEqual(model.Version, view.Version);
		AreEqual(model.ToArray(), view.ToArray());
	}

	[TestMethod]
	public void CopyFromDifferentLengthThrows()
	{
		var model = new SeriesDataProvider(4);
		var view = new SeriesDataProvider(8);

		ExpectedException<ArgumentException>(() => view.CopyFrom(model));
	}

	[TestMethod]
	public void CopyFromNullThrows()
	{
		var view = new SeriesDataProvider(4);
		ExpectedException<ArgumentNullException>(() => view.CopyFrom(null));
	}

	[TestMethod]
	public void CopyFromPreservesChronologicalOrderAfterWrap()
	{
		var model = new SeriesDataProvider(3);

		// 1,2,3,4,5 → ring holds 3,4,5
		model.AddRange([1, 2, 3, 4, 5]);

		var view = new SeriesDataProvider(3);
		view.CopyFrom(model);

		AreEqual(new[] { 3d, 4d, 5d }, view.ToArray());
		AreEqual(3d, view[0]);
		AreEqual(5d, view[2]);
		AreEqual(model.Version, view.Version);
	}

	[TestMethod]
	public void CopyFromSelfIsNoOp()
	{
		var series = new SeriesDataProvider(4);
		series.Add(1);
		var raises = 0;
		series.DataChanged += (_, _) => raises++;
		var version = series.Version;

		series.CopyFrom(series);

		AreEqual(version, series.Version);
		AreEqual(0, raises);
	}

	[TestMethod]
	public void IndexerSetDoesNotRaiseOrBumpVersion()
	{
		var series = new SeriesDataProvider(4);
		series.Add(1);
		var version = series.Version;
		var raises = 0;
		series.DataChanged += (_, _) => raises++;

		series[0] = 99;

		AreEqual(version, series.Version);
		AreEqual(0, raises);
		AreEqual(99d, series[0]);
	}

	[TestMethod]
	public void ReplaceAllWritesLinearAndRaisesOnce()
	{
		var series = new SeriesDataProvider(2);
		var raises = 0;
		series.DataChanged += (_, _) => raises++;

		series.ReplaceAll([27, 3]);

		AreEqual(1ul, series.Version);
		AreEqual(1, raises);
		AreEqual(0, series.NextIndex);
		AreEqual(new[] { 27d, 3d }, series.ToArray());
		AreEqual(27d, series[0]);
		AreEqual(3d, series[1]);
		AreEqual(3d, series.GetCurrentValue());
	}

	[TestMethod]
	public void ReplaceAllWrongLengthThrows()
	{
		var series = new SeriesDataProvider(3);
		ExpectedException<ArgumentException>(() => series.ReplaceAll([1, 2]));
	}

	#endregion
}