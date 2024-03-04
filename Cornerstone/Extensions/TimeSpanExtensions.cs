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

	#endregion
}