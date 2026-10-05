#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// Converts Grid column and row definitions between the element form and the attribute form
/// ColumnDefinitions="*,Auto" / RowDefinitions="Auto,*".
/// </summary>
internal static class GridDefinitionsSuggestion
{
	#region Nested

	internal sealed class Conversion
	{
		public Conversion(
			string attributeName,
			string attributeValue,
			int removeStart,
			int removeLength,
			int insertAt,
			string insertion,
			string displayText)
		{
			AttributeName = attributeName;
			AttributeValue = attributeValue;
			RemoveStart = removeStart;
			RemoveLength = removeLength;
			InsertAt = insertAt;
			Insertion = insertion ?? string.Empty;
			DisplayText = displayText ?? string.Empty;
		}

		public string AttributeName { get; }

		public string AttributeValue { get; }

		public string DisplayText { get; }

		public string Insertion { get; }

		public int InsertAt { get; }

		public int RemoveLength { get; }

		public int RemoveStart { get; }
	}

	private struct AttributePiece
	{
		public AttributePiece(string name, string value, int nameStart, int end, string leading, string text)
		{
			Name = name;
			Value = value;
			NameStart = nameStart;
			End = end;
			Leading = leading;
			Text = text;
		}

		public int End;
		public string Leading;
		public string Name;
		public int NameStart;
		public string Text;
		public string Value;
	}

	private struct OpenTag
	{
		public OpenTag(int closeAngle)
		{
			CloseAngle = closeAngle;
		}

		public int CloseAngle;
	}

	#endregion

	#region Methods

	public static bool TryConvert(string text, int caret, out Conversion conversion)
	{
		conversion = null;
		if (string.IsNullOrEmpty(text) || (caret < 0) || (caret > text.Length))
		{
			return false;
		}

		var index = 0;
		var stack = new List<OpenTag>();
		while (index < text.Length)
		{
			var next = text.IndexOf('<', index);
			if (next < 0)
			{
				break;
			}

			if (IsClosingTag(text, next))
			{
				if (stack.Count > 0)
				{
					stack.RemoveAt(stack.Count - 1);
				}

				index = SkipTo(text, next + 2, '>') + 1;
				continue;
			}

			if (IsIgnoredTag(text, next))
			{
				index = SkipIgnored(text, next);
				continue;
			}

			string name;
			var closeAngle = -1;
			var selfClosing = false;
			List<AttributePiece> pieces;
			if (!TryReadStartTag(text, next, out name, out closeAngle, out selfClosing, out pieces))
			{
				index = next + 1;
				continue;
			}

			if (TryConvertAttribute(text, caret, next, name, closeAngle, selfClosing, pieces, out conversion))
			{
				return conversion != null;
			}

			string attributeName;
			string childName;
			if (IsDefinitionsElement(name, out attributeName, out childName))
			{
				var elementEnd = selfClosing ? closeAngle + 1 : FindElementEnd(text, closeAngle + 1, name);
				if (elementEnd < 0)
				{
					return false;
				}

				if ((caret >= next) && (caret <= elementEnd))
				{
					if ((stack.Count == 0) || selfClosing)
					{
						return false;
					}

					string value;
					if (!TryReadLengths(text, closeAngle + 1, elementEnd, childName, out value))
					{
						return false;
					}

					var removeStart = ExpandRemoveStart(text, next);
					var removeLength = elementEnd - removeStart;
					var insertAt = stack[stack.Count - 1].CloseAngle;
					if ((insertAt < 0) || (insertAt >= removeStart) || ParentAlreadyHas(text, insertAt, attributeName))
					{
						return false;
					}

					var insertion = " " + attributeName + "=\"" + value + "\"";
					conversion = new Conversion(
						attributeName,
						value,
						removeStart,
						removeLength,
						insertAt,
						insertion,
						"Convert to attribute");
					return true;
				}

				index = elementEnd;
				continue;
			}

			if (!selfClosing)
			{
				stack.Add(new OpenTag(closeAngle));
			}

			index = closeAngle + 1;
		}

		return false;
	}

