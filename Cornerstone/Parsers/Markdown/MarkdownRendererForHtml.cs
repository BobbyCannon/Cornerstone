#region References

using System;
using System.Collections.Generic;
using System.Net;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Markdown;

[SourceReflection]
public class MarkdownRendererForHtml : MarkdownRenderer
{
	#region Constructors

	public MarkdownRendererForHtml()
		: this(new StringBuffer())
	{
	}

	public MarkdownRendererForHtml(StringBuffer buffer)
	{
		Buffer = buffer;
	}

	#endregion

	#region Properties

	protected StringBuffer Buffer { get; }

	#endregion

	#region Methods

	public string ToHtml(string markdown)
	{
		Buffer.Clear();
		markdown ??= string.Empty;
		var source = new StringBuffer(markdown);
		var parser = new MarkdownParser(source, null);
		var blocks = new List<Block>();
		foreach (var block in parser.Process())
		{
			blocks.Add(block);
		}

		var groups = BuildGroups(blocks, source.AsSpan());
		var first = true;
		foreach (var group in groups)
		{
			if (!first)
			{
				Buffer.Append('\n');
			}

			first = false;
			if ((group.Count == 1) && IsBlockLevel(group[0]))
			{
				AppendBlockLevel(source.AsSpan(), group[0]);
			}
			else
			{
				Buffer.Append("<p>");
				AppendInlines(source.AsSpan(), group);
				Buffer.Append("</p>");
			}
		}

		return Buffer.ToString();
	}

