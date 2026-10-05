#region References

using System;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// Token and timing data for a single model inference (one shell turn loop).
/// </summary>
public record InferenceUsage
{
	#region Properties

	/// <summary>
	/// Number of prompt tokens served from cache.
	/// </summary>
	public long CachedPromptTokens { get; init; }

	/// <summary>
	/// Completion (output) tokens generated for this inference.
	/// </summary>
	public long CompletionTokens { get; init; }

	/// <summary>
	/// Loop index within the turn. Negative when not reported.
	/// </summary>
	public int? LoopIndex { get; init; } = -1;

	/// <summary>
	/// Model wall-clock time in milliseconds. Negative when not reported.
	/// </summary>
	public long? ModelElapsedMs { get; init; } = -1;

	/// <summary>
	/// Resolved model id when attribution is available; otherwise empty.
	/// </summary>
	public string ModelId { get; init; } = "";

	/// <summary>
	/// Prompt (input) tokens for this inference.
	/// </summary>
	public long PromptTokens { get; init; }

	/// <summary>
	/// Reasoning tokens when reported by the model.
	/// </summary>
	public long ReasoningTokens { get; init; }

	/// <summary>
	/// Session id from the unified log line.
	/// </summary>
	public string SessionId { get; init; } = "";

	/// <summary>
	/// Timestamp of the inference_done log event.
	/// </summary>
	public DateTimeOffset Timestamp { get; init; }

	/// <summary>
	/// Tokens per second when reported; otherwise 0.
	/// </summary>
	public double? TokensPerSecond { get; init; }

	#endregion
}