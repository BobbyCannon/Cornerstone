namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Provides members describing the possible actions that can be applied to a document.
/// </summary>
public enum BinaryDocumentChangeType
{
	/// <summary>
	/// Indicates the document was modified in-place.
	/// </summary>
	Modify,

	/// <summary>
	/// Indicates bytes were inserted into the document.
	/// </summary>
	Insert,

	/// <summary>
	/// Indicates the bytes were removed from the document.
	/// </summary>
	Remove
}