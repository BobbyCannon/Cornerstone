#region References

using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class MarkdownViewTokenizer : Tokenizer
{
	#region Fields

	private static readonly SpeedyQueue<Token> _sharedTokenPool;

	#endregion

	#region Constructors

	public MarkdownViewTokenizer()
		: base(new StringBuffer(), _sharedTokenPool)
	{
	}

	static MarkdownViewTokenizer()
	{
		_sharedTokenPool = new SpeedyQueue<Token>();
	}

	#endregion

	#region Methods

	public override bool GetBold(int type)
	{
		return (type == MarkdownTokenizer.TokenTypeBold)
			|| (type == MarkdownTokenizer.TokenTypeBoldAndItalic);
	}

	public override SyntaxColor GetColor(int type)
	{
		return SyntaxColor.None;
	}

	public override bool GetItalic(int type)
	{
		return (type == MarkdownTokenizer.TokenTypeItalic)
			|| (type == MarkdownTokenizer.TokenTypeBoldAndItalic);
	}

	public override bool GetStrikethrough(int type)
	{
		return type == MarkdownTokenizer.TokenTypeStrikethrough;
	}

	public override bool IsStartCharacter()
	{
		return MarkdownTokenizer.IsStartCharacter(Buffer[Position], AtEndOfLine, AtIndentation, AtWhitespace);
	}

	#endregion
}