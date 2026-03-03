#region References

using System;
using System.IO;
using System.Text;

#endregion

namespace Cornerstone.Text;

public static class FileModifier
{
	#region Constants

	public const string SlashSectionFormat = "// Generated Code - {0}";
	public const string XmlGeneratedSectionFormat = "<!-- Generated Code - {0} -->";
	public const string XmlTemplateSectionFormat = "<!-- Code Template - {0} -->";

	#endregion

	#region Methods

	/// <summary>
	/// Replaces a named generated section while preserving the indentation level
	/// of the line where the start marker appears.
	/// Returns true if the file was modified.
	/// </summary>
	public static bool TryUpdateSection(
		FileInfo file,
		string sectionFormat,
		string sectionName,
		Action<StringBuilder> generateContent)
	{
		if (!file.Exists)
		{
			return false;
		}

		var original = File.ReadAllText(file.FullName, Encoding.UTF8);
		var startMarker = string.Format(sectionFormat, sectionName);
		var endMarker = string.Format(sectionFormat, "/" + sectionName);

		var startIdx = original.IndexOf(startMarker, StringComparison.Ordinal);
		if (startIdx < 0)
		{
			return false;
		}

		var endIdx = original.IndexOf(endMarker, startIdx + startMarker.Length, StringComparison.Ordinal);
		if (endIdx < 0)
		{
			return false;
		}
		endIdx += endMarker.Length;

		// Get base indentation from the line of the start marker
		var baseIndent = GetIndentOfLine(original, startIdx);

		var sbGenerated = new StringBuilder();
		generateContent(sbGenerated);

		// Re-indent the generated content to match baseIndent
		var reindented = Reindent(sbGenerated.ToString(), baseIndent);

		// Build new block (marker + content + marker)
		var newBlock = new StringBuilder();
		newBlock.Append(startMarker);
		if (reindented.Length > 0)
		{
			newBlock.AppendLine();
			newBlock.Append(reindented);
			if (!reindented.EndsWith('\n'))
			{
				newBlock.AppendLine();
			}
		}
		else
		{
			newBlock.Append(' ');
		}
		newBlock.Append(baseIndent);
		newBlock.Append(endMarker);

		var newBlockStr = newBlock.ToString();

		// Quick check if anything changed
		var oldBlockSpan = original.AsSpan(startIdx, endIdx - startIdx);
		if (oldBlockSpan.SequenceEqual(newBlockStr.AsSpan()))
		{
			return false;
		}

		// Efficient replacement
		var final = new StringBuilder((original.Length - (endIdx - startIdx)) + newBlockStr.Length + 16);
		final.Append(original, 0, startIdx);
		final.Append(newBlockStr);
		final.Append(original, endIdx, original.Length - endIdx);

		File.WriteAllText(file.FullName, final.ToString(), Encoding.UTF8);
		return true;
	}

	public static bool TryUpdateSection(
		FileInfo file,
		string sectionFormat,
		string sectionName,
		string generatedContent)
	{
		return TryUpdateSection(file, sectionFormat, sectionName, sb => sb.Append(generatedContent));
	}

	private static string GetIndentOfLine(string text, int position)
	{
		// Find start of the line
		var lineStart = position;
		while ((lineStart > 0) && (text[lineStart - 1] != '\n') && (text[lineStart - 1] != '\r'))
		{
			lineStart--;
		}

		// Count leading whitespace
		var i = lineStart;
		while ((i < text.Length) && ((text[i] == ' ') || (text[i] == '\t')))
		{
			i++;
		}

		return text.Substring(lineStart, i - lineStart);
	}

	private static string Reindent(string content, string baseIndent)
	{
		if (string.IsNullOrEmpty(content))
		{
			return "";
		}
		if (string.IsNullOrEmpty(baseIndent))
		{
			return content;
		}

		var text = content.AsSpan();
		var firstLineTabs = 0;

		while ((firstLineTabs < text.Length) && (text[firstLineTabs] == '\t'))
		{
			firstLineTabs++;
		}

		var tabsToRemove = firstLineTabs - baseIndent.Length;
		if (tabsToRemove == 0)
		{
			return content;
		}

		var sb = new StringBuilder(content.Length + (baseIndent.Length * 8));

		var isFirst = true;
		foreach (var r in text.Split('\n'))
		{
			var line = text[r];
			if (line.Length == 0)
			{
				continue;
			}
			
			if (!isFirst)
			{
				sb.Append('\n');
			}
			isFirst = false;

			var tabsInLine = 0;
			while ((tabsInLine < line.Length) && (line[tabsInLine] == '\t'))
			{
				tabsInLine++;
			}

			var skip = Math.Max(0, tabsToRemove);
			skip = Math.Min(skip, tabsInLine);

			sb.Append(baseIndent);
			sb.Append(line.Slice(skip));
		}

		var response = sb.ToString();
		return response;
	}

	#endregion
}