#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// One completed startup scope (inclusive duration) in a hierarchical startup report.
/// </summary>
public sealed class StartupSample
{
	#region Constructors

	public StartupSample(
		string name,
		int depth,
		TimeSpan offset,
		TimeSpan elapsed,
		IReadOnlyList<StartupSample> children = null)
	{
		Name = name ?? string.Empty;
		Depth = depth;
		Offset = offset;
		Elapsed = elapsed;
		Children = children ?? Array.Empty<StartupSample>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Nested scopes that completed while this scope was open.
	/// </summary>
	public IReadOnlyList<StartupSample> Children { get; }

	/// <summary>
	/// Nesting depth (0 = top-level under the session root).
	/// </summary>
	public int Depth { get; }

	/// <summary>
	/// Inclusive wall duration of this scope.
	/// </summary>
	public TimeSpan Elapsed { get; }

	/// <summary>
	/// Scope name (e.g. <c> AppDatabaseManager.Initialize </c>).
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Time from session start until this scope began.
	/// </summary>
	public TimeSpan Offset { get; }

	#endregion
}