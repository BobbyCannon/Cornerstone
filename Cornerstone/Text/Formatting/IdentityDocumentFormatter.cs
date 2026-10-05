#region References

using System.Collections.Generic;
using Cornerstone.Text.Parsing;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Text.Formatting;

public abstract class IdentityDocumentFormatter : IDocumentFormatter
{
	#region Methods

	public abstract bool CanFormat(Tokenizer tokenizer);

	public string Format(IStringBuffer buffer, IEnumerable<Token> tokens, DocumentFormatOptions options)
	{
		return buffer?.ToString() ?? string.Empty;
	}

	#endregion
}