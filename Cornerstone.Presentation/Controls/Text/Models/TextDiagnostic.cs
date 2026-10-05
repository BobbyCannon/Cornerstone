#region References

using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

/// <summary>
/// One compiler or analyzer diagnostic span in a text document.
/// </summary>
[SourceReflection]
public sealed class TextDiagnostic
{
	#region Properties

	public int EndOffset { get; set; }

	public string Id { get; set; }

	public int Line { get; set; }

	public string Message { get; set; }

	public string Severity { get; set; }

	public int StartOffset { get; set; }

	public bool IsError => string.Equals(Severity, "Error", System.StringComparison.OrdinalIgnoreCase);

	#endregion
}
