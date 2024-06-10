#region References

#endregion

using Cornerstone.Text.Document;

namespace Cornerstone.Avalonia.AvaloniaEdit.Indentation;

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
	void IndentLine(TextDocument document, DocumentLine line);

	/// <summary>
	/// Reindents a set of lines.
	/// </summary>
	void IndentLines(TextDocument document, int beginLine, int endLine);

	#endregion
}