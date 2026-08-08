#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class ThrottleTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DisposePreventsFurtherTriggersAndTrailing()
	{
		var count = 0;
		var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(80));

		throttle.Trigger();
		throttle.Trigger(); // pending trailing
		throttle.Dispose();
		throttle.Trigger();
		throttle.Trigger(true);

		Thread.Sleep(150);
		AreEqual(1, count);
	}

	[TestMethod]
	public void TriggerAfterWindowAllowsNewLeadingEdge()
	{
		var times = new List<DateTime>();
		using var throttle = new Throttle(
			() =>
			{
				lock (times)
				{
					times.Add(DateTime.UtcNow);
				}
			},
			TimeSpan.FromMilliseconds(50));

		throttle.Trigger();
		var completed = this.WaitUntil(
			_ =>
			{
				lock (times)
				{
					return times.Count >= 1;
				}
			},
			1000,
			1);
		IsTrue(completed);

		Thread.Sleep(60);
		throttle.Trigger();

		lock (times)
		{
			AreEqual(2, times.Count);
		}
	}

	[TestMethod]
	public void TriggerDuringCooldownSchedulesTrailingEdge()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(80));

		throttle.Trigger();
		AreEqual(1, count);

		// Burst during cooldown — coalesced into one trailing run.
		throttle.Trigger();
		throttle.Trigger();
		throttle.Trigger();
		AreEqual(1, count);

		var completed = this.WaitUntil(_ => Volatile.Read(ref count) >= 2, 2000, 5);
		IsTrue(completed, () => $"Expected trailing edge; count={Volatile.Read(ref count)}");
		AreEqual(2, count);
	}

	[TestMethod]
	public void TriggerForceRunsImmediatelyAndCancelsTrailing()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(200));

		throttle.Trigger();
		AreEqual(1, count);

		throttle.Trigger(); // would schedule trailing
		throttle.Trigger(true);
		AreEqual(2, count);

		// Trailing must not fire after force cleared it.
		Thread.Sleep(250);
		AreEqual(2, count);
	}

	[TestMethod]
	public void TriggerLeadingEdgeRunsImmediately()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(200));

		throttle.Trigger();

		AreEqual(1, count);
	}

	[TestMethod]
	public void TriggerZeroIntervalAlwaysRunsImmediately()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.Zero);

		throttle.Trigger();
		throttle.Trigger();
		throttle.Trigger();

		AreEqual(3, count);
	}

	#endregion
}