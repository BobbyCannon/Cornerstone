#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
		var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(80), this);

		throttle.Trigger();
		throttle.Trigger(); // pending trailing
		throttle.Dispose();
		throttle.Trigger();
		throttle.Trigger(true);

		IncrementTime(milliseconds: 150);
		throttle.ProcessPending();
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
					times.Add(UtcNow);
				}
			},
			TimeSpan.FromMilliseconds(50),
			this);

		throttle.Trigger();
		lock (times)
		{
			AreEqual(1, times.Count);
		}

		IncrementTime(milliseconds: 60);
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
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(80), this);

		throttle.Trigger();
		AreEqual(1, count);

		// Burst during cooldown — coalesced into one trailing run.
		throttle.Trigger();
		throttle.Trigger();
		throttle.Trigger();
		AreEqual(1, count);

		IncrementTime(milliseconds: 80);
		throttle.ProcessPending();
		AreEqual(2, count);
	}

	[TestMethod]
	public void TriggerForceRunsImmediatelyAndCancelsTrailing()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(200), this);

		throttle.Trigger();
		AreEqual(1, count);

		throttle.Trigger(); // would schedule trailing
		throttle.Trigger(true);
		AreEqual(2, count);

		// Trailing must not fire after force cleared it.
		IncrementTime(milliseconds: 250);
		throttle.ProcessPending();
		AreEqual(2, count);
	}

	[TestMethod]
	public void TriggerLeadingEdgeRunsImmediately()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(200), this);

		throttle.Trigger();

		AreEqual(1, count);
	}

	[TestMethod]
	public void TriggerZeroIntervalAlwaysRunsImmediately()
	{
		var count = 0;
		using var throttle = new Throttle(() => Interlocked.Increment(ref count), TimeSpan.Zero, this);

		throttle.Trigger();
		throttle.Trigger();
		throttle.Trigger();

		AreEqual(3, count);
	}

	[TestMethod]
	public void TriggerForceWhileActionRunningDoesNotOverlap()
	{
		var entered = new ManualResetEventSlim(false);
		var release = new ManualResetEventSlim(false);
		var current = 0;
		var max = 0;
		var count = 0;

		using var throttle = new Throttle(
			() =>
			{
				var running = Interlocked.Increment(ref current);
				var snapshot = running;
				int previous;
				do
				{
					previous = Volatile.Read(ref max);
					if (snapshot <= previous)
					{
						break;
					}
				} while (Interlocked.CompareExchange(ref max, snapshot, previous) != previous);

				Interlocked.Increment(ref count);
				entered.Set();
				release.Wait();
				Interlocked.Decrement(ref current);
			},
			TimeSpan.FromMilliseconds(200),
			this);

		var first = Task.Run(() => throttle.Trigger());
		IsTrue(entered.Wait(2000));

		throttle.Trigger(true);
		AreEqual(1, Volatile.Read(ref count));

		release.Set();
		IsTrue(first.Wait(2000));
		var completed = this.WaitUntil(_ => Volatile.Read(ref count) >= 2, 2000, 5);
		IsTrue(completed);

		AreEqual(2, Volatile.Read(ref count));
		AreEqual(1, Volatile.Read(ref max));
	}

	[TestMethod]
	public void TrailingTimerWhileActionRunningDoesNotOverlap()
	{
		var entered = new ManualResetEventSlim(false);
		var release = new ManualResetEventSlim(false);
		var current = 0;
		var max = 0;
		var count = 0;

		using var throttle = new Throttle(
			() =>
			{
				var running = Interlocked.Increment(ref current);
				var snapshot = running;
				int previous;
				do
				{
					previous = Volatile.Read(ref max);
					if (snapshot <= previous)
					{
						break;
					}
				} while (Interlocked.CompareExchange(ref max, snapshot, previous) != previous);

				Interlocked.Increment(ref count);
				if (Volatile.Read(ref count) == 1)
				{
					entered.Set();
					release.Wait();
				}

				Interlocked.Decrement(ref current);
			},
			TimeSpan.FromMilliseconds(40),
			this);

		var first = Task.Run(() => throttle.Trigger());
		IsTrue(entered.Wait(2000));

		throttle.Trigger();
		IncrementTime(milliseconds: 80);
		throttle.ProcessPending();

		AreEqual(1, Volatile.Read(ref count));

		release.Set();
		IsTrue(first.Wait(2000));
		var completed = this.WaitUntil(_ => Volatile.Read(ref count) >= 2, 2000, 5);
		IsTrue(completed);

		AreEqual(2, Volatile.Read(ref count));
		AreEqual(1, Volatile.Read(ref max));
	}

	#endregion
}
