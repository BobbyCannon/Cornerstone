#region References

using System;
using System.Collections.Generic;
using Cornerstone.Text.Parsing;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Text.Formatting;

public static class DocumentFormatter
{
	#region Fields

	private static readonly IDocumentFormatter[] Formatters =
	[
		new JsonDocumentFormatter(),
		new CSharpDocumentFormatter(),
		new XmlDocumentFormatter(),
		new MarkdownDocumentFormatter(),
		new PowerShellDocumentFormatter()
	];

	#endregion

	#region Methods

	public static IDocumentFormatter GetFormatter(Tokenizer tokenizer)
	{
		if (tokenizer == null)
		{
			return null;
		}

		for (var i = 0; i < Formatters.Length; i++)
		{
			if (Formatters[i].CanFormat(tokenizer))
			{
				return Formatters[i];
			}
		}

		return null;
	}

	public static bool TryFormat(Tokenizer tokenizer, IStringBuffer buffer, DocumentFormatOptions options, out string formatted)
	{
		return TryFormat(tokenizer, buffer, null, options, out formatted);
	}

	public static bool TryFormat(Tokenizer tokenizer, IStringBuffer buffer, IEnumerable<Token> tokens, DocumentFormatOptions options, out string formatted)
	{
		formatted = null;
		var formatter = GetFormatter(tokenizer);
		if ((formatter == null) || (buffer == null))
		{
			return false;
		}

		try
		{
			var tokenList = tokens ?? tokenizer.Process();
			formatted = formatter.Format(buffer, tokenList, options ?? new DocumentFormatOptions());
			return true;
		}
		catch
		{
			formatted = null;
			return false;
		}
	}

	#endregion
}