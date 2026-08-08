#region References

using System.Linq;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class ProfilerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IncrementCountsWithoutDuration()
	{
		var profiler = new Profiler("Profiler", this);
		profiler.Refresh();

		for (var i = 0; i < 50; i++)
		{
			profiler.Increment("Rate");
		}

		profiler.Increment("Rate", 10);

		IncrementTime(milliseconds: 1000);
		profiler.Refresh();

		var actual = profiler.ToArray();
		AreEqual(1, actual.Length, () => "Length should be 1.");
		AreEqual(0, actual[0].Count, () => "Accumulators reset after Refresh.");
		AreEqual(0, actual[0].TotalTicks, () => "Increment adds no ticks.");
		AreEqual("60", actual[0].CallsPerSecond.ToString("F0"), () => "50 + 10 over 1s => 60/s.");
		AreEqual(0, actual[0].AverageTicks, () => "Average duration stays 0 for count-only.");
	}

	[TestMethod]
	public void SimpleTest()
	{
		var profiler = new Profiler("Profiler", this);
		profiler.Refresh();
		for (var i = 0; i < 12000; i++)
		{
			var scope = profiler.Start("Test");
			IncrementTime(milliseconds: 10);
			scope.Dispose();
		}
		profiler.Refresh();
		var actual = profiler.ToArray();
		AreEqual(1, actual.Length, () => "Length should be 1.");
		AreEqual(0, actual[0].Count, () => "Count should be 0.");
		AreEqual(0, actual[0].TotalTicks, () => "Count should be 0.");
		AreEqual("100", actual[0].CallsPerSecond.ToString("F0"), () => "CallsPerSecond should be 100.");
		AreEqual("100000", actual[0].AverageTicks.ToString("F0"), () => "AverageTicks should be 100000.");
	}

	#endregion
}