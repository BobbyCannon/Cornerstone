#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Projects already-parsed markdown blocks into a display buffer + token manager.
/// Does not re-parse; structure and nesting come from <see cref="MarkdownParser" />.
/// </summary>
public static class MarkdownInlineProjector
{
	#region Fields

	private static readonly MarkdownViewTokenizer Tokenizer = new();

	#endregion

	#region Methods

	/// <summary>
	/// Typography + read-only presentation policy for every Markdown <see cref="TextRenderer" />.
	/// Always sets <see cref="TextEditorViewModel.ShowCaret" /> to false (selection still works).
	/// Independent of editor <c> IsReadOnly </c> — hosts may show a caret while read-only if they choose.
	/// </summary>
	public static void ApplyMarkdownSurface(TextRenderer renderer, MarkdownView view)
	{
		if (renderer?.ViewModel is null)
		{
			return;
		}

		if (view is not null)
		{
			if (view.FontFamily is not null)
			{
				renderer.FontFamily = view.FontFamily;
			}
			renderer.FontSize = view.FontSize;
			renderer.FontStyle = view.FontStyle;
			renderer.FontWeight = view.FontWeight;
			renderer.Foreground = view.Foreground;
		}

		renderer.ViewModel.WordWrap = view?.WordWrap ?? true;
		renderer.ViewModel.HighlightCurrentLine = false;
		renderer.ViewModel.ShowCaret = false;
		renderer.ViewModel.ShowLineNumbers = false;
		renderer.ViewModel.Caret.IsVisible = false;
	}

	/// <summary>
	/// Parses <paramref name="markdown" /> once, then projects the resulting blocks.
	/// </summary>
	public static IReadOnlyList<MarkdownProjectedLink> Project(
		string markdown,
		TextRenderer renderer,
		MarkdownView view)
	{
		var links = new List<MarkdownProjectedLink>(4);
		if (renderer?.ViewModel is null)
		{
			return links;
		}

		ApplyMarkdownSurface(renderer, view);
		renderer.ViewModel.TokenManager.Initialize(Tokenizer);

		markdown ??= string.Empty;
		if (markdown.Length == 0)
		{
			renderer.Text = string.Empty;
			return links;
		}

		var content = new StringBuffer();
		ProjectFragment(markdown, content, renderer.ViewModel.TokenManager, links);
		TrimTrailingDisplayWhitespace(content, renderer.ViewModel.TokenManager, links);
		renderer.Text = content.ToString();
		return links;
	}

	/// <summary>
	/// Parses a markdown fragment and appends projected display text + tokens into
	/// an existing buffer (used by cells, lists, and full <see cref="Project" />).
	/// Does not trim the full buffer — callers that own a complete surface should
	/// call <see cref="TrimTrailingDisplayWhitespace" /> after the last fragment.
	/// </summary>
	public static void ProjectFragment(
		string markdown,
		StringBuffer content,
		TokenManager tokens,
		List<MarkdownProjectedLink> links)
	{
		if (string.IsNullOrEmpty(markdown) || content is null || tokens is null)
		{
			return;
		}

		links ??= new List<MarkdownProjectedLink>(4);
		var pool = new SpeedyQueue<Block>();
		var parser = new MarkdownParser(new StringBuffer(markdown), pool);
		var source = markdown.AsSpan();

		foreach (var block in parser.Process())
		{
			ProjectBlock(block, source, content, tokens, links);
		}
	}

	/// <summary>
	/// Projects a paragraph group's already-parsed blocks from the document source snapshot.
	/// </summary>
	public static IReadOnlyList<MarkdownProjectedLink> ProjectParagraph(
		MarkdownBlockGroup group,
		ReadOnlySpan<char> source,
		TextRenderer renderer,
		MarkdownView view)
	{
		var links = new List<MarkdownProjectedLink>(4);
		if (group is null || renderer?.ViewModel is null)
		{
			return links;
		}

		ApplyMarkdownSurface(renderer, view);
		renderer.ViewModel.TokenManager.Initialize(Tokenizer);

		group.ContentBuffer.Clear();
		group.Links.Clear();

		// Skip trailing whitespace/newline blocks (belt-and-suspenders with BuildGroups trim).
		var last = group.Blocks.Count - 1;
		while ((last >= 0)
			&& ((group.Blocks[last].Type == TextProcessor.TokenTypeNewLine)
				|| (group.Blocks[last].Type == TextProcessor.TokenTypeWhitespace)))
		{
			last--;
		}

		for (var i = 0; i <= last; i++)
		{
			ProjectBlock(group.Blocks[i], source, group.ContentBuffer, renderer.ViewModel.TokenManager, links);
		}

		TrimTrailingDisplayWhitespace(group.ContentBuffer, renderer.ViewModel.TokenManager, links);

		foreach (var link in links)
		{
			group.Links.Add(link);
		}

		renderer.Text = group.ContentBuffer.ToString();
		return links;
	}

