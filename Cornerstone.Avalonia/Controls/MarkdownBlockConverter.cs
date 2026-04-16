#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Avalonia.Text;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class MarkdownBlockConverter : IMultiValueConverter
{
	#region Fields

	public static readonly Thickness CodeBlockBorderMargin;
	public static readonly Thickness CodeBlockBorderPadding;
	public static readonly Thickness CodeBlockBorderThickness;
	public static readonly Thickness HeaderBorderPadding;
	public static readonly Thickness ZeroThickness;
	private static readonly MarkdownViewTokenizer _markdownViewTokenizer;

	#endregion

	#region Constructors

	static MarkdownBlockConverter()
	{
		_markdownViewTokenizer = new MarkdownViewTokenizer();

		BlockQuoteBorderThickness = new(10, 2, 2, 2);
		CodeBlockBorderThickness = new(1);
		CodeBlockBorderMargin = new(0, 10);
		CodeBlockBorderPadding = new(10);
		HeaderBorderPadding = new(0, 0, 0, 10);
		ZeroThickness = new(0);
	}

	#endregion

	#region Properties

	public static Thickness BlockQuoteBorderThickness { get; }

	#endregion

	#region Methods

	public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [MarkdownBlockGroup blockGroup, TextRenderer renderer, Border border, MarkdownView view])
		{
			return string.Empty;
		}

		try
		{
			var buffer = view.Markdown.AsSpan();
			renderer.FontSize = view.FontSize;
			renderer.Foreground = view.Foreground;
			renderer.ViewModel.WordWrap = view.WordWrap;
			border.Background = null;
			border.BorderThickness = ZeroThickness;
			border.Margin = ZeroThickness;
			border.Padding = ZeroThickness;

			return (blockGroup.Blocks.Count == 1)
				&& MarkdownView.IsBlockLevel(blockGroup.Blocks[0])
					? ProcessSingleBlock(view, border, renderer, blockGroup.Blocks[0], buffer)
					: ProcessParagraph(view, border, renderer, blockGroup, buffer);
		}
		catch
		{
			return string.Empty;
		}
	}

	private string ProcessParagraph(MarkdownView view, Border border, TextRenderer renderer, MarkdownBlockGroup group, ReadOnlySpan<char> buffer)
	{
		group.Buffer.Clear();
		renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);

		foreach (var block in group.Blocks)
		{
			if ((block.Type == MarkdownTokenizer.TokenTypeBold)
				|| (block.Type == MarkdownTokenizer.TokenTypeItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic)
				|| (block.Type == MarkdownTokenizer.TokenTypeStrikethrough))
			{
				var start = block.Offsets[0];
				var end = block.Offsets[1];
				var length = end - start;
				var bufferStart = group.Buffer.WritePosition;
				var bufferEnd = bufferStart + length;
				group.Buffer.Add(buffer.Slice(start, length));
				renderer.ViewModel.TokenManager.Add(block.Type, bufferStart, bufferEnd);
			}
			else
			{
				group.Buffer.Add(buffer.Slice(block.StartOffset, block.Length));
			}
		}

		return group.Buffer.ToString();
	}

	private string ProcessSingleBlock(MarkdownView view, Border border, TextRenderer renderer, Block block, ReadOnlySpan<char> buffer)
	{
		if (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
		{
			renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			renderer.Foreground = view.Foreground;
			border.Background = ResourceService.GetColorAsBrush("Background04");
			border.BorderThickness = BlockQuoteBorderThickness;
			border.Margin = CodeBlockBorderMargin;
			border.Padding = CodeBlockBorderPadding;
			return buffer.Slice(block.StartOffset, block.Length).ToString();
		}

		if (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
		{
			var (language, contentStart, contentLength) = MarkdownRenderer.ExtractCodeBlockInfo(buffer, block);
			renderer.ViewModel.TokenManager.Initialize(language);
			renderer.FontSize = view.FontSize;
			renderer.Foreground = view.Foreground;
			border.BorderThickness = CodeBlockBorderThickness;
			border.Margin = CodeBlockBorderMargin;
			border.Padding = CodeBlockBorderPadding;
			return MarkdownTableFormatter.Format(buffer.Slice(contentStart, contentLength));
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
			border.BorderThickness = ZeroThickness;
			border.Margin = ZeroThickness;
			border.Padding = HeaderBorderPadding;
			return buffer.Slice(contentStart, contentLength).ToString();
		}

		if (block.Type == MarkdownTokenizer.TokenTypeTable)
		{
			renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			renderer.FontSize = view.FontSize;
			renderer.Foreground = view.Foreground;
			border.BorderThickness = CodeBlockBorderThickness;
			border.Margin = CodeBlockBorderMargin;
			border.Padding = CodeBlockBorderPadding;
			var tableContent = buffer.Slice(block.StartOffset, block.Length).ToString();
			var boundsWidth = renderer.ViewModel.WordWrap
				? view.Bounds.Width - view.Padding.Left - view.Padding.Right
					- border.Margin.Left - border.Padding.Left - border.Margin.Right - border.Padding.Right
					- renderer.Margin.Left - renderer.Margin.Right
				: int.MaxValue;
			var textLayout = renderer.GetTextLayout("x", boundsWidth);
			var maxCharacterWidth = (int) (boundsWidth / textLayout.WidthIncludingTrailingWhitespace);
			return MarkdownTableFormatter.Format(tableContent, maxCharacterWidth);
		}

		renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
		return buffer.Slice(block.StartOffset, block.Length).ToString();
	}

	#endregion
}