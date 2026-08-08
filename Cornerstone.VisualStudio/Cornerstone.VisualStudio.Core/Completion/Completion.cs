namespace Cornerstone.VisualStudio.Core.Completion;

public record Completion(
	string DisplayText,
	string InsertText,
	string Description,
	CompletionKind Kind,
	int? RecommendedCursorOffset = null,
	string? Suffix = null,
	int? DeleteTextOffset = null,
	byte Priority = 255
)
{
	#region Constructors

	public Completion(string insertText, CompletionKind kind, string? suffix = null, byte priority = 255) :
		this(insertText, insertText, insertText, kind, Suffix: suffix, Priority: priority)
	{
	}

	public Completion(string displayText, string insertText, CompletionKind kind, string? suffix = null, byte priority = 255) :
		this(displayText, insertText, displayText, kind, Priority: priority)
	{
	}

	#endregion

	#region Properties

	public bool TriggerCompletionAfterInsert { get; init; }

	#endregion

	#region Methods

	public override string ToString()
	{
		return DisplayText;
	}

	#endregion
}