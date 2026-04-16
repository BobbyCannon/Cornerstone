#region References

using System;
using Cornerstone.Data.Times;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class StringFormatterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetUnitName()
	{
		var scenarios = new (TimeUnit Unit, double Value, WordFormat Format, string Expected)[]
		{
			(TimeUnit.Nanosecond, 1, WordFormat.Abbreviation, "ns"),
			(TimeUnit.Nanosecond, 1, WordFormat.Full, "Nanosecond"),
			(TimeUnit.Nanosecond, 2, WordFormat.Full, "Nanoseconds"),
			(TimeUnit.Ticks, 1, WordFormat.Abbreviation, "t"),
			(TimeUnit.Ticks, 1, WordFormat.Full, "Ticks"),
			(TimeUnit.Ticks, 2, WordFormat.Full, "Ticks"),
			(TimeUnit.Microsecond, 1, WordFormat.Abbreviation, "µs"),
			(TimeUnit.Microsecond, 1, WordFormat.Full, "Microsecond"),
			(TimeUnit.Microsecond, 2, WordFormat.Full, "Microseconds"),
			(TimeUnit.Millisecond, 1, WordFormat.Abbreviation, "ms"),
			(TimeUnit.Millisecond, 1, WordFormat.Full, "Millisecond"),
			(TimeUnit.Millisecond, 2, WordFormat.Full, "Milliseconds"),
			(TimeUnit.Second, 1, WordFormat.Abbreviation, "s"),
			(TimeUnit.Second, 1, WordFormat.Full, "Second"),
			(TimeUnit.Second, 2, WordFormat.Full, "Seconds"),
			(TimeUnit.Minute, 1, WordFormat.Abbreviation, "m"),
			(TimeUnit.Minute, 1, WordFormat.Full, "Minute"),
			(TimeUnit.Minute, 2, WordFormat.Full, "Minutes"),
			(TimeUnit.Hour, 1, WordFormat.Abbreviation, "h"),
			(TimeUnit.Hour, 1, WordFormat.Full, "Hour"),
			(TimeUnit.Hour, 2, WordFormat.Full, "Hours"),
			(TimeUnit.Day, 1, WordFormat.Abbreviation, "d"),
			(TimeUnit.Day, 1, WordFormat.Full, "Day"),
			(TimeUnit.Day, 2, WordFormat.Full, "Days"),
			(TimeUnit.Week, 1, WordFormat.Abbreviation, "w"),
			(TimeUnit.Week, 1, WordFormat.Full, "Week"),
			(TimeUnit.Week, 2, WordFormat.Full, "Weeks"),
			(TimeUnit.Month, 1, WordFormat.Abbreviation, "mth"),
			(TimeUnit.Month, 1, WordFormat.Full, "Month"),
			(TimeUnit.Month, 2, WordFormat.Full, "Months"),
			(TimeUnit.Year, 1, WordFormat.Abbreviation, "y"),
			(TimeUnit.Year, 1, WordFormat.Full, "Year"),
			(TimeUnit.Year, 2, WordFormat.Full, "Years")
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Expected, StringFormatter.GetUnitName(scenario.Unit, scenario.Value, scenario.Format));
		}
	}

	[TestMethod]
	public void HumanizeForTimeSpan()
	{
		var scenarios = new (TimeSpan Value, IHumanizeSettings Settings, string Expected)[]
		{
			(TimeSpan.Zero, null, "0"),
			(new TimeSpan(0, 0, 0), null, "0"),
			(TimeSpan.FromTicks(1), null, "100 ns"),
			(new TimeSpan(0, 0, 0, 0, 0, 1), null, "1 µs"),
			(new TimeSpan(0, 0, 0, 0, 1), null, "1 ms"),
			(new TimeSpan(0, 0, 1), null, "1 s"),
			(new TimeSpan(0, 1, 0), null, "1 m"),
			(new TimeSpan(1, 0, 0), null, "1 h"),
			(new TimeSpan(1, 0, 0, 0), null, "1 d"),
			(new TimeSpan(7, 0, 0, 0), null, "1 w"),
			(new TimeSpan(30, 0, 0, 0), null, "1 mth"),
			(new TimeSpan(365, 0, 0, 0), null, "1 y"),
			(new TimeSpan(37, 0, 0, 0), new HumanizeSettings { MaxUnitSegments = 2 }, "1 mth 1 w"),
			(new TimeSpan(366, 0, 0, 0), new HumanizeSettings { MaxUnitSegments = 2 }, "1 y 1 d"),
			(TimeSpan.FromTicks(1), new HumanizeSettings { WordFormat = WordFormat.Full }, "100 Nanoseconds"),
			(new TimeSpan(0, 0, 0, 0, 0, 1), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Microsecond"),
			(new TimeSpan(0, 0, 0, 0, 1), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Millisecond"),
			(new TimeSpan(0, 0, 1), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Second"),
			(new TimeSpan(0, 1, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Minute"),
			(new TimeSpan(1, 0, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Hour"),
			(new TimeSpan(1, 0, 0, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Day"),
			(new TimeSpan(7, 0, 0, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Week"),
			(new TimeSpan(30, 0, 0, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Month"),
			(new TimeSpan(365, 0, 0, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Year"),
			(new TimeSpan(366, 0, 0, 0), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Year"),
			(new TimeSpan(0, 0, 0, 0, 1), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Millisecond"),
			(new TimeSpan(0, 0, 0, 0, 0, 1), new HumanizeSettings { WordFormat = WordFormat.Full }, "1 Microsecond"),
			(new TimeSpan(1, 0, 0, 0, 5), new HumanizeSettings { WordFormat = WordFormat.Full, MaxUnitSegments = 2 }, "1 Day and 5 Milliseconds"),
			(new TimeSpan(1, 2, 3), new HumanizeSettings { WordFormat = WordFormat.Full, MaxUnitSegments = 3 }, "1 Hour, 2 Minutes, and 3 Seconds"),
			(new TimeSpan(1, 2, 3), new HumanizeSettings { WordFormat = WordFormat.Full, MaxUnitSegments = 3, MinUnit = TimeUnit.Minute }, "1 Hour and 2 Minutes"),
			(new TimeSpan(1, 2, 3), new HumanizeSettings { WordFormat = WordFormat.Full, MaxUnitSegments = 2, MaxUnit = TimeUnit.Minute }, "62 Minutes and 3 Seconds")
		};

		foreach (var scenario in scenarios)
		{
			scenario.Value.ToString().Dump();
			AreEqual(scenario.Expected, scenario.Value.Humanize(scenario.Settings));
		}
	}

	[TestMethod]
	public void HumanizeForTimeSpanUnusualSettings()
	{
		var scenarios = new (TimeSpan Value, IHumanizeSettings Settings, string Expected)[]
		{
			(TimeSpan.FromDays(1), new HumanizeSettings { MaxUnit = TimeUnit.Ticks, MinUnit = TimeUnit.Ticks }, "")
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Expected, scenario.Value.Humanize(scenario.Settings));
		}
	}

	[TestMethod]
	public void MaxUnit()
	{
		var scenarios = new (TimeSpan Value, TimeUnit? MinAllowed, TimeUnit Expected)[]
		{
			(new TimeSpan(0, 0, 0, 0, 0, 0), null, TimeUnit.Nanosecond),
			(TimeSpan.FromTicks(TimeSpan.TicksPerMicrosecond - 1), null, TimeUnit.Nanosecond),
			(new TimeSpan(0, 0, 0, 0, 0, 1), null, TimeUnit.Microsecond),
			(new TimeSpan(0, 0, 0, 0, 1, 0), null, TimeUnit.Millisecond),
			(new TimeSpan(0, 0, 0, 1, 0, 0), null, TimeUnit.Second),
			(new TimeSpan(0, 0, 1, 0, 0, 0), null, TimeUnit.Minute),
			(new TimeSpan(0, 1, 0, 0, 0, 0), null, TimeUnit.Hour),
			(new TimeSpan(1, 0, 0, 0, 0, 0), null, TimeUnit.Day),
			(new TimeSpan(7, 0, 0, 0, 0, 0), null, TimeUnit.Week),
			(new TimeSpan(30, 0, 0, 0, 0, 0), null, TimeUnit.Month),
			(new TimeSpan(365, 0, 0, 0, 0, 0), null, TimeUnit.Year),
			(new TimeSpan(0, 0, 0, 0, 0, -1), null, TimeUnit.Microsecond),
			(new TimeSpan(0, 0, 0, 0, -1, 0), null, TimeUnit.Millisecond),
			(new TimeSpan(0, 0, 0, -1, 0, 0), null, TimeUnit.Second),
			(new TimeSpan(0, 0, -1, 0, 0, 0), null, TimeUnit.Minute),
			(new TimeSpan(0, -1, 0, 0, 0, 0), null, TimeUnit.Hour),
			(new TimeSpan(-1, 0, 0, 0, 0, 0), null, TimeUnit.Day),
			(new TimeSpan(-7, 0, 0, 0, 0, 0), null, TimeUnit.Week),
			(new TimeSpan(-30, 0, 0, 0, 0, 0), null, TimeUnit.Month),
			(new TimeSpan(-365, 0, 0, 0, 0, 0), null, TimeUnit.Year)
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Expected, StringFormatter.MaxUnit(scenario.Value));
		}
	}

	[TestMethod]
	public void MinUnit()
	{
		var scenarios = new (TimeSpan Value, TimeUnit Expected)[]
		{
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Nanosecond),
			(TimeSpan.FromTicks(TimeSpan.TicksPerMicrosecond - 1), TimeUnit.Nanosecond),
			(new TimeSpan(0, 0, 0, 0, 0, 1), TimeUnit.Microsecond),
			(new TimeSpan(0, 0, 0, 0, 1, 0), TimeUnit.Millisecond),
			(new TimeSpan(0, 0, 0, 1, 0, 0), TimeUnit.Second),
			(new TimeSpan(0, 0, 1, 0, 0, 0), TimeUnit.Minute),
			(new TimeSpan(0, 1, 0, 0, 0, 0), TimeUnit.Hour),
			(new TimeSpan(1, 0, 0, 0, 0, 0), TimeUnit.Day),
			(new TimeSpan(7, 0, 0, 0, 0, 0), TimeUnit.Week),
			(new TimeSpan(8, 0, 0, 0, 0, 0), TimeUnit.Day),
			(new TimeSpan(30, 0, 0, 0, 0, 0), TimeUnit.Month),
			(new TimeSpan(31, 0, 0, 0, 0, 0), TimeUnit.Day),
			(new TimeSpan(365, 0, 0, 0, 0, 0), TimeUnit.Year),
			(new TimeSpan(366, 0, 0, 0, 0, 0), TimeUnit.Day)
		};

		foreach (var scenario in scenarios)
		{
			scenario.Expected.Dump();
			AreEqual(scenario.Expected, StringFormatter.MinUnit(scenario.Value));
		}
	}

	[TestMethod]
	public void MinUnitWithMinAllowed()
	{
		var scenarios = new (TimeSpan Value, TimeUnit MinAllowed, TimeUnit Expected)[]
		{
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Microsecond, TimeUnit.Microsecond),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Millisecond, TimeUnit.Millisecond),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Second, TimeUnit.Second),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Minute, TimeUnit.Minute),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Hour, TimeUnit.Hour),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Day, TimeUnit.Day),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Week, TimeUnit.Week),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Month, TimeUnit.Month),
			(new TimeSpan(0, 0, 0, 0, 0, 0), TimeUnit.Year, TimeUnit.Year),
			(TimeSpan.FromTicks(-1), TimeUnit.Microsecond, TimeUnit.Microsecond),
			(TimeSpan.FromTicks(-1), TimeUnit.Millisecond, TimeUnit.Millisecond),
			(TimeSpan.FromTicks(-1), TimeUnit.Second, TimeUnit.Second),
			(TimeSpan.FromTicks(-1), TimeUnit.Minute, TimeUnit.Minute),
			(TimeSpan.FromTicks(-1), TimeUnit.Hour, TimeUnit.Hour),
			(TimeSpan.FromTicks(-1), TimeUnit.Day, TimeUnit.Day),
			(TimeSpan.FromTicks(-1), TimeUnit.Week, TimeUnit.Week),
			(TimeSpan.FromTicks(-1), TimeUnit.Month, TimeUnit.Month),
			(TimeSpan.FromTicks(-1), TimeUnit.Year, TimeUnit.Year)
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Expected, StringFormatter.MinUnit(scenario.Value, scenario.MinAllowed));
		}
	}

	#endregion
}