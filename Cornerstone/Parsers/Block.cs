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

	/// <summary>
	/// Emphasis applied by surrounding delimiters after interior expansion (parser-owned nesting).
	/// </summary>
	[Notify]
	public partial bool EmBold { get; set; }

	[Notify]
	public partial bool EmItalic { get; set; }

	[Notify]
	public partial bool EmStrikethrough { get; set; }

	[Notify]
	public partial int[] Offsets { get; set; }

	[Notify]
	public partial int Type { get; set; }

	#endregion

	#region Methods

	public override string ToString()
	{
		var em = (EmBold ? "B" : "") + (EmItalic ? "I" : "") + (EmStrikethrough ? "S" : "");
		var emPart = em.Length > 0 ? $" em={em}" : "";
		return $"{Type} @ {StartOffset}..{EndOffset} ({Length}) [{string.Join(",", Offsets)}]{emPart}";
	}

	public void Update(int type, int startOffset, int endOffset, int[] offsets)
	{
		Type = type;
		StartOffset = startOffset;
		EndOffset = endOffset;
		Offsets = offsets;
		EmBold = false;
		EmItalic = false;
		EmStrikethrough = false;
	}

	#endregion
}