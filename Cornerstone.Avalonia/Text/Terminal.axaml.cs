#region References

using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Avalonia.Extensions;

#endregion

namespace Cornerstone.Avalonia.Text;

/// <summary>
/// Represents the terminal console.
/// </summary>
/// <remarks>
/// https://en.wikipedia.org/wiki/ANSI_escape_code
/// </remarks>
public partial class Terminal : TextEditor<TerminalViewModel>
{
	#region Fields

	public static readonly Key[] ArrowKeys;
	public static readonly Key[] ModifierKeys;
	public static readonly Key[] NavigationKeys;

	#endregion

	#region Constructors

	public Terminal()
	{
		AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
	}

	static Terminal()
	{
		ArrowKeys = [Key.Up, Key.Down, Key.Left, Key.Right];
		ModifierKeys = [Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt, Key.LeftShift, Key.RightShift];
		NavigationKeys = [Key.Home, Key.End, Key.PageUp, Key.PageDown];
	}

	#endregion

	#region Properties

	public Color BackgroundColor => GetColor(Background, Colors.Blue);

	public Color ForegroundColor => GetColor(Foreground, Colors.White);

	#endregion

	#region Methods

	public void AppendText(string text, Color? foregroundColor = null, Color? backgroundColor = null)
	{
		if ((text != null)
			&& text.Contains('\e'))
		{
			ViewModel.Tokenizer.ProcessAnsiText(this, text);
			return;
		}

		if (((foregroundColor != null) && (foregroundColor != ForegroundColor))
			|| ((backgroundColor != null) && (backgroundColor != BackgroundColor)))
		{
			AppendTextWithColor(text, foregroundColor, backgroundColor);
			return;
		}

		ViewModel.Append(text);
	}

	/// <summary>
	/// Shows a new prompt and marks the terminal ready for input.
	/// Clears <see cref="TerminalViewModel.IsCommandProcessing" />.
	/// </summary>
	public void BeginPrompt()
	{
		PromptForCommand();
	}

	public override void Clear()
	{
		ViewModel.IsCommandProcessing = false;
		ViewModel.ClearHistoryNavigation();
		ViewModel.PromptOffset = 0;
		ViewModel.TokenManager.Clear();
		base.Clear();
	}

	/// <summary>
	/// Completes the current command: clears the processing lock and optionally re-prompts.
	/// </summary>
	/// <param name="prompt">When true (default), writes a new prompt via <see cref="PromptForCommand" />.</param>
	public void EndCommand(bool prompt = true)
	{
		if (prompt)
		{
			PromptForCommand();
			return;
		}

		ViewModel.IsCommandProcessing = false;
	}

	public void ExecuteCommand(string command)
	{
		if (ViewModel.IsCommandProcessing)
		{
			return;
		}

		SetInput(command);
		ExecuteInput();
	}

	public void ExecuteInput()
	{
		if (ViewModel.IsCommandProcessing)
		{
			return;
		}

		var command = ReadInput();
		ViewModel.ClearHistoryNavigation();
		AppendText(Environment.NewLine);
		OnCommandEntered(command);
	}

	/// <summary>
	/// Shows a new prompt and marks the terminal ready for input.
	/// Clears <see cref="TerminalViewModel.IsCommandProcessing" /> and history draft/browse state.
	/// </summary>
	public void PromptForCommand()
	{
		ViewModel.IsCommandProcessing = false;
		ViewModel.ClearHistoryNavigation();
		InternalPrompt();
	}

	public string ReadInput()
	{
		var length = ViewModel.DocumentLength - ViewModel.PromptOffset;
		return length <= 0
			? string.Empty
			: ViewModel.Buffer.Substring(ViewModel.PromptOffset, length);
	}

	public void SetInput(string value)
	{
		if (ViewModel.IsCommandProcessing
			|| (value == null))
		{
			return;
		}

		ViewModel.Buffer.RemoveAt(ViewModel.PromptOffset, ViewModel.DocumentLength - ViewModel.PromptOffset);
		AppendText(value);
		Dispatcher.Post(() => ViewModel.Caret.MoveToEnd());
	}

	/// <summary>
	/// Writes ANSI-aware output (same pipeline as <see cref="AppendText(string, Color?, Color?)" />).
	/// Allowed while a command is processing.
	/// </summary>
	public void WriteError(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		// Bright red via SGR when the host does not already include escapes.
		if (!text.Contains('\e'))
		{
			AppendText("\e[91m" + text + "\e[0m");
			return;
		}

		AppendText(text);
	}

	/// <summary>
	/// Writes ANSI-aware output (same pipeline as <see cref="AppendText(string, Color?, Color?)" />).
	/// Allowed while a command is processing.
	/// </summary>
	public void WriteOutput(string text)
	{
		AppendText(text);
	}

	protected virtual void OnCommandEntered(string e)
	{
		ViewModel.IsCommandProcessing = true;
		ViewModel.CommandHistoryProvider.Append(e);
		CommandEntered?.Invoke(this, e);
	}