	private static bool IsBlockLevel(Block block)
	{
		return (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
			|| (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
			|| (block.Type == MarkdownTokenizer.TokenTypeHeader)
			|| (block.Type == MarkdownTokenizer.TokenTypeHorizontalRule)
			|| (block.Type == MarkdownTokenizer.TokenTypeTable)
			|| (block.Type == MarkdownTokenizer.TokenTypeUnorderedList);
	}

	private static bool IsIgnorableDisplayWhitespace(Block block)
	{
		return (block.Type == TextProcessor.TokenTypeNewLine)
			|| (block.Type == TextProcessor.TokenTypeWhitespace);
	}

	private static bool IsParagraphBreak(IReadOnlyList<Block> blocks, int newlineIndex, out int resumeAt)
	{
		resumeAt = newlineIndex + 1;
		var j = newlineIndex + 1;
		var sawSecondNewline = false;

		while (j < blocks.Count)
		{
			var b = blocks[j];
			if (b.Type == TextProcessor.TokenTypeWhitespace)
			{
				j++;
				continue;
			}

			if (b.Type == TextProcessor.TokenTypeNewLine)
			{
				sawSecondNewline = true;
				j++;
				continue;
			}

			resumeAt = j;
			return sawSecondNewline;
		}

		resumeAt = j;
		return sawSecondNewline;
	}

	private void AppendBlockLevel(ReadOnlySpan<char> source, Block block)
	{
		if (block.Type == MarkdownTokenizer.TokenTypeHeader)
		{
			var (size, contentStart, contentLength) = ExtractHeaderInfo(source, block);
			size = Math.Clamp(size, 1, 6);
			var title = SafeSlice(source, contentStart, contentLength);
			var headingId = MarkdownLink.ToHeadingId(title);
			Buffer.Append("<h");
			Buffer.Append(size.ToString());
			if (headingId.Length > 0)
			{
				Buffer.Append(" id=\"");
				AppendEscaped(headingId);
				Buffer.Append('"');
			}

			Buffer.Append('>');
			AppendEscaped(title);
			Buffer.Append("</h");
			Buffer.Append(size.ToString());
			Buffer.Append('>');
			return;
		}

		if (block.Type == MarkdownTokenizer.TokenTypeHorizontalRule)
		{
			Buffer.Append("<hr />");
			return;
		}

		if (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
		{
			var (language, contentStart, contentLength) = ExtractCodeBlockInfo(source, block);
			var hasLanguage = !string.IsNullOrEmpty(language);
			if (hasLanguage)
			{
				Buffer.Append("<div class=\"code-block\"><div class=\"code-block-header\">");
				AppendEscaped(language);
				Buffer.Append("</div>");
			}

			Buffer.Append("<pre><code");
			if (hasLanguage)
			{
				Buffer.Append(" class=\"language-");
				AppendEscaped(language);
				Buffer.Append('"');
			}

			Buffer.Append('>');
			AppendEscaped(SafeSlice(source, contentStart, contentLength));
			Buffer.Append("</code></pre>");
			if (hasLanguage)
			{
				Buffer.Append("</div>");
			}

			return;
		}

		if (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
		{
			AppendBlockQuote(SafeSlice(source, block));
			return;
		}

		if (block.Type == MarkdownTokenizer.TokenTypeUnorderedList)
		{
			AppendUnorderedList(SafeSlice(source, block));
			return;
		}

		if (block.Type == MarkdownTokenizer.TokenTypeTable)
		{
			AppendTable(SafeSlice(source, block));
		}
	}

	private void AppendBlockQuote(ReadOnlySpan<char> quoteSource)
	{
		Buffer.Append("<blockquote>");
		var inner = new StringBuffer();
		var first = true;
		foreach (var line in quoteSource.EnumerateLines())
		{
			if (!first)
			{
				inner.Append('\n');
			}

			first = false;
			inner.Append(StripQuotePrefix(line));
		}

		AppendInlineMarkdown(inner.ToString());
		Buffer.Append("</blockquote>");
	}

	private void AppendEmphasisOpen(bool strike, bool italic, bool bold)
	{
		if (strike)
		{
			Buffer.Append("<del>");
		}

		if (italic)
		{
			Buffer.Append("<em>");
		}

		if (bold)
		{
			Buffer.Append("<strong>");
		}
	}

	private void AppendEmphasisClose(bool strike, bool italic, bool bold)
	{
		if (bold)
		{
			Buffer.Append("</strong>");
		}

		if (italic)
		{
			Buffer.Append("</em>");
		}

		if (strike)
		{
			Buffer.Append("</del>");
		}
	}

	private void AppendEscaped(ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
		{
			return;
		}

		AppendEscaped(text.ToString());
	}

	private void AppendEscaped(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		Buffer.Append(WebUtility.HtmlEncode(text));
	}

	private void AppendInlineMarkdown(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return;
		}

		var source = new StringBuffer(markdown);
		var parser = new MarkdownParser(source, null);
		var blocks = new List<Block>();
		foreach (var block in parser.Process())
		{
			if (IsBlockLevel(block))
			{
				continue;
			}

			blocks.Add(block);
		}

		AppendInlines(source.AsSpan(), blocks);
	}

	private void AppendInlines(ReadOnlySpan<char> source, IReadOnlyList<Block> blocks)
	{
		var last = blocks.Count - 1;
		while ((last >= 0) && IsIgnorableDisplayWhitespace(blocks[last]))
		{
			last--;
		}

		for (var i = 0; i <= last; i++)
		{
			AppendInlineBlock(source, blocks[i], trimTrailingBreaks: i == last);
		}
	}

	private void AppendInlineBlock(ReadOnlySpan<char> source, Block block, bool trimTrailingBreaks = false)
	{
		if (block is null)
		{
			return;
		}

		if (block.Type == TextProcessor.TokenTypeNewLine)
		{
			if (!trimTrailingBreaks)
			{
				Buffer.Append("<br />");
			}

			return;
		}

		var bold = block.EmBold
			|| (block.Type == MarkdownTokenizer.TokenTypeBold)
			|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic);
		var italic = block.EmItalic
			|| (block.Type == MarkdownTokenizer.TokenTypeItalic)
			|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic);
		var strike = block.EmStrikethrough
			|| (block.Type == MarkdownTokenizer.TokenTypeStrikethrough);

		if (((block.Type == MarkdownTokenizer.TokenTypeBold)
				|| (block.Type == MarkdownTokenizer.TokenTypeItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeStrikethrough))
			&& (block.Offsets is { Length: >= 2 }))
		{
			var emphasis = SafeSlice(source, block.Offsets[0], block.Offsets[1] - block.Offsets[0]);
			AppendEmphasisOpen(strike, italic, bold);
			AppendEscaped(emphasis);
			AppendEmphasisClose(strike, italic, bold);
			return;
		}

		if ((block.Type == MarkdownTokenizer.TokenTypeLink) && (block.Offsets is { Length: >= 4 }))
		{
			var text = SafeSlice(source, block.Offsets[0], block.Offsets[1] - block.Offsets[0]);
			var href = SafeSlice(source, block.Offsets[2], block.Offsets[3] - block.Offsets[2]);
			AppendEmphasisOpen(strike, italic, bold);
			Buffer.Append("<a href=\"");
			AppendEscaped(href);
			Buffer.Append("\">");
			AppendEscaped(text);
			Buffer.Append("</a>");
			AppendEmphasisClose(strike, italic, bold);
			return;
		}

		if ((block.Type == MarkdownTokenizer.TokenTypeInlineCode) && (block.Offsets is { Length: >= 2 }))
		{
			var code = SafeSlice(source, block.Offsets[0], block.Offsets[1] - block.Offsets[0]);
			AppendEmphasisOpen(strike, italic, bold);
			Buffer.Append("<code>");
			AppendEscaped(code);
			Buffer.Append("</code>");
			AppendEmphasisClose(strike, italic, bold);
			return;
		}

		var leaf = SafeSlice(source, block);
		if (trimTrailingBreaks)
		{
			while (!leaf.IsEmpty && (leaf[^1] is '\r' or '\n'))
			{
				leaf = leaf[..^1];
			}
		}

		if (leaf.IsEmpty)
		{
			return;
		}

		AppendEmphasisOpen(strike, italic, bold);
		AppendDisplayText(leaf);
		AppendEmphasisClose(strike, italic, bold);
	}

	private void AppendDisplayText(ReadOnlySpan<char> text)
	{
		var start = 0;
		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] is not ('\r' or '\n'))
			{
				continue;
			}

