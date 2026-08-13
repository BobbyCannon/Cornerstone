#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Avalonia.Text;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class MarkdownBlockConverter : IMultiValueConverter
{
	#region Fields

	public static readonly Thickness BlockQuoteBorderThickness;
	public static readonly CornerRadius BlockQuoteCornerRadius;
	public static readonly Thickness CodeBlockBorderPadding;
	public static readonly Thickness CodeBlockBorderThickness;
	public static readonly CornerRadius CodeBlockCornerRadius;
	public static readonly CornerRadius ZeroCornerRadius;
	public static readonly Thickness ZeroThickness;
	private static readonly MarkdownViewTokenizer _markdownViewTokenizer;

	#endregion

	#region Constructors

	static MarkdownBlockConverter()
	{
		_markdownViewTokenizer = new MarkdownViewTokenizer();

		BlockQuoteBorderThickness = new(1);
		BlockQuoteCornerRadius = new(4);
		CodeBlockBorderThickness = new(1);
		CodeBlockBorderPadding = new(10);
		CodeBlockCornerRadius = new(0, 0, 4, 4);
		ZeroCornerRadius = new(0);
		ZeroThickness = new(0);
	}

	#endregion

	#region Methods

	public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [MarkdownBlockGroup blockGroup, TextRenderer renderer, Grid grid, MarkdownView view])
		{
			return string.Empty;
		}

		try
		{
			var header = grid.FindDescendantOfType<Border>(false, x => x.Name == "Header");
			var border = grid.FindDescendantOfType<Border>(false, x => x.Name == "Border");
			if ((header == null) || (border == null))
			{
				return string.Empty;
			}

			// Prefer last render snapshot when available (stable for presenters while Document may still append).
			var source = view.SourceSnapshot;
			if (string.IsNullOrEmpty(source))
			{
				source = view.Document.ToString();
			}

			var buffer = source.AsSpan();
			MarkdownInlineProjector.ApplyMarkdownSurface(renderer, view);
			header.Background = null;
			header.IsVisible = false;
			renderer.IsVisible = true;
			border.Background = null;
			border.BorderThickness = ZeroThickness;
			border.CornerRadius = ZeroCornerRadius;
			border.Margin = ZeroThickness;
			border.Padding = ZeroThickness;

			return (blockGroup.Blocks.Count == 1)
				&& MarkdownView.IsBlockLevel(blockGroup.Blocks[0])
					? ProcessSingleBlock(view, header, border, renderer, blockGroup, buffer)
					: ProcessParagraph(view, border, renderer, blockGroup, buffer);
		}
		catch
		{
			return string.Empty;
		}
	}

	private string ProcessParagraph(MarkdownView view, Border border, TextRenderer renderer, MarkdownBlockGroup group, ReadOnlySpan<char> buffer)
	{
		group.ContentBuffer.Clear();
		renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);

		foreach (var block in group.Blocks)
		{
			if ((block.Type == MarkdownTokenizer.TokenTypeBold)
				|| (block.Type == MarkdownTokenizer.TokenTypeItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeStrikethrough))
			{
				if ((block.Offsets is not { Length: >= 2 }))
				{
					continue;
				}

				var start = block.Offsets[0];
				var end = block.Offsets[1];
				var emphasis = MarkdownRenderer.SafeSlice(buffer, start, end - start);
				if (emphasis.IsEmpty)
				{
					continue;
				}

				var bufferStart = group.ContentBuffer.WritePosition;
				var bufferEnd = bufferStart + emphasis.Length;
				group.ContentBuffer.Add(emphasis);
				renderer.ViewModel.TokenManager.Add(block.Type, bufferStart, bufferEnd);
			}
			else
			{
				var body = MarkdownRenderer.SafeSlice(buffer, block);
				if (!body.IsEmpty)
				{
					group.ContentBuffer.Add(body);
				}
			}
		}

		return group.ContentBuffer.ToString();
	}

	private string ProcessSingleBlock(MarkdownView view, Border header, Border border, TextRenderer renderer, MarkdownBlockGroup group, ReadOnlySpan<char> buffer)
	{
		var block = group.Blocks[0];
		if (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
		{
			renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			renderer.Foreground = view.Foreground;
			border.Background = ResourceService.GetColorAsBrush("Background04");
			border.BorderThickness = BlockQuoteBorderThickness;
			border.CornerRadius = BlockQuoteCornerRadius;
			border.Padding = CodeBlockBorderPadding;
			var quote = MarkdownRenderer.SafeSlice(buffer, block);
			return quote.IsEmpty ? string.Empty : quote.ToString();
		}

		if (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
		{
			var (language, contentStart, contentLength) = MarkdownRenderer.ExtractCodeBlockInfo(buffer, block);
			header.Background = ResourceService.GetColorAsBrush("Background04");
			header.IsVisible = true;
			var headerTitle = header.FindDescendantOfType<TextBlock>(false, x => x.Name == "HeaderTitle");
			headerTitle?.Text = language;
			renderer.ViewModel.TokenManager.Initialize(language);
			renderer.FontSize = view.FontSize;
			renderer.Foreground = view.Foreground;
			border.Background = ResourceService.GetColorAsBrush("Background04");
			border.BorderThickness = CodeBlockBorderThickness;
			border.CornerRadius = CodeBlockCornerRadius;
			border.Padding = CodeBlockBorderPadding;
			group.CopyRange.Update(contentStart, contentLength);
			var codeBody = MarkdownRenderer.SafeSlice(buffer, contentStart, contentLength);
			return codeBody.IsEmpty ? string.Empty : codeBody.ToString();
		}

		if (block.Type == MarkdownTokenizer.TokenTypeHeader)
		{
			var (size, contentStart, contentLength) = MarkdownRenderer.ExtractHeaderInfo(buffer, block);
			renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			renderer.FontSize = size switch
			{
				1 => (int) (view.FontSize * 2.6),
				2 => (int) (view.FontSize * 2.2),
				3 => (int) (view.FontSize * 2.0),
				4 => (int) (view.FontSize * 1.6),
				5 => (int) (view.FontSize * 1.4),
				_ => (int) (view.FontSize * 1.2)
			};
			renderer.Foreground = view.Foreground;
			var headerBody = MarkdownRenderer.SafeSlice(buffer, contentStart, contentLength);
			return headerBody.IsEmpty ? string.Empty : headerBody.ToString();
		}

		if (block.Type == MarkdownTokenizer.TokenTypeTable)
		{
			renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			renderer.FontSize = view.FontSize;
			renderer.Foreground = view.Foreground;
			border.BorderThickness = CodeBlockBorderThickness;
			border.Padding = CodeBlockBorderPadding;
			var tableSpan = MarkdownRenderer.SafeSlice(buffer, block);
			if (tableSpan.IsEmpty)
			{
				return string.Empty;
			}

			var tableContent = tableSpan.ToString();
			var boundsWidth = renderer.ViewModel.WordWrap
				? view.Bounds.Width - view.Padding.Left - view.Padding.Right
				- border.Margin.Left - border.Padding.Left - border.Margin.Right - border.Padding.Right
				- renderer.Margin.Left - renderer.Margin.Right
				: int.MaxValue;
			var textLayout = renderer.GetTextLayout("x", boundsWidth);
			var maxCharacterWidth = (int) (boundsWidth / textLayout.WidthIncludingTrailingWhitespace);
			return MarkdownTableFormatter.Format(tableContent, maxCharacterWidth);
		}

		if (block.Type == MarkdownTokenizer.TokenTypeHorizontalRule)
		{
			renderer.IsVisible = false;
			border.BorderBrush = ResourceService.GetColorAsBrush("BorderBrush");
			border.BorderThickness = MarkdownBlockPresenter.HorizontalRuleBorderThickness;
			border.Margin = MarkdownBlockPresenter.HorizontalRuleMargin;
			border.Padding = ZeroThickness;
			border.Background = null;
			return string.Empty;
		}

		if (block.Type == MarkdownTokenizer.TokenTypeUnorderedList)
		{
			renderer.FontSize = view.FontSize;
			renderer.Foreground = view.Foreground;
			MarkdownInlineProjector.ProjectUnorderedList(MarkdownRenderer.SafeSlice(buffer, block), renderer, view);
			return renderer.Text ?? string.Empty;
		}

		renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
		var fallback = MarkdownRenderer.SafeSlice(buffer, block);
		return fallback.IsEmpty ? string.Empty : fallback.ToString();
	}

	#endregion
}