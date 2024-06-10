#region References

using System;
using System.Collections.Generic;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Search;

/// <summary>
/// Basic interface for search algorithms.
/// </summary>
public interface ISearchStrategy : IEquatable<ISearchStrategy>
{
	#region Methods

	/// <summary>
	/// Finds all matches in the given ITextSource and the given range.
	/// </summary>
	/// <remarks>
	/// This method must be implemented thread-safe.
	/// All segments in the result must be within the given range, and they must be returned in order
	/// (e.g. if two results are returned, EndOffset of first result must be less than or equal StartOffset of second result).
	/// </remarks>
	IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length);

	/// <summary>
	/// Finds the next match in the given ITextSource and the given range.
	/// </summary>
	/// <remarks> This method must be implemented thread-safe. </remarks>
	ISearchResult FindNext(ITextSource document, int offset, int length);

	#endregion
}

/// <summary>
/// Represents a search result.
/// </summary>
public interface ISearchResult : ISegment
{
	#region Methods

	/// <summary>
	/// Replaces parts of the replacement string with parts from the match. (e.g. $1)
	/// </summary>
	string ReplaceWith(string replacement);

	#endregion
}

/// <summary>
/// Defines supported search modes.
/// </summary>
public enum SearchMode
{
	/// <summary>
	/// Standard search
	/// </summary>
	Normal,

	/// <summary>
	/// RegEx search
	/// </summary>
	RegEx,

	/// <summary>
	/// Wildcard search
	/// </summary>
	Wildcard
}

/// <inheritdoc />
public class SearchPatternException : Exception
{
	#region Constructors

	/// <inheritdoc />
	public SearchPatternException()
	{
	}

	/// <inheritdoc />
	public SearchPatternException(string message) : base(message)
	{
	}

	/// <inheritdoc />
	public SearchPatternException(string message, Exception innerException) : base(message, innerException)
	{
	}

	#endregion
}