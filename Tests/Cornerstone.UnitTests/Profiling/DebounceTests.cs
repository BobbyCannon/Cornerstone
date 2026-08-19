#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class DebounceTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DisposeCancelsPendingAndBlocksNewTriggers()
	{
		var count = 0;
		var debounce = new Debounce(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(80), this);

		debounce.Trigger();
		debounce.Dispose();
		debounce.Trigger();
		debounce.Trigger(true);

		IncrementTime(milliseconds: 150);
		debounce.ProcessPending();
		AreEqual(0, count);
	}

	[TestMethod]
	public void TriggerForceRunsImmediatelyAndCancelsPending()
	{
		var count = 0;
		using var debounce = new Debounce(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(200), this);

		debounce.Trigger();
		AreEqual(0, count);

		debounce.Trigger(true);
		AreEqual(1, count);

		IncrementTime(milliseconds: 250);
		debounce.ProcessPending();
		AreEqual(1, count);
	}

	[TestMethod]
	public void TriggerForceWhileActionRunningDoesNotOverlap()
	{
		var entered = new ManualResetEventSlim(false);
		var release = new ManualResetEventSlim(false);
		var current = 0;
		var max = 0;
		var count = 0;

		using var debounce = new Debounce(
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

		var first = Task.Run(() => debounce.Trigger(true));
		IsTrue(entered.Wait(2000));

		debounce.Trigger(true);
		AreEqual(1, Volatile.Read(ref count));

		release.Set();
		IsTrue(first.Wait(2000));
		var completed = this.WaitUntil(_ => Volatile.Read(ref count) >= 2, 2000, 5);
		IsTrue(completed);

		AreEqual(2, Volatile.Read(ref count));
		AreEqual(1, Volatile.Read(ref max));
	}

	[TestMethod]
	public void TriggerWaitsForSilence()
	{
		var count = 0;
		using var debounce = new Debounce(() => Interlocked.Increment(ref count), TimeSpan.FromMilliseconds(80), this);

		debounce.Trigger();
		debounce.Trigger();
		debounce.Trigger();
		AreEqual(0, count);

		IncrementTime(milliseconds: 80);
		debounce.ProcessPending();
		AreEqual(1, count);
	}

	#endregion
}