	protected override void OnTextInput(TextInputEventArgs e)
	{
		if (!CanModify())
		{
			e.Handled = true;
			base.OnTextInput(e);
			return;
		}

		base.OnTextInput(e);
	}

	internal void AppendTextWithColor(string text, Color? foregroundColor, Color? backgroundColor, bool? bold = null, bool? italic = null, bool? strikethrough = null)
	{
		var start = ViewModel.DocumentLength;
		ViewModel.Append(text);

		var token = ViewModel.Tokenizer
			.CreateOrUpdateSection(
				0, start, ViewModel.DocumentLength,
				foregroundColor?.ToUInt32(), backgroundColor?.ToUInt32(),
				bold, italic, strikethrough
			);
		ViewModel.TokenManager.Add(token);
	}

	private bool CanModify()
	{
		if (!ViewModel.CanModify(ViewModel.Caret.Offset))
		{
			return false;
		}

		var selection = ViewModel.Caret.Selection;
		if (selection is { Length: > 0 }
			&& (!ViewModel.CanModify(selection.StartOffset)
				|| !ViewModel.CanModify(selection.EndOffset)))
		{
			return false;
		}

		return true;
	}

	private static Color GetColor(IBrush brush, Color defaultColor)
	{
		try
		{
			var solidBrush = brush as SolidColorBrush;
			return solidBrush?.Color ?? ColorExtensions.ToColor(ConsoleColor.DarkBlue);
		}
		catch
		{
			return defaultColor;
		}
	}

	private void InternalPrompt()
	{
		if ((ViewModel.DocumentLength == ViewModel.PromptOffset)
			&& (ViewModel.PromptOffset > ViewModel.Prompt?.Length))
		{
			return;
		}

		if ((ViewModel.DocumentLength > 0)
			&& (ViewModel.Buffer[ViewModel.Buffer.Count - 1] != '\n'))
		{
			ViewModel.Append(Environment.NewLine);
		}

		AppendText(ViewModel.Prompt);
		ViewModel.PromptOffset = ViewModel.DocumentLength;
		ViewModel.Caret.Move(ViewModel.PromptOffset);
		Dispatcher.Post(() => { ScrollToOffset(ViewModel.PromptOffset); });
	}

	private bool IsSafeKey(Key key)
	{
		return ArrowKeys.Contains(key)
			|| ModifierKeys.Contains(key)
			|| NavigationKeys.Contains(key);
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		// While a command runs: allow navigation/modifiers (and copy shortcuts); block submit, history, and edits.
		if (ViewModel.IsCommandProcessing)
		{
			if (IsSafeKey(e.Key)
				|| IsCopyShortcut(e))
			{
				return;
			}

			e.Handled = true;
			return;
		}

		var caretIndex = ViewModel.Caret.Offset;
		if (caretIndex < ViewModel.PromptOffset)
		{
			if (e.Key == Key.None)
			{
				// Ignore modifier keys
			}
			else if (NavigationKeys.Contains(e.Key)
					|| ArrowKeys.Contains(e.Key)
					|| ModifierKeys.Contains(e.Key))
			{
				// Ignore navigation keys
			}
			else
			{
				ViewModel.Caret.Move(ViewModel.DocumentLength);
			}
		}

		if ((e.Key is Key.Up or Key.Down)
			&& (caretIndex >= ViewModel.PromptOffset)
			&& (ViewModel.CommandHistoryProvider != null))
		{
			NavigateCommandHistory(e.Key == Key.Up);
			e.Handled = true;
		}
		else if (!IsSafeKey(e.Key) && !CanModify())
		{
			e.Handled = true;
		}
		else if (e.Key is Key.Enter or Key.Return)
		{
			e.Handled = true;
			ExecuteInput();
		}
	}

	/// <summary>
	/// Up: stash draft on first press, then show older history.
	/// Down: newer history, or restore draft when past the newest entry.
	/// </summary>
	private void NavigateCommandHistory(bool older)
	{
		ViewModel.Caret.Move(ViewModel.DocumentLength);

		if (older)
		{
			var previous = ViewModel.HistoryPrevious(ReadInput());
			if (previous != null)
			{
				SetInput(previous);
			}

			return;
		}

		// Down: next history entry, or restored draft when past newest (HistoryNext returns draft string).
		var next = ViewModel.HistoryNext(out _);
		if (next != null)
		{
			SetInput(next);
		}
		// null only when not browsing — leave the live line alone
	}

	private static bool IsCopyShortcut(KeyEventArgs e)
	{
		// Ctrl+C / Ctrl+Insert — copy; clipboard layer still no-ops cut/paste via CanModify.
		return e.KeyModifiers.HasFlag(KeyModifiers.Control)
			&& e.Key is Key.C or Key.Insert;
	}

	#endregion

	#region Events

	public event EventHandler<string> CommandEntered;

	#endregion
}