#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Parsers.Markdown;

/// <summary>
/// Structured GFM table model for UI rendering (cells keep raw markdown for inline projection).
/// </summary>
public sealed class MarkdownTableModel
{
	#region Constructors

	public MarkdownTableModel(
		IReadOnlyList<MarkdownTableRow> rows,
		IReadOnlyList<ColumnAlignment> alignments,
		bool hasHeader)
	{
		Rows = rows ?? Array.Empty<MarkdownTableRow>();
		Alignments = alignments ?? Array.Empty<ColumnAlignment>();
		HasHeader = hasHeader;
		ColumnCount = 0;
		foreach (var row in Rows)
		{
			if (row.Cells.Count > ColumnCount)
			{
				ColumnCount = row.Cells.Count;
			}
		}
	}

	#endregion

	#region Properties

	public IReadOnlyList<ColumnAlignment> Alignments { get; }

	public int ColumnCount { get; }

	/// <summary>
	/// True when a separator row was present; first content row is the header.
	/// </summary>
	public bool HasHeader { get; }

	public IReadOnlyList<MarkdownTableRow> Rows { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Parses a markdown table source (header, separator, body). Cell text is preserved as-is
	/// (including <c>[links](url)</c>) for the Avalonia inline projector.
	/// </summary>
	public static MarkdownTableModel Parse(ReadOnlySpan<char> source)
	{
		var contentRows = new List<List<string>>(16);
		ColumnAlignment[] alignments = null;
		var separatorFound = false;

		foreach (var line in source.EnumerateLines())
		{
			var trimmed = line.Trim();
			if (trimmed.IsEmpty || !trimmed.StartsWith('|'))
			{
				continue;
			}

			if (IsSeparatorLine(trimmed))
			{
				if (!separatorFound)
				{
					separatorFound = true;
					alignments = ParseAlignments(trimmed);
				}
				continue;
			}

			contentRows.Add(ParseRow(trimmed));
		}

		if (contentRows.Count == 0)
		{
			return new MarkdownTableModel([], [], false);
		}

		var columnCount = 0;
		foreach (var row in contentRows)
		{
			if (row.Count > columnCount)
			{
				columnCount = row.Count;
			}
		}

		// Pad short rows; pad alignments to column count
		alignments ??= CreateLeftAlignments(columnCount);
		if (alignments.Length < columnCount)
		{
			var padded = new ColumnAlignment[columnCount];
			Array.Copy(alignments, padded, alignments.Length);
			for (var i = alignments.Length; i < columnCount; i++)
			{
				padded[i] = ColumnAlignment.Left;
			}
			alignments = padded;
		}

		var rows = new List<MarkdownTableRow>(contentRows.Count);
		foreach (var cells in contentRows)
		{
			while (cells.Count < columnCount)
			{
				cells.Add(string.Empty);
			}

			if (cells.Count > columnCount)
			{
				cells.RemoveRange(columnCount, cells.Count - columnCount);
			}

			var cellModels = new MarkdownTableCell[columnCount];
			for (var c = 0; c < columnCount; c++)
			{
				cellModels[c] = new MarkdownTableCell(cells[c] ?? string.Empty);
			}

			rows.Add(new MarkdownTableRow(cellModels));
		}

		return new MarkdownTableModel(rows, alignments, separatorFound);
	}

	public static MarkdownTableModel Parse(string source)
	{
		return Parse((source ?? string.Empty).AsSpan());
	}

	private static ColumnAlignment[] CreateLeftAlignments(int count)
	{
		var result = new ColumnAlignment[count];
		for (var i = 0; i < count; i++)
		{
			result[i] = ColumnAlignment.Left;
		}
		return result;
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

	private static ColumnAlignment[] ParseAlignments(ReadOnlySpan<char> separatorLine)
	{
		var alignments = new List<ColumnAlignment>(8);
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
			if (hasLeft && hasRight)
			{
				align = ColumnAlignment.Center;
			}
			else if (hasRight)
			{
				align = ColumnAlignment.Right;
			}

			alignments.Add(align);
			pos += nextPipe + 1;
		}

		return alignments.ToArray();
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

			cells.Add(line.Slice(pos, nextPipe).Trim().ToString());
			pos += nextPipe + 1;
		}
		return cells;
	}

	#endregion
}

public sealed class MarkdownTableRow
{
	#region Constructors

	public MarkdownTableRow(IReadOnlyList<MarkdownTableCell> cells)
	{
		Cells = cells ?? Array.Empty<MarkdownTableCell>();
	}

	#endregion

	#region Properties

	public IReadOnlyList<MarkdownTableCell> Cells { get; }

	#endregion
}

public sealed class MarkdownTableCell
{
	#region Constructors

	public MarkdownTableCell(string source)
	{
		Source = source ?? string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Raw cell markdown (may include links, emphasis, inline code).
	/// </summary>
	public string Source { get; }

	#endregion
}
