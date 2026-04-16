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
			else if (block.Type == MarkdownTokenizer.TokenTypeBold)
			{
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
				Buffer.Append(buffer.Substring(block.StartOffset, block.Length));
			}
		}
		return Buffer.ToString();
	}

	#endregion
}