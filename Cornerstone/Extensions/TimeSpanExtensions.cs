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

	/// <summary>
	/// Returns the percentage the span on the duration.
	/// </summary>
	/// <param name="span"> The span value. </param>
	/// <param name="duration"> The duration. </param>
	/// <param name="precision"> The decimal precision. Defaults to 2. </param>
	/// <returns> The percentage value. </returns>
	public static decimal PercentOf(this TimeSpan span, TimeSpan duration, int precision = 2)
	{
		if (duration.Ticks == 0)
		{
			return -2;
		}

		var response = span.Ticks * 1.0m;
		response = Math.Round((response / duration.Ticks) * 100.0m, precision, MidpointRounding.AwayFromZero);
		return response;
	}

	/// <summary>
	/// Return the minimal string of a timespan
	/// </summary>
	/// <param name="span"> The span value. </param>
	/// <returns> The minimal string. </returns>
	public static string ToMinString(this TimeSpan span)
	{
		var precisionLength = 0;
		var includePrecision = false;

		if (span.Milliseconds > 0)
		{
			precisionLength = 3;
			includePrecision = true;
		}
		
		#if (!NETSTANDARD2_0)
		if (span.Microseconds > 0)
		{
			precisionLength = 6;
			includePrecision = true;
		}
		if (span.Nanoseconds > 0)
		{
			precisionLength = 7;
			includePrecision = true;
		}
		#endif

		if (span.Days > 0)
		{
			return includePrecision ? span.ToString() : span.ToString("d\\:hh\\:mm\\:ss");
		}

		if (span.Hours > 0)
		{
			return includePrecision ? span.ToString() : span.ToString("hh\\:mm\\:ss");
		}

		if (span.Minutes > 0)
		{
			return includePrecision ? span.ToString() : span.ToString("mm\\:ss");
		}

		return includePrecision
			? span.ToString($"ss\\.{new string('f', precisionLength)}")
			: span.ToString("ss");
	}

	#endregion
}