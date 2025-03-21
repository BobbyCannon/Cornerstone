#region References

using System;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Snippets;

/// <summary>
/// A code snippet that can be inserted into the text editor.
/// </summary>
public class Snippet : SnippetContainerElement
{
	#region Methods

	/// <summary>
	/// Inserts the snippet into the text area.
	/// </summary>
	public InsertionContext Insert(TextArea textArea)
	{
		if (textArea == null)
		{
			throw new ArgumentNullException(nameof(textArea));
		}

		var selection = textArea.Selection.SurroundingRange;
		var insertionPosition = textArea.Caret.Offset;

		if (selection != null) // if something is selected
			// use selection start instead of caret position,
			// because caret could be at end of selection or anywhere inside.
			// Removal of the selected text causes the caret position to be invalid.
		{
			insertionPosition = selection.StartIndex + TextUtilities.GetWhitespaceAfter(textArea.Document, selection.StartIndex).Length;
		}

		var context = new InsertionContext(textArea, insertionPosition);

		using (context.Document.RunUpdate())
		{
			if (selection != null)
			{
				textArea.Document.Remove(insertionPosition, selection.EndIndex - insertionPosition);
			}
			Insert(context);
			context.RaiseInsertionCompleted(EventArgs.Empty);
		}

		return context;
	}

	#endregion
}