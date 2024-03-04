#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class IsoDateTimeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CompareTo()
	{
		#if (NET48)
		var duration = new TimeSpan(1, 2, 3, 4, 5);
		var expectedLess = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5));
		var expected1 = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5));
		var expected2 = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5));
		var expectedMore = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5));
		#else
		var duration = new TimeSpan(1, 2, 3, 4, 5, 6);
		var expectedLess = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5, 5));
		var expected1 = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5, 6));
		var expected2 = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5, 6));
		var expectedMore = new IsoDateTime(StartDateTime, new TimeSpan(1, 2, 3, 4, 5, 7));
		#endif

		AreEqual(1, expected1.CompareTo((object) expectedLess));
		AreEqual(0, expected1.CompareTo((object) expected2));
		AreEqual(-1, expected1.CompareTo((object) expectedMore));
		AreEqual(-1, expected1.CompareTo(1));
		AreEqual(-1, expected1.CompareTo("aoeu"));
		AreEqual(-1, expected1.CompareTo(null));

		expectedLess = new IsoDateTime(StartDateTime.AddTicks(-1), duration);
		expected1 = new IsoDateTime(StartDateTime, duration);
		expected2 = new IsoDateTime(StartDateTime, duration);
		expectedMore = new IsoDateTime(StartDateTime.AddTicks(1), duration);

		AreEqual(1, expected1.CompareTo((object) expectedLess));
		AreEqual(0, expected1.CompareTo((object) expected2));
		AreEqual(-1, expected1.CompareTo((object) expectedMore));
		AreEqual(-1, expected1.CompareTo(1));
		AreEqual(-1, expected1.CompareTo("aoeu"));
		AreEqual(-1, expected1.CompareTo(null));
	}

	[TestMethod]
	public void Equals()
	{
		var iso1 = NewIsoDateTime("2022-08-23T19:13:45.2434501Z", "01:23:45.789");
		var iso2 = NewIsoDateTime("2022-08-23T19:13:45.2434501Z", "01:23:45.789");
		Assert.IsTrue(iso1 == iso2);
		Assert.IsTrue(iso1.Equals(iso2));
		Assert.IsTrue(iso1.Equals((object) iso2));
		Assert.IsTrue(Equals(iso1, iso2));

		// Also not equals 
		Assert.IsFalse(iso1 != iso2);
	}

	[TestMethod]
	public void FromJson()
	{
		var scenarios = new Dictionary<string, IsoDateTime>
		{
			{
				"{ \"Date\": \"2022-08-22T04:00:00-04:00/P8DT6H\" }",
				NewIsoDateTime("2022-08-22T08:00:00.0000000Z", "8.06:00:00")
			},
			{
				"{ \"Date\": \"2022-08-22T00:00:00/P1Y2M8DT6H4M12.456S\" }",
				NewIsoDateTime("2022-08-22T00:00:00Z", "434.06:04:12.456")
			},
			{
				"{ \"Date\": \"2022-08-22T12:34:56/PT6H4M12.456S\" }",
				NewIsoDateTime("2022-08-22T12:34:56Z", "06:04:12.456")
			}
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.Key.FromJson<MyClass>();
			Assert.AreEqual(scenario.Value, actual.Date);
		}
	}

	[TestMethod]
	public void HashCode()
	{
		var iso = NewIsoDateTime("2022-08-23T19:13:45.2434501Z", "01:23:45.789");
		Assert.AreEqual(-8868163, iso.GetHashCode());
	}

	[TestMethod]
	public void ImplicitOperators()
	{
		var iso = NewIsoDateTime("2022-08-23T19:13:45.2434501Z", "01:23:45.789");

		DateTime datetime = iso;
		var expectedDateTime = DateTime.Parse("2022-08-23T19:13:45.2434501Z", null, DateTimeStyles.AdjustToUniversal);
		Assert.AreEqual(expectedDateTime, datetime);

		var timespan = (TimeSpan) iso;
		Assert.AreEqual(TimeSpan.Parse("01:23:45.789"), timespan);

		IsoDateTime isoDateTime = datetime;
		Assert.AreEqual(iso.DateTime, isoDateTime.DateTime);
		Assert.AreEqual(TimeSpan.Zero, isoDateTime.Duration);
	}

	[TestMethod]
	public void IsExpired()
	{
		SetTime("2022-08-23T01:23:45.789Z".ToUtcDateTime());

		var iso = NewIsoDateTime("2022-08-23T00:00:00.000Z", "01:23:45.789");
		iso.ExpiresAfter.ToString("O").Dump();
		Assert.IsFalse(iso.IsExpired(this));

		SetTime("2022-08-23T01:23:45.790Z".ToUtcDateTime());
		iso.ExpiresAfter.ToString("O").Dump();
		Assert.IsTrue(iso.IsExpired(this));
	}

	[TestMethod]
	public void Parse()
	{
		var scenarios = new Dictionary<string, IsoDateTime>
		{
			{
				"2000-01-02T03:04:35.0700000Z/P1DT2H3M4.005S",
				new IsoDateTime(
					new DateTime(2000, 01, 02, 03, 04, 35, 70, DateTimeKind.Utc),
					new TimeSpan(1, 2, 3, 4, 5)
				)
			},
			{
				"2000-01-02T03:04:35.0700000Z",
				new IsoDateTime(
					new DateTime(2000, 01, 02, 03, 04, 35, 70, DateTimeKind.Utc),
					TimeSpan.Zero
				)
			}
		};

		foreach (var scenario in scenarios)
		{
			var actual = IsoDateTime.Parse(scenario.Key);
			AreEqual(scenario.Value, actual);
			IsTrue(IsoDateTime.TryParse(scenario.Key, out actual));
			AreEqual(scenario.Value, actual);
		}

		ExpectedException<ArgumentNullException>(() => IsoDateTime.Parse(null), "Value cannot be null.");
		IsFalse(IsoDateTime.TryParse(null, out _));
		IsFalse(IsoDateTime.TryParse("2000/P1D", out _));
	}

	[TestMethod]
	public void ParseDuration()
	{
		var scenarios = new Dictionary<string, TimeSpan>
		{
			{
				"P1DT2H3M4.5S",
				new TimeSpan(1, 2, 3, 4, 500)
			}
		};

		var start = new DateTime(2000, 1, 1, 0,0,0, DateTimeKind.Utc);

		foreach (var scenario in scenarios)
		{
			var expected = IsoDateTime.ToDuration(start, scenario.Value);
			expected.Dump();
			var actual = IsoDateTime.ParseDuration(start, scenario.Key);
			AreEqual(scenario.Value, actual);
		}
	}

	[TestMethod]
	public void ShouldAcceptNonUtcDateTime()
	{
		var iso = new IsoDateTime { DateTime = new DateTime(2022, 08, 23, 0, 0, 0, DateTimeKind.Unspecified) };
		AreEqual(new DateTime(2022, 08, 23, 0, 0, 0, DateTimeKind.Utc), iso.DateTime);

		iso = new IsoDateTime { DateTime = new DateTime(2022, 08, 23, 10, 37, 54, DateTimeKind.Local) };
		AreEqual(new DateTime(2022, 08, 23, 10, 37, 54, DateTimeKind.Local).ToUniversalTime(), iso.DateTime);

		iso = new IsoDateTime { DateTime = new DateTime(2022, 08, 23, 10, 37, 54, DateTimeKind.Utc) };
		AreEqual(new DateTime(2022, 08, 23, 10, 37, 54, DateTimeKind.Utc), iso.DateTime);
	}

	[TestMethod]
	public void ShouldParseOnlyPeriod()
	{
		var scenarios = new Dictionary<string, (DateTime start, TimeSpan span)>
		{
			{ "P1Y", (DateTime.MinValue, TimeSpan.FromDays(365)) },
			{ "P1M", (DateTime.MinValue, TimeSpan.FromDays(31)) },
			{ "P1D", (DateTime.MinValue, TimeSpan.FromDays(1)) },
			{ "P1Y1M1D", (DateTime.MinValue, TimeSpan.FromDays(397)) },
			{ "PT1H", (DateTime.MinValue, TimeSpan.FromHours(1)) },
			{ "PT1M", (DateTime.MinValue, TimeSpan.FromMinutes(1)) },
			{ "PT1.250S", (DateTime.MinValue, TimeSpan.FromSeconds(1.25)) },
			{ "PT1H1M1.987S", (DateTime.MinValue, TimeSpan.Parse("01:01:01.987")) }
		};

		foreach (var scenario in scenarios)
		{
			var actual = IsoDateTime.ParseDuration(scenario.Value.start, scenario.Key);
			Assert.AreEqual(scenario.Value.span, actual);

			var duration = IsoDateTime.ToDuration(scenario.Value.start, scenario.Value.span);
			Assert.AreEqual(scenario.Key, duration);
		}
	}

	[TestMethod]
	public void ToDuration()
	{
		var scenarios = new Dictionary<string, (DateTime start, TimeSpan span)>
		{
			{ "P1Y", (DateTime.MinValue, TimeSpan.FromDays(365)) },
			{ "P1M", (DateTime.MinValue, TimeSpan.FromDays(31)) },
			{ "P1D", (DateTime.MinValue, TimeSpan.FromDays(1)) },
			{ "P1Y1M1D", (DateTime.MinValue, TimeSpan.FromDays(397)) },
			{ "P1Y8M29DT23H59M57.456S", (DateTime.Parse("2022-08-23T19:13:45.2434501Z"), TimeSpan.Parse("637.23:59:57.456")) },
			{ "PT1H", (DateTime.MinValue, TimeSpan.FromHours(1)) },
			{ "PT1M", (DateTime.MinValue, TimeSpan.FromMinutes(1)) },
			{ "PT1H20M", (DateTime.MinValue, TimeSpan.FromMinutes(80)) },
			{ "PT1.250S", (DateTime.MinValue, TimeSpan.FromSeconds(1.25)) },
			{ "PT1H1M1.987S", (DateTime.MinValue, TimeSpan.Parse("01:01:01.987")) },
			{ "PT59.999S", (DateTime.MinValue, TimeSpan.Parse("00:00:59.999")) }
		};

		foreach (var scenario in scenarios)
		{
			var actual = IsoDateTime.ToDuration(scenario.Value.start, scenario.Value.span);
			Assert.AreEqual(scenario.Key, actual);
		}
	}

	[TestMethod]
	public void ToStringShouldWork()
	{
		var scenarios = new Dictionary<string, IsoDateTime>
		{
			{
				"2022-08-22T08:00:00.0000000Z/P8DT6H",
				NewIsoDateTime("2022-08-22T08:00:00.0000000Z", "8.06:00:00")
			},
			{
				"2022-08-22T00:00:00.0000000Z/P1Y2M8DT6H4M12.456S",
				NewIsoDateTime("2022-08-22T00:00:00Z", "434.06:04:12.456")
			},
			{
				"2022-08-22T12:34:56.0000000Z/PT6H4M12.456S",
				NewIsoDateTime("2022-08-22T12:34:56Z", "06:04:12.456")
			}
		};

		foreach (var scenario in scenarios)
		{
			Assert.AreEqual(scenario.Key, scenario.Value.ToString());
		}
	}

	private IsoDateTime NewIsoDateTime(string datetime, string duration)
	{
		return new IsoDateTime
		{
			DateTime = datetime.ToUtcDateTime(),
			Duration = TimeSpan.Parse(duration)
		};
	}

	#endregion

	#region Classes

	public class MyClass
	{
		#region Properties

		public IsoDateTime Date { get; set; }

		#endregion
	}

	#endregion
}