#region References

using System.Text.RegularExpressions;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

internal class SearchResult : TextRange, ISearchResult
{
	#region Properties

	public Match Data { get; set; }

	#endregion

	#region Methods

	public string ReplaceWith(string replacement)
	{
		return Data.Result(replacement);
	}

	#endregion
}