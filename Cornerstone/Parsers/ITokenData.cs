namespace Cornerstone.Parsers;

/// <summary>
/// Represents data for a token.
/// </summary>
/// <typeparam name="T"> The type of the token. </typeparam>
public interface ITokenData<T>
{
	#region Properties

	/// <summary>
	/// Represents the line number.
	/// </summary>
	public int LineNumber { get; set; }

	/// <summary>
	/// Represents the character position.
	/// </summary>
	public int Position { get; set; }

	/// <summary>
	/// The type of the token.
	/// </summary>
	public T Type { get; set; }

	#endregion
}