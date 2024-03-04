#region References

using System;
using System.Globalization;

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

	#endregion

	#region Methods

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
	/// Return the start of the next month.
	/// </summary>
	/// <param name="date"> The date to get the start of. </param>
	/// <returns> The start of the next month. </returns>
	public static DateTime NextMonth(this DateTime date)
	{
		return date.MonthStart().AddMonths(1);
	}

	/// <summary>
	/// Converts the string representation of a date and time to its <see cref="T:System.DateTime"> </see> equivalent.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <returns> The date time value. </returns>
	public static DateTime ToUtcDateTime(this string value)
	{
		if (value
			is "0001-01-01T12:00:00"
			or "0001-01-01T12:00:00+00:00"
			or "0001-01-01T00:00:00"
			or "0001-01-01T00:00:00+00:00")
		{
			return DateTime.MinValue;
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
	/// Converts the date time to its UTC <see cref="T:System.DateTime"> </see> equivalent. Assumes
	/// "Unknown" is already UTC
	/// </summary>
	/// <param name="value"> The date time value. </param>
	/// <param name="unspecifiedIsUtc"> Assumes "Unspecified" kind is already UTC value. </param>
	/// <returns> The date time value. </returns>
	public static DateTime ToUtcDateTime(this DateTime value, bool unspecifiedIsUtc = true)
	{
		return value.Kind switch
		{
			DateTimeKind.Unspecified when unspecifiedIsUtc => DateTime.SpecifyKind(value, DateTimeKind.Utc),
			DateTimeKind.Unspecified => value.ToUniversalTime(),
			DateTimeKind.Local => value.ToUniversalTime(),
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

	#if (!NETSTANDARD)

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
	public static DateOnly NextMonth(this DateOnly date)
	{
		return date.MonthStart().AddMonths(1);
	}

	/// <summary>
	/// Convert a DateTime to a DateOnly.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The value as DateOnly. </returns>
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

	#endif
}