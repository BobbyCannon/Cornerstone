#region References

using System;
using System.Diagnostics;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class DateTimeProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConstructedProviderReturnsSnapshot()
	{
		var utc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
		var provider = new DateTimeProvider(Guid.Parse("11111111-1111-1111-1111-111111111111"), utc);

		AreEqual(utc, provider.UtcNow);
		AreEqual(utc.ToLocalTime(), provider.Now);
		AreEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"), provider.GetProviderId());
	}

	[TestMethod]
	public void LockProviderRejectsUpdate()
	{
		var provider = new DateTimeProvider(Guid.NewGuid(), DateTime.UtcNow);
		provider.LockProvider();

		ThrowsAny<InvalidOperationException>(() => provider.UpdateDateTime(DateTime.UtcNow));
	}

	[TestMethod]
	public void RealTimeIsLocked()
	{
		var realTime = (DateTimeProvider) DateTimeProvider.RealTime;

		ThrowsAny<InvalidOperationException>(() => realTime.UpdateDateTime(DateTime.UtcNow));
	}

	[TestMethod]
	public void RealTimeReadsOsWallClockAsUtc()
	{
		var wall = DateTime.UtcNow;
		var actual = DateTimeProvider.RealTime.UtcNow;

		AreEqual(DateTimeKind.Utc, actual.Kind);
		IsTrue(Math.Abs((actual - wall).TotalSeconds) < 1);
	}

	[TestMethod]
	public void RealTimeRecapturesAfterShortStopwatchStall()
	{
		var wall = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
		var qpc = 0L;
		var clock = new DateTimeRealTime(() => wall, () => qpc, DateTimeRealTime.DefaultRecaptureWindow);

		clock.GetUtcNow();
		wall = wall.AddMilliseconds(50);
		var afterSleep = clock.GetUtcNow();

		AreEqual(wall, afterSleep);

		qpc = Stopwatch.Frequency / 20;
		var afterWake = clock.GetUtcNow();
		AreEqual(wall + Stopwatch.GetElapsedTime(0, qpc), afterWake);
	}

	[TestMethod]
	public void RealTimeRecapturesWhenAheadOfWall()
	{
		var wall = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
		var qpc = 0L;
		var clock = new DateTimeRealTime(() => wall, () => qpc, DateTimeRealTime.DefaultRecaptureWindow);

		qpc = Stopwatch.Frequency;
		var actual = clock.GetUtcNow();

		AreEqual(wall, actual);
	}

	[TestMethod]
	public void RealTimeRecapturesWhenStopwatchStalls()
	{
		var wall = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
		var qpc = 0L;
		var clock = new DateTimeRealTime(() => wall, () => qpc, DateTimeRealTime.DefaultRecaptureWindow);

		clock.GetUtcNow();
		wall = wall.AddSeconds(5);
		var afterSleep = clock.GetUtcNow();

		AreEqual(wall, afterSleep);

		qpc = Stopwatch.Frequency / 20;
		var afterWake = clock.GetUtcNow();
		AreEqual(wall + Stopwatch.GetElapsedTime(0, qpc), afterWake);
	}

	[TestMethod]
	public void RealTimeRecapturesWhenWallJumpsAfterTimestamp()
	{
		var wall = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
		var qpc = 0L;
		var jumpWallAfterTimestamp = false;
		var clock = new DateTimeRealTime(
			() => wall,
			() =>
			{
				var timestamp = qpc;
				if (jumpWallAfterTimestamp)
				{
					wall = wall.AddHours(1);
					jumpWallAfterTimestamp = false;
				}

				return timestamp;
			},
			DateTimeRealTime.DefaultRecaptureWindow);

		clock.GetUtcNow();
		jumpWallAfterTimestamp = true;
		var actual = clock.GetUtcNow();

		AreEqual(wall, actual);
	}

	[TestMethod]
	public void RealTimeTracksStopwatchWhenAheadOfFrozenWall()
	{
		var wall = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
		var qpc = 0L;
		var clock = new DateTimeRealTime(() => wall, () => qpc, DateTimeRealTime.DefaultRecaptureWindow);

		qpc = Stopwatch.Frequency / 20;
		var actual = clock.GetUtcNow();

		AreEqual(DateTimeKind.Utc, actual.Kind);
		AreEqual(wall + Stopwatch.GetElapsedTime(0, qpc), actual);
	}

	[TestMethod]
	public void RealTimeTracksStopwatchWhenWallIsSteady()
	{
		var wall = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
		var qpc = 0L;
		var clock = new DateTimeRealTime(() => wall, () => qpc, DateTimeRealTime.DefaultRecaptureWindow);

		wall = wall.AddSeconds(1);
		qpc = Stopwatch.Frequency;
		var actual = clock.GetUtcNow();

		AreEqual(DateTimeKind.Utc, actual.Kind);
		AreEqual(wall, actual);
	}

	[TestMethod]
	public void UpdateDateTimeChangesUnlockedProvider()
	{
		var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var second = first.AddHours(1);
		var provider = new DateTimeProvider(Guid.NewGuid(), first);

		provider.UpdateDateTime(second);

		AreEqual(second, provider.UtcNow);
	}

	#endregion
}