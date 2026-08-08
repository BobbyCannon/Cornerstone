#region References

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

		var buffer = new StringBuffer(markdown);
		var parser = new MarkdownParser(buffer, null);
		foreach (var block in parser.Process())
		{
			if (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
			{
				var (language, contentStart, contentLength) = ExtractCodeBlockInfo(buffer.AsSpan(), block);
				Buffer.Append("<pre><code>");
				Buffer.Append(buffer.Substring(contentStart, contentLength));
				Buffer.Append("</code></pre>");
			}
			else if (block.Type == MarkdownTokenizer.TokenTypeHeader)
			{
				var (size, contentStart, contentLength) = ExtractHeaderInfo(buffer.AsSpan(), block);
				Buffer.Append("<h");
				Buffer.Append(size.ToString());
				Buffer.Append(">");
				Buffer.Append(buffer.Substring(contentStart, contentLength));
				Buffer.Append("</h");
				Buffer.Append(block.Offsets[0].ToString());
				Buffer.Append(">");
			}
			else if ((block.Type == MarkdownTokenizer.TokenTypeLink) && (block.Offsets is { Length: >= 4 }))
			{
				var text = buffer.Substring(block.Offsets[0], block.Offsets[1] - block.Offsets[0]);
				var href = buffer.Substring(block.Offsets[2], block.Offsets[3] - block.Offsets[2]);
				AppendWithEmphasis(block, () =>
				{
					Buffer.Append("<a href=\"");
					Buffer.Append(href);
					Buffer.Append("\">");
					Buffer.Append(text);
					Buffer.Append("</a>");
				});
			}
			else if (block.Type == MarkdownTokenizer.TokenTypeBold)
			{
				// Legacy container type (if present)
				Buffer.Append("<strong>");
				Buffer.Append(buffer.Substring(block.Offsets[0], block.Offsets[1] - block.Offsets[0]));
				Buffer.Append("</strong>");
			}
			else if (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic)
			{
				Buffer.Append("<em><strong>");
				Buffer.Append(buffer.Substring(block.Offsets[0], block.Offsets[1] - block.Offsets[0]));
				Buffer.Append("</strong></em>");
			}
			else if (block.Type == MarkdownTokenizer.TokenTypeItalic)
			{
				Buffer.Append("<em>");
				Buffer.Append(buffer.Substring(block.Offsets[0], block.Offsets[1] - block.Offsets[0]));
				Buffer.Append("</em>");
			}
			else
			{
				// Expanded emphasis leaves: text + EmBold / EmItalic
				AppendWithEmphasis(block, () => Buffer.Append(buffer.Substring(block.StartOffset, block.Length)));
			}
		}
		return Buffer.ToString();
	}

	private void AppendWithEmphasis(Block block, System.Action writeInner)
	{
		var openStrong = block.EmBold || (block.Type == MarkdownTokenizer.TokenTypeBold);
		var openEm = block.EmItalic || (block.Type == MarkdownTokenizer.TokenTypeItalic);
		if (block.Type == MarkdownTokenizer.TokenTypeBoldAndItalic)
		{
			openStrong = true;
			openEm = true;
		}

		if (openEm)
		{
			Buffer.Append("<em>");
		}
		if (openStrong)
		{
			Buffer.Append("<strong>");
		}

		writeInner();

		if (openStrong)
		{
			Buffer.Append("</strong>");
		}
		if (openEm)
		{
			Buffer.Append("</em>");
		}
	}

	#endregion
}