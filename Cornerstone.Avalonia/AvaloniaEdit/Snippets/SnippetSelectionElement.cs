#region References

using System.Text;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// Inserts the previously selected text at the selection marker.
/// </summary>
public class SnippetSelectionElement : SnippetElement
{
	#region Properties

	/// <summary>
	/// Gets/Sets the new indentation of the selected text.
	/// </summary>
	public int Indentation { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		var tabString = new StringBuilder();

		for (var i = 0; i < Indentation; i++)
		{
			tabString.Append(context.Tab);
		}

		var indent = tabString.ToString();

		var text = context.SelectedText.TrimStart(' ', '\t');

		text = text.Replace(context.LineTerminator,
			context.LineTerminator + indent);

		context.Document.Insert(context.InsertionPosition, text);
		context.InsertionPosition += text.Length;

		if (string.IsNullOrEmpty(context.SelectedText))
		{
			SnippetCaretElement.SetCaret(context);
		}
	}

	#endregion
}