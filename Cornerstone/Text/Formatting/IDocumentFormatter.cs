#region References

using System.Collections.Generic;
using Cornerstone.Text.Parsing;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Text.Formatting;

public interface IDocumentFormatter
{
	#region Methods

	bool CanFormat(Tokenizer tokenizer);

	string Format(IStringBuffer buffer, IEnumerable<Token> tokens, DocumentFormatOptions options);

	#endregion
}