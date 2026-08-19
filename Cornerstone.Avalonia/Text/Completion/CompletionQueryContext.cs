#region References

#endregion

namespace Cornerstone.Avalonia.Text.Completion;

/// <summary>
/// Immutable snapshot of the editor for a completion query. Built on the UI thread
/// so background sources do not read the live document.
/// </summary>
public class CompletionQueryContext
{
	#region Constructors

	public CompletionQueryContext()
	{
		Text = string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// False when the host should not query (for example a terminal command is running).
	/// </summary>
	public bool CanQuery { get; set; } = true;

	public int CaretOffset { get; set; }

	/// <summary>
	/// First document offset that belongs to the editable input (prompt end on a terminal).
	/// </summary>
	public int InputStart { get; set; }

	public string Text { get; set; }

	#endregion

	#region Methods

	public static CompletionQueryContext FromDocument(TextEditorViewModel document)
	{
		if (document == null)
		{
			return new CompletionQueryContext { CanQuery = false };
		}

		var context = new CompletionQueryContext
		{
			Text = document.ToString() ?? string.Empty,
			CaretOffset = document.Caret.Offset,
			InputStart = 0,
			CanQuery = true
		};

		if (document is TerminalViewModel terminal)
		{
			context.InputStart = terminal.PromptOffset;
			context.CanQuery = !terminal.IsCommandProcessing || terminal.IsPromptingForInput;
		}

		return context;
	}

	#endregion
}
