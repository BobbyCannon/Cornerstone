#region References

using System;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// Token total for one local calendar day (used by usage analytics charts).
/// </summary>
public record DailyTokenTotal
{
	#region Properties

	/// <summary>
	/// Local calendar date for the bucket (time is midnight local; Kind is unspecified or local).
	/// </summary>
	public DateTime Day { get; init; }

	/// <summary>
	/// Prompt plus completion tokens for inferences on this day.
	/// </summary>
	public long TotalTokens { get; init; }

	#endregion
}