namespace Cornerstone.VisualStudio.Core.Cleanup;

/// <summary>
/// High-level result of running cleanup on a single document.
/// </summary>
public enum CleanupOutcome
{
	/// <summary>Text was modified.</summary>
	Changed = 0,

	/// <summary>Pipeline ran; output matches input.</summary>
	Unchanged = 1,

	/// <summary>Structural XML rules were skipped (malformed); hygiene may still have run.</summary>
	StructuralSkipped = 2,

	/// <summary>File/path was not processed (wrong extension, too large, empty options, etc.).</summary>
	Skipped = 3,

	/// <summary>An unexpected failure occurred.</summary>
	Error = 4
}
