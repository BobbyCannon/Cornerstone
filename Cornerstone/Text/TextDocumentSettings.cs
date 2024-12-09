namespace Cornerstone.Text;

public class TextDocumentSettings
{
	#region Constructors

	public TextDocumentSettings()
	{
		IndentCharacter = '\t';
		IndentionCount = 1;
		EndOfLineCharacters = "\r\n";
	}

	#endregion

	#region Properties

	/// <summary>
	/// The character that represents the end of line (EOL) characters.
	/// </summary>
	public string EndOfLineCharacters { get; set; }

	/// <summary>
	/// The character for indentation.
	/// </summary>
	public char IndentCharacter { get; set; }

	/// <summary>
	/// The quantity of characters per indent.
	/// </summary>
	public int IndentionCount { get; set; }

	#endregion
}