#region References

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cornerstone.Data.Times;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text;

public static class StringFormatter
{
	#region Constants

	public const string DisplayAttributeTypeFullName = "System.ComponentModel.DataAnnotations.DisplayAttribute";

	#endregion

	#region Fields

	private static readonly IList<TimeUnit> _timeUnitDescending;
	private static readonly ImmutableDictionary<TimeUnit, EnumDetails> _timeUnitNames;

	#endregion

	#region Constructors

	static StringFormatter()
	{
		_timeUnitNames = SourceReflector.GetEnumDetailsDictionary<TimeUnit>();
		_timeUnitDescending = SourceReflector.GetEnumValues<TimeUnit>().AsEnumerable().Reverse().ToList();
	}

	#endregion

	#region Methods

	public static string GetUnitName(TimeUnit unit, double value, WordFormat wordFormat)
	{
		var details = _timeUnitNames[unit];

		if (unit == TimeUnit.Ticks)
		{
			return (wordFormat == WordFormat.Full
					? details.DisplayName
					: details.DisplayShortName)
				?? details.Name;
		}

		return wordFormat == WordFormat.Abbreviation
			? details.DisplayShortName ?? details.Name
			: value > 1
				? details.DisplayName + "s"
				: details.DisplayName;
	}

	/// <summary>
	/// Convert the timespan to a human string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <param name="settings"> The settings for conversion. </param>
	/// <returns> The human formatted string. </returns>
	public static string Humanize(this TimeSpan value, IHumanizeSettings settings = null)
	{
		if (value == TimeSpan.Zero)
		{
			return "0";
		}

		settings ??= new HumanizeSettings();

		var valueMaxUnit = MaxUnit(value, settings.MinUnit, settings.MaxUnit);
		var valueMinUnit = MinUnit(value, settings.MinUnit);
		var sections = new List<string>();
		var precisionFormat = "{0:0}";
		var remaining = value;

		foreach (var unit in _timeUnitDescending)
		{
			if ((unit < valueMinUnit) || (unit > valueMaxUnit))
			{
				continue;
			}

			long count = 0;
			switch (unit)
			{
				case TimeUnit.Year:
				{
					//count = (long) Math.Floor(remaining.TotalDays / 365.25);
					count = (long) Math.Floor(remaining.TotalDays / 365);
					remaining -= TimeSpan.FromDays(count * 365);
					break;
				}
				case TimeUnit.Month:
				{
					//count = (long) Math.Floor(remaining.TotalDays / 30.4375);
					count = (long) Math.Floor(remaining.TotalDays / 30);
					remaining -= TimeSpan.FromDays(count * 30);
					break;
				}
				case TimeUnit.Week:
				{
					count = (long) Math.Floor(remaining.TotalDays / 7);
					remaining -= TimeSpan.FromDays(count * 7);
					break;
				}
				case TimeUnit.Day:
				{
					count = (long) remaining.TotalDays;
					remaining -= TimeSpan.FromDays(count);
					break;
				}
				case TimeUnit.Hour:
				{
					count = (long) remaining.TotalHours;
					remaining -= TimeSpan.FromHours(count);
					break;
				}
				case TimeUnit.Minute:
				{
					count = (long) remaining.TotalMinutes;
					remaining -= TimeSpan.FromMinutes(count);
					break;
				}
				case TimeUnit.Second:
				{
					count = (long) remaining.TotalSeconds;
					remaining -= TimeSpan.FromSeconds(count);
					break;
				}
				case TimeUnit.Millisecond:
				{
					count = (long) remaining.TotalMilliseconds;
					remaining -= TimeSpan.FromMilliseconds(count);
					break;
				}
				case TimeUnit.Microsecond:
				{
					count = (long) remaining.TotalMicroseconds;
					remaining -= TimeSpan.FromMicroseconds(count);
					break;
				}
				case TimeUnit.Nanosecond:
				{
					count = (long) remaining.TotalNanoseconds;
					remaining -= new TimeSpan(count / TimeSpan.NanosecondsPerTick);
					break;
				}
			}

			if (count > 0)
			{
				var total = string.Format(precisionFormat, count);
				sections.Add($"{total} {GetUnitName(unit, count, settings.WordFormat)}");
			}

			if ((sections.Count >= settings.MaxUnitSegments)
				|| (remaining <= TimeSpan.Zero))
			{
				break;
			}
		}

		if (settings.WordFormat == WordFormat.Abbreviation)
		{
			return string.Join(" ", sections);
		}

		if (sections.Count == 1)
		{
			return sections[0];
		}

		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;

		try
		{
			var n = sections.Count;

			// All items except the last two
			for (var i = 0; i < (n - 2); i++)
			{
				builder.Append(sections[i]).Append(", ");
			}

			// Last two items (handles n == 2 correctly)
			if (n > 2)
			{
				builder.Append(sections[n - 2])
					.Append(", and ")
					.Append(sections[n - 1]);
			}
			else
			{
				builder.Append(sections[0])
					.Append(" and ")
					.Append(sections[1]);
			}

			return builder.ToString();
		}
		finally
		{
			StringBuilderPool.Return(builder);
		}
	}

