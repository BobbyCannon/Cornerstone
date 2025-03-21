#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

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

	public IEnumerable<IRange> GetDeletableSegments(IRange range)
	{
		if (range == null)
		{
			throw new ArgumentNullException(nameof(range));
		}
		// the segment is always deletable
		return ExtensionMethods.Sequence(range);
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

	public IEnumerable<IRange> GetDeletableSegments(IRange range)
	{
		return [];
	}

	#endregion
}