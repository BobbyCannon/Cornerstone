#region References

using System;

#endregion

namespace Cornerstone.VisualStudio.Core.Completion;

/// <summary>
/// Pure caret math for completion commit. Shared by the VS command handler and unit tests
/// so IDE behavior cannot drift from tested logic.
/// </summary>
public static class CompletionCaretPlacement
{
	/// <summary>
	/// Resolves the caret index within <paramref name="insertText"/> from engine metadata.
	/// </summary>
	public static int ResolveCaretIndexInInsert(string insertText, int? recommendedCursorOffset)
	{
		if (string.IsNullOrEmpty(insertText))
		{
			if (recommendedCursorOffset is int emptyIdx && (emptyIdx >= 0))
			{
				return emptyIdx;
			}

			return 0;
		}

		var betweenQuotes = TryGetCaretIndexBetweenEmptyQuotes(insertText);
		if (betweenQuotes != null)
		{
			return betweenQuotes.Value;
		}

		if (recommendedCursorOffset is int idx && (idx >= 0) && (idx <= insertText.Length))
		{
			return idx;
		}

		return insertText.Length;
	}

	/// <summary>
	/// For inserts like <c>RequestedThemeVariant=""</c>, caret after the opening quote.
	/// </summary>
	public static int? TryGetCaretIndexBetweenEmptyQuotes(string insertText)
	{
		if (string.IsNullOrEmpty(insertText))
		{
			return null;
		}

		var doubleQuotes = insertText.IndexOf("=\"\"", StringComparison.Ordinal);
		if (doubleQuotes >= 0)
		{
			return doubleQuotes + 2;
		}

		var singleQuotes = insertText.IndexOf("=''", StringComparison.Ordinal);
		if (singleQuotes >= 0)
		{
			return singleQuotes + 2;
		}

		return null;
	}

	/// <summary>
	/// If the caret is sitting on <c>Name|=""</c>, move it between the quotes.
	/// The XML editor often appends <c>=""</c> after the name and leaves the caret
	/// at the end of the name.
	/// </summary>
	public static int SlideCaretIntoEmptyAttributeQuotes(string document, int caret)
	{
		if (string.IsNullOrEmpty(document) || (caret < 0) || (caret > document.Length))
		{
			return caret;
		}

		if ((caret > 0) && (caret < document.Length)
			&& IsQuote(document[caret - 1]) && IsQuote(document[caret])
			&& (document[caret - 1] == document[caret]))
		{
			return caret;
		}

		if (StartsAt(document, caret, "=\"\"") || StartsAt(document, caret, "=''"))
		{
			return caret + 2;
		}

		return caret;
	}

	private static bool IsQuote(char c)
	{
		return (c == '"') || (c == '\'');
	}

	private static bool StartsAt(string document, int caret, string value)
	{
		if (caret + value.Length > document.Length)
		{
			return false;
		}

		return string.CompareOrdinal(document, caret, value, 0, value.Length) == 0;
	}

	/// <summary>
	/// Absolute caret position after replacing <c>[replaceStart, replaceStart+filterLen)</c>
	/// with <paramref name="insertText"/>.
	/// </summary>
	public static (string Document, int Caret) ApplyCustomCommit(
		string document,
		int applicableStart,
		int applicableLength,
		string insertText,
		int? recommendedCursorOffset)
	{
		if (document is null)
		{
			throw new ArgumentNullException(nameof(document));
		}

		if ((applicableStart < 0) || (applicableLength < 0) ||
			(applicableStart + applicableLength > document.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(applicableStart));
		}

		var after = document.Substring(0, applicableStart) + insertText +
			document.Substring(applicableStart + applicableLength);
		var caret = PlaceCaretOnCommittedLine(
			after, applicableStart, insertText, recommendedCursorOffset);
		return (after, caret);
	}

	public static int GetCaretAfterReplace(
		int replaceStart,
		string insertText,
		int? recommendedCursorOffset)
	{
		var index = ResolveCaretIndexInInsert(insertText, recommendedCursorOffset);
		return replaceStart + index;
	}

	/// <summary>
	/// After the buffer has the committed text: caret is insertStart plus the
	/// index into InsertionText (VS replace contract). Empty-quote inserts use
	/// the quotes in the insert text, not the first ="" on the line.
	/// </summary>
	public static int PlaceCaretOnCommittedLine(
		string document,
		int insertStart,
		string insertText,
		int? recommendedCursorOffset)
	{
		if (string.IsNullOrEmpty(document))
		{
			return 0;
		}

		insertStart = Math.Max(0, Math.Min(insertStart, document.Length));
		insertText ??= string.Empty;
		var caret = GetCaretAfterReplace(insertStart, insertText, recommendedCursorOffset);
		return Math.Max(0, Math.Min(caret, document.Length));
	}

