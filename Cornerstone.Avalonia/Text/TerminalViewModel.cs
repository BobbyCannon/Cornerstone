#region References

using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
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
	#region Fields

	private readonly StringBuilder _secureInput = new();

	#endregion

	#region Constructors

	public TerminalViewModel()
	{
		CommandHistoryProvider = new CommandHistoryProvider();
		HighlightCurrentLine = false;
		Prompt = "> ";
		PaintedPrompt = string.Empty;
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

	/// <summary>
	/// Nested ReadLine while a command is still running (old Terminal IsPromptingForInput).
	/// </summary>
	[Notify]
	public partial bool IsPromptingForInput { get; set; }

	/// <summary>
	/// Nested ReadPassword: typed characters are masked and stored off the document.
	/// </summary>
	[Notify]
	public partial bool IsPromptingForInputSecurely { get; set; }

	[Notify]
	public partial string Prompt { get; set; }

	[Notify]
	public partial int PromptOffset { get; set; }

	public TerminalTokenizer Tokenizer { get; }

	/// <summary>
	/// Prompt string last painted into the buffer. Used to update a live line
	/// in place when the child sends a new prompt string (e.g. after cd).
	/// </summary>
	internal string PaintedPrompt { get; private set; }

	private int PromptStartOffset => Math.Max(0, PromptOffset - (Prompt?.Length ?? 0));

	#endregion

	#region Methods

	/// <summary>
	/// Append output and apply ANSI SGR (color / bold / italic). Escape sequences are not stored.
	/// Style carries across calls so split PowerShell writes stay colored.
	/// </summary>
	public void AppendAnsi(ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
		{
			return;
		}

		Tokenizer.ProcessAnsiText(this, text);
	}

	public void AppendAnsi(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		AppendAnsi(text.AsSpan());
	}

	public void AppendSecureChar(char value)
	{
		if (!IsPromptingForInputSecurely
			|| char.IsControl(value))
		{
			return;
		}

		_secureInput.Append(value);
		Append("*");
	}

	/// <summary>
	/// Opens a nested input prompt. Does not clear <see cref="IsCommandProcessing" />.
	/// </summary>
	public void BeginPromptForInput(string message)
	{
		BeginPromptForInputCore(message, false);
	}

	/// <summary>
	/// Opens a nested password prompt. Typed characters are stored off the document.
	/// </summary>
	public void BeginPromptForInputSecurely(string message)
	{
		BeginPromptForInputCore(message, true);
	}

	public bool CanModify(int offset)
	{
		// Secure input is handled by the control (mask + off-document buffer).
		if (IsPromptingForInputSecurely)
		{
			return false;
		}

		// Freeze while a command runs, unless a nested ReadLine/ReadPassword is open.
		if (IsCommandProcessing && !IsPromptingForInput)
		{
			return false;
		}

		return offset >= PromptOffset;
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

	/// <summary>
	/// Closes a nested input prompt. Leaves <see cref="IsCommandProcessing" /> unchanged.
	/// </summary>
	public void EndPromptForInput()
	{
		_secureInput.Clear();
		IsPromptingForInput = false;
		IsPromptingForInputSecurely = false;
		PromptOffset = DocumentLength;
	}

	/// <inheritdoc />
	/// <remarks>
	/// Returns the intersection of <paramref name="range" /> with the editable input region
	/// [<see cref="PromptOffset" />, <see cref="TextEditorViewModel.DocumentLength" />).
	/// Empty when processing a command or when the range is fully before the prompt.
	/// </remarks>
	public IEnumerable<IRange> GetDeletableSegments(IRange range)
	{
		if (IsPromptingForInputSecurely
			|| (IsCommandProcessing && !IsPromptingForInput)
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

	/// <summary>
	/// Moves toward older history. On first call, stashes <paramref name="currentInput" /> as the draft.
	/// </summary>
	/// <returns> History command to show, or null if already at oldest / no history. </returns>
	public string HistoryPrevious(string currentInput)
	{
		if (!IsBrowsingHistory)
		{
			HistoryDraft = currentInput ?? string.Empty;
			IsBrowsingHistory = true;
		}

		return CommandHistoryProvider?.Previous();
	}

	public bool RemoveLastSecureChar()
	{
		if (!IsPromptingForInputSecurely
			|| (_secureInput.Length <= 0))
		{
			return false;
		}

		_secureInput.Length--;
		if (DocumentLength > PromptOffset)
		{
			RemoveAt(DocumentLength - 1, 1);
		}

		return true;
	}

	public string TakeSecureInput()
	{
		var value = _secureInput.ToString();
		_secureInput.Clear();
		return value;
	}

	/// <summary>
	/// Append visible text and record a style token (no escape characters).
	/// While a prompt is live, inserts before the prompt and pins the viewport.
	/// </summary>
	internal void AppendStyled(string text, Color? foreground, Color? background, bool? bold, bool? italic, bool? strikethrough)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (ShouldInsertBeforePrompt())
		{
			InsertBeforePrompt(text, foreground, background, bold, italic, strikethrough);
			return;
		}

		var start = DocumentLength;
		Append(text);
		AddStyleToken(start, DocumentLength, foreground, background, bold, italic, strikethrough);
	}

	internal bool BufferEndsWithPrompt()
	{
		return !string.IsNullOrEmpty(Prompt)
			&& (PromptOffset == DocumentLength)
			&& (PromptOffset >= Prompt.Length)
			&& BufferEquals(PromptOffset - Prompt.Length, Prompt);
	}

	internal void ClearPaintedPrompt()
	{
		PaintedPrompt = string.Empty;
	}

	/// <summary>
	/// If a live prompt is not on its own line, insert a newline before it.
	/// Call after a complete write (not between ANSI chunks).
	/// </summary>
	internal void EnsureNewlineBeforeLivePrompt()
	{
		if (!ShouldInsertBeforePrompt())
		{
			return;
		}

		var offset = PromptStartOffset;
		if ((offset > 0) && (Buffer[offset - 1] == '\n'))
		{
			return;
		}

		InsertAtPromptStart(Environment.NewLine, null, null, null, null, null);
	}

	/// <summary>
	/// True when the last painted prompt is the live suffix (optional typed input,
	/// no newline after <see cref="PromptOffset" />). Mid-document offsets from a
	/// submitted command are not live.
	/// </summary>
	internal bool IsLivePromptSuffix()
	{
		return IsLivePromptSuffix(Prompt);
	}

	internal bool IsLivePromptSuffix(string prompt)
	{
		if (IsCommandProcessing
			|| string.IsNullOrEmpty(prompt)
			|| (PromptOffset < prompt.Length)
			|| (PromptOffset > DocumentLength))
		{
			return false;
		}

		var start = PromptOffset - prompt.Length;
		if (!BufferEquals(start, prompt))
		{
			return false;
		}

		for (var i = PromptOffset; i < DocumentLength; i++)
		{
			var c = Buffer[i];
			if ((c == '\n') || (c == '\r'))
			{
				return false;
			}
		}

		return true;
	}

	internal void MarkPromptPainted()
	{
		PaintedPrompt = string.IsNullOrEmpty(Prompt) ? string.Empty : Prompt;
	}

	/// <summary>
	/// Shows a command prompt and clears <see cref="IsCommandProcessing" />.
	/// Reuses a live line when possible; otherwise appends a new prompt line.
	/// History browse is cleared only when a new line is painted.
	/// </summary>
	public void PaintCommandPrompt()
	{
		IsCommandProcessing = false;
		if (TryApplyLivePrompt())
		{
			return;
		}

		ClearHistoryNavigation();
		if (BufferEndsWithPrompt())
		{
			return;
		}

		if ((DocumentLength > 0) && (Buffer[DocumentLength - 1] != '\n'))
		{
			Append(Environment.NewLine);
		}

		if (!string.IsNullOrEmpty(Prompt))
		{
			Append(Prompt);
		}

		PromptOffset = DocumentLength;
		MarkPromptPainted();
		Caret.Move(PromptOffset);
	}

	/// <summary>
	/// If a live prompt is already on the last line, keep typed input and either
	/// no-op (same string) or replace the painted prompt in place. Returns false
	/// when a new prompt line must be appended.
	/// </summary>
	internal bool TryApplyLivePrompt()
	{
		if (IsLivePromptSuffix())
		{
			MarkPromptPainted();
			return true;
		}

		if (string.IsNullOrEmpty(PaintedPrompt)
			|| !IsLivePromptSuffix(PaintedPrompt))
		{
			return false;
		}

		if (string.IsNullOrEmpty(Prompt))
		{
			return true;
		}

		ReplaceLivePrompt(PaintedPrompt, Prompt);
		return true;
	}

	private void AddStyleToken(int start, int end, Color? foreground, Color? background, bool? bold, bool? italic, bool? strikethrough)
	{
		if (end <= start)
		{
			return;
		}

		var token = Tokenizer.CreateOrUpdateSection(
			0, start, end,
			foreground?.ToUInt32(), background?.ToUInt32(),
			bold, italic, strikethrough);
		TokenManager.Add(token);
	}

	private void BeginPromptForInputCore(string message, bool secure)
	{
		if (!string.IsNullOrEmpty(message))
		{
			Append(message);
		}

		_secureInput.Clear();
		PromptOffset = DocumentLength;
		IsPromptingForInputSecurely = secure;
		IsPromptingForInput = true;
		Caret.Move(DocumentLength);
	}

	private bool BufferEquals(int start, string value)
	{
		if ((start < 0) || ((start + value.Length) > DocumentLength))
		{
			return false;
		}

		for (var i = 0; i < value.Length; i++)
		{
			if (Buffer[start + i] != value[i])
			{
				return false;
			}
		}

		return true;
	}

	private void InsertAtPromptStart(string insert, Color? foreground, Color? background, bool? bold, bool? italic, bool? strikethrough)
	{
		var offset = PromptStartOffset;
		if (offset > DocumentLength)
		{
			offset = DocumentLength;
		}

		InsertUnrestricted(offset, insert, true);

		var delta = insert.Length;
		TokenManager.ShiftOffsets(offset, delta);
		AddStyleToken(offset, offset + insert.Length, foreground, background, bold, italic, strikethrough);

		PromptOffset += delta;
		ShiftCaretAndSelection(offset, delta);
	}

	private void InsertBeforePrompt(string text, Color? foreground, Color? background, bool? bold, bool? italic, bool? strikethrough)
	{
		InsertAtPromptStart(text, foreground, background, bold, italic, strikethrough);
	}

	private void ReplaceLivePrompt(string oldPrompt, string newPrompt)
	{
		var start = PromptOffset - oldPrompt.Length;
		if (start < 0)
		{
			start = 0;
		}

		var oldLength = oldPrompt.Length;
		var newLength = newPrompt.Length;
		var delta = newLength - oldLength;
		var caret = Caret.Offset;
		var selection = Caret.Selection;
		var selectionLength = selection.Length;
		var selectionStart = selection.StartOffset;
		var selectionEnd = selection.EndOffset;

		if (oldLength > 0)
		{
			RemoveAt(start, oldLength);
		}

		if (newLength > 0)
		{
			InsertUnrestricted(start, newPrompt, true);
		}

		if (delta != 0)
		{
			TokenManager.ShiftOffsets(start, delta);
		}

		PromptOffset += delta;
		MarkPromptPainted();

		var newCaret = caret >= start ? caret + delta : caret;
		if (newCaret < 0)
		{
			newCaret = 0;
		}
		if (newCaret > DocumentLength)
		{
			newCaret = DocumentLength;
		}

		Caret.Move(newCaret);

		if (selectionLength <= 0)
		{
			return;
		}

		var newStart = selectionStart >= start
			? selectionStart + delta
			: selectionStart;
		var newEnd = selectionEnd >= start
			? selectionEnd + delta
			: selectionEnd;
		selection.Update(newStart, newEnd);
	}

	private void ShiftCaretAndSelection(int insertOffset, int delta)
	{
		if (Caret.Offset >= insertOffset)
		{
			Caret.Move(Caret.Offset + delta);
		}

		var selection = Caret.Selection;
		if (selection.Length <= 0)
		{
			return;
		}

		var start = selection.StartOffset >= insertOffset
			? selection.StartOffset + delta
			: selection.StartOffset;
		var end = selection.EndOffset >= insertOffset
			? selection.EndOffset + delta
			: selection.EndOffset;
		selection.Update(start, end);
	}

	private bool ShouldInsertBeforePrompt()
	{
		return IsLivePromptSuffix();
	}

	#endregion
}