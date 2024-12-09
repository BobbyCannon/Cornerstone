namespace Cornerstone.Parsers;

/// <summary>
/// Represents a parser for text.
/// </summary>
public abstract class Parser<T> : Parser
{
	#region Constructors

	/// <summary>
	/// Initialize the parser.
	/// </summary>
	/// <param name="settings"> The options for the parser. </param>
	protected Parser(T settings)
	{
		Settings = settings;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The options for the parser.
	/// </summary>
	public T Settings { get; }

	#endregion
}

/// <summary>
/// Represents a parser for text.
/// </summary>
public abstract class Parser
{
}