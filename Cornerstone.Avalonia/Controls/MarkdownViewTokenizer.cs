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

	public override bool GetItalic(int type)
	{
		return (type == MarkdownTokenizer.TokenTypeItalic)
			|| (type == MarkdownTokenizer.TokenTypeBoldAndItalic);
	}

	public override bool GetStrikethrough(int type)
	{
		return type == MarkdownTokenizer.TokenTypeStrikethrough;
	}

	public override SyntaxKind GetSyntaxKind(int type)
	{
		// Links are painted with Theme.GetAccentBrush() in TextRenderer, not syntax colors.
		// Inline code uses String so it is visually distinct from surrounding prose.
		if (type == MarkdownTokenizer.TokenTypeLink)
		{
			return SyntaxKind.None;
		}

		if (type == MarkdownTokenizer.TokenTypeInlineCode)
		{
			return SyntaxKind.String;
		}

		return SyntaxKind.None;
	}

	public override bool IsStartCharacter()
	{
		return MarkdownTokenizer.IsStartCharacter(Buffer[Position], AtEndOfLine, AtIndentation, AtWhitespace);
	}

	#endregion
}