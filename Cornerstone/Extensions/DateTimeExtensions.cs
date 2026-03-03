#region References

using System;

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
	/// Converts the date time to its UTC <see cref="T:System.DateTime"> </see> equivalent.
	/// Assumes "Unspecified" is already UTC.
	/// </summary>
	/// <param name="value"> The date time value. </param>
	/// <returns> The date time value. </returns>
	public static DateTime ToUtcDateTime(this DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
			DateTimeKind.Local => value.ToUniversalTime(),
			_ => value
		};
	}

	#endregion
}