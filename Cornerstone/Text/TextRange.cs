#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text;

[SourceReflection]
public partial class TextRange : Notifiable, IComparable<TextRange>
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

	#region Properties

	/// <summary>
	/// The exclusive offset (end) of the range.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Length))]
	public partial int EndOffset { get; set; }

	/// <summary>
	/// The length of the selection.
	/// </summary>
	public int Length => EndOffset - StartOffset;

	/// <summary>
	/// The inclusive offset (start) of the range.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Length))]
	public partial int StartOffset { get; set; }

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

	#endregion
}