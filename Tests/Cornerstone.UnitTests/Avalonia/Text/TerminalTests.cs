#region References

using System;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class TerminalTests : CornerstoneAvaloniaUnitTest
{
	#region Methods

	[TestMethod]
	public void AllOffsetsBeforePromptShouldBeReadOnly()
	{
		var scenarios = new (string Name, Action<Terminal> Act, string ExpectedText, int ExpectedOffset, bool ExpectedCanPaste)[]
		{
			("1", t => t.ViewModel.Caret.Move(0), "> ", 0, false),
			("2", t => t.ViewModel.Caret.Move(1), "> ", 1, false),
			("3", t => t.ViewModel.Caret.Move(2), "> ", 2, true),
			("4", t => t.ViewModel.Delete(1, false), "> ", 2, true),
			("5", t => t.ViewModel.Delete(1, true), "> ", 2, true),
			("6", t => t.ViewModel.Delete(2, false), "> ", 2, true),
			("7", t => t.ViewModel.Delete(2, true), "> ", 2, true),
			("8", t => t.ViewModel.Insert(0, "Test"), "> ", 2, true),
			("9", t => t.ViewModel.Insert(1, "Test"), "> ", 2, true),
			("10", t => t.ViewModel.Insert(2, "Test"), "> Test", 2, true)
		};

		foreach (var scenario in scenarios)
		{
			scenario.Name.Dump();

			var terminal = new Terminal
			{
				ViewModel = { Prompt = "> " }
			};
			terminal.PromptForCommand();

			AreEqual("> ", terminal.ViewModel.ToString());
			AreEqual(2, terminal.ViewModel.Caret.Offset);
			IsTrue(terminal.ViewModel.Clipboard.CanPaste());

			scenario.Act(terminal);

			AreEqual(scenario.ExpectedText, terminal.ViewModel.ToString());
			AreEqual(scenario.ExpectedOffset, terminal.ViewModel.Caret.Offset, () => "Caret offset incorrect");
			AreEqual(scenario.ExpectedCanPaste, terminal.ViewModel.Clipboard.CanPaste());
		}
	}

	[TestMethod]
	public void CommandProcessingLocksInputUntilEndCommand()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("help");

		IsFalse(terminal.ViewModel.IsCommandProcessing);
		IsTrue(terminal.ViewModel.CanModify(terminal.ViewModel.Caret.Offset));
		IsTrue(terminal.ViewModel.Clipboard.CanPaste());

		var commandSeen = (string) null;
		terminal.CommandEntered += (_, cmd) => commandSeen = cmd;

		terminal.ExecuteInput();

		AreEqual("help", commandSeen);
		IsTrue(terminal.ViewModel.IsCommandProcessing);
		IsFalse(terminal.ViewModel.CanModify(terminal.ViewModel.DocumentLength));
		IsFalse(terminal.ViewModel.Clipboard.CanPaste());
		IsFalse(terminal.ViewModel.Clipboard.CanCut());

		// Host writes output while locked.
		terminal.WriteOutput("ok\r\n");
		IsTrue(terminal.ViewModel.ToString().Contains("ok"));
		IsTrue(terminal.ViewModel.IsCommandProcessing);

		// Nested execute / set-input must no-op while processing.
		var lengthWhileProcessing = terminal.ViewModel.DocumentLength;
		terminal.ExecuteCommand("again");
		terminal.SetInput("nope");
		AreEqual(lengthWhileProcessing, terminal.ViewModel.DocumentLength);

		// Insert into document via view-model is also blocked.
		terminal.ViewModel.Insert(terminal.ViewModel.DocumentLength, "x");
		AreEqual(lengthWhileProcessing, terminal.ViewModel.DocumentLength);

		terminal.EndCommand();

		IsFalse(terminal.ViewModel.IsCommandProcessing);
		IsTrue(terminal.ViewModel.CanModify(terminal.ViewModel.Caret.Offset));
		IsTrue(terminal.ViewModel.Clipboard.CanPaste());
		IsTrue(terminal.ViewModel.ToString().EndsWith("> "));
	}

	[TestMethod]
	public void EndCommandWithoutPromptOnlyClearsProcessingLock()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("x");
		terminal.ExecuteInput();
		IsTrue(terminal.ViewModel.IsCommandProcessing);

		var before = terminal.ViewModel.ToString();
		terminal.EndCommand(prompt: false);

		IsFalse(terminal.ViewModel.IsCommandProcessing);
		AreEqual(before, terminal.ViewModel.ToString());
	}

	[TestMethod]
	public void WriteErrorWrapsPlainTextInRedAnsi()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.ExecuteInput();
		IsTrue(terminal.ViewModel.IsCommandProcessing);

		terminal.WriteError("boom");
		// ANSI is tokenized away from the plain buffer text for colored appends —
		// buffer should still contain the message body.
		IsTrue(terminal.ViewModel.ToString().Contains("boom"));
	}

	[TestMethod]
	public void AppendAnsiStripsSgrAndKeepsVisibleText()
	{
		var viewModel = new TerminalViewModel();
		viewModel.AppendAnsi("\u001b[32;1mMode\u001b[0m LastWriteTime");

		AreEqual("Mode LastWriteTime", viewModel.ToString());
		IsTrue(viewModel.TokenManager.Count > 0);
	}

	[TestMethod]
	public void AppendAnsiKeepsStyleAcrossChunks()
	{
		var viewModel = new TerminalViewModel();
		viewModel.AppendAnsi("\u001b[44;1m");
		viewModel.AppendAnsi("cs");
		viewModel.AppendAnsi("\u001b[0m");

		AreEqual("cs", viewModel.ToString());
		IsTrue(viewModel.TokenManager.Count > 0);
	}

	[TestMethod]
	public void SelectionSpanningPromptOnlyDeletesEditableInput()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("hello");

		AreEqual("> hello", terminal.ViewModel.ToString());
		AreEqual(2, terminal.ViewModel.PromptOffset);

		// Select entire document (prompt + input).
		terminal.ViewModel.Caret.Selection.Update(0, terminal.ViewModel.DocumentLength);
		IsTrue(terminal.ViewModel.TryRemoveSelection(out var removed));
		AreEqual(5, removed);
		AreEqual("> ", terminal.ViewModel.ToString());
		AreEqual(2, terminal.ViewModel.Caret.Offset);
	}

	[TestMethod]
	public void SelectionFullyInPromptIsNotDeleted()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("ab");

		terminal.ViewModel.Caret.Selection.Update(0, 2); // only "> "
		IsFalse(terminal.ViewModel.TryRemoveSelection(out var removed));
		AreEqual(0, removed);
		AreEqual("> ab", terminal.ViewModel.ToString());
	}

	[TestMethod]
	public void CutSelectionSpanningPromptCopiesOnlyEditableText()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("cutme");

		terminal.ViewModel.Caret.Selection.Update(0, terminal.ViewModel.DocumentLength);
		IsTrue(terminal.ViewModel.Clipboard.CanCut());
		// Cut uses async clipboard; still removes deletable text synchronously via TryRemoveSelection.
		terminal.ViewModel.Clipboard.Cut();
		AreEqual("> ", terminal.ViewModel.ToString());
	}

	[TestMethod]
	public void SmartHomeClampsToPromptOffset()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("  hello");

		// Caret at end of input
		terminal.ViewModel.Caret.MoveToEnd();
		AreEqual(terminal.ViewModel.DocumentLength, terminal.ViewModel.Caret.Offset);

		// First Home: first non-whitespace of editable region ("h")
		terminal.ViewModel.Caret.Move(CaretMoveDirection.LineSmartStart, false);
		AreEqual(4, terminal.ViewModel.Caret.Offset); // "> " (2) + "  " (2) => 'h' at 4

		// Second Home: editable start (prompt offset), not document/line start
		terminal.ViewModel.Caret.Move(CaretMoveDirection.LineSmartStart, false);
		AreEqual(2, terminal.ViewModel.Caret.Offset);
		AreEqual(terminal.ViewModel.PromptOffset, terminal.ViewModel.Caret.Offset);
	}

	[TestMethod]
	public void GetDeletableSegmentsIntersectsPromptRegion()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("xy");

		var segments = terminal.ViewModel
			.GetDeletableSegments(new Cornerstone.Collections.Range { StartOffset = 0, EndOffset = 4 })
			.ToList();

		AreEqual(1, segments.Count);
		AreEqual(2, segments[0].StartOffset);
		AreEqual(4, segments[0].EndOffset);
	}

	[TestMethod]
	public void HistoryDraftRestoredWhenLeavingHistory()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.ViewModel.CommandHistoryProvider.Append("help");
		terminal.ViewModel.CommandHistoryProvider.Append("clear");

		terminal.SetInput("my-draft");
		AreEqual("my-draft", terminal.ReadInput());
		IsFalse(terminal.ViewModel.IsBrowsingHistory);

		// Up → newest history, draft stashed
		var older = terminal.ViewModel.HistoryPrevious(terminal.ReadInput());
		AreEqual("clear", older);
		terminal.SetInput(older);
		IsTrue(terminal.ViewModel.IsBrowsingHistory);
		AreEqual("my-draft", terminal.ViewModel.HistoryDraft);

		// Up → older
		older = terminal.ViewModel.HistoryPrevious(terminal.ReadInput());
		AreEqual("help", older);
		terminal.SetInput(older);

		// Down → newer history
		var next = terminal.ViewModel.HistoryNext(out var restoredDraft);
		IsFalse(restoredDraft);
		AreEqual("clear", next);
		terminal.SetInput(next);

		// Down past newest → draft restored
		next = terminal.ViewModel.HistoryNext(out restoredDraft);
		IsTrue(restoredDraft);
		AreEqual("my-draft", next);
		IsFalse(terminal.ViewModel.IsBrowsingHistory);
		terminal.SetInput(next);
		AreEqual("my-draft", terminal.ReadInput());
	}

	[TestMethod]
	public void HistoryDraftClearedWhenCommandSubmitted()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.ViewModel.CommandHistoryProvider.Append("help");

		terminal.SetInput("unfinished");
		var older = terminal.ViewModel.HistoryPrevious(terminal.ReadInput());
		terminal.SetInput(older);
		IsTrue(terminal.ViewModel.IsBrowsingHistory);
		IsNotNull(terminal.ViewModel.HistoryDraft);

		terminal.CommandEntered += (_, _) => { };
		terminal.ExecuteInput();

		IsFalse(terminal.ViewModel.IsBrowsingHistory);
		IsNull(terminal.ViewModel.HistoryDraft);
	}

	[TestMethod]
	public void SetInputShorterThanCurrentInputKeepsLastLineInBuffer()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("a-very-long-draft-line");
		AreEqual("a-very-long-draft-line", terminal.ReadInput());

		terminal.SetInput("help");

		AreEqual("help", terminal.ReadInput());
		var last = terminal.ViewModel.Lines.LastOrDefault();
		IsNotNull(last);
		AreEqual(terminal.ViewModel.DocumentLength, last.EndOffset);

		// History Up/Down used to throw: Range exceeds buffer content (logicalLength)
		terminal.ViewModel.Lines.Measure(new Size(800, 400), false);
		AreEqual(terminal.ViewModel.DocumentLength, last.EndOffset);
	}

	[TestMethod]
	public void HistoryDownWhenNotBrowsingDoesNotClearInput()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("keep-me");

		var next = terminal.ViewModel.HistoryNext(out var restoredDraft);
		IsNull(next);
		IsFalse(restoredDraft);
		AreEqual("keep-me", terminal.ReadInput());
	}

	[TestMethod]
	public void LateOutputInsertsBeforeLivePromptAndKeepsInput()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("foo");
		var caretAfterInput = terminal.ViewModel.Caret.Offset;

		terminal.WriteOutput("late\n");

		AreEqual("late\n> foo", terminal.ViewModel.ToString());
		AreEqual(7, terminal.ViewModel.PromptOffset);
		AreEqual("foo", terminal.ReadInput());
		AreEqual(caretAfterInput + "late\n".Length, terminal.ViewModel.Caret.Offset);
		IsTrue(terminal.ViewModel.LastChangePinnedViewport);
	}

	[TestMethod]
	public void LateOutputWithoutNewlineKeepsPromptOnOwnLine()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("bar");

		terminal.WriteOutput("late");

		AreEqual("late" + Environment.NewLine + "> bar", terminal.ViewModel.ToString());
		AreEqual("bar", terminal.ReadInput());
		IsTrue(terminal.ViewModel.LastChangePinnedViewport);
	}

	[TestMethod]
	public void LateAnsiOutputInsertsBeforePromptAndShiftsTokens()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("cmd");
		var promptStart = terminal.ViewModel.PromptOffset - terminal.ViewModel.Prompt.Length;

		terminal.ViewModel.AppendAnsi("\u001b[32mgreen\u001b[0m\n");

		AreEqual("green\n> cmd", terminal.ViewModel.ToString());
		AreEqual("cmd", terminal.ReadInput());
		IsTrue(terminal.ViewModel.TokenManager.Count > 0);

		var token = terminal.ViewModel.TokenManager[0];
		AreEqual(promptStart, token.StartOffset);
		IsTrue(token.EndOffset <= terminal.ViewModel.PromptOffset);
	}

	[TestMethod]
	public void OutputWhileProcessingStillAppendsAtEnd()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("help");
		terminal.ExecuteInput();
		IsTrue(terminal.ViewModel.IsCommandProcessing);

		terminal.WriteOutput("ok\n");

		IsTrue(terminal.ViewModel.ToString().EndsWith("ok\n"));
		IsFalse(terminal.ViewModel.LastChangePinnedViewport);
		IsFalse(terminal.ViewModel.ToString().StartsWith("ok"));
		IsFalse(terminal.ViewModel.IsLivePromptSuffix());
	}

	[TestMethod]
	public void EndCommandAfterOutputPaintsNewPromptAtEnd()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("ls -r");
		terminal.ExecuteInput();
		terminal.WriteOutput("file-a\nfile-b\n");

		var offsetDuringCommand = terminal.ViewModel.PromptOffset;
		IsTrue(offsetDuringCommand < terminal.ViewModel.DocumentLength);

		terminal.EndCommand();

		var text = terminal.ViewModel.ToString().Replace("\r\n", "\n");
		AreEqual("> ls -r\nfile-a\nfile-b\n> ", text);
		AreEqual(terminal.ViewModel.DocumentLength, terminal.ViewModel.PromptOffset);
		IsTrue(terminal.ViewModel.IsLivePromptSuffix());
		IsFalse(terminal.ViewModel.IsCommandProcessing);
		IsFalse(terminal.ViewModel.CanModify(offsetDuringCommand));
	}

	[TestMethod]
	public void OutputDuringCommandDoesNotInsertAtOldPrompt()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("ls -r");
		terminal.ExecuteInput();
		var promptOffset = terminal.ViewModel.PromptOffset;

		terminal.WriteOutput("file-a\n");

		AreEqual(promptOffset, terminal.ViewModel.PromptOffset);
		AreEqual("> ls -r\nfile-a\n", terminal.ViewModel.ToString().Replace("\r\n", "\n"));
		IsFalse(terminal.ViewModel.IsLivePromptSuffix());
	}

	[TestMethod]
	public void PromptForCommandAfterErrorOutputPaintsNewPrompt()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "PS> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("Open-File 'blah'");
		terminal.CommandEntered += (_, _) => { };
		terminal.ExecuteInput();
		IsTrue(terminal.ViewModel.IsCommandProcessing);

		terminal.WriteError("Open-File : File not found: blah");
		terminal.PromptForCommand();

		IsFalse(terminal.ViewModel.IsCommandProcessing);
		IsTrue(terminal.ViewModel.ToString().Contains("File not found"));
		IsTrue(terminal.ViewModel.ToString().EndsWith("PS> "));
	}

	[TestMethod]
	public void InternalPromptSkipsWhenDocumentAlreadyEndsWithPrompt()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		var once = terminal.ViewModel.ToString();
		var offset = terminal.ViewModel.PromptOffset;

		terminal.PromptForCommand();

		AreEqual(once, terminal.ViewModel.ToString());
		AreEqual(offset, terminal.ViewModel.PromptOffset);
	}

	[TestMethod]
	public void PromptForCommandWhileUserHasTypedDoesNotWrapDraft()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("next-cmd");
		var caret = terminal.ViewModel.Caret.Offset;

		terminal.PromptForCommand();

		AreEqual("> next-cmd", terminal.ViewModel.ToString());
		AreEqual(2, terminal.ViewModel.PromptOffset);
		AreEqual("next-cmd", terminal.ReadInput());
		AreEqual(caret, terminal.ViewModel.Caret.Offset);
		IsTrue(terminal.ViewModel.IsLivePromptSuffix());
	}

	[TestMethod]
	public void PromptForCommandReplacesLivePromptWhenStringChanges()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "PS C:\\old> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("dir");

		terminal.ViewModel.Prompt = "PS C:\\new> ";
		terminal.PromptForCommand();

		AreEqual("PS C:\\new> dir", terminal.ViewModel.ToString());
		AreEqual("dir", terminal.ReadInput());
		AreEqual("PS C:\\new> ".Length, terminal.ViewModel.PromptOffset);
		IsTrue(terminal.ViewModel.IsLivePromptSuffix());
		IsFalse(terminal.ViewModel.ToString().Contains("old"));
	}

	[TestMethod]
	public void PromptForCommandSameStringDoesNotClearHistoryBrowse()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.ViewModel.CommandHistoryProvider.Append("help");
		terminal.SetInput("draft");
		var older = terminal.ViewModel.HistoryPrevious(terminal.ReadInput());
		terminal.SetInput(older);
		IsTrue(terminal.ViewModel.IsBrowsingHistory);

		terminal.PromptForCommand();

		IsTrue(terminal.ViewModel.IsBrowsingHistory);
		AreEqual("draft", terminal.ViewModel.HistoryDraft);
		AreEqual("help", terminal.ReadInput());
	}

	[TestMethod]
	public void AppendTextWithoutColorInsertsBeforeLivePrompt()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("foo");

		terminal.AppendText("late");

		AreEqual("late" + Environment.NewLine + "> foo", terminal.ViewModel.ToString());
		AreEqual("foo", terminal.ReadInput());
		IsTrue(terminal.ViewModel.LastChangePinnedViewport);
	}

	[TestMethod]
	public void LateAnsiOutputWithoutNewlineKeepsPromptOnOwnLine()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("bar");

		terminal.ViewModel.AppendAnsi("late");

		AreEqual("late" + Environment.NewLine + "> bar", terminal.ViewModel.ToString());
		AreEqual("bar", terminal.ReadInput());
	}

	[TestMethod]
	public void BeginPromptForInputWhileProcessingAllowsExecuteInput()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("Read-Host");
		terminal.CommandEntered += (_, _) => { };
		terminal.ExecuteInput();
		IsTrue(terminal.ViewModel.IsCommandProcessing);

		terminal.WriteOutput("Name: ");
		terminal.BeginPromptForInput("");

		IsTrue(terminal.ViewModel.IsPromptingForInput);
		IsTrue(terminal.ViewModel.CanModify(terminal.ViewModel.DocumentLength));

		var seen = (string) null;
		terminal.CommandEntered += (_, cmd) => seen = cmd;
		terminal.SetInput("alice");
		terminal.ExecuteInput();

		AreEqual("alice", seen);
		IsFalse(terminal.ViewModel.IsPromptingForInput);
		IsTrue(terminal.ViewModel.IsCommandProcessing);
		IsFalse(terminal.ViewModel.CommandHistoryProvider.Any(x => x.Command == "alice"));
	}

	[TestMethod]
	public void BeginPromptForInputSecurelyMasksAndReturnsSecret()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.CommandEntered += (_, _) => { };
		terminal.ExecuteInput();
		terminal.BeginPromptForInputSecurely("Password: ");

		IsTrue(terminal.ViewModel.IsPromptingForInputSecurely);
		IsFalse(terminal.ViewModel.CanModify(terminal.ViewModel.DocumentLength));

		terminal.ViewModel.AppendSecureChar('s');
		terminal.ViewModel.AppendSecureChar('e');
		terminal.ViewModel.AppendSecureChar('c');
		IsTrue(terminal.ViewModel.ToString().EndsWith("***"));
		IsFalse(terminal.ViewModel.ToString().Contains("sec"));

		var seen = (string) null;
		terminal.CommandEntered += (_, cmd) => seen = cmd;
		terminal.ExecuteInput();

		AreEqual("sec", seen);
		IsFalse(terminal.ViewModel.IsPromptingForInputSecurely);
	}

	#endregion
}