	/// <summary>
	/// Where a position after the current leading whitespace moves when that
	/// whitespace is replaced with a different indent. Positions inside the
	/// whitespace clamp to the end of the restored indent.
	/// </summary>
	public static int AdjustPositionAfterLeadingWhitespaceReplace(
		int position,
		int lineStart,
		int currentWhitespaceLength,
		int restoredWhitespaceLength)
	{
		if (position <= lineStart)
		{
			return position;
		}

		var currentWsEnd = lineStart + Math.Max(0, currentWhitespaceLength);
		if (position >= currentWsEnd)
		{
			return position + (restoredWhitespaceLength - currentWhitespaceLength);
		}

		return lineStart + restoredWhitespaceLength;
	}

	/// <summary>
	/// Replace only the leading whitespace of the line that contains
	/// <paramref name="positionOnLine"/>. Insert/caret positions after that
	/// whitespace shift by the indent delta — the tag is not in the replaced span.
	/// </summary>
	public static (string Document, int InsertStart) RestoreLeadingWhitespaceOnly(
		string document,
		int positionOnLine,
		string originalLine,
		int insertStart)
	{
		if (document is null)
		{
			throw new ArgumentNullException(nameof(document));
		}

		var lineStart = LineStartIndex(document, positionOnLine);
		var lineEnd = LineEndIndex(document, lineStart);
		var currentLine = document.Substring(lineStart, lineEnd - lineStart);
		var currentWs = CompletionEngine.GetLeadingWhitespace(currentLine);
		var originalWs = CompletionEngine.GetLeadingWhitespace(originalLine ?? string.Empty);
		if (currentWs == originalWs)
		{
			return (document, insertStart);
		}

		var restoredLine = originalWs + currentLine.Substring(currentWs.Length);
		var after = document.Substring(0, lineStart) + restoredLine + document.Substring(lineEnd);
		var shifted = AdjustPositionAfterLeadingWhitespaceReplace(
			insertStart, lineStart, currentWs.Length, originalWs.Length);
		return (after, shifted);
	}

	/// <summary>
	/// Full commit simulation: replace filter, place caret as insertStart + index,
	/// optionally grow then pin leading whitespace without replacing the tag.
	/// </summary>
	public static (string Document, int Caret) SimulateCommit(
		string documentBefore,
		int filterStart,
		int caretBefore,
		string insertText,
		int? recommendedCursorOffset,
		string smartIndentedLeadingWs = null)
	{
		if (documentBefore is null)
		{
			throw new ArgumentNullException(nameof(documentBefore));
		}

		if ((filterStart < 0) || (caretBefore < filterStart) || (caretBefore > documentBefore.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(caretBefore));
		}

		var afterReplace = CompletionEngine.ApplyCompletionReplace(
			documentBefore, filterStart, caretBefore, insertText);

		var insertStart = filterStart;
		var caretIndex = ResolveCaretIndexInInsert(insertText, recommendedCursorOffset);

		if (smartIndentedLeadingWs != null)
		{
			var lineStart = LineStartIndex(afterReplace, insertStart);
			var lineEnd = LineEndIndex(afterReplace, insertStart);
			var line = afterReplace.Substring(lineStart, lineEnd - lineStart);
			var originalLine = documentBefore.Substring(
				LineStartIndex(documentBefore, filterStart),
				LineEndIndex(documentBefore, filterStart) - LineStartIndex(documentBefore, filterStart));

			var currentWs = CompletionEngine.GetLeadingWhitespace(line);
			var grown = smartIndentedLeadingWs + line.Substring(currentWs.Length);
			afterReplace = afterReplace.Substring(0, lineStart) + grown + afterReplace.Substring(lineEnd);
			insertStart = AdjustPositionAfterLeadingWhitespaceReplace(
				insertStart, lineStart, currentWs.Length, smartIndentedLeadingWs.Length);

			var pin = RestoreLeadingWhitespaceOnly(afterReplace, insertStart, originalLine, insertStart);
			afterReplace = pin.Document;
			insertStart = pin.InsertStart;
		}

		var caret = PlaceCaretOnCommittedLine(
			afterReplace, insertStart, insertText, recommendedCursorOffset);
		return (afterReplace, caret);
	}

	/// <summary>
	/// Assert helpers: character immediately before/after caret.
	/// </summary>
	public static (char? Before, char? After) CharsAroundCaret(string document, int caret)
	{
		char? before = caret > 0 ? document[caret - 1] : null;
		char? after = caret < document.Length ? document[caret] : null;
		return (before, after);
	}

	internal static int LineStartIndex(string text, int position)
	{
		position = Math.Max(0, Math.Min(position, Math.Max(0, text.Length - 1)));
		var i = position;
		while (i > 0 && text[i - 1] != '\n' && text[i - 1] != '\r')
		{
			i--;
		}

		return i;
	}

	internal static int LineEndIndex(string text, int position)
	{
		position = Math.Max(0, Math.Min(position, text.Length));
		var i = position;
		while (i < text.Length && text[i] != '\n' && text[i] != '\r')
		{
			i++;
		}

		return i;
	}
}
