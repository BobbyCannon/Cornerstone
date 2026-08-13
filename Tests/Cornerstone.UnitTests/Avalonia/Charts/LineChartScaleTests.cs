#region References

using Cornerstone.Avalonia.Charts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Charts;

[TestClass]
public class LineChartScaleTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AbsoluteScaleUsesZeroMin()
	{
		LineChart.ResolveVerticalScale(40_000_000, 65_000_000, false, 0.1, out var min, out var max);
		AreEqual(0, min);
		AreEqual(65_000_000, max);
	}

	[TestMethod]
	public void AbsoluteScaleAllZerosUsesMinZeroAndMaxAtLeastOne()
	{
		// Empty / no-activity charts: line sits on the bottom (0…1), not mid-plot.
		LineChart.ResolveVerticalScale(0, 0, false, 0.1, out var min, out var max);
		AreEqual(0, min);
		IsTrue(max >= 1);
	}

	[TestMethod]
	public void AbsoluteScaleHonorsScaleMaximumFloor()
	{
		LineChart.ResolveVerticalScale(10, 62.4, false, 0.1, 100, out var min, out var max);
		AreEqual(0, min);
		AreEqual(100, max);

		// Data above the floor still expands the max.
		LineChart.ResolveVerticalScale(10, 120, false, 0.1, 100, out min, out max);
		AreEqual(0, min);
		AreEqual(120, max);
	}

	[TestMethod]
	public void RelativeScaleUsesSmallestMinusTenPercent()
	{
		// min = 40M - 10% of 40M = 36M; max = 65M
		LineChart.ResolveVerticalScale(40_000_000, 65_000_000, true, 0.1, out var min, out var max);
		AreEqual(36_000_000, min);
		AreEqual(65_000_000, max);
	}

	[TestMethod]
	public void RelativeScaleZeroMinStaysZero()
	{
		LineChart.ResolveVerticalScale(0, 65_000_000, true, 0.1, out var min, out var max);
		AreEqual(0, min);
		AreEqual(65_000_000, max);
	}

	[TestMethod]
	public void RelativeScaleFlatSeriesPullsMinDown()
	{
		// Same samples: min = 50 − 10% = 45, max stays 50 (non-zero range).
		LineChart.ResolveVerticalScale(50, 50, true, 0.1, out var min, out var max);
		AreEqual(45, min);
		AreEqual(50, max);
	}

	[TestMethod]
	public void RelativeScaleAllZerosExpandsRange()
	{
		LineChart.ResolveVerticalScale(0, 0, true, 0.1, out var min, out var max);
		IsTrue(max > min);
	}

	#endregion
}