#region References

using System;
using System.Globalization;
using System.Text;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// Allows copying HTML text to the clipboard.
/// </summary>
public static class HtmlClipboard
{
	#region Methods

	/// <summary>
	/// Creates an HTML fragment from a part of a document.
	/// </summary>
	/// <param name="document"> The document to create HTML from. </param>
	/// <param name="highlighter"> The highlighter used to highlight the document. <c> null </c> is valid and will create HTML without any highlighting. </param>
	/// <param name="range"> The part of the document to create HTML for. You can pass <c> null </c> to create HTML for the whole document. </param>
	/// <param name="options"> The options for the HTML creation. </param>
	/// <returns> HTML code for the document part. </returns>
	public static string CreateHtmlFragment(ITextEditorDocument document, IHighlighter highlighter, IRange range, HtmlOptions options)
	{
		if (document == null)
		{
			throw new ArgumentNullException(nameof(document));
		}
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}
		if ((highlighter != null) && (highlighter.Document != document))
		{
			throw new ArgumentException("Highlighter does not belong to the specified document.");
		}
		if (range == null)
		{
			range = new SimpleRange(0, document.TextLength);
		}

		var html = new StringBuilder();
		var segmentEndOffset = range.EndIndex;
		var line = document.GetLineByOffset(range.StartIndex);
		while ((line != null) && (line.StartIndex < segmentEndOffset))
		{
			// ReSharper disable once UnusedVariable
			var highlightedLine = highlighter != null ? highlighter.HighlightLine(line.LineNumber) : new HighlightedLine(document, line);
			if (html.Length > 0)
			{
				html.AppendLine("<br>");
			}
			// TODO: html
			var s = range.GetOverlap(line);
			html.Append(highlightedLine.ToHtml(s.Offset, s.EndIndex, options));
			line = line.NextLine;
		}
		return html.ToString();
	}

	/// <summary>
	/// Builds a header for the CF_HTML clipboard format.
	/// </summary>
	private static string BuildHeader(int startHTML, int endHTML, int startFragment, int endFragment)
	{
		var b = new StringBuilder();
		b.AppendLine("Version:0.9");
		b.AppendLine("StartHTML:" + startHTML.ToString("d8", CultureInfo.InvariantCulture));
		b.AppendLine("EndHTML:" + endHTML.ToString("d8", CultureInfo.InvariantCulture));
		b.AppendLine("StartFragment:" + startFragment.ToString("d8", CultureInfo.InvariantCulture));
		b.AppendLine("EndFragment:" + endFragment.ToString("d8", CultureInfo.InvariantCulture));
		return b.ToString();
	}

	#endregion
}