	/// <summary>
	/// Projects an unordered-list block: strips list markers, prefixes bullets, and runs
	/// each item body through the same inline projection as paragraphs/cells.
	/// </summary>
	public static IReadOnlyList<MarkdownProjectedLink> ProjectUnorderedList(
		ReadOnlySpan<char> listSource,
		TextRenderer renderer,
		MarkdownView view)
	{
		var links = new List<MarkdownProjectedLink>(4);
		if (renderer?.ViewModel is null)
		{
			return links;
		}

		ApplyMarkdownSurface(renderer, view);
		renderer.ViewModel.TokenManager.Initialize(Tokenizer);

		var content = new StringBuffer();
		var firstItem = true;

		foreach (var line in listSource.EnumerateLines())
		{
			if (!TrySplitListItem(line, out var indentLength, out var body))
			{
				// Continuation or blank line — keep raw (rare with current list parser)
				if (!firstItem)
				{
					content.Add('\n');
				}
				content.Add(line);
				firstItem = false;
				continue;
			}

			if (!firstItem)
			{
				content.Add('\n');
			}

			// Indent for nested markers (2 spaces per level of leading whitespace)
			var indentSpaces = indentLength > 0 ? (indentLength / 2) * 2 : 0;
			for (var i = 0; i < indentSpaces; i++)
			{
				content.Add(' ');
			}

			content.Add("• ");
			if (!body.IsEmpty)
			{
				ProjectFragment(body.ToString(), content, renderer.ViewModel.TokenManager, links);
			}

			firstItem = false;
		}

		TrimTrailingDisplayWhitespace(content, renderer.ViewModel.TokenManager, links);
		renderer.Text = content.ToString();
		return links;
	}

