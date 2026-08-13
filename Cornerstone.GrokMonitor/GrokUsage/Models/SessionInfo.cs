#region References

using System;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// Metadata for a Grok Build CLI session (from summary.json when present).
/// </summary>
public record SessionInfo
{
	#region Properties

	/// <summary>
	/// Session creation time when available; otherwise default.
	/// </summary>
	public DateTimeOffset? CreatedAt { get; init; }

	/// <summary>
	/// Model id last recorded on the session summary; empty when unknown.
	/// </summary>
	public string CurrentModelId { get; init; } = "";

	/// <summary>
	/// Total messages recorded on the session when available.
	/// </summary>
	public int MessageCount { get; init; }

	/// <summary>
	/// Unique session identifier.
	/// </summary>
	public string SessionId { get; init; } = "";

	/// <summary>
	/// Display title (generated title or session summary text); empty when unknown.
	/// </summary>
	public string Title { get; init; } = "";

	/// <summary>
	/// Last update time when available; otherwise default.
	/// </summary>
	public DateTimeOffset? UpdatedAt { get; init; }

	/// <summary>
	/// Working directory for the session when known; empty when unknown.
	/// </summary>
	public string WorkingDirectory { get; init; } = "";

	#endregion
}