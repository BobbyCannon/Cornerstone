#region References

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for date time
/// </summary>
public static class DateTimeExtensions
{
	#region Constants

	/// <summary>
	/// The amount of ticks in the Max Date / Time value.
	/// </summary>
	public const long MaxDateTimeTicks = 3155378975999999999L;

	/// <summary>
	/// The amount of ticks in the Min Date / Time value.
	/// </summary>
	public const long MinDateTimeTicks = 0L;

	/// <summary>
	/// The day number of a DateOnly structure for unix epoch.
	/// </summary>
	public const int UnixEpochDateOnlyDayNumber = 719162;

	/// <summary>
	/// The ticks of a DateTime structure for unix epoch.
	/// </summary>
	public const long UnixEpochDateTimeTicks = 621355968000000000;

	/// <summary>
	/// The day number of a DateOnly structure for windows epoch.
	/// </summary>
	public const int WindowsEpochDateOnlyDayNumber = 584388;

	/// <summary>
	/// The ticks of a DateTime structure for windows epoch.
	/// </summary>
	public const long WindowsEpochDateTimeTicks = 504911232000000000;

	#endregion

	#region Fields

	/// <summary>
	/// Represents the start of Posix / Unix Date Only.
	/// </summary>
	public static DateOnly UnixEpochDateOnly = new(1970, 01, 01);

	/// <summary>
	/// Represents the start of Posix / Unix Date and Time.
	/// </summary>
	public static DateTime UnixEpochDateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Represents the start of Posix / Unix Date and Time.
	/// </summary>
	public static DateTimeOffset UnixEpochDateTimeOffset = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

	/// <summary>
	/// Represents the start of Windows Date Only.
	/// </summary>
	public static DateOnly WindowsEpochDateOnly = new(1601, 01, 01);

	/// <summary>
	/// Represents the start of Windows Date and Time.
	/// </summary>
	public static DateTime WindowsEpochDateTime = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Represents the start of Windows Date and Time.
	/// </summary>
	public static DateTimeOffset WindowsEpochDateTimeOffset = new(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);

	#endregion

	#region Methods

	/// <summary>
	/// Converts the epoch time to a date time.
	/// </summary>
	/// <param name="value"> The date time value as epoch time (seconds). </param>
	/// <returns> The date time value. </returns>
	public static DateTime FromUnixEpochTime(this int value)
	{
		return UnixEpochDateTime.AddSeconds(value);
	}

	/// <summary>
	/// Returns the max date of the two dates.
	/// </summary>
	/// <param name="first"> The first date. </param>
	/// <param name="second"> The second date. </param>
	/// <returns> </returns>
	public static DateTime Max(DateTime first, DateTime second)
	{
		return first >= second ? first : second;
	}

	/// <summary>
	/// Return the start of the month.
	/// </summary>
	/// <param name="date"> The date to get the start of. </param>
	/// <returns> The start of the month. </returns>
	public static DateTime MonthStart(this DateTime date)
	{
		return new DateTime(date.Year, date.Month, 1);
	}

	/// <summary>
	/// Return the start of the month.
	/// </summary>
	/// <param name="date"> The date to get the start of. </param>
	/// <returns> The start of the month. </returns>
	public static DateOnly MonthStart(this DateOnly date)
	{
		return new DateOnly(date.Year, date.Month, 1);
	}

	/// <summary>
	/// Return the start of the next month.
	/// </summary>
	/// <param name="date"> The date to get the start of. </param>
	/// <returns> The start of the next month. </returns>
	public static DateTime NextMonth(this DateTime date)
	{
		return date.MonthStart().AddMonths(1);
	}

	/// <summary>
	/// Return the start of the next month.
	/// </summary>
	/// <param name="date"> The date to get the start of. </param>
	/// <returns> The start of the next month. </returns>
	public static DateOnly NextMonth(this DateOnly date)
	{
		return date.MonthStart().AddMonths(1);
	}

	/// <summary>
	/// Convert a DateTime to a DateOnly.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The value as DateOnly. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DateOnly ToDateOnly(this DateTime value)
	{
		return new DateOnly(value.Year, value.Month, value.Day);
	}

	/// <summary>
	/// Convert a DateTimeOffset to a DateOnly.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The value as DateOnly. </returns>
	public static DateOnly ToDateOnly(this DateTimeOffset value)
	{
		return new DateOnly(value.Year, value.Month, value.Day);
	}

