#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace Cornerstone.Parsers.Markdown;

public enum ColumnAlignment
{
	Left,
	Center,
	Right
}

public static class MarkdownTableFormatter
{
	#region Methods

	/// <summary>
	/// Formats a Markdown table with optional maximum total table width.
	/// If the table exceeded maxTableWidth, columns are scaled proportionally and content is wrapped.
	/// </summary>
	/// <param name="markdownTable"> Input markdown table </param>
	/// <param name="maxTableWidth">
	/// Maximum total width of the rendered table in characters (including pipes and spaces).
	/// Default int.MaxValue = no limit.
	/// </param>
	public static string Format(string markdownTable, int maxTableWidth = int.MaxValue)
	{
		if (string.IsNullOrWhiteSpace(markdownTable))
		{
			return markdownTable;
		}

		return Format(markdownTable.AsSpan(), maxTableWidth);
	}

	public static string Format(ReadOnlySpan<char> inputSpan, int maxTableWidth = int.MaxValue)
	{
		if (maxTableWidth < 10) // absolute minimum sensible width
		{
			maxTableWidth = 10;
		}

		var (numCols, maxWidths, alignments, explicitAlign) = ComputeTableMetadata(inputSpan, maxTableWidth);
		if (numCols == 0)
		{
			return inputSpan.ToString();
		}

		return BuildFormattedTable(inputSpan, numCols, maxWidths, alignments, explicitAlign);
	}

	private static void AppendAlignedRow(StringBuilder sb, List<List<string>> rowLines,
		int[] maxWidths, ColumnAlignment[] alignments)
	{
		var maxLines = 0;
		foreach (var cellLines in rowLines)
		{
			if (cellLines.Count > maxLines)
			{
				maxLines = cellLines.Count;
			}
		}

		for (var lineIdx = 0; lineIdx < maxLines; lineIdx++)
		{
			sb.Append('|');

			for (var col = 0; col < rowLines.Count; col++)
			{
				var cellLines = rowLines[col];
				var cellLine = lineIdx < cellLines.Count ? cellLines[lineIdx] : string.Empty;
				var width = maxWidths[col];
				var align = col < alignments.Length ? alignments[col] : ColumnAlignment.Left;

				sb.Append(' ');

				var padding = width - cellLine.Length;
				if (padding < 0)
				{
					padding = 0;
				}

				if ((align == ColumnAlignment.Left) || (padding <= 0))
				{
					sb.Append(cellLine);
					sb.Append(' ', padding);
				}
				else if (align == ColumnAlignment.Right)
				{
					sb.Append(' ', padding);
					sb.Append(cellLine);
				}
				else
				{
					var leftPad = padding / 2;
					sb.Append(' ', leftPad);
					sb.Append(cellLine);
					sb.Append(' ', padding - leftPad);
				}

				sb.Append(" |");
			}
			sb.AppendLine();
		}
	}

	private static void AppendSeparator(StringBuilder sb, int[] maxWidths,
		ColumnAlignment[] alignments, bool[] explicitAlign)
	{
		sb.Append('|');

		for (var i = 0; i < maxWidths.Length; i++)
		{
			var align = i < alignments.Length ? alignments[i] : ColumnAlignment.Left;
			var isExplicit = (i < explicitAlign.Length) && explicitAlign[i];
			var w = maxWidths[i];

			// Each block between | must be exactly w + 2 characters long
			// (leading space + w content chars + trailing space in rendered rows)
			if (align == ColumnAlignment.Center)
			{
				sb.Append(':');
				sb.Append('-', w);
				sb.Append(':');
			}
			else if (align == ColumnAlignment.Right)
			{
				sb.Append('-', w + 1); // FIXED: was w
				sb.Append(':');
			}
			else // Left
			{
				if (isExplicit)
				{
					sb.Append(':');
					sb.Append('-', w + 1); // FIXED: was w
				}
				else
				{
					sb.Append('-', w + 2); // FIXED: was w + 1
				}
			}

			sb.Append('|');
		}
	}

	private static string BuildFormattedTable(ReadOnlySpan<char> input, int numCols,
		int[] maxWidths, ColumnAlignment[] alignments, bool[] explicitAlign)
	{
		var contentRows = new List<List<List<string>>>(16);

		foreach (var line in input.EnumerateLines())
		{
			var trimmed = line.Trim();
			if (trimmed.IsEmpty || !trimmed.StartsWith('|') || IsSeparatorLine(trimmed))
			{
				continue;
			}

			var cells = ParseRow(trimmed);
			while (cells.Count < numCols)
			{
				cells.Add(string.Empty);
			}

			var wrappedRow = new List<List<string>>(numCols);
			for (var i = 0; (i < cells.Count) && (i < maxWidths.Length); i++)
			{
				var wrapped = WrapText(cells[i], maxWidths[i]);
				wrappedRow.Add(wrapped);
			}
			contentRows.Add(wrappedRow);
		}

		if (contentRows.Count == 0)
		{
			return input.ToString();
		}

		var sb = new StringBuilder(2048);

		// Header row
		AppendAlignedRow(sb, contentRows[0], maxWidths, alignments);

		// Separator
		AppendSeparator(sb, maxWidths, alignments, explicitAlign);
		sb.AppendLine();

		// Data rows
		for (var i = 1; i < contentRows.Count; i++)
		{
			AppendAlignedRow(sb, contentRows[i], maxWidths, alignments);
		}

		// Trim trailing whitespace
		while ((sb.Length > 0) && char.IsWhiteSpace(sb[^1]))
		{
			sb.Length--;
		}

		return sb.ToString();
	}

