#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.State;

/// <summary>
/// One session row projected into the usage grid (UI-free).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class GrokSessionUsageState : CornerstoneObject
{
	#region Properties

	/// <summary>
	/// Cached prompt tokens for this session.
	/// </summary>
	public partial long CachedPromptTokens { get; set; }

	/// <summary>
	/// Completion tokens for this session.
	/// </summary>
	public partial long CompletionTokens { get; set; }

	/// <summary>
	/// Model id last attributed to this session; empty when unknown.
	/// </summary>
	public partial string CurrentModelId { get; set; }

	/// <summary>
	/// Absolute path to events.jsonl when present; empty otherwise.
	/// </summary>
	public partial string EventsPath { get; set; }

	/// <summary>
	/// Earliest inference timestamp; default when none.
	/// </summary>
	public partial DateTimeOffset FirstInferenceAt { get; set; }

	/// <summary>
	/// Number of inference_done events attributed to this session.
	/// Zero is normal when only summary.json exists (no log rows yet / different home log).
	/// </summary>
	public partial int InferenceCount { get; set; }

	/// <summary>
	/// Latest inference timestamp; default when none.
	/// </summary>
	public partial DateTimeOffset LastInferenceAt { get; set; }

	/// <summary>
	/// Formatted last inference timestamp for display in the Sessions grid.
	/// Returns empty string when no inference time is known.
	/// </summary>
	public string LastInferenceAtStr => LastInferenceAt == default ? string.Empty : LastInferenceAt.ToString("u");

	/// <summary>
	/// Message count from session summary when known.
	/// </summary>
	public partial int MessageCount { get; set; }

	/// <summary>
	/// Prompt tokens for this session.
	/// </summary>
	public partial long PromptTokens { get; set; }

	/// <summary>
	/// Reasoning tokens for this session.
	/// </summary>
	public partial long ReasoningTokens { get; set; }

	/// <summary>
	/// Absolute path to the session folder under the Grok home; empty when not found.
	/// </summary>
	public partial string SessionDirectory { get; set; }

	/// <summary>
	/// Unique session identifier.
	/// </summary>
	public partial string SessionId { get; set; }

	/// <summary>
	/// Absolute path to summary.json when present; empty otherwise.
	/// </summary>
	public partial string SummaryPath { get; set; }

	/// <summary>
	/// Display title; empty when unknown.
	/// </summary>
	public partial string Title { get; set; }

	/// <summary>
	/// Prompt plus completion tokens.
	/// </summary>
	public partial long TotalTokens { get; set; }

	/// <summary>
	/// Allocated share of the home credit-usage percent for the selected period.
	/// Zero when unknown or when this home does not report credit usage.
	/// </summary>
	public partial double UsagePercent { get; set; }

	/// <summary>
	/// True when <see cref="UsagePercent" /> is an allocated credit share (not just zero / unknown).
	/// </summary>
	public partial bool HasAllocatedUsage { get; set; }

	/// <summary>
	/// Working directory when known; empty otherwise.
	/// </summary>
	public partial string WorkingDirectory { get; set; }

	#endregion
}