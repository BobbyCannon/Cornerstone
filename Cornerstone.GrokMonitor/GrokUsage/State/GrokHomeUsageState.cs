#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.State;

/// <summary>
/// Domain snapshot for one Grok home folder (e.g. ~/.grok or ~/.grok-work).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class GrokHomeUsageState : CornerstoneObject
{
	#region Constructors

	public GrokHomeUsageState()
	{
		Id = Guid.NewGuid();
		DisplayName = string.Empty;
		Path = string.Empty;
		ProgressText = string.Empty;
		ErrorText = string.Empty;
		SubscriptionTier = string.Empty;
		PeriodType = string.Empty;
		UsageRateSource = string.Empty;
		AnalyticsNote = string.Empty;
		Sessions = [];
		DailyTokenTotals = [];
		DailyUsageTotals = [];
		AvailablePeriods = [];
	}

	public GrokHomeUsageState(Guid id) : this()
	{
		Id = id;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Note when credit ETA cannot be formed (for example Insufficient data).
	/// </summary>
	public partial string AnalyticsNote { get; set; }

	/// <summary>
	/// Billing/usage periods available for the period dropdown (newest first).
	/// </summary>
	public SpeedyList<GrokUsagePeriodState> AvailablePeriods { get; }

	/// <summary>
	/// Continuous local-day token totals for the analytics window (full replace on refresh).
	/// </summary>
	public SpeedyList<DailyTokenTotal> DailyTokenTotals { get; }

	/// <summary>
	/// Continuous local-day credit usage for the analytics window (full replace on refresh).
	/// </summary>
	public SpeedyList<DailyUsageTotal> DailyUsageTotals { get; }

	/// <summary>
	/// Friendly label (Personal, Work, …).
	/// </summary>
	public partial string DisplayName { get; set; }

	/// <summary>
	/// Last refresh or path error for this home.
	/// </summary>
	public partial string ErrorText { get; set; }

	/// <summary>
	/// Estimated time when credits hit 100% at current rate; default when unknown.
	/// </summary>
	public partial DateTimeOffset EstimatedUsageExhaustionAt { get; set; }

	/// <summary>
	/// Grand total cached prompt tokens across sessions.
	/// </summary>
	public partial long GrandTotalCachedPromptTokens { get; set; }

	/// <summary>
	/// Grand total completion tokens across sessions.
	/// </summary>
	public partial long GrandTotalCompletionTokens { get; set; }

	/// <summary>
	/// Grand total prompt tokens across sessions.
	/// </summary>
	public partial long GrandTotalPromptTokens { get; set; }

	/// <summary>
	/// Grand total reasoning tokens across sessions.
	/// </summary>
	public partial long GrandTotalReasoningTokens { get; set; }

	/// <summary>
	/// Prompt plus completion across sessions.
	/// </summary>
	public partial long GrandTotalTokens { get; set; }

	/// <summary>
	/// True when billing fields were populated from a log snapshot.
	/// </summary>
	public partial bool HasBilling { get; set; }

	/// <summary>
	/// True when billing reports credit-allowance percent (SuperGrok-style). False for
	/// Business-like accounts that only report tier without creditUsagePercent.
	/// </summary>
	public partial bool HasCreditUsage { get; set; }

	/// <summary>
	/// True when <see cref="EstimatedUsageExhaustionAt" /> is a real estimate.
	/// </summary>
	public partial bool HasUsageEstimate { get; set; }

	/// <summary>
	/// True when the home directory exists on disk.
	/// </summary>
	public partial bool HomeExists { get; set; }

	/// <summary>
	/// Stable id for bus messages and selection.
	/// </summary>
	public Guid Id { get; }

	/// <summary>
	/// True while a refresh is in flight for this home.
	/// </summary>
	public partial bool IsBusy { get; set; }

	/// <summary>
	/// When usage data was last successfully applied; default when never.
	/// </summary>
	public partial DateTimeOffset LastRefreshedAt { get; set; }

	/// <summary>
	/// Linear expected credit percent by now; 0 when period bounds unknown.
	/// </summary>
	public partial double LinearPacePercent { get; set; }

	/// <summary>
	/// On-demand spend cap when reported; 0 when unknown.
	/// </summary>
	public partial double OnDemandCap { get; set; }

	/// <summary>
	/// On-demand spend used when reported; 0 when unknown.
	/// </summary>
	public partial double OnDemandUsed { get; set; }

	/// <summary>
	/// Absolute path of this Grok home.
	/// </summary>
	public partial string Path { get; set; }

	/// <summary>
	/// Billing period end when known; default otherwise.
	/// </summary>
	public partial DateTimeOffset PeriodEnd { get; set; }

	/// <summary>
	/// Billing period start when known; default otherwise.
	/// </summary>
	public partial DateTimeOffset PeriodStart { get; set; }

	/// <summary>
	/// Period type string from billing config; empty when unknown.
	/// </summary>
	public partial string PeriodType { get; set; }

	/// <summary>
	/// Prepaid balance when reported; 0 when unknown.
	/// </summary>
	public partial double PrepaidBalance { get; set; }

	/// <summary>
	/// Short progress string while busy.
	/// </summary>
	public partial string ProgressText { get; set; }

	/// <summary>
	/// Selected period exclusive end for totals/charts; default means use current/latest.
	/// </summary>
	public partial DateTimeOffset SelectedPeriodEnd { get; set; }

	/// <summary>
	/// Selected period inclusive start for totals/charts; default means use current/latest.
	/// </summary>
	public partial DateTimeOffset SelectedPeriodStart { get; set; }

	/// <summary>
	/// Session rows for this home.
	/// </summary>
	public SpeedyList<GrokSessionUsageState> Sessions { get; }

	/// <summary>
	/// Subscription tier label; empty when unknown.
	/// </summary>
	public partial string SubscriptionTier { get; set; }

	/// <summary>
	/// Token burn per hour over the last 24 wall-clock hours.
	/// </summary>
	public partial double TokenBurnPerHourLast24h { get; set; }

	/// <summary>
	/// Token burn per hour over the analytics window so far.
	/// </summary>
	public partial double TokenBurnPerHourPeriod { get; set; }

	/// <summary>
	/// Credit usage percent from the latest billing snapshot; 0 when unknown.
	/// </summary>
	public partial double UsagePercent { get; set; }

	/// <summary>
	/// Credit percent consumed per hour for linear ETA; 0 when unknown.
	/// </summary>
	public partial double UsagePercentPerHour { get; set; }

	/// <summary>
	/// How <see cref="UsagePercentPerHour" /> was derived; empty when unknown.
	/// </summary>
	public partial string UsageRateSource { get; set; }

	#endregion
}