#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// High-level aggregate of sessions and the latest billing snapshot.
/// </summary>
public record GrokUsageSummary
{
	#region Properties

	/// <summary>
	/// All billing snapshots from the unified log (chronological order).
	/// Used for credit burn slope; may be empty.
	/// </summary>
	public IReadOnlyList<BillingSnapshot> BillingHistory { get; init; } = [];

	/// <summary>
	/// Sum of cached prompt tokens across all sessions.
	/// </summary>
	public long GrandTotalCachedPromptTokens => Sessions.Sum(s => s.TotalCachedPromptTokens);

	/// <summary>
	/// Sum of completion tokens across all sessions.
	/// </summary>
	public long GrandTotalCompletionTokens => Sessions.Sum(s => s.TotalCompletionTokens);

	/// <summary>
	/// Sum of prompt tokens across all sessions.
	/// </summary>
	public long GrandTotalPromptTokens => Sessions.Sum(s => s.TotalPromptTokens);

	/// <summary>
	/// Sum of reasoning tokens across all sessions.
	/// </summary>
	public long GrandTotalReasoningTokens => Sessions.Sum(s => s.TotalReasoningTokens);

	/// <summary>
	/// Prompt plus completion tokens across all sessions.
	/// </summary>
	public long GrandTotalTokens => Sessions.Sum(s => s.TotalTokens);

	/// <summary>
	/// Most recent billing snapshot. Check <see cref="BillingSnapshot.HasValue" />.
	/// </summary>
	public BillingSnapshot LatestBilling { get; init; } = new();

	/// <summary>
	/// Usage periods stored in the archive (dropdown source). Empty before first import.
	/// </summary>
	public IReadOnlyList<UsagePeriodOption> Periods { get; init; } = [];

	/// <summary>
	/// Per-session usage rows included in this summary.
	/// </summary>
	public IReadOnlyList<SessionUsage> Sessions { get; init; } = [];

	#endregion
}