#region References

using System.Collections.Generic;
using System.Collections.Specialized;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Provides read-only access to a disjoint union of binary ranges in a document.
/// </summary>
public interface IReadOnlyBitRangeUnion : IReadOnlyCollection<BitRange>, INotifyCollectionChanged
{
	#region Properties

	/// <summary>
	/// Gets the minimum range that encloses all sub ranges included in the union.
	/// </summary>
	BitRange EnclosingRange { get; }

	/// <summary>
	/// Gets a value indicating whether the union consists of multiple disjoint ranges.
	/// </summary>
	bool IsFragmented { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Determines whether the provided location is within the included ranges.
	/// </summary>
	/// <param name="location"> The location. </param>
	/// <returns> <c> true </c> if the location is included, <c> false </c> otherwise. </returns>
	bool Contains(BitLocation location);

	/// <summary>
	/// Gets an enumerator that enumerates all the ranges in the union.
	/// </summary>
	new BitRangeUnion.Enumerator GetEnumerator();

	/// <summary>
	/// Determines whether the provided range intersects with any of the ranges in the union.
	/// </summary>
	/// <param name="range"> The range to test. </param>
	/// <returns> <c> true </c> if the union interesects with the provided range, <c> false </c> otherwise. </returns>
	bool IntersectsWith(BitRange range);

	/// <summary>
	/// Determines whether the provided range is within the included ranges.
	/// </summary>
	/// <param name="range"> The range to test. </param>
	/// <returns> <c> true </c> if the union is a super-set of the provided range, <c> false </c> otherwise. </returns>
	bool IsSuperSetOf(BitRange range);

	#endregion
}