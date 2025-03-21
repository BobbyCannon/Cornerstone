#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Avalonia.Media;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// RichTextWriter implementation that produces HTML.
/// </summary>
internal class HtmlRichTextWriter : RichTextWriter
{
	#region Fields

	private readonly Stack<string> endTagStack = new();
	private bool hasSpace;
	private readonly TextWriter htmlWriter;
	private int indentationLevel;
	private bool needIndentation = true;
	private readonly HtmlOptions options;
	private bool spaceNeedsEscaping = true;

	private static readonly char[] specialChars = [' ', '\t', '\r', '\n'];

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new HtmlRichTextWriter instance.
	/// </summary>
	/// <param name="htmlWriter">
	/// The text writer where the raw HTML is written to.
	/// The HtmlRichTextWriter does not take ownership of the htmlWriter;
	/// disposing the HtmlRichTextWriter will not dispose the underlying htmlWriter!
	/// </param>
	/// <param name="options"> Options that control the HTML output. </param>
	public HtmlRichTextWriter(TextWriter htmlWriter, HtmlOptions options = null)
	{
		if (htmlWriter == null)
		{
			throw new ArgumentNullException("htmlWriter");
		}
		this.htmlWriter = htmlWriter;
		this.options = options ?? new HtmlOptions();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override Encoding Encoding => htmlWriter.Encoding;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void BeginHyperlinkSpan(Uri uri)
	{
		WriteIndentationAndSpace();
		#if DOTNET4
		string link = WebUtility.HtmlEncode(uri.ToString());
		#else
		var link = HttpUtility.HtmlEncode(uri.ToString());
		#endif
		htmlWriter.Write("<a href=\"" + link + "\">");
		endTagStack.Push("</a>");
	}

	/// <inheritdoc />
	public override void BeginSpan(Color foregroundColor)
	{
		BeginSpan(new HighlightingColor { Foreground = new SimpleHighlightingBrush(foregroundColor) });
	}

	/// <inheritdoc />
	public override void BeginSpan(FontFamily fontFamily)
	{
		BeginUnhandledSpan(); // TODO
	}

	/// <inheritdoc />
	public override void BeginSpan(FontStyle fontStyle)
	{
		BeginSpan(new HighlightingColor { FontStyle = fontStyle });
	}

	/// <inheritdoc />
	public override void BeginSpan(FontWeight fontWeight)
	{
		BeginSpan(new HighlightingColor { FontWeight = fontWeight });
	}

	/// <inheritdoc />
	public override void BeginSpan(HighlightingColor highlightingColor)
	{
		WriteIndentationAndSpace();
		if (options.ColorNeedsSpanForStyling(highlightingColor))
		{
			htmlWriter.Write("<span");
			options.WriteStyleAttributeForColor(htmlWriter, highlightingColor);
			htmlWriter.Write('>');
			endTagStack.Push("</span>");
		}
		else
		{
			endTagStack.Push(null);
		}
	}

	/// <inheritdoc />
	public override void EndSpan()
	{
		htmlWriter.Write(endTagStack.Pop());
	}

	/// <inheritdoc />
	public override void Flush()
	{
		FlushSpace(true); // next char potentially might be whitespace
		htmlWriter.Flush();
	}

	/// <inheritdoc />
	public override void Indent()
	{
		indentationLevel++;
	}

	/// <inheritdoc />
	public override void Unindent()
	{
		if (indentationLevel == 0)
		{
			throw new NotSupportedException();
		}
		indentationLevel--;
	}

	/// <inheritdoc />
	public override void Write(char value)
	{
		WriteIndentation();
		WriteChar(value);
	}

	/// <inheritdoc />
	public override void Write(string value)
	{
		var pos = 0;
		do
		{
			var endPos = value.IndexOfAny(specialChars, pos);
			if (endPos < 0)
			{
				WriteSimpleString(value.Substring(pos));
				return; // reached end of string
			}
			if (endPos > pos)
			{
				WriteSimpleString(value.Substring(pos, endPos - pos));
			}
			WriteChar(value[pos]);
			pos = endPos + 1;
		} while (pos < value.Length);
	}

	/// <inheritdoc />
	protected override void BeginUnhandledSpan()
	{
		endTagStack.Push(null);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			FlushSpace(true);
		}
		base.Dispose(disposing);
	}

	private void FlushSpace(bool nextIsWhitespace)
	{
		if (hasSpace)
		{
			if (spaceNeedsEscaping || nextIsWhitespace)
			{
				htmlWriter.Write("&nbsp;");
			}
			else
			{
				htmlWriter.Write(' ');
			}
			hasSpace = false;
			spaceNeedsEscaping = true;
		}
	}

	private void WriteChar(char c)
	{
		var isWhitespace = char.IsWhiteSpace(c);
		FlushSpace(isWhitespace);
		switch (c)
		{
			case ' ':
				if (spaceNeedsEscaping)
				{
					htmlWriter.Write("&nbsp;");
				}
				else
				{
					hasSpace = true;
				}
				break;
			case '\t':
				for (var i = 0; i < options.TabSize; i++)
				{
					htmlWriter.Write("&nbsp;");
				}
				break;
			case '\r':
				break; // ignore; we'll write the <br/> with the following \n
			case '\n':
				htmlWriter.Write("<br/>");
				needIndentation = true;
				break;
			default:
				#if DOTNET4
				WebUtility.HtmlEncode(c.ToString(), htmlWriter);
				#else
				HttpUtility.HtmlEncode(c.ToString(), htmlWriter);
				#endif
				break;
		}
		// If we just handled a space by setting hasSpace = true,
		// we mustn't set spaceNeedsEscaping as doing so would affect our own space,
		// not just the following spaces.
		if (c != ' ')
		{
			// Following spaces must be escaped if c was a newline/tab;
			// and they don't need escaping if c was a normal character.
			spaceNeedsEscaping = isWhitespace;
		}
	}

	private void WriteIndentation()
	{
		if (needIndentation)
		{
			for (var i = 0; i < indentationLevel; i++)
			{
				WriteChar('\t');
			}
			needIndentation = false;
		}
	}

	private void WriteIndentationAndSpace()
	{
		WriteIndentation();
		FlushSpace(false);
	}

	private void WriteSimpleString(string value)
	{
		if (value.Length == 0)
		{
			return;
		}
		WriteIndentationAndSpace();
		#if DOTNET4
		WebUtility.HtmlEncode(value, htmlWriter);
		#else
		HttpUtility.HtmlEncode(value, htmlWriter);
		#endif
	}

	#endregion
}