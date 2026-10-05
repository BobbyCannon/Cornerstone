#region References

using System;
using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Models;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Range = Cornerstone.Collections.Range;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class TerminalTests : CornerstoneCornerstoneUnitTest
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
	public void AppendAnsiBackgroundDoesNotSpanNewlineOntoPrompt()
	{
		var viewModel = new TerminalViewModel { Prompt = "PS> " };

		// Newline inside the SGR span (legacy host wrapping) must not paint the prompt.
		viewModel.AppendAnsi("\u001b[43mYellow\r\n\u001b[0m");
		viewModel.PaintCommandPrompt();

		AreEqual("Yellow\nPS> ", viewModel.ToString().Replace("\r\n", "\n"));

		var promptStart = viewModel.PromptOffset - viewModel.Prompt.Length;
		for (var i = 0; i < viewModel.Prompt.Length; i++)
		{
			var token = viewModel.TokenManager.GetTokenForOffset(promptStart + i);
			IsTrue(token == null);
		}
	}

	[TestMethod]
	public void AppendAnsiBackgroundStillAppliesAfterNewlineUntilReset()
	{
		var viewModel = new TerminalViewModel();
		viewModel.AppendAnsi("\u001b[43mYellow\r\nNext\u001b[0m");

		var buffer = viewModel.ToString();
		AreEqual("Yellow\nNext", buffer.Replace("\r\n", "\n"));

		var yellow = viewModel.TokenManager.GetTokenForOffset(0);
		var next = viewModel.TokenManager.GetTokenForOffset(buffer.IndexOf('N'));
		IsTrue(yellow?.Background != null);
		IsTrue(next?.Background != null);
		AreEqual(yellow.Background, next.Background);
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
	public void AppendAnsiStripsSgrAndKeepsVisibleText()
	{
		var viewModel = new TerminalViewModel();
		viewModel.AppendAnsi("\u001b[32;1mMode\u001b[0m LastWriteTime");

		AreEqual("Mode LastWriteTime", viewModel.ToString());
		IsTrue(viewModel.TokenManager.Count > 0);
	}

	[TestMethod]
	public void AppendAnsiWhiteOnRedKeepsDistinctForegroundAndBackground()
	{
		var viewModel = new TerminalViewModel();
		viewModel.AppendAnsi("\u001b[97;41m Failed \u001b[0m");

		AreEqual(" Failed ", viewModel.ToString());
		var token = viewModel.TokenManager.GetTokenForOffset(1);
		IsTrue(token != null);
		IsTrue(token.Foreground != null);
		IsTrue(token.Background != null);
		AreNotEqual(token.Foreground, token.Background);
		AreEqual(Colors.White.ToUInt32(), token.Foreground);
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
	public void AutoScrollMovesCaretToEndOfTypedInputOnLateOutput()
	{
		var terminal = new Terminal
		{
			AutoScroll = true,
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("foo");

		terminal.WriteOutput("late\n");

		AreEqual("late\n> foo", terminal.ViewModel.ToString());
		AreEqual(terminal.ViewModel.DocumentLength, terminal.ViewModel.Caret.Offset);
	}

	[TestMethod]
	public void AutoScrollMovesCaretToEndOnOutput()
	{
		var terminal = new Terminal
		{
			AutoScroll = true,
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("help");
		terminal.ExecuteInput();
		terminal.WriteOutput("ok\r\nmore\r\n");

		AreEqual(terminal.ViewModel.DocumentLength, terminal.ViewModel.Caret.Offset);
	}

	[TestMethod]
	public void AutoScrollOffLeavesCaretOnOutput()
	{
		var terminal = new Terminal
		{
			AutoScroll = false,
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("help");
		terminal.ExecuteInput();
		terminal.AutoScroll = false;
		var caret = terminal.ViewModel.Caret.Offset;

		terminal.WriteOutput("ok\r\nmore\r\n");

		AreEqual(caret, terminal.ViewModel.Caret.Offset);
		AreNotEqual(terminal.ViewModel.DocumentLength, terminal.ViewModel.Caret.Offset);
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
	public void CopyShortcutNotHandledWhenSelectingReadOnlyHistory()
	{
		var terminal = new Terminal
		{
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("help");
		terminal.ExecuteInput();
		terminal.WriteOutput("output\n");
		terminal.EndCommand();

		var text = terminal.ViewModel.ToString();
		var start = text.IndexOf("output", StringComparison.Ordinal);
		IsTrue(start >= 0);
		terminal.ViewModel.Caret.Selection.Update(start, start + 6);
		AreEqual("output", terminal.ViewModel.Clipboard.GetCopyText());

		var args = new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.C,
			KeyModifiers = KeyModifiers.Control
		};
		terminal.RaiseEvent(args);

		IsFalse(args.Handled);
		IsTrue(terminal.ViewModel.Clipboard.CanCopy());
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
		terminal.EndCommand(false);

		IsFalse(terminal.ViewModel.IsCommandProcessing);
		AreEqual(before, terminal.ViewModel.ToString());
	}

	[TestMethod]
	public void ExecuteInputTurnsAutoScrollOn()
	{
		var terminal = new Terminal
		{
			AutoScroll = false,
			ViewModel = { Prompt = "> " }
		};
		terminal.PromptForCommand();
		terminal.SetInput("help");
		terminal.ExecuteInput();

		IsTrue(terminal.AutoScroll);
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
			.GetDeletableSegments(new Range { StartOffset = 0, EndOffset = 4 })
			.ToList();

		AreEqual(1, segments.Count);
		AreEqual(2, segments[0].StartOffset);
		AreEqual(4, segments[0].EndOffset);
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
	public void NewViewWithSameViewModelKeepsAutoScrollAtBottom()
	{
		RunOnUi(() =>
		{
			var (window, terminal) = ShowTerminal(true);
			try
			{
				terminal.ScrollToEnd();
				RunUiJobs();
				var bottom = terminal.GetScrollOffset();
				IsTrue(bottom.Y > 1);
				IsTrue(terminal.AutoScroll);

				var viewModel = terminal.ViewModel;
				window.Content = new Border();
				window.UpdateLayout();
				RunUiJobs();

				var restored = new Terminal
				{
					AutoScroll = true,
					Width = 400,
					Height = 200,
					ViewModel = viewModel
				};
				window.Content = restored;
				window.UpdateLayout();
				RunUiJobs();

				IsTrue(restored.AutoScroll);
				IsTrue(restored.GetScrollOffset().Y > 1);
				IsTrue(viewModel.ViewMetrics.Offset.Y > 1);
			}
			finally
			{
				window.Close();
			}
		});
	}

	[TestMethod]
	public void NewViewWithSameViewModelKeepsManualScrollOffset()
	{
		RunOnUi(() =>
		{
			var (window, terminal) = ShowTerminal(true);
			try
			{
				terminal.ScrollToEnd();
				RunUiJobs();
				var bottom = terminal.GetScrollOffset();
				IsTrue(bottom.Y > 40);

				terminal.AutoScroll = false;
				var mid = new Vector(0, Math.Max(20, bottom.Y / 2));
				terminal.SetScrollOffset(mid);
				RunUiJobs();

				var viewModel = terminal.ViewModel;
				window.Content = new Border();
				window.UpdateLayout();
				RunUiJobs();

				var restored = new Terminal
				{
					AutoScroll = false,
					Width = 400,
					Height = 200,
					ViewModel = viewModel
				};
				window.Content = restored;
				window.UpdateLayout();
				RunUiJobs();

				IsFalse(restored.AutoScroll);
				AreEqual(viewModel.ViewMetrics.Offset.Y, restored.GetScrollOffset().Y, 2.0);
				IsTrue(restored.GetScrollOffset().Y > 1);
			}
			finally
			{
				window.Close();
			}
		});
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
	public void SetScrollOffsetIsRememberedWithoutViewport()
	{
		var editor = new TextEditor();
		IsFalse(editor.HasStableScrollViewport());

		editor.SetScrollOffset(new Vector(12, 48));

		AreEqual(new Vector(12, 48), editor.GetScrollOffset());
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
	public void TextEditorAutoScrollMovesCaretOnAppend()
	{
		var editor = new TextEditor { AutoScroll = true };
		editor.ViewModel.Load("hello");
		editor.ViewModel.Caret.Move(0);
		editor.ViewModel.Append("\r\nworld");

		AreEqual(editor.ViewModel.DocumentLength, editor.ViewModel.Caret.Offset);
	}

	[TestMethod]
	public void TextEditorAutoScrollOffLeavesCaretOnAppend()
	{
		var editor = new TextEditor { AutoScroll = false };
		editor.ViewModel.Load("hello");
		editor.ViewModel.Caret.Move(0);
		editor.ViewModel.Append("\r\nworld");

		AreEqual(0, editor.ViewModel.Caret.Offset);
	}

	[TestMethod]
	public void TextEditorAutoScrollRaisesControlPropertyWhenViewModelChanges()
	{
		var editor = new TextEditor { AutoScroll = true };
		var seen = (bool?) null;
		editor.PropertyChanged += (_, e) =>
		{
			if (e.Property == TextEditor<TextEditorViewModel>.AutoScrollProperty)
			{
				seen = editor.AutoScroll;
			}
		};

		editor.ViewModel.AutoScroll = false;

		AreEqual(false, seen);
		IsFalse(editor.AutoScroll);
	}

	[TestMethod]
	public void TextEditorForwardedPropertiesRaiseWhenViewModelChanges()
	{
		var editor = new TextEditor
		{
			HighlightCurrentLine = true,
			IsReadOnly = false,
			ShowLineNumbers = true,
			WordWrap = false
		};
		var highlight = (bool?) null;
		var readOnly = (bool?) null;
		var lineNumbers = (bool?) null;
		var wordWrap = (bool?) null;
		editor.PropertyChanged += (_, e) =>
		{
			if (e.Property == TextEditor<TextEditorViewModel>.HighlightCurrentLineProperty)
			{
				highlight = editor.HighlightCurrentLine;
			}
			else if (e.Property == TextEditor<TextEditorViewModel>.IsReadOnlyProperty)
			{
				readOnly = editor.IsReadOnly;
			}
			else if (e.Property == TextEditor<TextEditorViewModel>.ShowLineNumbersProperty)
			{
				lineNumbers = editor.ShowLineNumbers;
			}
			else if (e.Property == TextEditor<TextEditorViewModel>.WordWrapProperty)
			{
				wordWrap = editor.WordWrap;
			}
		};

		editor.ViewModel.HighlightCurrentLine = false;
		editor.ViewModel.IsReadOnly = true;
		editor.ViewModel.ShowLineNumbers = false;
		editor.ViewModel.WordWrap = true;

		AreEqual(false, highlight);
		AreEqual(true, readOnly);
		AreEqual(false, lineNumbers);
		AreEqual(true, wordWrap);
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

	private static (Window Window, Terminal Terminal) ShowTerminal(bool autoScroll)
	{
		var terminal = new Terminal
		{
			AutoScroll = autoScroll,
			Width = 400,
			Height = 200
		};
		terminal.ViewModel.Load(string.Join("\r\n", Enumerable.Repeat("line of terminal output", 80)));
		var window = new Window
		{
			Width = 480,
			Height = 320,
			Content = terminal
		};
		window.Show();
		window.UpdateLayout();
		RunUiJobs();
		return (window, terminal);
	}

	#endregion
}