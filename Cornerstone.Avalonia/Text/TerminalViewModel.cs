#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia.Text.History;
using Cornerstone.Avalonia.Text.Input;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Range = Cornerstone.Collections.Range;

#endregion

namespace Cornerstone.Avalonia.Text;

[SourceReflection]
public partial class TerminalViewModel : TextEditorViewModel, IReadOnlySectionProvider
{
	#region Constructors

	public TerminalViewModel()
	{
		CommandHistoryProvider = new CommandHistoryProvider();
		HighlightCurrentLine = false;
		Prompt = "> ";
		ReadOnlySectionProvider = this;
		ShowLineNumbers = false;
		Tokenizer = new TerminalTokenizer();
		TokenManager.Initialize(Tokenizer);
	}

	#endregion

	#region Properties

	public ICommandHistoryProvider CommandHistoryProvider { get; }

	/// <summary>
	/// Unsubmitted input captured when the user first leaves the live line into history (Up).
	/// Restored when they Down past the newest history entry.
	/// </summary>
	public string HistoryDraft { get; private set; }

	/// <summary>
	/// True while the input line is showing a history entry rather than the live draft.
	/// </summary>
	public bool IsBrowsingHistory { get; private set; }

	[Notify]
	public partial bool IsCommandProcessing { get; set; }

	[Notify]
	public partial string Prompt { get; set; }

	[Notify]
	public partial int PromptOffset { get; set; }

	public TerminalTokenizer Tokenizer { get; }

	#endregion

	#region Methods

	public bool CanModify(int offset)
	{
		// Freeze input while a command is running; otherwise only the post-prompt range is editable.
		return !IsCommandProcessing && (offset >= PromptOffset);
	}

	/// <summary>
	/// Clears draft + browse state (new prompt, submit, clear).
	/// </summary>
	public void ClearHistoryNavigation()
	{
		HistoryDraft = null;
		IsBrowsingHistory = false;
		CommandHistoryProvider?.Reset();
	}

	/// <inheritdoc />
	/// <remarks>
	/// Returns the intersection of <paramref name="range" /> with the editable input region
	/// [<see cref="PromptOffset" />, <see cref="TextEditorViewModel.DocumentLength" />).
	/// Empty when processing a command or when the range is fully before the prompt.
	/// </remarks>
	public IEnumerable<IRange> GetDeletableSegments(IRange range)
	{
		if (IsCommandProcessing
			|| (range == null)
			|| (range.Length <= 0))
		{
			yield break;
		}

		// Editable half-open interval [PromptOffset, DocumentLength)
		var start = Math.Max(range.StartOffset, PromptOffset);
		var end = Math.Min(range.EndOffset, DocumentLength);

		if (start < end)
		{
			yield return new Range
			{
				StartOffset = start,
				EndOffset = end
			};
		}
	}

	/// <summary>
	/// Moves toward older history. On first call, stashes <paramref name="currentInput" /> as the draft.
	/// </summary>
	/// <returns>History command to show, or null if already at oldest / no history.</returns>
	public string HistoryPrevious(string currentInput)
	{
		if (!IsBrowsingHistory)
		{
			HistoryDraft = currentInput ?? string.Empty;
			IsBrowsingHistory = true;
		}

		return CommandHistoryProvider?.Previous();
	}

	/// <summary>
	/// Moves toward newer history. When past the newest entry, returns the draft and ends browse mode.
	/// </summary>
	/// <param name="restoredDraft">
	/// True when the return value is the saved draft (caller left history), not a history entry.
	/// </param>
	/// <returns>
	/// Next history command, the restored draft when leaving history, or null if not browsing.
	/// </returns>
	public string HistoryNext(out bool restoredDraft)
	{
		restoredDraft = false;

		if (!IsBrowsingHistory
			|| (CommandHistoryProvider == null))
		{
			return null;
		}

		var next = CommandHistoryProvider.Next();
		if (next != null)
		{
			return next;
		}

		// Past newest entry → live line again.
		restoredDraft = true;
		IsBrowsingHistory = false;
		return HistoryDraft ?? string.Empty;
	}

	#endregion
}