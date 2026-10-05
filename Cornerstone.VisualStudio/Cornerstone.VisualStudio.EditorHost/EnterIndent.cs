#region References

using System;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// Newline plus indentation inserted when Enter is pressed in a XAML buffer.
/// Copies the current line's leading tabs or spaces. An unclosed start tag
/// gains two indent levels. A finished opening tag gains one.
/// </summary>
public static class EnterIndent
{
	#region Methods

	/// <summary>
	/// Text to insert at the caret. Does not include the tail that already follows the caret.
	/// </summary>
	public static string BuildInsertion(
		string line,
		int caretIndex,
		string previousLineLeadingWhitespace,
		int indentSize,
		string newLine)
	{
		if (line == null)
		{
			line = string.Empty;
		}

		if (caretIndex < 0)
		{
			caretIndex = 0;
		}

		if (caretIndex > line.Length)
		{
			caretIndex = line.Length;
		}

		if (string.IsNullOrEmpty(newLine))
		{
			newLine = "\r\n";
		}

		if (indentSize < 0)
		{
			indentSize = 0;
		}

		var blank = string.IsNullOrWhiteSpace(line);
		var baseIndent = blank
			? previousLineLeadingWhitespace ?? string.Empty
			: LeadingWhitespace(line);

		var extraLevels = 0;
		if (!blank && IsInsideUnclosedStartTag(line, caretIndex))
		{
			extraLevels = 2;
		}
		else if (!blank && IsFinishedOpeningTagAtEnd(line, caretIndex))
		{
			extraLevels = 1;
		}

		var indent = baseIndent;
		if (extraLevels > 0)
		{
			indent += ExtraUnit(baseIndent, indentSize, extraLevels);
		}

		return newLine + indent;
	}

	public static string LeadingWhitespace(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return string.Empty;
		}

		var i = 0;
		while ((i < line.Length) && char.IsWhiteSpace(line[i]))
		{
			i++;
		}

		return line.Substring(0, i);
	}

	private static string ExtraUnit(string baseIndent, int indentSize, int levels)
	{
		if (levels <= 0)
		{
			return string.Empty;
		}

		if ((baseIndent != null) && (baseIndent.IndexOf('\t') >= 0))
		{
			return new string('\t', levels);
		}

		if (indentSize <= 0)
		{
			return string.Empty;
		}

		return new string(' ', indentSize * levels);
	}

	private static bool IsFinishedOpeningTagAtEnd(string line, int caretIndex)
	{
		for (var i = caretIndex; i < line.Length; i++)
		{
			if (!char.IsWhiteSpace(line[i]))
			{
				return false;
			}
		}

		var trimmed = line.Trim();
		if (trimmed.Length < 3)
		{
			return false;
		}

		if (trimmed[0] != '<')
		{
			return false;
		}

		var second = trimmed[1];
		if ((second == '/') || (second == '!') || (second == '?'))
		{
			return false;
		}

		if (trimmed[trimmed.Length - 1] != '>')
		{
			return false;
		}

		if (trimmed.EndsWith("/>", StringComparison.Ordinal))
		{
			return false;
		}

		return trimmed.IndexOf('>') == trimmed.Length - 1;
	}

	private static bool IsInsideUnclosedStartTag(string line, int caretIndex)
	{
		for (var i = caretIndex - 1; i >= 0; i--)
		{
			var ch = line[i];
			if (ch == '>')
			{
				return false;
			}

			if (ch == '<')
			{
				var next = (i + 1 < line.Length) ? line[i + 1] : '\0';
				return (next != '/') && (next != '!') && (next != '?');
			}
		}

		return false;
	}

	#endregion
}
