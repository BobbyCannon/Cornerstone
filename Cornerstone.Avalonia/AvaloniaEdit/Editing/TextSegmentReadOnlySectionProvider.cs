#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Implementation for <see cref="IReadOnlySectionProvider" /> that stores the segments
/// in a <see cref="TextSegmentCollection{T}" />.
/// </summary>
public class TextSegmentReadOnlySectionProvider<T> : IReadOnlySectionProvider where T : TextSegment
{
	#region Constructors

	/// <summary>
	/// Creates a new TextSegmentReadOnlySectionProvider instance for the specified document.
	/// </summary>
	public TextSegmentReadOnlySectionProvider(TextDocument textDocument)
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
			.All(segment => (segment.StartOffset >= offset) || (offset >= segment.EndOffset));
	}

	/// <summary>
	/// Gets the deletable segments inside the given segment.
	/// </summary>
	public virtual IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
	{
		if (segment == null)
		{
			throw new ArgumentNullException(nameof(segment));
		}

		if ((segment.Length == 0) && CanInsert(segment.Offset))
		{
			yield return segment;
			yield break;
		}

		var readonlyUntil = segment.Offset;
		foreach (var ts in Segments.FindOverlappingSegments(segment))
		{
			var start = ts.StartOffset;
			var end = start + ts.Length;
			if (start > readonlyUntil)
			{
				yield return new SimpleSegment(readonlyUntil, start - readonlyUntil);
			}
			if (end > readonlyUntil)
			{
				readonlyUntil = end;
			}
		}
		var endOffset = segment.EndOffset;
		if (readonlyUntil < endOffset)
		{
			yield return new SimpleSegment(readonlyUntil, endOffset - readonlyUntil);
		}
	}

	#endregion
}