namespace Cornerstone.VisualStudio.Core.Cleanup;

/// <summary>
/// How <see cref="CleanupPipeline" /> normalizes line endings.
/// </summary>
public enum CleanupLineEndingMode
{
	/// <summary>Keep the document's dominant line ending (CRLF if mixed or none detected).</summary>
	Keep = 0,

	/// <summary>Force Windows (CRLF) line endings.</summary>
	Crlf = 1,

	/// <summary>Force Unix (LF) line endings.</summary>
	Lf = 2
}
