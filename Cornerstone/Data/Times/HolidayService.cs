#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Data.Times;

public static class HolidayService
{
	#region Fields

	private static readonly ConcurrentDictionary<int, HolidayValue[]> _cache;

	#endregion

	#region Constructors

	static HolidayService()
	{
		_cache = new ConcurrentDictionary<int, HolidayValue[]>();
	}

	#endregion

	#region Methods

	public static DateTime AdjustForWeekendHoliday(DateTime holiday)
	{
		if (holiday.DayOfWeek == DayOfWeek.Saturday)
		{
			return holiday.AddDays(-1);
		}
		if (holiday.DayOfWeek == DayOfWeek.Sunday)
		{
			return holiday.AddDays(1);
		}
		return holiday;
	}

	public static HolidayValue[] GetHolidays(int year)
	{
		return _cache.GetOrAdd(year, x =>
		{
			var holidays = new List<HolidayValue>();

			// New Years
			var newYearsDate = new DateTime(x, 1, 1, 0, 0, 0, DateTimeKind.Local);
			var inLieuNewYearsDate = AdjustForWeekendHoliday(newYearsDate);
			holidays.Add(new HolidayValue(Holiday.NewYearsDay, newYearsDate, inLieuNewYearsDate));

			var easter = GetEasterSunday(x);
			holidays.Add(new HolidayValue(Holiday.Easter, easter));

			// Good Friday -- The friday before the first Sunday after the full Moon that occurs on or after the spring equinox
			var goodFriday = easter;
			var dayOfWeek = goodFriday.DayOfWeek;
			while (dayOfWeek != DayOfWeek.Friday)
			{
				goodFriday = goodFriday.AddDays(-1);
				dayOfWeek = goodFriday.DayOfWeek;
			}
			holidays.Add(new HolidayValue(Holiday.GoodFriday, goodFriday));

			// Labour Day -- 1st Monday in September 
			var labourDay = new DateTime(x, 9, 1);
			dayOfWeek = labourDay.DayOfWeek;
			while (dayOfWeek != DayOfWeek.Monday)
			{
				labourDay = labourDay.AddDays(1);
				dayOfWeek = labourDay.DayOfWeek;
			}
			holidays.Add(new HolidayValue(Holiday.LaborDay, labourDay));

			// Thanksgiving Day
			var first = new DateTime(x, 11, 1);
			var firstDayOfWeek = (int) first.DayOfWeek;
			holidays.Add(new HolidayValue(Holiday.ThanksgivingDay, new DateTime(x, 11, 22 + ((11 - firstDayOfWeek) % 7))));

			// Christmas Day 
			var christmasDay = new DateTime(x, 12, 25);
			var christmasDayOffset = AdjustForWeekendHoliday(christmasDay);
			holidays.Add(new HolidayValue(Holiday.ChristmasDay, christmasDay, christmasDayOffset));

			return holidays.ToArray();
		});
	}

	public static DateTime GetNextHoliday(DateTime currentTime, Holiday holiday)
	{
		var holidayThisYear = GetHolidays(currentTime.Year).FirstOrDefault(x => x.Holiday == holiday);
		if (currentTime.Date <= holidayThisYear?.Date)
		{
			return holidayThisYear.Date;
		}

		var holidayNextYear = GetHolidays(currentTime.Year + 1).FirstOrDefault(x => x.Holiday == holiday);
		return holidayNextYear?.Date ?? DateTime.MinValue;
	}

	private static int GetCurrentCenturyLeapYears(int year)
	{
		var leapCounter = 0;
		var century = System.Convert.ToInt32(Math.Floor(year / 100d)) * 100;
		for (var i = century; i <= year; i++)
		{
			if ((((i % 4) == 0) && ((i % 100) != 0)) || (((i % 400) == 0) && ((i % 100) == 0)))
			{
				leapCounter++;
			}
		}
		return leapCounter;
	}

	private static DateTime GetEasterSunday(int year)
	{
		var springEquinox = GetSpringEquinox(year);
		var moon = new Moon();
		var foundDate = moon.GetNextFullMoon(springEquinox);
		var dayOfWeek = foundDate.DayOfWeek;
		while (dayOfWeek != DayOfWeek.Sunday)
		{
			foundDate = foundDate.AddDays(1);
			dayOfWeek = foundDate.DayOfWeek;
		}
		return foundDate;
	}

	private static DateTime GetSpringEquinox(int year)
	{
		var lastTwoDigits = year % 100;
		var calculatedDate = (int) (((lastTwoDigits * 0.2422) + 20.646) - GetCurrentCenturyLeapYears(year));
		var foundDate = new DateTime(year, 3, calculatedDate);
		return foundDate;
	}

	#endregion
}