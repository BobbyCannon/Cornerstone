#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// Derived burn-rate / pace metrics for a Grok home refresh.
/// </summary>
public record UsageAnalytics
{
	#region Properties

	/// <summary>
	/// Human-readable note when estimates cannot be formed (for example Insufficient data).
	/// </summary>
	public string AnalyticsNote { get; init; } = string.Empty;

	/// <summary>
	/// Continuous local-day token totals for the analytics window.
	/// </summary>
	public IReadOnlyList<DailyTokenTotal> DailyTokenTotals { get; init; } = [];

	/// <summary>
	/// Continuous local-day credit usage (end-of-day % and daily delta) for the analytics window.
	/// </summary>
	public IReadOnlyList<DailyUsageTotal> DailyUsageTotals { get; init; } = [];

	/// <summary>
	/// Estimated UTC time when credits hit 100% at the current rate; default when unknown.
	/// </summary>
	public DateTimeOffset EstimatedUsageExhaustionAt { get; init; }

	/// <summary>
	/// True when <see cref="EstimatedUsageExhaustionAt" /> is a real estimate.
	/// </summary>
	public bool HasUsageEstimate { get; init; }

	/// <summary>
	/// Linear expected credit percent by now (elapsed / period length * 100); 0 when unknown.
	/// </summary>
	public double LinearPacePercent { get; init; }

	/// <summary>
	/// Token burn per hour over the last 24 wall-clock hours.
	/// </summary>
	public double TokenBurnPerHourLast24h { get; init; }

	/// <summary>
	/// Token burn per hour over the period (or fallback window) so far.
	/// </summary>
	public double TokenBurnPerHourPeriod { get; init; }

	/// <summary>
	/// Credit percent consumed per hour used for ETA; 0 when unknown.
	/// </summary>
	public double UsagePercentPerHour { get; init; }

	/// <summary>
	/// How credit rate was derived (billing history or period average); empty when unknown.
	/// </summary>
	public string UsageRateSource { get; init; } = string.Empty;

	#endregion
}