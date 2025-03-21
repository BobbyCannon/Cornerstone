#region References

using System.Collections.Generic;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Determines whether the document can be modified.
/// </summary>
public interface IReadOnlySectionProvider
{
	#region Methods

	/// <summary>
	/// Gets whether insertion is possible at the specified offset.
	/// </summary>
	bool CanInsert(int offset);

	/// <summary>
	/// Gets the deletable segments inside the given segment.
	/// </summary>
	/// <remarks>
	/// All segments in the result must be within the given segment, and they must be returned in order
	/// (e.g. if two segments are returned, EndOffset of first segment must be less than StartOffset of second segment).
	/// For replacements, the last segment being returned will be replaced with the new text. If an empty list is returned,
	/// no replacement will be done.
	/// </remarks>
	IEnumerable<IRange> GetDeletableSegments(IRange range);

	#endregion
}