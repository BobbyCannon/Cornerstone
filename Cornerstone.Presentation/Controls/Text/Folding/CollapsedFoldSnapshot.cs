#region References

using System.Text.Json.Serialization;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Folding;

/// <summary>
/// Persisted collapsed-fold identity. Offset is the fast match; hint and path recover after external edits.
/// </summary>
public class CollapsedFoldSnapshot
{
	#region Properties

	[JsonPropertyName("hint")]
	public string Hint { get; set; }

	[JsonPropertyName("line")]
	public int Line { get; set; }

	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("path")]
	public string Path { get; set; }

	#endregion
}
