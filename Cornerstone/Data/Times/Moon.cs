#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Data.Times;

public class Moon
{
	#region Constants

	private const double MoonCycleLength = 29.53058770576;

	#endregion

	#region Constructors

	public Moon()
	{
		GetMoonPhases =
		[
			new MoonPhase(0, 1, MoonPhase.Phase.NewMoon),
			new MoonPhase((MoonCycleLength / 4) - 1, (MoonCycleLength / 4) + 1, MoonPhase.Phase.FirstQuarter),
			new MoonPhase((MoonCycleLength / 2) - 1, (MoonCycleLength / 2) + 1, MoonPhase.Phase.FullMoon),
			new MoonPhase((MoonCycleLength * .75) - 1, (MoonCycleLength * .75) + 1, MoonPhase.Phase.LastQuarter),
			new MoonPhase(MoonCycleLength - 1, MoonCycleLength, MoonPhase.Phase.NewMoon)
		];
	}

	#endregion

	#region Properties

	public List<MoonPhase> GetMoonPhases { get; }

	#endregion

	#region Methods

	public int GetJulianDate(int day, int month, int year)
	{
		year = year - ((12 - month) / 10);

		month = month + 9;

		if (month >= 12)
		{
			month = month - 12;
		}

		var k1 = (int) (365.25 * (year + 4712));
		var k2 = (int) ((30.6001 * month) + 0.5);

		// 'j' for dates in Julian calendar:
		var julianDate = k1 + k2 + day + 59;

		//Gregorian calendar
		if (julianDate > 2299160)
		{
			var k3 = (int) (((year / 100) + 49) * 0.75) - 38;
			julianDate = julianDate - k3; //at 12h UT (Universal Time)
		}

		return julianDate;
	}

	public double GetMoonAge(DateTime fromDate)
	{
		var day = fromDate.Day;
		var month = fromDate.Month;
		var year = fromDate.Year;
		double ip, age;

		var julianDate = GetJulianDate(day, month, year);

		ip = (julianDate + 4.867) / MoonCycleLength;
		ip = ip - Math.Floor(ip);

		age = (ip * MoonCycleLength) + (MoonCycleLength / 2);

		if (age > MoonCycleLength)
		{
			age -= MoonCycleLength;
		}

		return age;
	}

	public DateTime GetNextFullMoon(DateTime fromDate)
	{
		var foundDate = fromDate;
		var start = GetMoonPhases.FirstOrDefault(x => x.PhaseOfMoon == MoonPhase.Phase.FullMoon)?.FromAge ?? 0;
		var end = GetMoonPhases.FirstOrDefault(x => x.PhaseOfMoon == MoonPhase.Phase.FullMoon)?.ToAge ?? 0;

		while (!IsBetween(GetMoonAge(foundDate), start, end))
		{
			foundDate = foundDate.AddDays(1);
		}

		return foundDate;
	}

	private static bool IsBetween(double item, double start, double end)
	{
		return (Comparer<double>.Default.Compare(item, start) >= 0)
			&& (Comparer<double>.Default.Compare(item, end) <= 0);
	}

	#endregion

	#region Classes

	public class MoonPhase
	{
		#region Fields

		public double FromAge;
		public Phase PhaseOfMoon;
		public double ToAge;

		#endregion

		#region Constructors

		public MoonPhase(double fromAge, double toAge, Phase phase)
		{
			FromAge = fromAge;
			ToAge = toAge;
			PhaseOfMoon = phase;
		}

		#endregion

		#region Enumerations

		public enum Phase
		{
			NewMoon,
			FirstQuarter,
			FullMoon,
			LastQuarter
		}

		#endregion
	}

	#endregion
}