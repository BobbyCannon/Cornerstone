#region References

using System;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extension for TimeSpan
/// </summary>
public static class TimeSpanExtensions
{
	#region Methods

	public static TimeSpan Min(TimeSpan a, TimeSpan b)
	{
		return a < b ? a : b;
	}

	/// <summary>
	/// Get a TimeSpan that represents the nanoseconds.
	/// </summary>
	/// <param name="nanoseconds"> The nanoseconds. </param>
	/// <returns> The TimeSpan for the nanoseconds. </returns>
	public static TimeSpan ToTimeSpan(double nanoseconds)
	{
		var ticks = (long) Math.Round((nanoseconds * TimeSpan.TicksPerMillisecond) / 1_000_000.0);
		var ts = TimeSpan.FromTicks(ticks);
		return ts;
	}

	#endregion
}