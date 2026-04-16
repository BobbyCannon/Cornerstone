#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class TimeSpanExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToTimeSpan()
	{
		var scenarios = new (double nanoseconds, TimeSpan expected)[]
		{
			(TimeSpan.NanosecondsPerTick, TimeSpan.FromTicks(1)),
			(TimeSpan.NanosecondsPerTick * 2, TimeSpan.FromTicks(2)),
			(1000, TimeSpan.FromMicroseconds(1)),
			(1000000, TimeSpan.FromMilliseconds(1))
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.expected, TimeSpanExtensions.ToTimeSpan(scenario.nanoseconds));
		}
	}

	#endregion
}