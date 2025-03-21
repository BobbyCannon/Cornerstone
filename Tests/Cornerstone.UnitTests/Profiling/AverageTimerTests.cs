#region References

using System;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class AverageTimerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AverageTimerWithMovingAverage()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 00));

		var timer = new AverageTimer(this);

		timer.Time(() => IncrementTime(TimeSpan.FromTicks(1)));

		IsFalse(timer.IsRunning);
		AreEqual(1, timer.Elapsed.Ticks);
		AreEqual(1, timer.Average.Ticks);
		AreEqual(0, timer.Samples);
		AreEqual(1, timer.Count);

		timer.Time(() => IncrementTime(TimeSpan.FromTicks(2)));

		IsFalse(timer.IsRunning);
		AreEqual(2, timer.Elapsed.Ticks);
		AreEqual(1, timer.Average.Ticks);
		AreEqual(0, timer.Samples);
		AreEqual(2, timer.Count);

		timer.Time(() => IncrementTime(TimeSpan.FromTicks(3)));

		IsFalse(timer.IsRunning);
		AreEqual(3, timer.Elapsed.Ticks);
		AreEqual(2, timer.Average.Ticks);
		AreEqual(0, timer.Samples);
		AreEqual(3, timer.Count);

		timer.Time(() => IncrementTime(TimeSpan.FromTicks(4)));

		IsFalse(timer.IsRunning);
		AreEqual(4, timer.Elapsed.Ticks);
		AreEqual(3, timer.Average.Ticks);
		AreEqual(0, timer.Samples);
		AreEqual(4, timer.Count);

		timer.Time(() => IncrementTime(TimeSpan.FromTicks(5)));

		IsFalse(timer.IsRunning);
		AreEqual(5, timer.Elapsed.Ticks);
		AreEqual(4, timer.Average.Ticks);
		AreEqual(0, timer.Samples);
		AreEqual(5, timer.Count);
	}

	[TestMethod]
	public void CancelShouldResetTimer()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 00));

		var timer = new AverageTimer(4, this, null);
		timer.Start();

		IncrementTime(TimeSpan.FromMilliseconds(123));

		IsTrue(timer.IsRunning);
		AreEqual(123, timer.Elapsed.Milliseconds);
		AreEqual(0, timer.Average.Ticks);

		// Cancel should reset state to empty
		timer.Cancel();

		IsFalse(timer.IsRunning);
		AreEqual(123, timer.Elapsed.Milliseconds);
		AreEqual(0, timer.Average.Ticks);

		// Calling stop later should not change state
		timer.Stop();

		IsFalse(timer.IsRunning);
		AreEqual(123, timer.Elapsed.Milliseconds);
		AreEqual(0, timer.Average.Ticks);
	}

	[TestMethod]
	public void CancelShouldResetTimerWithoutChangingHistory()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 00));

		var timer = new AverageTimer(4, this, null);
		timer.Start();

		IncrementTime(TimeSpan.FromMilliseconds(123));
		IsTrue(timer.IsRunning);
		AreEqual(123, timer.Elapsed.Milliseconds);
		AreEqual(0, timer.Average.Ticks);

		timer.Stop();
		IsFalse(timer.IsRunning);
		AreEqual(123, timer.Elapsed.Milliseconds);
		AreEqual(1230000, timer.Average.Ticks);
		AreEqual(1, timer.Samples);

		// Restart timer
		IncrementTime(TimeSpan.FromMilliseconds(12));
		timer.Start();
		IncrementTime(TimeSpan.FromMilliseconds(13));
		IsTrue(timer.IsRunning);
		AreEqual(13, timer.Elapsed.Milliseconds);
		AreEqual(1230000, timer.Average.Ticks);
		AreEqual(1, timer.Samples);

		// Cancel should reset state to empty
		timer.Cancel();
		IsFalse(timer.IsRunning);
		AreEqual(13, timer.Elapsed.Milliseconds);
		AreEqual(1230000, timer.Average.Ticks);
		AreEqual(1, timer.Samples);

		// Calling stop later should not change state
		timer.Stop();
		IsFalse(timer.IsRunning);
		AreEqual(13, timer.Elapsed.Milliseconds);
		AreEqual(1230000, timer.Average.Ticks);
		AreEqual(1, timer.Samples);
	}

	[TestMethod]
	public void IsRunning()
	{
		var timer = new AverageTimer();
		IsFalse(timer.IsRunning);

		timer.Start();
		IsTrue(timer.IsRunning);

		timer.Stop();
		IsFalse(timer.IsRunning);

		timer.Time(() => { IsTrue(timer.IsRunning); });

		IsFalse(timer.IsRunning);

		var actual = timer.Time(() =>
		{
			IsTrue(timer.IsRunning);
			return true;
		});

		IsTrue(actual);
		IsFalse(timer.IsRunning);
	}

	[TestMethod]
	public void ShouldAverageOverTime()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 12));

		var timer = new AverageTimer(10, this, null);
		IsFalse(timer.IsRunning);

		timer.Start();
		IsTrue(timer.IsRunning);

		IncrementTime(TimeSpan.FromTicks(10));
		timer.Stop();

		IsFalse(timer.IsRunning);
		AreEqual(10, timer.Elapsed.Ticks);
		AreEqual(10, timer.Average.Ticks);
		AreEqual(1, timer.Samples);

		// Just bump up to ensure average is borked by time moving
		IncrementTime(TimeSpan.FromTicks(100));

		timer.Start();

		IsTrue(timer.IsRunning);
		IncrementTime(TimeSpan.FromTicks(20));

		timer.Stop();

		// 10 + 20 = 30 / 2 = 15
		IsFalse(timer.IsRunning);
		AreEqual(20, timer.Elapsed.Ticks);
		AreEqual(15, timer.Average.Ticks);
		AreEqual(2, timer.Samples);

		// Just bump up to ensure average is borked by time moving
		IncrementTime(TimeSpan.FromTicks(131));

		timer.Start();

		IsTrue(timer.IsRunning);
		IncrementTime(TimeSpan.FromTicks(9));

		timer.Stop();

		// 10 + 20 + 9 = 39 / 3 = 13
		IsFalse(timer.IsRunning);
		AreEqual(9, timer.Elapsed.Ticks);
		AreEqual(13, timer.Average.Ticks);
		AreEqual(3, timer.Samples);
	}

	[TestMethod]
	public void ShouldAverageWithLimit()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 00));

		var timer = new AverageTimer(4, this, null);
		var values = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

		for (var i = 0; i < values.Length; i++)
		{
			var value = values[i];

			timer.Start();
			IncrementTime(TimeSpan.FromTicks(value));
			timer.Stop();

			// Just bump up to ensure average is not borked by time moving
			IncrementTime(TimeSpan.FromTicks(50 + i));
		}

		// 6 + 7 + 8 + 9 = 30 / 4 = 7
		IsFalse(timer.IsRunning);
		AreEqual(9, timer.Elapsed.Ticks);
		AreEqual(7, timer.Average.Ticks);
		AreEqual(4, timer.Samples);
	}

	[TestMethod]
	public void TimeShouldHaveAccurateCount()
	{
		var timer = new AverageTimer(this);
		var count = 0;

		for (var i = 0; i < 10000; i++)
		{
			timer.Time(() =>
			{
				count++;
				IncrementTime(seconds: 1);
				return count;
			});
		}

		AreEqual(10000, count);
		AreEqual(10000, timer.Count);
		AreEqual(0, timer.Samples);
		AreEqual(TimeSpan.FromSeconds(1), timer.Average);
	}

	#endregion
}