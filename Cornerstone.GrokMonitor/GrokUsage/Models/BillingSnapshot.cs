#region References

using System;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// A point-in-time billing / credits snapshot from the unified log.
/// Default instance (Timestamp default) means no snapshot was found.
/// </summary>
public record BillingSnapshot
{
	#region Properties

	/// <summary>
	/// True when this instance came from a parsed log event.
	/// </summary>
	public bool HasValue => Timestamp != default;

	/// <summary>
	/// Whether the account is on unified billing.
	/// </summary>
	public bool? IsUnifiedBillingUser { get; init; }

	/// <summary>
	/// On-demand spend cap when reported.
	/// </summary>
	public double? OnDemandCap { get; init; }

	/// <summary>
	/// On-demand spend used when reported.
	/// </summary>
	public double? OnDemandUsed { get; init; }

	/// <summary>
	/// End of the current usage period when reported; otherwise default.
	/// </summary>
	public DateTimeOffset? PeriodEnd { get; init; }

	/// <summary>
	/// Start of the current usage period when reported; otherwise default.
	/// </summary>
	public DateTimeOffset? PeriodStart { get; init; }

	/// <summary>
	/// Period type string (for example USAGE_PERIOD_TYPE_WEEKLY); empty when unknown.
	/// </summary>
	public string PeriodType { get; init; } = "";

	/// <summary>
	/// Prepaid balance when reported.
	/// </summary>
	public double? PrepaidBalance { get; init; }

	/// <summary>
	/// Subscription tier label when reported (for example SuperGrok Plus); empty when unknown.
	/// </summary>
	public string SubscriptionTier { get; init; } = "";

	/// <summary>
	/// Timestamp of the billing log event; default means no snapshot.
	/// </summary>
	public DateTimeOffset Timestamp { get; init; }

	/// <summary>
	/// Usage as a percent of the period allowance (log field creditUsagePercent); null when not reported.
	/// </summary>
	public double? UsagePercent { get; init; }

	#endregion
}