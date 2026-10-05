namespace Cornerstone.VisualStudio.Protocol;

public class CompletionItemMessage
{
	#region Constructors

	public CompletionItemMessage()
	{
		DisplayText = string.Empty;
		InsertText = string.Empty;
		Description = string.Empty;
		Suffix = string.Empty;
		RecommendedCursorOffset = -1;
		DeleteTextOffset = -1;
		Priority = 255;
	}

	#endregion

	#region Properties

	public int DeleteTextOffset { get; set; }

	public string Description { get; set; }

	public string DisplayText { get; set; }

	public string InsertText { get; set; }

	public int Kind { get; set; }

	public byte Priority { get; set; }

	public int RecommendedCursorOffset { get; set; }

	public string Suffix { get; set; }

	public bool TriggerCompletionAfterInsert { get; set; }

	#endregion
}
