#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data.Bytes;
using Cornerstone.Data.Times;
using Cornerstone.Extensions;
using Cornerstone.Generators;

#endregion

namespace Cornerstone.Text.Human;

/// <summary>
/// A string formatter.
/// </summary>
/// <remarks>
/// Inspiration for some features come from the following projects:
/// https://github.com/Humanizr/Humanizer
/// </remarks>
public static class StringFormatter
{
	#region Fields

	private static readonly Dictionary<TimeUnit, Func<TimeSpan, bool>> _timeSpanUnitMaxChecks;
	private static readonly Dictionary<TimeUnit, Func<TimeSpan, bool>> _timeSpanUnitMinChecks;
	private static readonly IList<TimeUnit> _timeUnitAscending;
	private static readonly IList<TimeUnit> _timeUnitDescending;
	private static readonly IReadOnlyDictionary<TimeUnit, EnumExtensions.EnumDetails> _timeUnitNames;

	#endregion

	#region Constructors

	static StringFormatter()
	{
		_timeUnitNames = EnumExtensions.GetAllEnumDetails<TimeUnit>();
		_timeUnitAscending = EnumExtensions.GetEnumValues<TimeUnit>().ToList();
		_timeUnitDescending = _timeUnitAscending.Reverse().ToList();
		_timeSpanUnitMaxChecks = new Dictionary<TimeUnit, Func<TimeSpan, bool>>
		{
			{ TimeUnit.Year, x => x.Days >= 365 },
			{ TimeUnit.Day, x => x.Days > 0 },
			{ TimeUnit.Hour, x => x.Hours > 0 },
			{ TimeUnit.Minute, x => x.Minutes > 0 },
			{ TimeUnit.Second, x => x.Seconds > 0 },
			{ TimeUnit.Millisecond, x => x.Milliseconds > 0 },
			#if NET7_0_OR_GREATER
			{ TimeUnit.Microsecond, x => x.Microseconds > 0 },
			{ TimeUnit.Nanosecond, x => x.Nanoseconds > 0 }
			#endif
		};
		_timeSpanUnitMinChecks = new Dictionary<TimeUnit, Func<TimeSpan, bool>>
		{
			{ TimeUnit.Year, x => true },
			{ TimeUnit.Day, x => x.Days < 365 },
			{ TimeUnit.Hour, x => x.Hours < 24 },
			{ TimeUnit.Minute, x => x.Minutes < 60 },
			{ TimeUnit.Second, x => x.Seconds < 60 },
			{ TimeUnit.Millisecond, x => x.Milliseconds < 1000 },
			#if NET7_0_OR_GREATER
			{ TimeUnit.Microsecond, x => x.Microseconds <= 1000 },
			{ TimeUnit.Nanosecond, x => x.Nanoseconds <= 1000 }
			#endif
		};
	}

	#endregion

	#region Methods

	/// <summary>
	/// Convert the byte unit into string format value.
	/// </summary>
	/// <param name="byteUnit"> The unit of the byte value. </param>
	/// <param name="count"> The size of the data. </param>
	/// <param name="format"> The word format for the unit. </param>
	/// <returns> The string value of the unit. </returns>
	public static string GetHumanizeStringFormat(this ByteUnit byteUnit, decimal count, WordFormat format = WordFormat.Abbreviation)
	{
		var abbreviated = format == WordFormat.Abbreviation;
		var resourceKey = abbreviated
			? $"DataUnit_{byteUnit}Symbol"
			: $"DataUnit_{byteUnit}";
		var resourceValue = HumanFormatter.GetStringFormat(resourceKey);

		if (!abbreviated && (count > 1))
		{
			resourceValue += 's';
		}

		return resourceValue;
	}

	/// <summary>
	/// Humanize the time unit.
	/// </summary>
	/// <param name="timeUnit"> The time unit to humanize. </param>
	/// <param name="options"> The options for humanizing. </param>
	/// <returns> The human formatted string. </returns>
	public static string Humanize(this TimeUnit timeUnit, IHumanizeOptions options = null)
	{
		return options?.WordFormat == WordFormat.Full
			? timeUnit.GetDisplayName()
			: timeUnit.GetDisplayShortName();
	}

	/// <summary>
	/// Convert the DateTime to a human string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <param name="options"> The options for humanizing. </param>
	/// <returns> The human formatted string. </returns>
	public static string Humanize(this DateTime value, IHumanizeOptions options = null)
	{
		// todo: place-holder until we can get to this.
		return value.ToString();
	}

	/// <summary>
	/// Convert the int to a human string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <param name="options"> The settings for conversion. </param>
	/// <returns> The human formatted string. </returns>
	public static string Humanize(this int value, IHumanizeOptions options = null)
	{
		// todo: place-holder until we can get to this.
		return value.ToString();
	}

