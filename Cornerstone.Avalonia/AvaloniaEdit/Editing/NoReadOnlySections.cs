#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// <see cref="IReadOnlySectionProvider" /> that has no read-only sections; all text is editable.
/// </summary>
internal sealed class NoReadOnlySections : IReadOnlySectionProvider
{
	#region Fields

	public static readonly NoReadOnlySections Instance = new();

	#endregion

	#region Methods

	public bool CanInsert(int offset)
	{
		return true;
	}

	public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
	{
		if (segment == null)
		{
			throw new ArgumentNullException(nameof(segment));
		}
		// the segment is always deletable
		return ExtensionMethods.Sequence(segment);
	}

	#endregion
}

/// <summary>
/// <see cref="IReadOnlySectionProvider" /> that completely disables editing.
/// </summary>
internal sealed class ReadOnlySectionDocument : IReadOnlySectionProvider
{
	#region Fields

	public static readonly ReadOnlySectionDocument Instance = new();

	#endregion

	#region Methods

	public bool CanInsert(int offset)
	{
		return false;
	}

	public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
	{
		return Enumerable.Empty<ISegment>();
	}

	#endregion
}