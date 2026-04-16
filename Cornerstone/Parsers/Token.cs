#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public sealed partial class Token : TextRange
{
	#region Constructors

	public Token() : this(0, 0, 0, SyntaxColor.None, false, false, false)
	{
	}

	public Token(int type, int startOffset, int endOffset, SyntaxColor color,
		bool bold, bool italic, bool strikethrough)
		: base(startOffset, endOffset)
	{
		Update(type, startOffset, endOffset, color, bold, italic, strikethrough);
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool Bold { get; set; }

	[Notify]
	public partial SyntaxColor Color { get; set; }

	[Notify]
	public partial bool Italic { get; set; }

	[Notify]
	public partial bool Strikethrough { get; set; }

	[Notify]
	public partial int Type { get; set; }

	#endregion

	#region Methods

	public override string ToString()
	{
		return $"{Tokenizer.TryGetTokenizerStateOrTypeCode(nameof(Type), this)} @ {StartOffset}..{EndOffset} ({Length})";
	}

	public void Update(int type, int startOffset, int endOffset, SyntaxColor color,
		bool bold, bool italic, bool strikethrough)
	{
		Type = type;
		StartOffset = startOffset;
		EndOffset = endOffset;
		Color = color;
		Bold = bold;
		Italic = italic;
		Strikethrough = strikethrough;
	}

	#endregion
}