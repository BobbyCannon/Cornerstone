#region References

using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.CSharp;

#endregion

namespace Cornerstone.Text.Formatting;

public class CSharpDocumentFormatter : IdentityDocumentFormatter
{
	#region Methods

	public override bool CanFormat(Tokenizer tokenizer)
	{
		return tokenizer is CSharpTokenizer;
	}

	#endregion
}