	/// <summary>
	/// Convert the long to a human string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <param name="options"> The settings for conversion. </param>
	/// <returns> The human formatted string. </returns>
	public static string Humanize(this long value, IHumanizeOptions options = null)
	{
		// todo: place-holder until we can get to this.
		return value.ToString();
	}

	/// <summary>
	/// Convert the timespan to a human string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <param name="options"> The settings for conversion. </param>
	/// <returns> The human formatted string. </returns>
	public static string Humanize(this TimeSpan value, IHumanizeOptions options = null)
	{
		options ??= new HumanizeOptions();

		var valueMinUnit = MinUnit(value, options.MinUnit);
		var valueMaxUnit = MaxUnit(value, valueMinUnit, options.MaxUnit);
		var sections = new List<string>();
		var precisionFormat = $"{{0:0.{RandomGenerator.NextString(options.Precision, "#")}}}";

		foreach (var unit in _timeUnitDescending)
		{
			if (unit > valueMaxUnit)
			{
				continue;
			}

			if (unit < valueMinUnit)
			{
				continue;
			}

			switch (unit)
			{
				case TimeUnit.Year when value.Days > 365:
				{
					var years = value.Days / 365;
					sections.Add($"{years} {GetUnitName(unit, years, options.WordFormat)}");
					value -= TimeSpan.FromDays(years * 365);
					break;
				}
				case TimeUnit.Day when value.Days > 0:
				{
					sections.Add($"{value.Days} {GetUnitName(unit, value.Days, options.WordFormat)}");
					value -= TimeSpan.FromDays(value.Days);
					break;
				}
				case TimeUnit.Hour when (value.Hours > 0) || (valueMinUnit == unit):
				{
					double number = value.Hours;
					value -= TimeSpan.FromHours(value.Hours);

					if ((valueMinUnit == TimeUnit.Hour) && (value > TimeSpan.Zero))
					{
						number += value.TotalHours;
						var total = string.Format(precisionFormat, number);
						sections.Add($"{total} {GetUnitName(unit, number, options.WordFormat)}");
					}
					else
					{
						sections.Add($"{number} {GetUnitName(unit, number, options.WordFormat)}");
					}
					break;
				}
				case TimeUnit.Minute when (value.Minutes > 0) || (valueMinUnit == unit):
				{
					double number = value.Minutes;
					value -= TimeSpan.FromMinutes(value.Minutes);

					if ((valueMinUnit == TimeUnit.Minute) && (value > TimeSpan.Zero))
					{
						number += value.TotalMinutes;
						var total = string.Format(precisionFormat, number);
						sections.Add($"{total} {GetUnitName(unit, number, options.WordFormat)}");
					}
					else
					{
						sections.Add($"{number} {GetUnitName(unit, number, options.WordFormat)}");
					}
					break;
				}
				case TimeUnit.Second when (value.Seconds > 0) || (valueMinUnit == unit):
				{
					double number = value.Seconds;
					value -= TimeSpan.FromSeconds(value.Seconds);

					if ((valueMinUnit == TimeUnit.Second) && (value > TimeSpan.Zero))
					{
						number += value.TotalSeconds;
						var total = string.Format(precisionFormat, number);
						sections.Add($"{total} {GetUnitName(unit, number, options.WordFormat)}");
					}
					else
					{
						sections.Add($"{number} {GetUnitName(unit, number, options.WordFormat)}");
					}
					break;
				}
				case TimeUnit.Millisecond when (value.Milliseconds > 0) || (valueMinUnit == unit):
				{
					double number = value.Milliseconds;
					value -= TimeSpan.FromMilliseconds(value.Milliseconds);

					if ((valueMinUnit == TimeUnit.Millisecond) && (value > TimeSpan.Zero))
					{
						number += value.TotalMilliseconds;
						var total = string.Format(precisionFormat, number);
						sections.Add($"{total} {GetUnitName(unit, number, options.WordFormat)}");
					}
					else
					{
						sections.Add($"{number} {GetUnitName(unit, number, options.WordFormat)}");
					}
					break;
				}
				#if NET7_0_OR_GREATER
				case TimeUnit.Microsecond when value.Microseconds > 0:
				{
					sections.Add($"{value.Microseconds} {GetUnitName(unit, value.Microseconds, options.WordFormat)}");
					value -= TimeSpan.FromMilliseconds(value.Microseconds);
					break;
				}
				#endif
			}

			if (sections.Count >= options.MaxUnitSegments)
			{
				break;
			}

			if (value == TimeSpan.Zero)
			{
				break;
			}
		}

		if (options.WordFormat == WordFormat.Abbreviation)
		{
			return string.Join(" ", sections);
		}

		var builder = new TextBuilder();
		var end = sections.Count - 1;

		for (var i = 0; i < end; i++)
		{
			builder.Append(sections[i]);
			builder.Append(sections.Count > 2 ? ", " : " ");
		}

		if ((builder.Length > 0) && (sections.Count >= 2))
		{
			builder.Append("and ");
		}

		if (sections.Count > 0)
		{
			builder.Append(sections[end]);
		}

		return builder.ToString();
	}

