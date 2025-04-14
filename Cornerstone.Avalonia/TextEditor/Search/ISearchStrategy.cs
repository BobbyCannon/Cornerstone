#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

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