	/// <summary>
	/// Drops trailing spaces/tabs/newlines from a projected display buffer and clamps tokens/links.
	/// O(trailing length + tokens from the end) — not a full reparse.
	/// Prevents LineManager from painting an extra empty line when content ends with \n.
	/// </summary>
	public static void TrimTrailingDisplayWhitespace(
		StringBuffer content,
		TokenManager tokens,
		List<MarkdownProjectedLink> links)
	{
		if ((content is null) || (content.Count == 0))
		{
			return;
		}

		var end = content.Count;
		while (end > 0)
		{
			var c = content[end - 1];
			if (c is not (' ' or '\t' or '\r' or '\n'))
			{
				break;
			}

			end--;
		}

		if (end == content.Count)
		{
			return;
		}

		content.RemoveRange(end, content.Count - end);

		if (tokens is not null)
		{
			for (var i = tokens.Count - 1; i >= 0; i--)
			{
				var token = tokens[i];
				if (token.StartOffset >= end)
				{
					tokens.RemoveAt(i);
					continue;
				}

				if (token.EndOffset > end)
				{
					token.EndOffset = end;
				}

				// Tokens are appended in order; once fully inside range, earlier ones are fine.
				break;
			}
		}

		if (links is null)
		{
			return;
		}

		for (var i = links.Count - 1; i >= 0; i--)
		{
			if (links[i].StartOffset >= end)
			{
				links.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Splits a list line into leading indent length and item body (after marker + whitespace).
	/// </summary>
	public static bool TrySplitListItem(ReadOnlySpan<char> line, out int indentLength, out ReadOnlySpan<char> body)
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

	private static void AppendStyled(
		StringBuffer content,
		TokenManager tokens,
		ReadOnlySpan<char> text,
		bool bold,
		bool italic,
		bool strikethrough)
	{
		if (text.IsEmpty)
		{
			return;
		}

		var bufferStart = content.WritePosition;
		content.Add(text);
		var bufferEnd = content.WritePosition;

		if (!bold && !italic && !strikethrough)
		{
			return;
		}

		var type = bold && italic
			? MarkdownTokenizer.TokenTypeBoldAndItalic
			: bold
				? MarkdownTokenizer.TokenTypeBold
				: italic
					? MarkdownTokenizer.TokenTypeItalic
					: MarkdownTokenizer.TokenTypeStrikethrough;

		var token = Tokenizer.CreateOrUpdateSection(
			type,
			bufferStart,
			bufferEnd,
			bold: bold,
			italic: italic,
			strikethrough: strikethrough);
		tokens.Add(token);
	}

	private static void ProjectBlock(
		Block block,
		ReadOnlySpan<char> source,
		StringBuffer content,
		TokenManager tokens,
		List<MarkdownProjectedLink> links)
	{
		if (block is null)
		{
			return;
		}

		// Legacy leaf emphasis types (if any remain) + Em* from expanded nesting
		var bold = block.EmBold
			|| (block.Type == MarkdownTokenizer.TokenTypeBold)
			|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic);
		var italic = block.EmItalic
			|| (block.Type == MarkdownTokenizer.TokenTypeItalic)
			|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic);
		var strike = block.EmStrikethrough
			|| (block.Type == MarkdownTokenizer.TokenTypeStrikethrough);

		// Legacy container-style emphasis with offsets (parser normally expands these)
		if (((block.Type == MarkdownTokenizer.TokenTypeBold)
				|| (block.Type == MarkdownTokenizer.TokenTypeItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeStrikethrough))
			&& block.Offsets is { Length: >= 2 }
			&& (block.Offsets[0] >= 0)
			&& (block.Offsets[1] <= source.Length)
			&& (block.Offsets[1] >= block.Offsets[0]))
		{
			var start = block.Offsets[0];
			var end = block.Offsets[1];
			AppendStyled(content, tokens, source.Slice(start, end - start), bold, italic, strike);
			return;
		}

		if ((block.Type == MarkdownTokenizer.TokenTypeLink) && block.Offsets is { Length: >= 4 })
		{
			var textStart = block.Offsets[0];
			var textEnd = block.Offsets[1];
			var destStart = block.Offsets[2];
			var destEnd = block.Offsets[3];
			var textLength = textEnd - textStart;
			if ((textLength >= 0)
				&& (textStart >= 0)
				&& (textEnd <= source.Length)
				&& (destStart >= 0)
				&& (destEnd <= source.Length)
				&& (destEnd >= destStart))
			{
				var bufferStart = content.WritePosition;
				var linkText = source.Slice(textStart, textLength);
				var href = source.Slice(destStart, destEnd - destStart).ToString();
				content.Add(linkText);
				var bufferEnd = bufferStart + textLength;
				var token = Tokenizer.CreateOrUpdateSection(
					MarkdownTokenizer.TokenTypeLink,
					bufferStart,
					bufferEnd,
					bold: bold,
					italic: italic,
					strikethrough: strike);
				tokens.Add(token);
				links.Add(new MarkdownProjectedLink(bufferStart, bufferEnd, href, linkText.ToString()));
				return;
			}
		}

		if (MarkdownView.IsBlockLevel(block))
		{
			AppendStyled(content, tokens, source.Slice(block.StartOffset, block.Length), bold, italic, strike);
			return;
		}

		if ((block.Type == MarkdownTokenizer.TokenTypeInlineCode)
			&& block.Offsets is { Length: >= 2 }
			&& (block.Offsets[0] >= 0)
			&& (block.Offsets[1] <= source.Length)
			&& (block.Offsets[1] > block.Offsets[0]))
		{
			var bufferStart = content.WritePosition;
			content.Add(source.Slice(block.Offsets[0], block.Offsets[1] - block.Offsets[0]));
			var token = Tokenizer.CreateOrUpdateSection(
				MarkdownTokenizer.TokenTypeInlineCode,
				bufferStart,
				content.WritePosition,
				bold: bold,
				italic: italic,
				strikethrough: strike);
			tokens.Add(token);
			return;
		}

		// Text / whitespace / newlines / expanded emphasis leaves
		if ((block.StartOffset >= 0)
			&& (block.EndOffset <= source.Length)
			&& (block.Length > 0))
		{
			AppendStyled(content, tokens, source.Slice(block.StartOffset, block.Length), bold, italic, strike);
		}
	}

	#endregion
}