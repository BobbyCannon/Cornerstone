#region References

#endregion

using Cornerstone.Avalonia.TextEditor.Document;

namespace Cornerstone.Avalonia.TextEditor.Indentation;

/// <summary>
/// Strategy how the text editor handles indentation when new lines are inserted.
/// </summary>
public interface IIndentationStrategy
{
	#region Methods

	/// <summary>
	/// Sets the indentation for the specified line.
	/// Usually this is constructed from the indentation of the previous line.
	/// </summary>
	void IndentLine(TextEditorDocument document, DocumentLine line);

	/// <summary>
	/// Reindents a set of lines.
	/// </summary>
	void IndentLines(TextEditorDocument document, int beginLine, int endLine);

	#endregion
}