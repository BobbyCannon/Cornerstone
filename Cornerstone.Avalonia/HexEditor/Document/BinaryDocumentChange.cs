namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Describes a change in a binary document.
/// </summary>
/// <param name="Type"> The action that was performed. </param>
/// <param name="AffectedRange"> The range within the document that was affected. </param>
public record struct BinaryDocumentChange(BinaryDocumentChangeType Type, BitRange AffectedRange)
{
	/// <summary> The action that was performed. </summary>
	public BinaryDocumentChangeType Type { get; set; } = Type;

	/// <summary> The range within the document that was affected. </summary>
	public BitRange AffectedRange { get; set; } = AffectedRange;
}