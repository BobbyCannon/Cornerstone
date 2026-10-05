#region References

using System;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// A selectable usage window (typically one billing week). PeriodEnd is exclusive.
/// </summary>
public record UsagePeriodOption
{
	#region Properties

	/// <summary>
	/// UI label (e.g. "Aug 4 – Aug 10 · current").
	/// </summary>
	public string DisplayName { get; init; } = string.Empty;

	/// <summary>
	/// True when this option is the account's current billing period.
	/// </summary>
	public bool IsCurrent { get; init; }

	/// <summary>
	/// Exclusive end of the period.
	/// </summary>
	public DateTimeOffset PeriodEnd { get; init; }

	/// <summary>
	/// Inclusive start of the period.
	/// </summary>
	public DateTimeOffset PeriodStart { get; init; }

	/// <summary>
	/// Period type from billing (e.g. USAGE_PERIOD_TYPE_WEEKLY); empty when unknown.
	/// </summary>
	public string PeriodType { get; init; } = string.Empty;

	#endregion
}