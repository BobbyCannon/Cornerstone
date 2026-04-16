#region References

using System;
using Cornerstone.Data;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public sealed partial class Block : TextRange, IComparable<Block>
{
	#region Constructors

	public Block() : this(0, 0, 0, [])
	{
	}

	public Block(int type, int startOffset, int endOffset, int[] offsets)
		: base(startOffset, endOffset)
	{
		Update(type, startOffset, endOffset, offsets);
	}

	#endregion

	#region Properties

	[Notify]
	public partial int[] Offsets { get; set; }

	[Notify]
	public partial int Type { get; set; }

	#endregion

	#region Methods

	public override string ToString()
	{
		if (Type == MarkdownTokenizer.TokenTypeHeader)
		{
		}

		return $"{Type} @ {StartOffset}..{EndOffset} ({Length}) [{string.Join(",", Offsets)}]";
	}

	public void Update(int type, int startOffset, int endOffset, int[] offsets)
	{
		Type = type;
		StartOffset = startOffset;
		EndOffset = endOffset;
		Offsets = offsets;
	}

	#endregion
}