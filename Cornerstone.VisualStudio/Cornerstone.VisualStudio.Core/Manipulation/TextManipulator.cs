#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.VisualStudio.Core.Parsing;

#endregion

namespace Cornerstone.VisualStudio.Core.Manipulation;

/// <summary>
/// Manipulates document as user types text
/// Closes xml tags, renames start and end tags at same time etc.
/// </summary>
public class TextManipulator
{
	#region Fields

	private readonly int _position;
	private readonly XmlParser _state;
	private readonly ReadOnlyMemory<char> _text;

	private readonly char[] _xmlNameSpecialCharacters = ['-', '_', '.'];

	#endregion

	#region Constructors

	public TextManipulator(string text, int position)
	{
		_position = position;
		_text = text.AsMemory();

		var parserStart = 0;
		var parserEnd = 0;

		// To improve performance parse only last tag
		if (text.Length > 0)
		{
			// Find last < tag
			parserStart = position;
			if (position >= text.Length)
			{
				parserStart = text.Length - 1;
			}
			parserStart = text.LastIndexOf('<', parserStart);
			if (parserStart < 0)
			{
				parserStart = 0;
			}

			parserEnd = text.Length > position ? position : text.Length;
		}

		_state = XmlParser.Parse(_text, parserStart, parserEnd);
	}

	#endregion

	#region Methods

	public IList<TextManipulation> ManipulateText(ITextChange textChange)
	{
		var manipulations = new List<TextManipulation>();

		// IntelliSense commits insert whole snippets (e.g. "Grid></Grid>") — never run tag
		// sync on those. Single-char typing (and letter-only multi-char name typing) is fine.
		if (IsCompletionShapedChange(textChange))
		{
			return manipulations;
		}

		var span = _text.Span;
		var parserPos = _state.ParserPos;

		if ((_state.State == XmlParser.ParserState.StartElement)
			|| ((_state.State == XmlParser.ParserState.None)
				&& (parserPos >= 0)
				&& (parserPos < span.Length)
				&& (span[parserPos] == '>')))
		{
			SynchronizeStartAndEndTag(textChange, manipulations);
		}

		if ((_state.State == XmlParser.ParserState.StartElement)
			|| (_state.State == XmlParser.ParserState.AfterAttributeValue)
			|| (_state.State == XmlParser.ParserState.InsideElement))
		{
			new CloseXmlTagManipulation(_state, _text, _position).TryCloseTag(textChange, manipulations);
		}
		else if ((_state.State == XmlParser.ParserState.None)
			&& string.IsNullOrEmpty(textChange.OldText)
			&& (textChange.NewText == ">"))
		{
			var pp = textChange.NewPosition - 2;
			// if xml tag already closed ignore '>'
			if ((pp > -1)
				&& (pp + 1 < span.Length)
				&& (span[pp] == '/')
				&& (span[pp + 1] == '>'))
			{
				manipulations.Add(TextManipulation.Delete(textChange.NewPosition, 1));
			}
		}

		return manipulations.OrderByDescending(n => n.Start).ToList();
	}

	/// <summary>
	/// True for bulk IntelliSense / paste inserts that include markup structure, not a typed name.
	/// </summary>
	private static bool IsCompletionShapedChange(ITextChange textChange)
	{
		var neu = textChange.NewText ?? string.Empty;

		if (neu.Length <= 1)
		{
			return false;
		}

		// "Grid></Grid>", "TextBlock />", attributes with quotes, etc.
		foreach (var c in neu)
		{
			if ((c == '<') || (c == '>') || (c == '/') || (c == '"') || (c == '\'') ||
				(c == '=') || (c == '\n') || (c == '\r') || (c == '\t') || (c == ' '))
			{
				return true;
			}
		}

		return false;
	}

	private void SynchronizeStartAndEndTag(ITextChange textChange, List<TextManipulation> maniplations)
	{
		// Empty NewText = pure delete (still sync closing tag). Non-empty must be name chars only.
		var neu = textChange.NewText ?? string.Empty;
		if ((neu.Length > 0) &&
			!neu.All(n => char.IsLetterOrDigit(n) || _xmlNameSpecialCharacters.Contains(n)))
		{
			return;
		}

		var startTag = _state.ParseCurrentTagName();
		var maybeTagStart = _state.CurrentValueStart;
		if ((maybeTagStart == null) || startTag is null)
		{
			return;
		}

		var startPos = maybeTagStart.Value; // add 1 to take opening < into account
		if (startTag.EndsWith("/"))
		{
			return; // start tag is self-closing
		}
		if ((textChange.NewPosition < startPos) || (textChange.NewPosition > (startPos + startTag.Length)))
		{
			return; //we are not editing tag name
		}

		var searchEndTag = _state.Clone();
		if (searchEndTag.SeekClosingTag())
		{
			var endTag = searchEndTag.ParseCurrentTagName();
			if (endTag is null || string.IsNullOrEmpty(endTag) || (endTag[0] != '/'))
			{
				return;
			}

			maybeTagStart = searchEndTag.CurrentValueStart;
			if (maybeTagStart == null)
			{
				return;
			}

			var endPos = maybeTagStart.Value; // add 1 to take opening < into account

			// reverse change to start tag
			startTag = textChange.ReverseOn(startTag, startPos);

			var isTheSameTag = (endTag.Length > 0) && (endTag.Substring(1) == startTag);
			if (isTheSameTag)
			{
				maniplations.AddRange(textChange.AsManipulations(endPos - startPos));
			}
		}
	}

	#endregion
}