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
public partial class GrokSessionUsageState : CornerstoneObject, IGrokSessionUsage, IUpdateable<IGrokSessionUsage>
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
	/// True when <see cref="UsagePercent" /> is an allocated credit share (not just zero / unknown).
	/// </summary>
	public partial bool HasAllocatedUsage { get; set; }

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
	/// Working directory when known; empty otherwise.
	/// </summary>
	public partial string WorkingDirectory { get; set; }

	#endregion
}

/// <summary>
/// Shared session usage contract for State and the usage-grid row ViewModel.
/// Setters exist so UpdateWith can copy; the grid does not write these back to State.
/// </summary>
public interface IGrokSessionUsage
{
	#region Properties

	long CachedPromptTokens { get; set; }

	long CompletionTokens { get; set; }

	string CurrentModelId { get; set; }

	string EventsPath { get; set; }

	DateTimeOffset FirstInferenceAt { get; set; }

	bool HasAllocatedUsage { get; set; }

	int InferenceCount { get; set; }

	DateTimeOffset LastInferenceAt { get; set; }

	int MessageCount { get; set; }

	long PromptTokens { get; set; }

	long ReasoningTokens { get; set; }

	string SessionDirectory { get; set; }

	string SessionId { get; set; }

	string SummaryPath { get; set; }

	string Title { get; set; }

	long TotalTokens { get; set; }

	double UsagePercent { get; set; }

	string WorkingDirectory { get; set; }

	#endregion
}