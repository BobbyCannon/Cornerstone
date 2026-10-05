#region References

using System.Collections.Generic;
using System.Text;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.Json;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Text.Formatting;

public class JsonDocumentFormatter : IDocumentFormatter
{
	#region Methods

	public bool CanFormat(Tokenizer tokenizer)
	{
		return tokenizer is JsonTokenizer;
	}

	public string Format(IStringBuffer buffer, IEnumerable<Token> tokens, DocumentFormatOptions options)
	{
		if (buffer == null)
		{
			return string.Empty;
		}

		options ??= new JsonFormatOptions();
		var jsonOptions = options as JsonFormatOptions;
		var minify = jsonOptions?.Minify ?? false;
		var spaceAfterColon = jsonOptions?.SpaceAfterColon ?? !minify;
		var newLine = options.ResolveNewLine(buffer);

		var significant = new List<Token>();
		if (tokens != null)
		{
			foreach (var token in tokens)
			{
				if ((token == null) || IsIgnorable(token.Type))
				{
					continue;
				}

				significant.Add(token);
			}
		}

		var builder = new StringBuilder(buffer.Count);
		var depth = 0;
		var pendingIndent = false;

		for (var i = 0; i < significant.Count; i++)
		{
			var token = significant[i];
			var type = token.Type;
			var next = (i + 1) < significant.Count ? significant[i + 1] : null;

			if ((type == JsonTokenizer.TokenTypeLeftBrace) || (type == JsonTokenizer.TokenTypeLeftBracket))
			{
				WritePendingIndent(builder, options, depth, pendingIndent, minify);
				pendingIndent = false;
				AppendToken(builder, buffer, token);

				var isEmpty = IsMatchingClose(type, next);
				if (isEmpty)
				{
					AppendToken(builder, buffer, next);
					i++;
					continue;
				}

				if (!minify)
				{
					depth++;
					builder.Append(newLine);
					pendingIndent = true;
				}

				continue;
			}

			if ((type == JsonTokenizer.TokenTypeRightBrace) || (type == JsonTokenizer.TokenTypeRightBracket))
			{
				if (!minify)
				{
					if (depth > 0)
					{
						depth--;
					}

					builder.Append(newLine);
					builder.Append(options.Indent(depth));
				}

				pendingIndent = false;
				AppendToken(builder, buffer, token);
				continue;
			}

			if (type == JsonTokenizer.TokenTypeComma)
			{
				AppendToken(builder, buffer, token);
				if (minify)
				{
					continue;
				}

				builder.Append(newLine);
				pendingIndent = true;
				continue;
			}

			if (type == JsonTokenizer.TokenTypeColon)
			{
				AppendToken(builder, buffer, token);
				if (spaceAfterColon && !minify)
				{
					builder.Append(' ');
				}

				continue;
			}

			WritePendingIndent(builder, options, depth, pendingIndent, minify);
			pendingIndent = false;
			AppendToken(builder, buffer, token);
		}

		return builder.ToString();
	}

	private static void AppendToken(StringBuilder builder, IStringBuffer buffer, Token token)
	{
		if ((token.StartOffset < 0) || (token.Length <= 0) || (token.StartOffset >= buffer.Count))
		{
			return;
		}

		var length = token.Length;
		if ((token.StartOffset + length) > buffer.Count)
		{
			length = buffer.Count - token.StartOffset;
		}

		builder.Append(buffer.Substring(token.StartOffset, length));
	}

	private static bool IsIgnorable(int type)
	{
		return (type == TextProcessor.TokenTypeWhitespace)
			|| (type == TextProcessor.TokenTypeNewLine);
	}

	private static bool IsMatchingClose(int openType, Token next)
	{
		if (next == null)
		{
			return false;
		}

		if (openType == JsonTokenizer.TokenTypeLeftBrace)
		{
			return next.Type == JsonTokenizer.TokenTypeRightBrace;
		}

		if (openType == JsonTokenizer.TokenTypeLeftBracket)
		{
			return next.Type == JsonTokenizer.TokenTypeRightBracket;
		}

		return false;
	}

	private static void WritePendingIndent(StringBuilder builder, DocumentFormatOptions options, int depth, bool pendingIndent, bool minify)
	{
		if (!pendingIndent || minify)
		{
			return;
		}

		builder.Append(options.Indent(depth));
	}

	#endregion
}