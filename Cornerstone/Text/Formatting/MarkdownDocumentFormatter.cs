#region References

using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.Markdown;

#endregion

namespace Cornerstone.Text.Formatting;

public class MarkdownDocumentFormatter : IdentityDocumentFormatter
{
	#region Methods

	public override bool CanFormat(Tokenizer tokenizer)
	{
		return tokenizer is MarkdownTokenizer;
	}

	#endregion
}