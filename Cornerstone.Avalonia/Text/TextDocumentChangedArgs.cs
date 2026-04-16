namespace Cornerstone.Avalonia.Text;

public readonly struct TextDocumentChangedArgs
{
	#region Constructors

	public TextDocumentChangedArgs(int offset, string text, TextDocumentChangeType type)
	{
		Offset = offset;
		Text = text;
		Type = type;
	}

	#endregion

	#region Properties

	public int Offset { get; init; }

	public string Text { get; init; }

	public TextDocumentChangeType Type { get; init; }

	#endregion
}