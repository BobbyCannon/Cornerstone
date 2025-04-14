#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Collections;
using TextRange = Cornerstone.Avalonia.TextEditor.Document.TextRange;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Implementation for <see cref="IReadOnlySectionProvider" /> that stores the segments
/// in a <see cref="TextSegmentCollection{T}" />.
/// </summary>
public class TextSegmentReadOnlySectionProvider<T> : IReadOnlySectionProvider where T : TextRange
{
	#region Constructors

	/// <summary>
	/// Creates a new TextSegmentReadOnlySectionProvider instance for the specified document.
	/// </summary>
	public TextSegmentReadOnlySectionProvider(TextEditorDocument textDocument)
	{
		Segments = new TextSegmentCollection<T>(textDocument);
	}

	/// <summary>
	/// Creates a new TextSegmentReadOnlySectionProvider instance using the specified TextSegmentCollection.
	/// </summary>
	public TextSegmentReadOnlySectionProvider(TextSegmentCollection<T> segments)
	{
		Segments = segments ?? throw new ArgumentNullException(nameof(segments));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the collection storing the read-only segments.
	/// </summary>
	public TextSegmentCollection<T> Segments { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets whether insertion is possible at the specified offset.
	/// </summary>
	public virtual bool CanInsert(int offset)
	{
		return Segments.FindSegmentsContaining(offset)
			.All(segment => (segment.StartOffset >= offset) || (offset >= segment.EndIndex));
	}

	/// <summary>
	/// Gets the deletable segments inside the given segment.
	/// </summary>
	public virtual IEnumerable<IRange> GetDeletableSegments(IRange range)
	{
		if (range == null)
		{
			throw new ArgumentNullException(nameof(range));
		}

		if ((range.Length == 0) && CanInsert(range.StartIndex))
		{
			yield return range;
			yield break;
		}

		var readonlyUntil = range.StartIndex;
		foreach (var ts in Segments.FindOverlappingSegments(range))
		{
			var start = ts.StartOffset;
			var end = start + ts.Length;
			if (start > readonlyUntil)
			{
				yield return new SimpleRange(readonlyUntil, start - readonlyUntil);
			}
			if (end > readonlyUntil)
			{
				readonlyUntil = end;
			}
		}
		var endOffset = range.EndIndex;
		if (readonlyUntil < endOffset)
		{
			yield return new SimpleRange(readonlyUntil, endOffset - readonlyUntil);
		}
	}

	#endregion
}