	/// <summary>
	/// Convert the text to a sentence case string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The string in the desired format. </returns>
	public static string ToSentenceCase(this string value)
	{
		var builder = new TextBuilder();
		var nextCharUpper = false;
		var previousCharLower = false;
		var previousCharIsSpace = false;

		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			if ((builder.Length <= 0) && (c == ' '))
			{
				continue;
			}

			if (builder.Length == 0)
			{
				builder.Append(char.IsLower(c) ? char.ToUpper(c) : c);
				continue;
			}

			if ((c == ' ') || !char.IsLetterOrDigit(c))
			{
				if (!previousCharIsSpace)
				{
					builder.Append(' ');
					previousCharIsSpace = true;
				}
				nextCharUpper = true;
				continue;
			}

			if (char.IsUpper(c)
				&& previousCharLower 
				&& !previousCharIsSpace)
			{
				builder.Append(' ');
			}

			if (nextCharUpper)
			{
				builder.Append(char.ToUpper(c));
				nextCharUpper = false;
				continue;
			}

			builder.Append(c);
			previousCharLower = char.IsLower(c);
			previousCharIsSpace = c == ' ';
		}

		return builder.Trim().ToString();
	}
	
	/// <summary>
	/// Convert the text to a camel case string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The string in the desired format. </returns>
	public static string ToCamelCase(this string value)
	{
		var builder = new TextBuilder();
		var nextCharUpper = false;

		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			if (i == 0)
			{
				builder.Append(char.IsUpper(c) ? char.ToLower(c) : c);
				continue;
			}

			if ((c == ' ') || !char.IsLetterOrDigit(c))
			{
				nextCharUpper = true;
				continue;
			}

			if (nextCharUpper)
			{
				builder.Append(char.ToUpper(c));
				nextCharUpper = false;
				continue;
			}

			builder.Append(c);
		}
		return builder.ToString();
	}

	/// <summary>
	/// Convert the text to a camel case string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The string in a camel case format. </returns>
	public static string ToPascalCase(this string value)
	{
		var builder = new TextBuilder();
		var nextCharUpper = false;

		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			if (i == 0)
			{
				builder.Append(char.IsLower(c) ? char.ToUpper(c) : c);
				continue;
			}

			if ((c == ' ') || !char.IsLetterOrDigit(c))
			{
				nextCharUpper = true;
				continue;
			}

			if (nextCharUpper)
			{
				builder.Append(char.ToUpper(c));
				nextCharUpper = false;
				continue;
			}

			builder.Append(c);
		}
		return builder.ToString();
	}

	private static string GetUnitName(TimeUnit unit, double value, WordFormat wordFormat)
	{
		var details = _timeUnitNames[unit];

		if (unit == TimeUnit.Ticks)
		{
			return wordFormat == WordFormat.Full
				? details.Name
				: details.ShortName;
		}

		return wordFormat == WordFormat.Abbreviation
			? details.ShortName
			: value > 1
				? details.Name + "s"
				: details.Name;
	}

	private static TimeUnit MaxUnit(TimeSpan value, TimeUnit minAllowed, TimeUnit maxAllowed)
	{
		foreach (var unit in _timeUnitDescending)
		{
			if ((unit > maxAllowed) || !_timeSpanUnitMinChecks.ContainsKey(unit))
			{
				continue;
			}

			if (_timeSpanUnitMaxChecks[unit].Invoke(value))
			{
				return unit;
			}

			if (unit == minAllowed)
			{
				return minAllowed;
			}
		}

		return TimeUnit.Max;
	}

	private static TimeUnit MinUnit(TimeSpan value, TimeUnit minAllowed)
	{
		foreach (var unit in _timeUnitAscending)
		{
			if ((unit < minAllowed) || !_timeSpanUnitMinChecks.ContainsKey(unit))
			{
				continue;
			}

			if (_timeSpanUnitMinChecks[unit].Invoke(value))
			{
				return unit;
			}

			if (unit == minAllowed)
			{
				return minAllowed;
			}
		}

		return TimeUnit.Min;
	}

	#endregion
}