#region References

using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.PowerShell;

#endregion

namespace Cornerstone.Text.Formatting;

public class PowerShellDocumentFormatter : IdentityDocumentFormatter
{
	#region Methods

	public override bool CanFormat(Tokenizer tokenizer)
	{
		return tokenizer is PowerShellTokenizer;
	}

	#endregion
}