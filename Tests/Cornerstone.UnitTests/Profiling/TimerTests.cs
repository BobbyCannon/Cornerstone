#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class TimerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddAverageTimerShouldWork()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 12));

		var timer = new Timer(this);
		var count = 0;

		timer.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(Timer.Elapsed))
			{
				count++;
			}
		};

		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.Ticks);

		var averageTimer = new AverageTimer(0, this, null);
		AreEqual(0, averageTimer.Elapsed.TotalMilliseconds);
		averageTimer.Start();
		AreEqual(0, averageTimer.Elapsed.TotalMilliseconds);
		AreEqual(0, count);
		IncrementTime(TimeSpan.FromMilliseconds(123456));
		AreEqual(123456, averageTimer.Elapsed.TotalMilliseconds);
		AreEqual(0, count);
		averageTimer.Stop();

		AreEqual(123456, averageTimer.Elapsed.TotalMilliseconds);
		IsFalse(timer.IsRunning);
		AreEqual(0, count);

		timer.Add(averageTimer);

		IsFalse(timer.IsRunning);
		AreEqual(123456, timer.Elapsed.TotalMilliseconds);
		AreEqual("00:02:03.4560000", timer.Elapsed.ToString());
		AreEqual(1, count);
	}

	[TestMethod]
	public void AddTimeSpanShouldWork()
	{
		var timer = new Timer();
		var count = 0;

		timer.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(Timer.Elapsed))
			{
				count++;
			}
		};

		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.Ticks);
		AreEqual(0, count);

		timer.Add(TimeSpan.FromMilliseconds(123456));

		IsFalse(timer.IsRunning);
		AreEqual(123456, timer.Elapsed.TotalMilliseconds);
		AreEqual("00:02:03.4560000", timer.Elapsed.ToString());
		AreEqual(1, count);
	}

	[TestMethod]
	public void ResetTimer()
	{
		var timer = new Timer();
		var actual = new List<string>();

		try
		{
			timer.Add(TimeSpan.FromSeconds(1.234));
			timer.PropertyChanged += TimerOnPropertyChanged;
			timer.Reset();
		}
		finally
		{
			timer.PropertyChanged -= TimerOnPropertyChanged;
		}

		var expected = new[] { nameof(Timer.Elapsed) };
		AreEqual(expected, actual);

		void TimerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			actual.Add(e.PropertyName);
		}
	}

	[TestMethod]
	public void ResetWhileRunning()
	{
		var timer = new Timer();
		var actual = new List<string>();

		try
		{
			timer.Add(TimeSpan.FromSeconds(1.234));
			timer.Start();
			timer.PropertyChanged += TimerOnPropertyChanged;
			timer.Reset();
		}
		finally
		{
			timer.PropertyChanged -= TimerOnPropertyChanged;
		}

		var expected = new[] { nameof(Timer.Elapsed), nameof(Timer.Elapsed), nameof(Timer.IsRunning), nameof(Timer.StartedOn) };
		AreEqual(expected, actual);

		void TimerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			actual.Add(e.PropertyName);
		}
	}

	[TestMethod]
	public void ShouldResetToProvidedElapsed()
	{
		var timer = new Timer();
		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.Ticks);

		timer.Reset(TimeSpan.FromMilliseconds(1234));
		IsFalse(timer.IsRunning);
		AreEqual(1234, timer.Elapsed.TotalMilliseconds);

		timer.Reset();
		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.TotalMilliseconds);
	}

	[TestMethod]
	public void ShouldRestartWithProvidedStartTime()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 12));

		var timer = new Timer(this);
		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.Ticks);

		timer.Restart(new DateTime(2020, 04, 23, 07, 53, 46));

		IsTrue(timer.IsRunning);
		AreEqual(146000, timer.Elapsed.TotalMilliseconds);

		timer.Restart();
		IsTrue(timer.IsRunning);
		AreEqual(0, timer.Elapsed.TotalMilliseconds);

		timer.Restart();
		IncrementTime(TimeSpan.FromMilliseconds(123456));

		IsTrue(timer.IsRunning);
		AreEqual(123456, timer.Elapsed.TotalMilliseconds);
	}

	[TestMethod]
	public void ShouldTrackUsingTimeService()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 12));
		var timer = new Timer(this);

		IsFalse(timer.IsRunning);

		timer.Start();

		IsTrue(timer.IsRunning);
		IncrementTime(TimeSpan.FromTicks(1));

		timer.Stop();

		IsFalse(timer.IsRunning);
		AreEqual(1, timer.Elapsed.Ticks);
	}

	[TestMethod]
	public void StartWithDateTimeShouldStartTimerInPast()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 12));

		var timer = new Timer(this);
		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.Ticks);

		timer.Start(UtcNow.AddMilliseconds(-12345));
		IsTrue(timer.IsRunning);
		AreEqual(12345, timer.Elapsed.TotalMilliseconds);

		timer.Stop();
		IsFalse(timer.IsRunning);
		AreEqual(12345, timer.Elapsed.TotalMilliseconds);
	}

	[TestMethod]
	public void StopWithDateTimeShouldStopTimerInPast()
	{
		SetTime(new DateTime(2020, 04, 23, 07, 56, 12));

		var timer = new Timer(this);
		IsFalse(timer.IsRunning);
		AreEqual(0, timer.Elapsed.Ticks);

		timer.Start();
		IsTrue(timer.IsRunning);
		AreEqual(0, timer.Elapsed.TotalMilliseconds);

		IncrementTime(TimeSpan.FromSeconds(12));

		timer.Stop(new DateTime(2020, 04, 23, 07, 56, 15));
		IsFalse(timer.IsRunning);
		AreEqual(3000, timer.Elapsed.TotalMilliseconds);
	}

	#endregion
}