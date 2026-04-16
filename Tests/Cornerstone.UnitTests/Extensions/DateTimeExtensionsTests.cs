#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class DateTimeExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConstantsAreCorrectForUnixEpoch()
	{
		AreEqual(719162, DateTimeExtensions.UnixEpochDateOnlyDayNumber);
		AreEqual(621355968000000000L, DateTimeExtensions.UnixEpochDateTimeTicks);

		AreEqual(new DateOnly(1970, 1, 1), DateTimeExtensions.UnixEpochDateOnly);
		AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTimeExtensions.UnixEpochDateTime);
		AreEqual(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimeExtensions.UnixEpochDateTimeOffset);
	}

	[TestMethod]
	public void ConstantsAreCorrectForWindowsEpoch()
	{
		AreEqual(584388, DateTimeExtensions.WindowsEpochDateOnlyDayNumber);
		AreEqual(504911232000000000L, DateTimeExtensions.WindowsEpochDateTimeTicks);

		AreEqual(new DateOnly(1601, 1, 1), DateTimeExtensions.WindowsEpochDateOnly);
		AreEqual(new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTimeExtensions.WindowsEpochDateTime);
		AreEqual(new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimeExtensions.WindowsEpochDateTimeOffset);
	}

	[TestMethod]
	public void MaxDateTimeTicksAndMinDateTimeTicksAreAsExpected()
	{
		AreEqual(3155378975999999999L, DateTimeExtensions.MaxDateTimeTicks);
		AreEqual(0L, DateTimeExtensions.MinDateTimeTicks);

		// These should match the framework limits
		AreEqual(DateTime.MaxValue.Ticks, DateTimeExtensions.MaxDateTimeTicks);
		AreEqual(DateTime.MinValue.Ticks, DateTimeExtensions.MinDateTimeTicks);
	}

	[TestMethod]
	public void ToUtcDateTimeConvertsLocalToUtc()
	{
		var localTime = new DateTime(2025, 4, 11, 10, 30, 0, DateTimeKind.Local);
		var result = localTime.ToUtcDateTime();
		AreEqual(DateTimeKind.Utc, result.Kind);

		// The exact ticks/value will depend on the system's timezone offset, but Kind must be Utc
		IsTrue(result.Kind == DateTimeKind.Utc);
	}

	[TestMethod]
	public void ToUtcDateTimeHandlesMaxValueCorrectly()
	{
		var maxValue = DateTime.MaxValue; // Kind is Unspecified by default
		var result = maxValue.ToUtcDateTime();
		AreEqual(DateTime.MaxValue.Ticks, result.Ticks);
		AreEqual(DateTimeKind.Utc, result.Kind);
	}

	[TestMethod]
	public void ToUtcDateTimeHandlesMinValueCorrectly()
	{
		var minValue = DateTime.MinValue; // Kind is Unspecified by default
		var result = minValue.ToUtcDateTime();
		AreEqual(DateTime.MinValue.Ticks, result.Ticks);
		AreEqual(DateTimeKind.Utc, result.Kind);
	}

	[TestMethod]
	public void ToUtcDateTimePreservesTicksWhenAlreadyUtcOrUnspecified()
	{
		var scenarios = new[]
		{
			new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
			new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Unix epoch
		};

		foreach (var scenario in scenarios)
		{
			var result = scenario.ToUtcDateTime();
			AreEqual(scenario.Ticks, result.Ticks);
			AreEqual(DateTimeKind.Utc, result.Kind);
		}
	}

	[TestMethod]
	public void ToUtcDateTimeReturnsUnchangedWhenKindIsUtc()
	{
		var utcTime = new DateTime(2025, 4, 11, 14, 30, 0, DateTimeKind.Utc);
		var result = utcTime.ToUtcDateTime();
		AreEqual(utcTime, result);
		AreEqual(DateTimeKind.Utc, result.Kind);
	}

	[TestMethod]
	public void ToUtcDateTimeTreatsUnspecifiedAsUtc()
	{
		var unspecifiedTime = new DateTime(2025, 4, 11, 14, 30, 0, DateTimeKind.Unspecified);
		var result = unspecifiedTime.ToUtcDateTime();
		AreEqual(unspecifiedTime.Ticks, result.Ticks);
		AreEqual(DateTimeKind.Utc, result.Kind);
	}

	#endregion
}