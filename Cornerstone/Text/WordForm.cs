namespace Cornerstone.Text;

/// <summary>
/// Represents the form of a word. Unknown, Full, or Abbreviated.
/// </summary>
public enum WordFormat
{
	/// <summary>
	/// Unknown form.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Represents a full word.
	/// </summary>
	Full = 1,

	/// <summary>
	/// Represents an abbreviated (short) word.
	/// </summary>
	Abbreviation = 2
}