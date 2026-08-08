#region References

using System;
using Cornerstone.Reflection;
using Range = Cornerstone.Collections.Range;

#endregion

namespace Cornerstone.Text;

[SourceReflection]
public partial class TextRange : Range, IComparable<TextRange>
{
	#region Constructors

	public TextRange()
	{
	}

	public TextRange(int startOffset, int endOffset)
	{
		StartOffset = startOffset;
		EndOffset = endOffset;
	}

	#endregion

	#region Methods

	public bool Contains(int offset)
	{
		return (offset >= StartOffset)
			&& (offset < EndOffset);
	}

	public bool Overlaps(TextRange range)
	{
		if (range == null)
		{
			return false;
		}

		return (StartOffset == range.StartOffset)
			|| ((StartOffset < range.EndOffset)
				&& (range.StartOffset < EndOffset));
	}

	public void Update(int startOffset, int length)
	{
		StartOffset = startOffset;
		EndOffset = startOffset + length;
	}

	#endregion
}