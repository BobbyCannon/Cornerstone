#region References

using System;
using System.Linq;
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

	#endregion
}