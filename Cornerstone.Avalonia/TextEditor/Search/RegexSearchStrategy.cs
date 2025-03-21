#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

internal class RegexSearchStrategy : ISearchStrategy
{
	#region Fields

	private readonly bool _matchWholeWords;
	private readonly Regex _searchPattern;

	#endregion

	#region Constructors

	public RegexSearchStrategy(Regex searchPattern, bool matchWholeWords)
	{
		_searchPattern = searchPattern ?? throw new ArgumentNullException(nameof(searchPattern));
		_matchWholeWords = matchWholeWords;
	}

	#endregion

	#region Methods

	public bool Equals(ISearchStrategy other)
	{
		var strategy = other as RegexSearchStrategy;
		return (strategy != null) &&
			(strategy._searchPattern.ToString() == _searchPattern.ToString()) &&
			(strategy._searchPattern.Options == _searchPattern.Options) &&
			(strategy._searchPattern.RightToLeft == _searchPattern.RightToLeft);
	}

	public IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length)
	{
		var endOffset = offset + length;
		foreach (Match result in _searchPattern.Matches(document.Text))
		{
			var resultEndOffset = result.Length + result.Index;
			if ((offset > result.Index) || (endOffset < resultEndOffset))
			{
				continue;
			}
			if (_matchWholeWords && (!IsWordBorder(document, result.Index) || !IsWordBorder(document, resultEndOffset)))
			{
				continue;
			}
			yield return new SearchResult { StartOffset = result.Index, Length = result.Length, Data = result };
		}
	}

	public ISearchResult FindNext(ITextSource document, int offset, int length)
	{
		return FindAll(document, offset, length).FirstOrDefault();
	}

	private static bool IsWordBorder(ITextSource document, int offset)
	{
		return TextUtilities.GetNextCaretPosition(document, offset - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder) == offset;
	}

	#endregion
}