	/// <summary>
	/// Converts the date time to its Local <see cref="T:System.DateTime"> </see> equivalent. Assumes
	/// "Unknown" is already local.
	/// </summary>
	/// <param name="value"> The date time value. </param>
	/// <param name="unspecifiedIsLocalTime"> Assumes "Unspecified" kind is already local value. </param>
	/// <returns> The date time value. </returns>
	public static DateTime ToLocalTime(this DateTime value, bool unspecifiedIsLocalTime = true)
	{
		return value.Kind switch
		{
			DateTimeKind.Unspecified when unspecifiedIsLocalTime => DateTime.SpecifyKind(value, DateTimeKind.Local),
			DateTimeKind.Unspecified => value.ToLocalTime(),
			DateTimeKind.Utc => value.ToLocalTime(),
			_ => value
		};
	}

	/// <summary>
	/// Convert a datetime to unix epoch time (seconds since 1970-01-01T00:00:00Z).
	/// </summary>
	/// <param name="dateTime"> The date time value. </param>
	/// <returns> The date time value in unix epoch. </returns>
	public static ulong ToUnixEpochTime(this DateTime dateTime)
	{
		return (ulong) dateTime.Subtract(UnixEpochDateTime).TotalSeconds;
	}

	/// <summary>
	/// Converts the string representation of a date and time to its <see cref="T:System.DateTime"> </see> equivalent.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <returns> The date time value. </returns>
	public static DateOnly ToUtcDateOnly(this string value)
	{
		if (value
			is "0001-01-01T12:00:00"
			or "0001-01-01T12:00:00+00:00"
			or "0001-01-01T00:00:00"
			or "0001-01-01T00:00:00+00:00")
		{
			return DateOnly.MinValue;
		}

		return DateOnly.Parse(value);
	}

	/// <summary>
	/// Converts the string representation of a date and time to its <see cref="T:System.DateTime"> </see> equivalent.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <returns> The date time value. </returns>
	public static DateTime ToUtcDateTime(this string value)
	{
		if (value.StartsWith("0001-01-01T12:00:00")
			|| value.StartsWith("0001-01-01T00:00:00")
			|| value.StartsWith("0001-01-01 00:00:00"))
		{
			return DateTime.MinValue;
		}

		if (value.StartsWith("9999-12-31T23:59:59.9999999")
			|| value.StartsWith("9999-12-31 23:59:59.9999999"))
		{
			return DateTime.MaxValue;
		}

		return DateTime.Parse(value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
	}

	/// <summary>
	/// Converts the string representation of a date and time to its <see cref="T:System.DateTime"> </see> equivalent.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <param name="format"> The exact format to parse. </param>
	/// <returns> The date time value. </returns>
	public static DateTime ToUtcDateTime(this string value, string format)
	{
		return DateTime.ParseExact(value, format, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
	}

	/// <summary>
	/// Converts the date time to its UTC <see cref="T:System.DateTime"> </see> equivalent.
	/// Assumes "Unspecified" is already UTC.
	/// </summary>
	/// <param name="value"> The date time value. </param>
	/// <returns> The date time value. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DateTime ToUtcDateTime(this DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
			DateTimeKind.Local => value.ToUniversalTime(),

			// Already UTC no need to adjust
			_ => value
		};
	}

	/// <summary>
	/// Converts the date time into a ISO8601 format.
	/// </summary>
	/// <param name="dateTime"> </param>
	/// <returns> </returns>
	public static string ToUtcString(this DateTime dateTime)
	{
		string dateTimeString;
		if ((dateTime.Kind == DateTimeKind.Local)
			&& (dateTime != DateTime.MinValue)
			&& (dateTime != DateTime.MaxValue))
		{
			dateTimeString = dateTime.ToUniversalTime().ToString("O");
		}
		else
		{
			dateTimeString = dateTime.ToString("O");
		}

		if (!dateTimeString.EndsWith("Z"))
		{
			dateTimeString += "Z";
		}

		return dateTimeString;
	}

	/// <summary>
	/// Move the month and day to a specific year
	/// </summary>
	/// <param name="date"> The date to move to a year. </param>
	/// <param name="year"> The year to move the month/day to. </param>
	/// <returns> The month/day moved to the provided year. </returns>
	public static DateTime ToYear(this DateTime date, int year)
	{
		return new DateTime(year, date.Month, date.Day);
	}

	/// <summary>
	/// Converts the string representation of a date and time to its <see cref="T:System.DateTime"> </see> equivalent.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <param name="dateValue"> The date time value. </param>
	/// <returns> True if the value was parsed otherwise false. </returns>
	public static bool TryParseUtcDateTime(this string value, out DateTime dateValue)
	{
		return DateTime.TryParse(value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateValue);
	}

	#endregion
}