	public static TimeUnit MaxUnit(TimeSpan value, TimeUnit minAllowed = TimeUnit.Nanosecond, TimeUnit maxAllowed = TimeUnit.Year)
	{
		if (value == TimeSpan.Zero)
		{
			return minAllowed;
		}

		// Normalize to positive duration (most common use-case)
		long days;
		int hours, minutes, seconds, millis, micros, nanos;

		if (value < TimeSpan.Zero)
		{
			var abs = value.Negate();
			days = abs.Days;
			hours = abs.Hours;
			minutes = abs.Minutes;
			seconds = abs.Seconds;
			millis = abs.Milliseconds;
			micros = abs.Microseconds;
			nanos = abs.Nanoseconds;
		}
		else
		{
			days = value.Days;
			hours = value.Hours;
			minutes = value.Minutes;
			seconds = value.Seconds;
			millis = value.Milliseconds;
			micros = value.Microseconds;
			nanos = value.Nanoseconds;
		}

		return maxAllowed switch
		{
			// From largest to smallest - return first match
			>= TimeUnit.Year when (minAllowed <= TimeUnit.Year) && (days >= 365) => TimeUnit.Year,
			>= TimeUnit.Month when (minAllowed <= TimeUnit.Month) && (days >= 30) => TimeUnit.Month,
			>= TimeUnit.Week when (minAllowed <= TimeUnit.Week) && (days >= 7) => TimeUnit.Week,
			>= TimeUnit.Day when (minAllowed <= TimeUnit.Day) && (days > 0) => TimeUnit.Day,
			>= TimeUnit.Hour when (minAllowed <= TimeUnit.Hour) && (hours > 0) => TimeUnit.Hour,
			>= TimeUnit.Minute when (minAllowed <= TimeUnit.Minute) && (minutes > 0) => TimeUnit.Minute,
			>= TimeUnit.Second when (minAllowed <= TimeUnit.Second) && (seconds > 0) => TimeUnit.Second,
			>= TimeUnit.Millisecond when (minAllowed <= TimeUnit.Millisecond) && (millis > 0) => TimeUnit.Millisecond,
			>= TimeUnit.Microsecond when (minAllowed <= TimeUnit.Microsecond) && (micros > 0) => TimeUnit.Microsecond,
			>= TimeUnit.Nanosecond when (minAllowed <= TimeUnit.Nanosecond) && (nanos > 0) => TimeUnit.Nanosecond,
			_ => minAllowed
		};
	}

	public static TimeUnit MinUnit(TimeSpan value, TimeUnit minAllowed = TimeUnit.Nanosecond)
	{
		if (value == TimeSpan.Zero)
		{
			return minAllowed;
		}

		var ts = value.Duration();

		// Early returns for sub-day precision (unchanged, but using Duration)
		if ((minAllowed <= TimeUnit.Nanosecond) && (ts.Nanoseconds > 0))
		{
			return TimeUnit.Nanosecond;
		}
		if ((minAllowed <= TimeUnit.Microsecond) && (ts.Microseconds > 0))
		{
			return TimeUnit.Microsecond;
		}
		if ((minAllowed <= TimeUnit.Millisecond) && (ts.Milliseconds > 0))
		{
			return TimeUnit.Millisecond;
		}
		if ((minAllowed <= TimeUnit.Second) && (ts.Seconds > 0))
		{
			return TimeUnit.Second;
		}
		if ((minAllowed <= TimeUnit.Minute) && (ts.Minutes > 0))
		{
			return TimeUnit.Minute;
		}
		if ((minAllowed <= TimeUnit.Hour) && (ts.Hours > 0))
		{
			return TimeUnit.Hour;
		}

		var days = ts.Days;

		// Ordered from largest to smallest compound unit
		// We only promote if it's an **exact** multiple AND the unit is allowed
		if ((minAllowed <= TimeUnit.Year) && (days >= 365) && ((days % 365) == 0))
		{
			return TimeUnit.Year;
		}
		if ((minAllowed <= TimeUnit.Month) && (days >= 30) && ((days % 30) == 0))
		{
			return TimeUnit.Month;
		}
		if ((minAllowed <= TimeUnit.Week) && (days >= 7) && ((days % 7) == 0))
		{
			return TimeUnit.Week;
		}
		if ((minAllowed <= TimeUnit.Day) && (days > 0))
		{
			return TimeUnit.Day;
		}

		// Technically will never get hit?
		return minAllowed;
	}

	#endregion
}