#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Represents a version identifier for a text source.
/// </summary>
/// <remarks>
/// Versions can be used to efficiently detect whether a document has changed and needs reparsing;
/// or even to implement incremental parsers.
/// It is a separate class from ITextSource to allow the GC to collect the text source while
/// the version checkpoint is still in use.
/// </remarks>
public interface ITextSourceVersion
{
	#region Methods

	/// <summary>
	/// Gets whether this checkpoint belongs to the same document as the other checkpoint.
	/// </summary>
	/// <remarks>
	/// Returns false when given <c> null </c>.
	/// </remarks>
	bool BelongsToSameDocumentAs(ITextSourceVersion other);

	/// <summary>
	/// Compares the age of this checkpoint to the other checkpoint.
	/// </summary>
	/// <remarks> This method is thread-safe. </remarks>
	/// <exception cref="ArgumentException"> Raised if 'other' belongs to a different document than this version. </exception>
	/// <returns>
	/// -1 if this version is older than <paramref name="other" />.
	/// 0 if <c> this </c> version instance represents the same version as <paramref name="other" />.
	/// 1 if this version is newer than <paramref name="other" />.
	/// </returns>
	int CompareAge(ITextSourceVersion other);

	/// <summary>
	/// Gets the changes from this checkpoint to the other checkpoint.
	/// If 'other' is older than this checkpoint, reverse changes are calculated.
	/// </summary>
	/// <remarks> This method is thread-safe. </remarks>
	/// <exception cref="ArgumentException"> Raised if 'other' belongs to a different document than this checkpoint. </exception>
	IEnumerable<TextChangeEventArgs> GetChangesTo(ITextSourceVersion other);

	/// <summary>
	/// Calculates where the offset has moved in the other buffer version.
	/// </summary>
	/// <exception cref="ArgumentException"> Raised if 'other' belongs to a different document than this checkpoint. </exception>
	int MoveOffsetTo(ITextSourceVersion other, int oldOffset, AnchorMovementType movement = AnchorMovementType.Default);

	#endregion
}