			if (i > start)
			{
				AppendEscaped(text.Slice(start, i - start));
			}

			Buffer.Append("<br />");
			if ((text[i] == '\r') && ((i + 1) < text.Length) && (text[i + 1] == '\n'))
			{
				i++;
			}

			start = i + 1;
		}

		if (start < text.Length)
		{
			AppendEscaped(text[start..]);
		}
	}

	private void AppendTable(ReadOnlySpan<char> tableSource)
	{
		var model = MarkdownTableModel.Parse(tableSource);
		if ((model.ColumnCount == 0) || (model.Rows.Count == 0))
		{
			return;
		}

		Buffer.Append("<table>");
		var start = 0;
		if (model.HasHeader)
		{
			Buffer.Append("<thead><tr>");
			AppendTableCells(model.Rows[0], model, header: true);
			Buffer.Append("</tr></thead>");
			start = 1;
		}

		if (start < model.Rows.Count)
		{
			Buffer.Append("<tbody>");
			for (var r = start; r < model.Rows.Count; r++)
			{
				Buffer.Append("<tr>");
				AppendTableCells(model.Rows[r], model, header: false);
				Buffer.Append("</tr>");
			}

			Buffer.Append("</tbody>");
		}

		Buffer.Append("</table>");
	}

	private void AppendTableCells(MarkdownTableRow row, MarkdownTableModel model, bool header)
	{
		var tag = header ? "th" : "td";
		for (var c = 0; c < model.ColumnCount; c++)
		{
			var cell = c < row.Cells.Count ? row.Cells[c].Source : string.Empty;
			var align = c < model.Alignments.Count ? model.Alignments[c] : ColumnAlignment.Left;
			Buffer.Append('<');
			Buffer.Append(tag);
			if (align != ColumnAlignment.Left)
			{
				Buffer.Append(" style=\"text-align:");
				Buffer.Append(align == ColumnAlignment.Center ? "center" : "right");
				Buffer.Append('"');
			}

			Buffer.Append('>');
			if (header)
			{
				Buffer.Append("<strong>");
			}

			AppendInlineMarkdown(cell);
			if (header)
			{
				Buffer.Append("</strong>");
			}

			Buffer.Append("</");
			Buffer.Append(tag);
			Buffer.Append('>');
		}
	}

	private void AppendUnorderedList(ReadOnlySpan<char> listSource)
	{
		Buffer.Append("<ul>");
		foreach (var line in listSource.EnumerateLines())
		{
			if (!TrySplitListItem(line, out var indentLength, out var body))
			{
				continue;
			}

			Buffer.Append("<li>");
			var indentSpaces = indentLength > 0 ? (indentLength / 2) * 2 : 0;
			for (var i = 0; i < indentSpaces; i++)
			{
				Buffer.Append(' ');
			}

			AppendInlineMarkdown(body.ToString());
			Buffer.Append("</li>");
		}

		Buffer.Append("</ul>");
	}

	private static bool IsParagraphBreakingNewline(ReadOnlySpan<char> source, Block block)
	{
		var slice = SafeSlice(source, block);
		var lineBreaks = 0;
		for (var i = 0; i < slice.Length; i++)
		{
			if (slice[i] == '\n')
			{
				lineBreaks++;
			}
			else if ((slice[i] == '\r') && (((i + 1) >= slice.Length) || (slice[i + 1] != '\n')))
			{
				lineBreaks++;
			}
		}

		return lineBreaks >= 2;
	}

	private static List<List<Block>> BuildGroups(List<Block> parsedBlocks, ReadOnlySpan<char> source)
	{
		var parsedGroups = new List<List<Block>>();
		List<Block> currentParagraph = null;

		for (var i = 0; i < parsedBlocks.Count; i++)
		{
			var block = parsedBlocks[i];
			if (IsBlockLevel(block))
			{
				currentParagraph = null;
				parsedGroups.Add([block]);
				continue;
			}

			if (currentParagraph is null)
			{
				if (IsIgnorableDisplayWhitespace(block))
				{
					continue;
				}

				currentParagraph = [];
				parsedGroups.Add(currentParagraph);
				currentParagraph.Add(block);
				continue;
			}

			if (block.Type == TextProcessor.TokenTypeNewLine)
			{
				if (IsParagraphBreakingNewline(source, block))
				{
					currentParagraph = null;
					continue;
				}

				if (IsParagraphBreak(parsedBlocks, i, out var resumeAt))
				{
					currentParagraph = null;
					i = resumeAt - 1;
					continue;
				}
			}

			currentParagraph.Add(block);
		}

		for (var i = parsedGroups.Count - 1; i >= 0; i--)
		{
			var group = parsedGroups[i];
			while (group.Count > 0)
			{
				var last = group[^1];
				if (IsIgnorableDisplayWhitespace(last))
				{
					group.RemoveAt(group.Count - 1);
				}
				else
				{
					break;
				}
			}

			if (group.Count == 0)
			{
				parsedGroups.RemoveAt(i);
			}
		}

		return parsedGroups;
	}

	private static ReadOnlySpan<char> StripQuotePrefix(ReadOnlySpan<char> line)
	{
		var i = 0;
		while ((i < line.Length) && ((line[i] == ' ') || (line[i] == '\t')))
		{
			i++;
		}

		if ((i < line.Length) && (line[i] == '>'))
		{
			i++;
			if ((i < line.Length) && (line[i] == ' '))
			{
				i++;
			}
		}

		return line[i..];
	}

	private static bool TrySplitListItem(ReadOnlySpan<char> line, out int indentLength, out ReadOnlySpan<char> body)
	{
		indentLength = 0;
		body = default;

		var i = 0;
		while ((i < line.Length) && ((line[i] == ' ') || (line[i] == '\t')))
		{
			i++;
		}

		indentLength = i;
		if (i >= line.Length)
		{
			return false;
		}

		var marker = line[i];
		if ((marker != '-') && (marker != '*') && (marker != '+'))
		{
			return false;
		}

		i++;
		if ((i >= line.Length) || !char.IsWhiteSpace(line[i]))
		{
			return false;
		}

		while ((i < line.Length) && char.IsWhiteSpace(line[i]))
		{
			i++;
		}

		body = line[i..];
		return true;
	}

	#endregion
}
