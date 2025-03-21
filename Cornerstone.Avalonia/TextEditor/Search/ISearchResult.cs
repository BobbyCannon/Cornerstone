#region References

using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

/// <summary>
/// Represents a search result.
/// </summary>
public interface ISearchResult : IRange
{
	#region Methods

	/// <summary>
	/// Replaces parts of the replacement string with parts from the match. (e.g. $1)
	/// </summary>
	string ReplaceWith(string replacement);

	#endregion
}