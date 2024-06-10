namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// Represents a text element in a snippet.
/// </summary>
public class SnippetTextElement : SnippetElement
{
	#region Properties

	/// <summary>
	/// The text to be inserted.
	/// </summary>
	public string Text { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		if (Text != null)
		{
			context.InsertText(Text);
		}
	}

	#endregion

	///// <inheritdoc/>
	//public override Inline ToTextRun()
	//{
	//	return new Run(Text ?? string.Empty);
	//}
}