	private static bool TryConvertAttribute(
		string text,
		int caret,
		int tagStart,
		string tagName,
		int closeAngle,
		bool selfClosing,
		List<AttributePiece> pieces,
		out Conversion conversion)
	{
		conversion = null;
		AttributePiece piece;
		if (!TryPickDefinitionsPiece(pieces, caret, out piece))
		{
			return false;
		}

		string attributeName;
		string childName;
		if (!IsDefinitionsElement(piece.Name, out attributeName, out childName))
		{
			return false;
		}

		if (!selfClosing && HasDirectChildDefinitions(text, closeAngle + 1, attributeName))
		{
			return true;
		}

		string[] lengths;
		if (!TrySplitLengths(piece.Value, out lengths))
		{
			return true;
		}

		var openTag = RebuildOpenTag(tagName, pieces, piece.NameStart);
		var insertion = BuildElements(text, tagStart, tagName, attributeName, childName, openTag, lengths, selfClosing);
		var removeLength = (closeAngle + 1) - tagStart;
		conversion = new Conversion(attributeName, JoinLengths(lengths), tagStart, removeLength, tagStart, insertion, "Convert to element");
		return true;
	}

	private static bool TryPickDefinitionsPiece(List<AttributePiece> pieces, int caret, out AttributePiece piece)
	{
		piece = new AttributePiece(null, null, 0, 0, null, null);
		if (pieces == null)
		{
			return false;
		}

		for (var i = 0; i < pieces.Count; i++)
		{
			var candidate = pieces[i];
			string ignoredAttribute;
			string ignoredChild;
			if (!IsDefinitionsElement(candidate.Name, out ignoredAttribute, out ignoredChild))
			{
				continue;
			}

			if ((caret >= candidate.NameStart) && (caret <= candidate.End))
			{
				piece = candidate;
				return true;
			}
		}

		return false;
	}

	private static bool HasDirectChildDefinitions(string text, int contentStart, string definitionsName)
	{
		var index = contentStart;
		var depth = 0;
		while (index < text.Length)
		{
			var next = text.IndexOf('<', index);
			if (next < 0)
			{
				return true;
			}

			if (IsClosingTag(text, next))
			{
				string closeName;
				int closeEnd;
				if (!TryReadCloseTag(text, next, out closeName, out closeEnd))
				{
					return true;
				}

				if (depth == 0)
				{
					return false;
				}

				depth--;
				index = closeEnd;
				continue;
			}

			if (IsIgnoredTag(text, next))
			{
				index = SkipIgnored(text, next);
				continue;
			}

			string name;
			int closeAngle;
			bool selfClosing;
			if (!TryReadStartTag(text, next, out name, out closeAngle, out selfClosing))
			{
				return true;
			}

			if (depth == 0)
			{
				string attributeName;
				string childName;
				if (IsDefinitionsElement(name, out attributeName, out childName) &&
					string.Equals(attributeName, definitionsName, StringComparison.Ordinal))
				{
					return true;
				}
			}

			if (!selfClosing)
			{
				depth++;
			}

			index = closeAngle + 1;
		}

		return true;
	}

	private static bool TrySplitLengths(string raw, out string[] lengths)
	{
		lengths = null;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		var parts = raw.Split(',');
		var values = new List<string>();
		for (var i = 0; i < parts.Length; i++)
		{
			var part = parts[i].Trim();
			if (part.Length == 0)
			{
				return false;
			}

			var length = NormalizeLength(part);
			if ((length == null) || (length.IndexOf('"') >= 0) || (length.IndexOf('<') >= 0))
			{
				return false;
			}

			values.Add(length);
		}

		if (values.Count == 0)
		{
			return false;
		}

		lengths = values.ToArray();
		return true;
	}

	private static string JoinLengths(string[] lengths)
	{
		var builder = new StringBuilder();
		for (var i = 0; i < lengths.Length; i++)
		{
			if (i > 0)
			{
				builder.Append(',');
			}

			builder.Append(lengths[i]);
		}

		return builder.ToString();
	}

	private static string RebuildOpenTag(string tagName, List<AttributePiece> pieces, int removedNameStart)
	{
		var builder = new StringBuilder();
		builder.Append('<');
		builder.Append(tagName);
		if (pieces != null)
		{
			for (var i = 0; i < pieces.Count; i++)
			{
				var piece = pieces[i];
				if (piece.NameStart == removedNameStart)
				{
					continue;
				}

				builder.Append(piece.Leading);
				builder.Append(piece.Text);
			}
		}

		builder.Append('>');
		return builder.ToString();
	}

