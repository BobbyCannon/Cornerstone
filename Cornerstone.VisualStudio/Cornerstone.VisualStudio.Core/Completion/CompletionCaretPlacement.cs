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
			return 0;
		}

		if (recommendedCursorOffset is int idx && (idx >= 0) && (idx <= insertText.Length))
		{
			return idx;
		}

		// Fallback: end of insert.
		return insertText.Length;
	}

	/// <summary>
	/// Absolute caret position after replacing <c>[replaceStart, replaceStart+filterLen)</c>
	/// with <paramref name="insertText"/>.
	/// </summary>
	public static int GetCaretAfterReplace(
		int replaceStart,
		string insertText,
		int? recommendedCursorOffset)
	{
		var index = ResolveCaretIndexInInsert(insertText, recommendedCursorOffset);
		return replaceStart + index;
	}

	/// <summary>
	/// Full commit simulation: replace filter, place caret, optionally apply indent pin
	/// (as if smart-indent grew leading whitespace then we restored it).
	/// </summary>
	/// <returns>Final document text and caret position.</returns>
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

		var caret = GetCaretAfterReplace(filterStart, insertText, recommendedCursorOffset);

		// Optional: simulate XML smart-indent growing leading ws on the edited line, then pin.
		if (smartIndentedLeadingWs != null)
		{
			var lineStart = LineStartIndex(afterReplace, filterStart);
			var lineEnd = LineEndIndex(afterReplace, filterStart);
			var line = afterReplace.Substring(lineStart, lineEnd - lineStart);
			var originalLine = documentBefore.Substring(
				LineStartIndex(documentBefore, filterStart),
				LineEndIndex(documentBefore, filterStart) - LineStartIndex(documentBefore, filterStart));

			// Grow indent artificially (what the editor does on Enter commit).
			var grown = smartIndentedLeadingWs + line.Substring(CompletionEngine.GetLeadingWhitespace(line).Length);
			afterReplace = afterReplace.Substring(0, lineStart) + grown + afterReplace.Substring(lineEnd);

			// Caret shifts by the grown indent delta.
			var grownDelta = smartIndentedLeadingWs.Length - CompletionEngine.GetLeadingWhitespace(line).Length;
			caret += grownDelta;

			// Pin back to original leading ws (our restore).
			var line2Start = LineStartIndex(afterReplace, Math.Min(caret, afterReplace.Length - 1));
			var line2End = LineEndIndex(afterReplace, line2Start);
			var line2 = afterReplace.Substring(line2Start, line2End - line2Start);
			var fixedLine = CompletionEngine.PreserveLineLeadingWhitespace(originalLine, line2);
			var pinDelta = CompletionEngine.GetLeadingWhitespace(fixedLine).Length
				- CompletionEngine.GetLeadingWhitespace(line2).Length;
			afterReplace = afterReplace.Substring(0, line2Start) + fixedLine + afterReplace.Substring(line2End);
			caret += pinDelta;
		}

		caret = Math.Max(0, Math.Min(caret, afterReplace.Length));
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

	private static int LineStartIndex(string text, int position)
	{
		position = Math.Max(0, Math.Min(position, Math.Max(0, text.Length - 1)));
		var i = position;
		while (i > 0 && text[i - 1] != '\n' && text[i - 1] != '\r')
		{
			i--;
		}

		return i;
	}

	private static int LineEndIndex(string text, int position)
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
