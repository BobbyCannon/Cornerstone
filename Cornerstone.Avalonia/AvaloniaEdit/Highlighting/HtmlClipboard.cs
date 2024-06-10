#region References

using System;
using System.Globalization;
using System.Text;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// Allows copying HTML text to the clipboard.
/// </summary>
public static class HtmlClipboard
{
	#region Methods

	/// <summary>
	/// Sets the TextDataFormat.Html on the data object to the specified html fragment.
	/// This helper methods takes care of creating the necessary CF_HTML header.
	/// </summary>
	//public static void SetHtml(DataObject dataObject, string htmlFragment)
	//{
	//	if (dataObject == null)
	//		throw new ArgumentNullException("dataObject");
	//	if (htmlFragment == null)
	//		throw new ArgumentNullException("htmlFragment");

	//	string htmlStart = @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">" + Environment.NewLine
	//		+ "<HTML>" + Environment.NewLine
	//		+ "<BODY>" + Environment.NewLine
	//		+ "<!--StartFragment-->" + Environment.NewLine;
	//	string htmlEnd = "<!--EndFragment-->" + Environment.NewLine + "</BODY>" + Environment.NewLine + "</HTML>" + Environment.NewLine;
	//	string dummyHeader = BuildHeader(0, 0, 0, 0);
	//	// the offsets are stored as UTF-8 bytes (see CF_HTML documentation)
	//	int startHTML = dummyHeader.Length;
	//	int startFragment = startHTML + htmlStart.Length;
	//	int endFragment = startFragment + Encoding.UTF8.GetByteCount(htmlFragment);
	//	int endHTML = endFragment + htmlEnd.Length;
	//	string cf_html = BuildHeader(startHTML, endHTML, startFragment, endFragment) + htmlStart + htmlFragment + htmlEnd;
	//	Debug.WriteLine(cf_html);
	//	dataObject.SetText(cf_html, TextDataFormat.Html);
	//}

	/// <summary>
	/// Creates a HTML fragment from a part of a document.
	/// </summary>
	/// <param name="document"> The document to create HTML from. </param>
	/// <param name="highlighter"> The highlighter used to highlight the document. <c> null </c> is valid and will create HTML without any highlighting. </param>
	/// <param name="segment"> The part of the document to create HTML for. You can pass <c> null </c> to create HTML for the whole document. </param>
	/// <param name="options"> The options for the HTML creation. </param>
	/// <returns> HTML code for the document part. </returns>
	public static string CreateHtmlFragment(IDocument document, IHighlighter highlighter, ISegment segment, HtmlOptions options)
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
		if (segment == null)
		{
			segment = new SimpleSegment(0, document.TextLength);
		}

		var html = new StringBuilder();
		var segmentEndOffset = segment.EndOffset;
		var line = document.GetLineByOffset(segment.Offset);
		while ((line != null) && (line.Offset < segmentEndOffset))
		{
			// ReSharper disable once UnusedVariable
			var highlightedLine = highlighter != null ? highlighter.HighlightLine(line.LineNumber) : new HighlightedLine(document, line);
			if (html.Length > 0)
			{
				html.AppendLine("<br>");
			}
			// TODO: html
			var s = segment.GetOverlap(line);
			html.Append(highlightedLine.ToHtml(s.Offset, s.EndOffset, options));
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