	private static string BuildElements(
		string text,
		int tagStart,
		string tagName,
		string attributeName,
		string childName,
		string openTag,
		string[] lengths,
		bool selfClosing)
	{
		var newline = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
		var indent = LineIndent(text, tagStart);
		var unit = indent.IndexOf('\t') >= 0 ? "\t" : "    ";
		var childIndent = indent + unit;
		var definitionIndent = childIndent + unit;
		var sizeName = string.Equals(childName, "RowDefinition", StringComparison.Ordinal) ? "Height" : "Width";
		var builder = new StringBuilder();
		builder.Append(openTag);
		builder.Append(newline);
		builder.Append(childIndent);
		builder.Append('<');
		builder.Append(tagName);
		builder.Append('.');
		builder.Append(attributeName);
		builder.Append('>');
		for (var i = 0; i < lengths.Length; i++)
		{
			builder.Append(newline);
			builder.Append(definitionIndent);
			builder.Append('<');
			builder.Append(childName);
			builder.Append(' ');
			builder.Append(sizeName);
			builder.Append("=\"");
			builder.Append(lengths[i]);
			builder.Append("\" />");
		}

		builder.Append(newline);
		builder.Append(childIndent);
		builder.Append("</");
		builder.Append(tagName);
		builder.Append('.');
		builder.Append(attributeName);
		builder.Append('>');
		if (selfClosing)
		{
			builder.Append(newline);
			builder.Append(indent);
			builder.Append("</");
			builder.Append(tagName);
			builder.Append('>');
		}

		return builder.ToString();
	}

	private static string LineIndent(string text, int index)
	{
		var line = index;
		while ((line > 0) && (text[line - 1] != '\n') && (text[line - 1] != '\r'))
		{
			line--;
		}

		var end = line;
		while ((end < text.Length) && ((text[end] == ' ') || (text[end] == '\t')))
		{
			end++;
		}

		return text.Substring(line, end - line);
	}

	private static bool ParentAlreadyHas(string text, int closeAngle, string attributeName)
	{
		var start = text.LastIndexOf('<', closeAngle);
		if (start < 0)
		{
			return false;
		}

		string name;
		int end;
		bool selfClosing;
		Dictionary<string, string> attributes;
		if (!TryReadStartTag(text, start, out name, out end, out selfClosing, out attributes))
		{
			return false;
		}

		return (attributes != null) && attributes.ContainsKey(attributeName);
	}

	private static int ExpandRemoveStart(string text, int elementStart)
	{
		var line = elementStart;
		while ((line > 0) && (text[line - 1] != '\n') && (text[line - 1] != '\r'))
		{
			line--;
		}

		for (var i = line; i < elementStart; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
			{
				return elementStart;
			}
		}

		if ((line > 0) && (text[line - 1] == '\n'))
		{
			var start = line - 1;
			if ((start > 0) && (text[start - 1] == '\r'))
			{
				start--;
			}

			return start;
		}

		if ((line > 0) && (text[line - 1] == '\r'))
		{
			return line - 1;
		}

		return elementStart;
	}

	private static int FindElementEnd(string text, int contentStart, string name)
	{
		var index = contentStart;
		var depth = 1;
		while (index < text.Length)
		{
			var next = text.IndexOf('<', index);
			if (next < 0)
			{
				return -1;
			}

			if (IsClosingTag(text, next))
			{
				string closeName;
				int closeEnd;
				if (!TryReadCloseTag(text, next, out closeName, out closeEnd))
				{
					return -1;
				}

				if (string.Equals(closeName, name, StringComparison.Ordinal))
				{
					depth--;
					if (depth == 0)
					{
						return closeEnd;
					}
				}

				index = closeEnd;
				continue;
			}

			if (IsIgnoredTag(text, next))
			{
				index = SkipIgnored(text, next);
				continue;
			}

			string child;
			int closeAngle;
			bool selfClosing;
			if (!TryReadStartTag(text, next, out child, out closeAngle, out selfClosing))
			{
				return -1;
			}

			if (!selfClosing && string.Equals(child, name, StringComparison.Ordinal))
			{
				depth++;
			}

			index = closeAngle + 1;
		}

		return -1;
	}

