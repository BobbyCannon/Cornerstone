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
	/// <param name="options"> The options for the parser. </param>
	protected Parser(T options)
	{
		Options = options;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The options for the parser.
	/// </summary>
	public T Options { get; }

	#endregion
}

/// <summary>
/// Represents a parser for text.
/// </summary>
public abstract class Parser
{
}