	private static (int numCols, int[] maxWidths, ColumnAlignment[] alignments, bool[] explicitAlign)
		ComputeTableMetadata(ReadOnlySpan<char> input, int maxTableWidth)
	{
		if (maxTableWidth < 10)
		{
			maxTableWidth = 10;
		}

		const int softMinWidth = 8;
		const int maxNaturalBoost = 4;

		var naturalWidths = new List<int>(16);
		List<ColumnAlignment> alignList = null;
		List<bool> explicitList = null;
		var separatorFound = false;

		// First pass: discover natural column widths + parse the first separator line for alignments
		foreach (var line in input.EnumerateLines())
		{
			var trimmed = line.Trim();
			if (trimmed.IsEmpty || !trimmed.StartsWith('|'))
			{
				continue;
			}

			if (!separatorFound && IsSeparatorLine(trimmed))
			{
				separatorFound = true;
				var (al, ex) = ParseAlignments(trimmed);
				alignList = al.ToList();
				explicitList = ex.ToList();
				continue;
			}

			if (separatorFound && IsSeparatorLine(trimmed))
			{
				continue;
			}

			// Content row – compute cell lengths only (no string allocations)
			var colIndex = 0;
			var pos = 1;
			while (pos < trimmed.Length)
			{
				var nextPipe = trimmed[pos..].IndexOf('|');
				if (nextPipe == -1)
				{
					nextPipe = trimmed.Length - pos;
				}

				var cell = trimmed.Slice(pos, nextPipe).Trim();
				var length = cell.Length;

				if (colIndex == naturalWidths.Count)
				{
					naturalWidths.Add(length);
				}
				else if (naturalWidths[colIndex] < length)
				{
					naturalWidths[colIndex] = length;
				}

				colIndex++;
				pos += nextPipe + 1;
			}
		}

		var numCols = naturalWidths.Count;
		if (numCols == 0)
		{
			return (0, Array.Empty<int>(), Array.Empty<ColumnAlignment>(), Array.Empty<bool>());
		}

		// Convert to arrays and enforce absolute minimum width
		var widths = naturalWidths.ToArray();
		for (var c = 0; c < numCols; c++)
		{
			if (widths[c] < 3)
			{
				widths[c] = 3;
			}
		}

		var naturalWidthsArr = (int[]) widths.Clone();

		// ====================== IMPROVED WIDTH DISTRIBUTION ======================
		// Gentle boost for naturally small columns (makes the table more readable)
		for (var c = 0; c < numCols; c++)
		{
			if (naturalWidthsArr[c] < softMinWidth)
			{
				widths[c] = Math.Min(naturalWidthsArr[c] + maxNaturalBoost, softMinWidth);
			}
		}

		// Fixed overhead: (numCols + 1) pipes + 2 * numCols spaces
		var fixedOverhead = (3 * numCols) + 1;
		var currentContentWidth = widths.Sum();
		var currentTotal = fixedOverhead + currentContentWidth;

		if (currentTotal <= maxTableWidth)
		{
			// Table already fits → return the boosted widths
			return FinalizeMetadata(numCols, widths, alignList, explicitList);
		}

		// Table is too wide → shrink intelligently
		var availableContentWidth = Math.Max(numCols * 3, maxTableWidth - fixedOverhead);

		var totalSoftMin = numCols * softMinWidth;
		var excessNatural = currentContentWidth - totalSoftMin;

		if (excessNatural > 0)
		{
			// Prefer shrinking only the long columns (short columns stay protected)
			var excessAvailable = Math.Max(0, availableContentWidth - totalSoftMin);
			var scale = (double) excessAvailable / excessNatural;

			for (var i = 0; i < numCols; i++)
			{
				var excessInThisCol = widths[i] - softMinWidth;
				if (excessInThisCol > 0)
				{
					widths[i] = softMinWidth + (int) Math.Floor(excessInThisCol * scale);
				}

				// short columns keep their boosted width
			}
		}
		else
		{
			// Fallback: simple proportional scaling from the boosted widths
			var scale = (double) availableContentWidth / currentContentWidth;
			for (var i = 0; i < numCols; i++)
			{
				widths[i] = Math.Max(3, (int) Math.Floor(widths[i] * scale));
			}
		}

		// Fine-tune integer distribution (add/remove remainder preferring widest columns)
		currentContentWidth = widths.Sum();
		var remainder = availableContentWidth - currentContentWidth;

		if (remainder > 0)
		{
			// Give extra space to originally widest columns first
			var indices = Enumerable.Range(0, numCols)
				.OrderByDescending(i => naturalWidthsArr[i])
				.ToArray();

			for (var j = 0; (remainder > 0) && (j < numCols); j++)
			{
				widths[indices[j]]++;
				remainder--;
			}
		}
		else if (remainder < 0)
		{
			// Remove excess from currently widest columns first
			var indices = Enumerable.Range(0, numCols)
				.OrderByDescending(i => widths[i])
				.ToArray();

			var excess = -remainder;
			for (var j = 0; (excess > 0) && (j < numCols); j++)
			{
				var i = indices[j];
				if (widths[i] > 3)
				{
					widths[i]--;
					excess--;
				}
			}
		}

		return FinalizeMetadata(numCols, widths, alignList, explicitList);
	}

