﻿#region References

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Folding;

/// <summary>
/// Holds information about the start of a fold in a xml string.
/// </summary>
internal sealed class XmlFoldStart : NewFolding
{
	#region Fields

	internal int StartLine;

	#endregion
}

/// <summary>
/// Determines folds for a xml string in the editor.
/// </summary>
public class XmlFoldingStrategy
{
	#region Properties

	/// <summary>
	/// Flag indicating whether attributes should be displayed on folded
	/// elements.
	/// </summary>
	public bool ShowAttributesWhenFolded { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Create <see cref="NewFolding" />s for the specified document.
	/// </summary>
	public IEnumerable<NewFolding> CreateNewFoldings(TextEditorDocument document, out int firstErrorOffset)
	{
		try
		{
			var reader = XmlReader.Create(document.CreateReader());
			return CreateNewFoldings(document, reader, out firstErrorOffset);
		}
		catch (XmlException)
		{
			firstErrorOffset = 0;
			return [];
		}
	}

	/// <summary>
	/// Create <see cref="NewFolding" />s for the specified document.
	/// </summary>
	public IEnumerable<NewFolding> CreateNewFoldings(TextEditorDocument document, XmlReader reader, out int firstErrorOffset)
	{
		var stack = new Stack<XmlFoldStart>();
		var foldMarkers = new List<NewFolding>();
		try
		{
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						if (!reader.IsEmptyElement)
						{
							var newFoldStart = CreateElementFoldStart(document, reader);
							stack.Push(newFoldStart);
						}
						break;

					case XmlNodeType.EndElement:
						var foldStart = stack.Pop();
						CreateElementFold(document, foldMarkers, reader, foldStart);
						break;

					case XmlNodeType.Comment:
						CreateCommentFold(document, foldMarkers, reader);
						break;
				}
			}
			firstErrorOffset = -1;
		}
		catch (XmlException ex)
		{
			// ignore errors at invalid positions (prevent ArgumentOutOfRangeException)
			if ((ex.LineNumber >= 1) && (ex.LineNumber <= document.LineCount))
			{
				firstErrorOffset = document.GetOffset(ex.LineNumber, ex.LinePosition);
			}
			else
			{
				firstErrorOffset = 0;
			}
		}
		foldMarkers.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
		return foldMarkers;
	}

	/// <summary>
	/// Create <see cref="NewFolding" />s for the specified document and updates the folding manager with them.
	/// </summary>
	public void UpdateFoldings(FoldingManager manager, TextEditorDocument document)
	{
		var foldings = CreateNewFoldings(document, out var firstErrorOffset);
		manager.UpdateFoldings(foldings, firstErrorOffset);
	}

	/// <summary>
	/// Creates a comment fold if the comment spans more than one line.
	/// </summary>
	/// <remarks>
	/// The text displayed when the comment is folded is the first
	/// line of the comment.
	/// </remarks>
	private static void CreateCommentFold(TextEditorDocument document, List<NewFolding> foldMarkers, XmlReader reader)
	{
		var comment = reader.Value;
		if (comment != null)
		{
			var firstNewLine = comment.IndexOf('\n');
			if (firstNewLine >= 0)
			{
				// Take off 4 chars to get the actual comment start (takes
				// into account the <!-- chars.

				var startOffset = GetOffset(document, reader) - 4;
				var endOffset = startOffset + comment.Length + 7;

				var foldText = string.Concat("<!--", comment.Substring(0, firstNewLine).TrimEnd('\r'), "-->");
				foldMarkers.Add(new NewFolding(startOffset, endOffset) { Name = foldText });
			}
		}
	}

	/// <summary>
	/// Create an element fold if the start and end tag are on
	/// different lines.
	/// </summary>
	private static void CreateElementFold(TextEditorDocument document, List<NewFolding> foldMarkers, XmlReader reader, XmlFoldStart foldStart)
	{
		var lineInfo = (IXmlLineInfo) reader;
		var endLine = lineInfo.LineNumber;
		if (endLine > foldStart.StartLine)
		{
			var endCol = lineInfo.LinePosition + reader.Name.Length + 1;
			foldStart.EndIndex = document.GetOffset(endLine, endCol);
			foldMarkers.Add(foldStart);
		}
	}

	/// <summary>
	/// Creates an XmlFoldStart for the start tag of an element.
	/// </summary>
	private XmlFoldStart CreateElementFoldStart(TextEditorDocument document, XmlReader reader)
	{
		// Take off 1 from the offset returned
		// from the xml since it points to the start
		// of the element name and not the beginning
		// tag.
		var newFoldStart = new XmlFoldStart();

		var lineInfo = (IXmlLineInfo) reader;
		newFoldStart.StartLine = lineInfo.LineNumber;
		newFoldStart.StartOffset = document.GetOffset(newFoldStart.StartLine, lineInfo.LinePosition - 1);

		if (ShowAttributesWhenFolded && reader.HasAttributes)
		{
			newFoldStart.Name = string.Concat("<", reader.Name, " ", GetAttributeFoldText(reader), ">");
		}
		else
		{
			newFoldStart.Name = string.Concat("<", reader.Name, ">");
		}

		return newFoldStart;
	}

	/// <summary>
	/// Gets the element's attributes as a string on one line that will
	/// be displayed when the element is folded.
	/// </summary>
	/// <remarks>
	/// Currently this puts all attributes from an element on the same
	/// line of the start tag.  It does not cater for elements where attributes
	/// are not on the same line as the start tag.
	/// </remarks>
	private static string GetAttributeFoldText(XmlReader reader)
	{
		var text = new StringBuilder();

		for (var i = 0; i < reader.AttributeCount; ++i)
		{
			reader.MoveToAttribute(i);

			text.Append(reader.Name);
			text.Append("=");
			text.Append('"');
			text.Append(XmlEncodeAttributeValue(reader.Value, '"'));
			text.Append('"');

			// Append a space if this is not the
			// last attribute.
			if (i < (reader.AttributeCount - 1))
			{
				text.Append(" ");
			}
		}

		return text.ToString();
	}

	private static int GetOffset(TextEditorDocument document, XmlReader reader)
	{
		if (reader is IXmlLineInfo info && info.HasLineInfo())
		{
			return document.GetOffset(info.LineNumber, info.LinePosition);
		}
		throw new ArgumentException("XmlReader does not have positioning information.");
	}

	/// <summary>
	/// Xml encode the attribute string since the string returned from
	/// the XmlTextReader is the plain unencoded string and .NET
	/// does not provide us with an xml encode method.
	/// </summary>
	private static string XmlEncodeAttributeValue(string attributeValue, char quoteChar)
	{
		var encodedValue = new StringBuilder(attributeValue);

		encodedValue.Replace("&", "&amp;");
		encodedValue.Replace("<", "&lt;");
		encodedValue.Replace(">", "&gt;");

		if (quoteChar == '"')
		{
			encodedValue.Replace("\"", "&quot;");
		}
		else
		{
			encodedValue.Replace("'", "&apos;");
		}

		return encodedValue.ToString();
	}

	#endregion
}