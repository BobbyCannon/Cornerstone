namespace Cornerstone.Avalonia.Text;

public readonly struct TextDocumentChangedArgs
{
	#region Constructors

	public TextDocumentChangedArgs(int offset, string text, TextDocumentChangeType type, bool pinViewport = false)
	{
		Offset = offset;
		Text = text;
		Type = type;
		PinViewport = pinViewport;
	}

	#endregion

	#region Properties

	public int Offset { get; init; }

	/// <summary>
	/// When true, the editor keeps the current viewport (bump scroll offset by
	/// inserted height) instead of scrolling to the end.
	/// </summary>
	public bool PinViewport { get; init; }

	public string Text { get; init; }

	public TextDocumentChangeType Type { get; init; }

	#endregion
}