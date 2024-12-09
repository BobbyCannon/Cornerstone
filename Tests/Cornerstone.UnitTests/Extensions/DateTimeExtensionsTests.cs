#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class DateTimeExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToDateOnly()
	{
		AreEqual(new DateOnly(2024, 01, 02), new DateTime(2024, 01, 02, 03, 04, 05, 06, DateTimeKind.Utc).ToDateOnly());
	}

	[TestMethod]
	public void ToUtcDateTime()
	{
		var scenarios = new Dictionary<string, DateTime>
		{
			{ "9999-12-31 23:59:59.9999999", DateTime.MaxValue },
			{ "9999-12-31 23:59:59.9999999+00:00", DateTime.MaxValue },
			{ "9999-12-31 23:59:59.9999999-00:00", DateTime.MaxValue },
			{ "9999-12-31T23:59:59.9999999", DateTime.MaxValue },
			{ "9999-12-31T23:59:59.9999999+00:00", DateTime.MaxValue },
			{ "9999-12-31T23:59:59.9999999-00:00", DateTime.MaxValue },
			{ "9999-12-31T23:59:59.9999999Z", DateTime.MaxValue },
			{ "0001-01-01T12:00:00", DateTime.MinValue },
			{ "0001-01-01T12:00:00+00:00", DateTime.MinValue },
			{ "0001-01-01T12:00:00-00:00", DateTime.MinValue },
			{ "0001-01-01 00:00:00", DateTime.MinValue },
			{ "0001-01-01 00:00:00+00:00", DateTime.MinValue },
			{ "0001-01-01 00:00:00-00:00", DateTime.MinValue },
			{ "0001-01-01T00:00:00", DateTime.MinValue },
			{ "0001-01-01T00:00:00+00:00", DateTime.MinValue },
			{ "0001-01-01T00:00:00-00:00", DateTime.MinValue },
			{ "2024-06-12 06:36:45", new DateTime(2024, 06, 12, 06, 36, 45, DateTimeKind.Utc) },
			{ "2024-06-12 06:36:45 AM", new DateTime(2024, 06, 12, 06, 36, 45, DateTimeKind.Utc) },
			{ "2024-06-12 06:36:45 PM", new DateTime(2024, 06, 12, 18, 36, 45, DateTimeKind.Utc) },
			{ "2024-06-12T06:36:45", new DateTime(2024, 06, 12, 06, 36, 45, DateTimeKind.Utc) },
			{ "2024-06-12T06:36:45+00:00", new DateTime(2024, 06, 12, 06, 36, 45, DateTimeKind.Utc) },
			{ "2024-06-12T06:36:45-00:00", new DateTime(2024, 06, 12, 06, 36, 45, DateTimeKind.Utc) },
			{ "2024-06-12T06:36:45Z", new DateTime(2024, 06, 12, 06, 36, 45, DateTimeKind.Utc) }
		};

		foreach (var scenario in scenarios)
		{
			var actual = DetailedTimer.Benchmark(scenario.Key.ToUtcDateTime, out var elapsed);
			AreEqual(scenario.Value, actual);
			elapsed.Dump();
		}
	}

	#endregion
}