	private static bool TryReadLengths(string text, int contentStart, int elementEnd, string childName, out string value)
	{
		value = string.Empty;
		var parts = new List<string>();
		var index = contentStart;
		while (index < elementEnd)
		{
			var next = text.IndexOf('<', index);
			if ((next < 0) || (next >= elementEnd))
			{
				break;
			}

			if (IsClosingTag(text, next))
			{
				break;
			}

			if (IsIgnoredTag(text, next))
			{
				index = SkipIgnored(text, next);
				continue;
			}

			string name;
			int closeAngle;
			bool selfClosing;
			Dictionary<string, string> attributes;
			if (!TryReadStartTag(text, next, out name, out closeAngle, out selfClosing, out attributes))
			{
				return false;
			}

			if (!string.Equals(name, childName, StringComparison.Ordinal))
			{
				return false;
			}

			if (!selfClosing)
			{
				var innerEnd = FindElementEnd(text, closeAngle + 1, name);
				if (innerEnd < 0)
				{
					return false;
				}

				var contentEnd = text.LastIndexOf("</", innerEnd - 1, StringComparison.Ordinal);
				if ((contentEnd > closeAngle) && !IsWhitespace(text, closeAngle + 1, contentEnd))
				{
					return false;
				}

				index = innerEnd;
			}
			else
			{
				index = closeAngle + 1;
			}

			string length;
			if (!TryLength(attributes, childName, out length))
			{
				return false;
			}

			parts.Add(length);
		}

		if (parts.Count == 0)
		{
			return false;
		}

		var builder = new StringBuilder();
		for (var i = 0; i < parts.Count; i++)
		{
			if (i > 0)
			{
				builder.Append(',');
			}

			builder.Append(parts[i]);
		}

		value = builder.ToString();
		return true;
	}

	private static bool TryLength(Dictionary<string, string> attributes, string childName, out string length)
	{
		length = "*";
		if ((attributes == null) || (attributes.Count == 0))
		{
			return true;
		}

		var sizeName = string.Equals(childName, "RowDefinition", StringComparison.Ordinal) ? "Height" : "Width";
		foreach (var pair in attributes)
		{
			if (!string.Equals(pair.Key, sizeName, StringComparison.Ordinal))
			{
				return false;
			}
		}

		string raw;
		if (!attributes.TryGetValue(sizeName, out raw) || string.IsNullOrWhiteSpace(raw))
		{
			return true;
		}

		length = NormalizeLength(raw.Trim());
		return length != null;
	}

	private static string NormalizeLength(string raw)
	{
		if (raw.Equals("Auto", StringComparison.OrdinalIgnoreCase))
		{
			return "Auto";
		}

		if (raw == "*")
		{
			return "*";
		}

		if (raw.EndsWith("*", StringComparison.Ordinal))
		{
			var number = raw.Substring(0, raw.Length - 1).Trim();
			if (number.Length == 0)
			{
				return "*";
			}

			double value;
			if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				return null;
			}

			if (value == 1d)
			{
				return "*";
			}

			return value.ToString(CultureInfo.InvariantCulture) + "*";
		}

