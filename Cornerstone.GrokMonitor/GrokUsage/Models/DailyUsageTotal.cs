#region References

using System;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// Allowance usage for one local calendar day (from billing snapshots in the unified log).
/// </summary>
public record DailyUsageTotal
{
	#region Properties

	/// <summary>
	/// Percentage points of allowance used this day (end of day minus prior baseline).
	/// First day with snapshots uses end-of-day vs 0% when no prior end exists.
	/// </summary>
	public double DailyDelta { get; init; }

	/// <summary>
	/// Local calendar date for the bucket (midnight local).
	/// </summary>
	public DateTime Day { get; init; }

	/// <summary>
	/// Usage percent at end of day (last snapshot that day, or carried from a prior day).
	/// </summary>
	public double EndOfDayPercent { get; init; }

	/// <summary>
	/// True when at least one billing snapshot fell on this local day.
	/// </summary>
	public bool HasSnapshot { get; init; }

	#endregion
}