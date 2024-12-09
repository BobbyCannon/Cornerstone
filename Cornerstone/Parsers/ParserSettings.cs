namespace Cornerstone.Parsers;

/// <summary>
/// The settings for a parser.
/// </summary>
public class ParserSettings
{
	#region Constructors

	/// <summary>
	/// Initialize the parser options.
	/// </summary>
	public ParserSettings()
	{
		IndentCharacter = '\t';
		IndentionCount = 1;
		Minify = false;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The character for indentation.
	/// </summary>
	public char IndentCharacter { get; set; }

	/// <summary>
	/// The quantity of characters per indent.
	/// </summary>
	public int IndentionCount { get; set; }

	/// <summary>
	/// Represents option to minify during processing.
	/// </summary>
	public bool Minify { get; set; }

	#endregion
}