		double pixels;
		if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out pixels))
		{
			return null;
		}

		return pixels.ToString(CultureInfo.InvariantCulture);
	}

	private static bool IsDefinitionsElement(string name, out string attributeName, out string childName)
	{
		attributeName = null;
		childName = null;
		var local = name;
		var dot = name.LastIndexOf('.');
		if (dot >= 0)
		{
			local = name.Substring(dot + 1);
		}

		if (local == "ColumnDefinitions")
		{
			attributeName = "ColumnDefinitions";
			childName = "ColumnDefinition";
			return true;
		}

		if (local == "RowDefinitions")
		{
			attributeName = "RowDefinitions";
			childName = "RowDefinition";
			return true;
		}

		return false;
	}

	private static bool TryReadStartTag(string text, int start, out string name, out int closeAngle, out bool selfClosing)
	{
		Dictionary<string, string> ignoredAttributes;
		List<AttributePiece> ignoredPieces;
		return TryReadStartTag(text, start, out name, out closeAngle, out selfClosing, out ignoredAttributes, out ignoredPieces);
	}

	private static bool TryReadStartTag(string text, int start, out string name, out int closeAngle, out bool selfClosing, out List<AttributePiece> pieces)
	{
		Dictionary<string, string> ignoredAttributes;
		return TryReadStartTag(text, start, out name, out closeAngle, out selfClosing, out ignoredAttributes, out pieces);
	}

	private static bool TryReadStartTag(string text, int start, out string name, out int closeAngle, out bool selfClosing, out Dictionary<string, string> attributes)
	{
		List<AttributePiece> ignoredPieces;
		return TryReadStartTag(text, start, out name, out closeAngle, out selfClosing, out attributes, out ignoredPieces);
	}

	private static bool TryReadStartTag(
		string text,
		int start,
		out string name,
		out int closeAngle,
		out bool selfClosing,
		out Dictionary<string, string> attributes,
		out List<AttributePiece> pieces)
	{
		name = null;
		closeAngle = -1;
		selfClosing = false;
		attributes = null;
		pieces = null;
		if ((start >= text.Length) || (text[start] != '<') || IsClosingTag(text, start) || IsIgnoredTag(text, start))
		{
			return false;
		}

		var index = start + 1;
		var nameStart = index;
		while ((index < text.Length) && IsNameChar(text[index]))
		{
			index++;
		}

		if (index == nameStart)
		{
			return false;
		}

		name = text.Substring(nameStart, index - nameStart);
		attributes = new Dictionary<string, string>(StringComparer.Ordinal);
		pieces = new List<AttributePiece>();
		while (index < text.Length)
		{
			var leadStart = index;
			while ((index < text.Length) && char.IsWhiteSpace(text[index]))
			{
				index++;
			}

			var leading = text.Substring(leadStart, index - leadStart);

			if (index >= text.Length)
			{
				return false;
			}

			if (text[index] == '>')
			{
				closeAngle = index;
				return true;
			}

			if ((text[index] == '/') && ((index + 1) < text.Length) && (text[index + 1] == '>'))
			{
				selfClosing = true;
				closeAngle = index + 1;
				return true;
			}

			var attrStart = index;
			while ((index < text.Length) && IsNameChar(text[index]))
			{
				index++;
			}

			if (index == attrStart)
			{
				return false;
			}

			var attrName = text.Substring(attrStart, index - attrStart);
			while ((index < text.Length) && char.IsWhiteSpace(text[index]))
			{
				index++;
			}

			if ((index >= text.Length) || (text[index] != '='))
			{
				return false;
			}

			index++;
			while ((index < text.Length) && char.IsWhiteSpace(text[index]))
			{
				index++;
			}

			if (index >= text.Length)
			{
				return false;
			}

			var quote = text[index];
			if ((quote != '"') && (quote != '\''))
			{
				return false;
			}

			index++;
			var valueStart = index;
			while ((index < text.Length) && (text[index] != quote))
			{
				index++;
			}

			if (index >= text.Length)
			{
				return false;
			}

			var value = text.Substring(valueStart, index - valueStart);
			attributes[attrName] = value;
			pieces.Add(new AttributePiece(
				attrName,
				value,
				attrStart,
				index,
				leading,
				text.Substring(attrStart, (index + 1) - attrStart)));
			index++;
		}

		return false;
	}

	private static bool TryReadCloseTag(string text, int start, out string name, out int end)
	{
		name = null;
		end = -1;
		if (!IsClosingTag(text, start))
		{
			return false;
		}

		var index = start + 2;
		var nameStart = index;
		while ((index < text.Length) && IsNameChar(text[index]))
		{
			index++;
		}

		if (index == nameStart)
		{
			return false;
		}

		name = text.Substring(nameStart, index - nameStart);
		while ((index < text.Length) && char.IsWhiteSpace(text[index]))
		{
			index++;
		}

		if ((index >= text.Length) || (text[index] != '>'))
		{
			return false;
		}

		end = index + 1;
		return true;
	}

	private static bool IsClosingTag(string text, int index)
	{
		return ((index + 1) < text.Length) && (text[index] == '<') && (text[index + 1] == '/');
	}

	private static bool IsIgnoredTag(string text, int index)
	{
		return StartsAt(text, index, "<!--") ||
			StartsAt(text, index, "<?") ||
			StartsAt(text, index, "<![CDATA[");
	}

	private static int SkipIgnored(string text, int index)
	{
		if (StartsAt(text, index, "<!--"))
		{
			var end = text.IndexOf("-->", index + 4, StringComparison.Ordinal);
			return end < 0 ? text.Length : end + 3;
		}

		if (StartsAt(text, index, "<![CDATA["))
		{
			var end = text.IndexOf("]]>", index + 9, StringComparison.Ordinal);
			return end < 0 ? text.Length : end + 3;
		}

		return SkipTo(text, index + 2, '>') + 1;
	}

	private static int SkipTo(string text, int index, char value)
	{
		var found = text.IndexOf(value, index);
		return found < 0 ? text.Length - 1 : found;
	}

	private static bool StartsAt(string text, int index, string value)
	{
		if ((index < 0) || (index + value.Length > text.Length))
		{
			return false;
		}

		return string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
	}

	private static bool IsNameChar(char value)
	{
		return char.IsLetterOrDigit(value) || (value == '.') || (value == ':') || (value == '_') || (value == '-');
	}

	private static bool IsWhitespace(string text, int start, int end)
	{
		for (var i = start; i < end && i < text.Length; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
			{
				return false;
			}
		}

		return true;
	}

	#endregion
}
