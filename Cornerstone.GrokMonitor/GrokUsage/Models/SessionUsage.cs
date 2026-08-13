#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// A session plus its inference events and convenience token totals.
/// </summary>
public record SessionUsage
{
	#region Properties

	/// <summary>
	/// Timestamp of the earliest inference in this session, or default when none.
	/// </summary>
	public DateTimeOffset FirstInference =>
		Inferences.Count == 0
			? default
			: Inferences.MinBy(x => x.Timestamp).Timestamp;

	/// <summary>
	/// Inferences for this session, ordered by timestamp ascending.
	/// </summary>
	public IReadOnlyList<InferenceUsage> Inferences { get; init; } = [];

	/// <summary>
	/// Session metadata.
	/// </summary>
	public SessionInfo Info { get; init; } = new();

	/// <summary>
	/// Timestamp of the latest inference in this session, or default when none.
	/// </summary>
	public DateTimeOffset? LastInference =>
		Inferences.Count == 0
			? default
			: Inferences.MaxBy(x => x.Timestamp).Timestamp;

	/// <summary>
	/// Sum of cached prompt tokens across inferences.
	/// </summary>
	public long TotalCachedPromptTokens => Inferences.Sum(x => x.CachedPromptTokens);

	/// <summary>
	/// Sum of completion tokens across inferences.
	/// </summary>
	public long TotalCompletionTokens => Inferences.Sum(x => x.CompletionTokens);

	/// <summary>
	/// Sum of prompt tokens across inferences.
	/// </summary>
	public long TotalPromptTokens => Inferences.Sum(x => x.PromptTokens);

	/// <summary>
	/// Sum of reasoning tokens across inferences.
	/// </summary>
	public long TotalReasoningTokens => Inferences.Sum(x => x.ReasoningTokens);

	/// <summary>
	/// Prompt plus completion tokens (reasoning is tracked separately).
	/// </summary>
	public long TotalTokens => TotalPromptTokens + TotalCompletionTokens;

	#endregion
}