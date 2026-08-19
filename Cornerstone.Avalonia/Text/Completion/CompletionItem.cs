#region References

using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Completion;

/// <summary>
/// One candidate in a completion session.
/// </summary>
[SourceReflection]
public class CompletionItem
{
	#region Constructors

	public CompletionItem()
	{
		CompletionText = string.Empty;
		Description = string.Empty;
		DisplayText = string.Empty;
	}

	public CompletionItem(string displayText, string completionText, string description = "", int caretDelta = 0, decimal priority = 0)
	{
		DisplayText = displayText ?? string.Empty;
		CompletionText = completionText ?? string.Empty;
		Description = description ?? string.Empty;
		CaretDelta = caretDelta;
		Priority = priority;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Extra caret movement after the completed text is inserted (for example -1 inside parentheses).
	/// </summary>
	public int CaretDelta { get; set; }

	/// <summary>
	/// Text written into the document.
	/// </summary>
	public string CompletionText { get; set; }

	/// <summary>
	/// Optional tooltip / description.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Text shown in the list and used for filtering.
	/// </summary>
	public string DisplayText { get; set; }

	/// <summary>
	/// Lower values sort first.
	/// </summary>
	public decimal Priority { get; set; }

	#endregion
}