	private static (int numCols, int[] maxWidths, ColumnAlignment[] alignments, bool[] explicitAlign)
		FinalizeMetadata(int numCols, int[] widths, List<ColumnAlignment> alignList, List<bool> explicitList)
	{
		// Handle alignment list (pad or truncate to match actual column count)
		if (alignList == null)
		{
			alignList = new List<ColumnAlignment>(numCols);
			explicitList = new List<bool>(numCols);
		}

		// Truncate if separator had more columns than content
		if (alignList.Count > numCols)
		{
			alignList.RemoveRange(numCols, alignList.Count - numCols);
			explicitList.RemoveRange(numCols, explicitList.Count - numCols);
		}

		// Pad with Left alignment if separator had fewer columns
		while (alignList.Count < numCols)
		{
			alignList.Add(ColumnAlignment.Left);
			explicitList.Add(false);
		}

		return (numCols, widths, alignList.ToArray(), explicitList.ToArray());
	}

	private static bool IsSeparatorLine(ReadOnlySpan<char> line)
	{
		var hasDash = false;
		foreach (var c in line)
		{
			if (c is '|' or ':' or ' ')
			{
				continue;
			}
			if (c != '-')
			{
				return false;
			}
			hasDash = true;
		}
		return hasDash;
	}

	private static (ColumnAlignment[] alignments, bool[] explicitAlign) ParseAlignments(ReadOnlySpan<char> separatorLine)
	{
		var alignments = new List<ColumnAlignment>(8);
		var explicitAlign = new List<bool>(8);

		var pos = 1;
		while (pos < separatorLine.Length)
		{
			var nextPipe = separatorLine[pos..].IndexOf('|');
			if (nextPipe == -1)
			{
				nextPipe = separatorLine.Length - pos;
			}

			var cell = separatorLine.Slice(pos, nextPipe).Trim();

			var hasLeft = !cell.IsEmpty && (cell[0] == ':');
			var hasRight = !cell.IsEmpty && (cell[^1] == ':');

			var align = ColumnAlignment.Left;
			var isExplicit = hasLeft || hasRight;

			if (hasLeft && hasRight)
			{
				align = ColumnAlignment.Center;
			}
			else if (hasRight)
			{
				align = ColumnAlignment.Right;
			}

			alignments.Add(align);
			explicitAlign.Add(isExplicit);

			pos += nextPipe + 1;
		}

		return (alignments.ToArray(), explicitAlign.ToArray());
	}

	private static List<string> ParseRow(ReadOnlySpan<char> line)
	{
		var cells = new List<string>(8);
		var pos = 1;

		while (pos < line.Length)
		{
			var nextPipe = line[pos..].IndexOf('|');
			if (nextPipe == -1)
			{
				nextPipe = line.Length - pos;
			}

			var cellSpan = line.Slice(pos, nextPipe).Trim();
			cells.Add(cellSpan.ToString());

			pos += nextPipe + 1;
		}
		return cells;
	}

	/// <summary>
	/// Wraps text preferring word boundaries, falls back to character wrapping.
	/// </summary>
	private static List<string> WrapText(string text, int maxWidth)
	{
		if (string.IsNullOrEmpty(text) || (maxWidth < 1))
		{
			return new List<string> { text ?? "" };
		}

		var lines = new List<string>();
		var current = new StringBuilder(maxWidth);

		foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			var addedLength = word.Length + (current.Length > 0 ? 1 : 0);

			if ((current.Length + addedLength) <= maxWidth)
			{
				if (current.Length > 0)
				{
					current.Append(' ');
				}
				current.Append(word);
			}
			else
			{
				if (current.Length > 0)
				{
					lines.Add(current.ToString());
					current.Clear();
				}

				// Force character wrap for long unbreakable words
				if (word.Length > maxWidth)
				{
					for (var start = 0; start < word.Length; start += maxWidth)
					{
						var len = Math.Min(maxWidth, word.Length - start);
						lines.Add(word.Substring(start, len));
					}
				}
				else
				{
					current.Append(word);
				}
			}
		}

		if (current.Length > 0)
		{
			lines.Add(current.ToString());
		}

		return lines.Count > 0 ? lines : new List<string> { "" };
	}

	#endregion
}