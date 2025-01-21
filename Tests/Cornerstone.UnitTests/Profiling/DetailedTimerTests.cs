#region References

using System;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class DetailedTimerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void PercentOf()
	{
		var parentTimer = new DetailedTimer("Parent", null, this, null);
		parentTimer.Start();
		AreEqual(TimeSpan.Zero, parentTimer.Elapsed);
		AreEqual(100, parentTimer.Percent);
		IncrementTime(seconds: 1);
		AreEqual(TimeSpan.FromSeconds(1), parentTimer.Elapsed);
	}

	[TestMethod]
	public void ToDetailedString()
	{
		var root = DetailedTimer.StartNewTimer("Root", this);
		root.Start();
		IncrementTime(seconds: 1);
		var child = root.StartTimer("Child");
		IncrementTime(seconds: 2);
		var grandChild = child.StartTimer("GrandChild");
		IncrementTime(ticks: 1);

		// Stopping parent will stop all timers
		root.Stop();

		IsFalse(grandChild.IsRunning);
		IsFalse(child.IsRunning);
		IsFalse(root.IsRunning);

		var actual = root.ToDetailedString();
		var expected = """
						100.00% 0:00:00:03.0000001: Root
							 66.67% 0:00:00:02.0000001: Child
								  0.00% 0:00:00:00.0000001: GrandChild
								100.00% 0:00:00:02.0000000: Remainder
							 33.33% 0:00:00:01.0000000: Remainder

						""";

		AreEqual(expected, actual);
	}

	#endregion
}