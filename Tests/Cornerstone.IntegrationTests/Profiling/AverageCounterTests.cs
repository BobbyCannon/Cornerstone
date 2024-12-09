#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Profiling;

[TestClass]
public class AverageCounterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IncrementPerInterval()
	{
		using var counter = new AverageCounter(TimeSpan.FromMilliseconds(25), 10, this, null);
		counter.Start();

		AreEqual(Array.Empty<DateTimeValue<long>>(), counter.Values);
		
		counter.Increment();
		counter.Increment();
		counter.Increment();
		
		IsTrue(counter.WaitUntil(x => x.Values.Count >= 1, 250, 10));
		IncrementTime(milliseconds: 25);

		counter.Increment();
		counter.Increment();
		counter.Increment();
		counter.Increment();
		
		IsTrue(counter.WaitUntil(x => x.Values.Count >= 2, 250, 10));
		AreEqual(new DateTimeValue<long>[]
			{
				new(StartDateTime, 3),
				new(StartDateTime.AddMilliseconds(25), 4)
			},
			counter.Values
		);
	}

	#endregion
}