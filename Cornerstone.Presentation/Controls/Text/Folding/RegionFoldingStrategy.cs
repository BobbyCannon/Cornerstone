#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls.Text.Models;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Folding;

/// <summary>
/// Folds nested #region / #endregion blocks (C#, PowerShell, and similar).
/// </summary>
public class RegionFoldingStrategy : IFoldingStrategy
{
	#region Methods

	public IEnumerable<NewFolding> CreateNewFoldings(TextEditorViewModel viewModel)
	{
		if ((viewModel?.Lines == null) || (viewModel.Lines.Count == 0))
		{
			return [];
		}

		var stack = new Stack<(int StartOffset, string Name)>();
		var foldings = new List<NewFolding>();

		foreach (var line in viewModel.Lines)
		{
			if (!TryReadDirective(line, out var isEnd, out var name))
			{
				continue;
			}

			if (!isEnd)
			{
				stack.Push((line.StartOffset, name));
				continue;
			}

			if (stack.Count == 0)
			{
				continue;
			}

			var start = stack.Pop();
			if (line.EndOffset <= start.StartOffset)
			{
				continue;
			}

			foldings.Add(new NewFolding(start.StartOffset, line.EndOffset)
			{
				Name = string.IsNullOrEmpty(start.Name) ? "..." : start.Name
			});
		}

		foldings.Sort((left, right) => left.StartOffset.CompareTo(right.StartOffset));
		return foldings;
	}

	private static bool StartsWithKeyword(string text, int index, string keyword)
	{
		if ((index + keyword.Length) > text.Length)
		{
			return false;
		}

		if (!text.AsSpan(index, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var next = index + keyword.Length;
		return (next >= text.Length) || !char.IsLetterOrDigit(text[next]);
	}

	private static bool TryReadDirective(Text.Models.Line line, out bool isEnd, out string name)
	{
		isEnd = false;
		name = null;

		var text = line.ToString();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		var index = 0;
		while ((index < text.Length) && char.IsWhiteSpace(text[index]))
		{
			index++;
		}

		if ((index >= text.Length) || (text[index] != '#'))
		{
			return false;
		}

		index++;
		while ((index < text.Length) && char.IsWhiteSpace(text[index]))
		{
			index++;
		}

		if (StartsWithKeyword(text, index, "endregion"))
		{
			isEnd = true;
			return true;
		}

		if (!StartsWithKeyword(text, index, "region"))
		{
			return false;
		}

		index += 6;
		name = index < text.Length ? text[index..].Trim() : string.Empty;
		return true;
	}

	#endregion
}
