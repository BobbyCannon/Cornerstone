#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Completion;
using Cornerstone.Presentation.Controls.Text.Input;
using Cornerstone.Presentation.Controls.Text.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class CompletionManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ApplyDirectoryRetriggersCompletionForNextSegment()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"C:\\\"");
		model.Caret.Move(model.ToString().LastIndexOf('\\') + 1);
		model.CompletionManager.Source = new DirectoryThenChildSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		IsTrue(model.CompletionManager.ApplySelected());
		AreEqual("Get-Item -Path \"C:\\AMD\\\"", model.ToString());
		AreEqual(model.ToString().Length - 1, model.Caret.Offset);
		IsTrue(model.CompletionManager.IsOpen);
		AreEqual("Drivers", model.CompletionManager.VisibleItems[0].DisplayText);
	}

	[TestMethod]
	public void ApplyFileDoesNotRetriggerCompletion()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"C:\\\"");
		model.Caret.Move(model.ToString().LastIndexOf('\\') + 1);
		model.CompletionManager.Source = new QuotedPathMissingCloserSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.ApplySelected());
		AreEqual("Get-Item -Path \"C:\\AMD\"", model.ToString());
		IsFalse(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void ApplySelectedDoesNotDuplicateClosingQuoteOnPath()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"C:\\\"");
		model.Caret.Move(model.ToString().LastIndexOf('\\') + 1);
		model.CompletionManager.Source = new QuotedPathMissingCloserSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		IsTrue(model.CompletionManager.ApplySelected());
		AreEqual("Get-Item -Path \"C:\\AMD\"", model.ToString());
	}

	[TestMethod]
	public void ApplySelectedReplacesRangeAndMovesCaret()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new PrefixReplaceSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		model.CompletionManager.ApplySelected();
		AreEqual("Get-Process", model.ToString());
		AreEqual(11, model.Caret.Offset);
		IsFalse(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void ApplySelectedRestoresWhenInsertCannotApply()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new PrefixReplaceSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);
		model.ReadOnlySectionProvider = new RejectAllEditsProvider();
		IsFalse(model.CompletionManager.ApplySelected());
		AreEqual("Get-", model.ToString());
		IsFalse(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void BackgroundQueryDoesNotBlockThenOpens()
	{
		var started = new ManualResetEventSlim(false);
		var release = new ManualResetEventSlim(false);
		var done = new ManualResetEventSlim(false);
		var model = new TextEditorViewModel();
		model.CompletionManager.Source = new BlockingBackgroundSource(started, release);
		model.CompletionManager.PropertyChanged += (_, e) =>
		{
			if ((e.PropertyName == nameof(CompletionManager.IsQuerying))
				&& !model.CompletionManager.IsQuerying)
			{
				done.Set();
			}
		};

		model.CompletionManager.RequestCompletions();
		IsFalse(model.CompletionManager.IsOpen);
		IsTrue(model.CompletionManager.IsQuerying);
		IsTrue(started.Wait(2000));
		release.Set();
		IsTrue(done.Wait(2000));
		IsTrue(model.CompletionManager.IsOpen);
		AreEqual("Get-Process", model.CompletionManager.VisibleItems[0].DisplayText);
	}

	[TestMethod]
	public void BackgroundQueryDropsStaleResults()
	{
		var firstStarted = new ManualResetEventSlim(false);
		var firstRelease = new ManualResetEventSlim(false);
		var secondStarted = new ManualResetEventSlim(false);
		var done = new ManualResetEventSlim(false);
		var model = new TextEditorViewModel();
		var first = new BlockingBackgroundSource(firstStarted, firstRelease, "Old");
		var second = new ImmediateBackgroundSource(secondStarted, "New");
		model.CompletionManager.Source = first;
		model.CompletionManager.PropertyChanged += (_, e) =>
		{
			if ((e.PropertyName == nameof(CompletionManager.IsQuerying))
				&& !model.CompletionManager.IsQuerying
				&& model.CompletionManager.IsOpen)
			{
				done.Set();
			}
		};

		model.CompletionManager.RequestCompletions();
		IsTrue(firstStarted.Wait(2000));
		model.CompletionManager.Source = second;
		model.CompletionManager.RequestCompletions();
		IsTrue(secondStarted.Wait(2000));
		firstRelease.Set();
		IsTrue(done.Wait(2000));
		AreEqual("New", model.CompletionManager.VisibleItems[0].DisplayText);
	}

	[TestMethod]
	public void BackgroundQueryKeepsTextTypedWhileQuerying()
	{
		var started = new ManualResetEventSlim(false);
		var release = new ManualResetEventSlim(false);
		var done = new ManualResetEventSlim(false);
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new BlockingBackgroundSource(started, release);
		model.CompletionManager.PropertyChanged += (_, e) =>
		{
			if ((e.PropertyName == nameof(CompletionManager.IsQuerying))
				&& !model.CompletionManager.IsQuerying)
			{
				done.Set();
			}
		};

		model.CompletionManager.RequestCompletions();
		IsTrue(started.Wait(2000));
		model.ProcessTextInput("I");
		model.ProcessTextInput("tem");
		AreEqual("Get-Item", model.ToString());
		release.Set();
		IsTrue(done.Wait(2000));
		AreEqual("Get-Item", model.ToString());
		AreEqual(8, model.Caret.Offset);
	}

	[TestMethod]
	public void BackspaceAfterTriggerKeepsCompletionUntilTriggerRemoved()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new MinusTriggerSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		model.Insert(4, "P");
		model.Caret.Move(5);
		model.CompletionManager.UpdateFilterFromDocument();
		IsTrue(model.CompletionManager.IsOpen);

		model.Delete(model.Caret.Offset, false);
		model.CompletionManager.UpdateFilterFromDocument();
		IsTrue(model.CompletionManager.IsOpen);
		AreEqual("Get-", model.ToString());

		model.Delete(model.Caret.Offset, false);
		model.CompletionManager.UpdateFilterFromDocument();
		IsFalse(model.CompletionManager.IsOpen);
		AreEqual("Get", model.ToString());
	}

	[TestMethod]
	public void BackspaceBeforeReplaceStartClosesCompletion()
	{
		var model = new TextEditorViewModel();
		model.Load(" Get-P");
		model.Caret.Move(6);
		model.CompletionManager.Source = new OffsetReplaceSource(1);
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);
		AreEqual(1, model.CompletionManager.ReplaceStart);

		model.Delete(model.Caret.Offset, false);
		model.CompletionManager.UpdateFilterFromDocument();
		IsFalse(model.CompletionManager.IsOpen);
		AreEqual(" Get-", model.ToString());
	}

	[TestMethod]
	public void BackspaceOfTriggerCharacterClosesCompletion()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new MinusTriggerSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		model.Delete(model.Caret.Offset, false);
		model.CompletionManager.UpdateFilterFromDocument();
		IsFalse(model.CompletionManager.IsOpen);
		AreEqual("Get", model.ToString());
	}

	[TestMethod]
	public void BackspaceOverDirectorySeparatorAfterRetriggerClosesCompletion()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"C:\\\"");
		model.Caret.Move(model.ToString().LastIndexOf('\\') + 1);
		model.CompletionManager.Source = new DirectoryThenChildSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.ApplySelected());
		AreEqual("Get-Item -Path \"C:\\AMD\\\"", model.ToString());
		IsTrue(model.CompletionManager.IsOpen);

		model.Delete(model.Caret.Offset, false);
		model.CompletionManager.UpdateFilterFromDocument();
		IsFalse(model.CompletionManager.IsOpen);
		AreEqual("Get-Item -Path \"C:\\AMD\"", model.ToString());
	}

	[TestMethod]
	public void CaretMovedBeforeReplaceStartClosesCompletion()
	{
		var model = new TextEditorViewModel();
		model.Load(" Get-P");
		model.Caret.Move(6);
		model.CompletionManager.Source = new OffsetReplaceSource(1);
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		model.Caret.Move(0);
		model.CompletionManager.UpdateFilterFromDocument();
		IsFalse(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void EscapeClosesSession()
	{
		var model = new TextEditorViewModel();
		model.CompletionManager.Source = new StaticCompletionSource(
			[
				new CompletionItem("One", "One"),
				new CompletionItem("Two", "Two")
			],
			new CompletionTrigger(Key.Space, KeyModifiers.Control, true));

		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		var args = new KeyEventArgs { Key = Key.Escape };
		IsTrue(model.CompletionManager.TryHandleKey(args));
		IsFalse(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void FilterHidesNonMatchingItems()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-P");
		model.Caret.Move(5);
		model.CompletionManager.Source = new PrefixReplaceSource();
		model.CompletionManager.RequestCompletions();

		IsTrue(model.CompletionManager.IsOpen);
		AreEqual(1, model.CompletionManager.VisibleItems.Count);
		AreEqual("Get-Process", model.CompletionManager.VisibleItems[0].DisplayText);
	}

	[TestMethod]
	public void FilterIgnoresTextAfterCaretInReplaceRange()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"c:\\\"");
		model.Caret.Move(model.ToString().Length - 1);
		model.CompletionManager.Source = new QuotedPathSource();
		model.CompletionManager.RequestCompletions();

		IsTrue(model.CompletionManager.IsOpen);
		AreEqual(1, model.CompletionManager.VisibleItems.Count);
		AreEqual("AMD", model.CompletionManager.VisibleItems[0].DisplayText);
		AreEqual("\"C:\\AMD\"", model.CompletionManager.VisibleItems[0].CompletionText);
		AreEqual(model.ToString().Length, model.CompletionManager.ReplaceStart + model.CompletionManager.ReplaceLength);
	}

	[TestMethod]
	public void InsertedBackslashStartsCompletionWithoutKeyTrigger()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"C:");
		model.Caret.Move(model.DocumentLength);
		model.CompletionManager.Source = new BackslashTextTriggerSource();

		model.ProcessTextInput("\\");
		AreEqual("Get-Item -Path \"C:\\", model.ToString());
		IsTrue(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void NonSilentTriggerDoesNotConsumeKeyDown()
	{
		var model = new TextEditorViewModel();
		model.Load("Console");
		model.Caret.Move(7);
		model.CompletionManager.Source = new PeriodTriggerSource();

		var args = new KeyEventArgs { Key = Key.OemPeriod, KeySymbol = "." };
		IsFalse(model.CompletionManager.TryHandleKey(args));
		AreEqual("Console", model.ToString());
		IsFalse(model.CompletionManager.IsOpen);

		model.ProcessTextInput(".");
		AreEqual("Console.", model.ToString());
		IsTrue(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void NonSilentTriggerWithoutKeySymbolStillDoesNotConsumeKeyDown()
	{
		var model = new TextEditorViewModel();
		model.Load("Console");
		model.Caret.Move(7);
		model.CompletionManager.Source = new PeriodTriggerSource();

		var args = new KeyEventArgs { Key = Key.OemPeriod };
		IsFalse(model.CompletionManager.TryHandleKey(args));
		AreEqual("Console", model.ToString());

		model.ProcessTextInput(".");
		AreEqual("Console.", model.ToString());
	}

	[TestMethod]
	public void SilentCtrlSpaceConsumesKeyWithoutInserting()
	{
		var model = new TextEditorViewModel();
		model.Load("Console");
		model.Caret.Move(7);
		model.CompletionManager.Source = new StaticCompletionSource(
			[new CompletionItem("WriteLine", "WriteLine"), new CompletionItem("Write", "Write")],
			new CompletionTrigger(Key.Space, KeyModifiers.Control, true));

		var args = new KeyEventArgs { Key = Key.Space, KeyModifiers = KeyModifiers.Control, KeySymbol = " " };
		IsTrue(model.CompletionManager.TryHandleKey(args));
		AreEqual("Console", model.ToString());
		IsTrue(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void SilentTabAppliesSingleItem()
	{
		var model = new TextEditorViewModel();
		model.Load("g");
		model.Caret.Move(1);
		model.CompletionManager.Source = new SingleItemReplaceSource();

		var args = new KeyEventArgs { Key = Key.Tab, KeyModifiers = KeyModifiers.None };
		IsTrue(model.CompletionManager.TryHandleKey(args));
		AreEqual("Get-Process", model.ToString());
		IsFalse(model.CompletionManager.IsOpen);
	}

	[TestMethod]
	public void ThrowingCompletionSourceDoesNotCrashQuery()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new ThrowingCompletionSource();
		model.CompletionManager.RequestCompletions();
		IsFalse(model.CompletionManager.IsOpen);
		AreEqual("Get-", model.ToString());
	}

	[TestMethod]
	public void ThrowingTriggerDoesNotCrashKeyOrTextInput()
	{
		var model = new TextEditorViewModel();
		model.Load("x");
		model.Caret.Move(1);
		model.CompletionManager.Source = new ThrowingCompletionSource();
		IsFalse(model.CompletionManager.TryHandleKey(new KeyEventArgs { Key = Key.Tab }));
		model.ProcessTextInput(".");
		AreEqual("x.", model.ToString());
	}

	[TestMethod]
	public void UnterminatedQuotedPathKeepsCompletionOpen()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-Item -Path \"C:\\");
		model.Caret.Move(model.DocumentLength);
		model.CompletionManager.Source = new UnterminatedQuotedPathSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);
		AreEqual(1, model.CompletionManager.VisibleItems.Count);
		AreEqual("AMD", model.CompletionManager.VisibleItems[0].DisplayText);
	}

	#endregion

	#region Classes

	private sealed class BackslashTextTriggerSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool ShouldTriggerText(string text, out bool silent)
		{
			silent = false;
			return text == "\\";
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items = [new CompletionItem("AMD", "\"C:\\AMD\\\"", "", -1, 0, true)];
			replaceStart = context.Text.LastIndexOf('"');
			replaceLength = Math.Max(0, context.CaretOffset - replaceStart);
			return true;
		}

		#endregion
	}

	private sealed class BlockingBackgroundSource : ICompletionSource
	{
		#region Fields

		private readonly string _name;
		private readonly ManualResetEventSlim _release;
		private readonly ManualResetEventSlim _started;

		#endregion

		#region Constructors

		public BlockingBackgroundSource(
			ManualResetEventSlim started,
			ManualResetEventSlim release,
			string name = "Get-Process")
		{
			_started = started;
			_release = release;
			_name = name;
		}

		#endregion

		#region Properties

		public bool QueryOnBackgroundThread => true;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = true;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			_started.Set();
			_release.Wait();
			items = [new CompletionItem(_name, _name)];
			replaceStart = 0;
			replaceLength = 0;
			return true;
		}

		#endregion
	}

	private sealed class DirectoryThenChildSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			var text = context.Text ?? string.Empty;
			var quote = text.LastIndexOf('"');
			var open = text.LastIndexOf('"', quote - 1);
			if (text.Contains("AMD"))
			{
				items = [new CompletionItem("Drivers", "\"C:\\AMD\\Drivers\\\"", "", -1, 0, true)];
				replaceStart = context.CaretOffset;
				replaceLength = 0;
				return true;
			}

			items = [new CompletionItem("AMD", "\"C:\\AMD\\\"", "", -1, 0, true)];
			replaceStart = open;
			replaceLength = quote - open;
			return true;
		}

		#endregion
	}

	private sealed class ImmediateBackgroundSource : ICompletionSource
	{
		#region Fields

		private readonly string _name;
		private readonly ManualResetEventSlim _started;

		#endregion

		#region Constructors

		public ImmediateBackgroundSource(ManualResetEventSlim started, string name)
		{
			_started = started;
			_name = name;
		}

		#endregion

		#region Properties

		public bool QueryOnBackgroundThread => true;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = true;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			_started.Set();
			items = [new CompletionItem(_name, _name)];
			replaceStart = 0;
			replaceLength = 0;
			return true;
		}

		#endregion
	}

	private sealed class MinusTriggerSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return key == Key.OemMinus;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items =
			[
				new CompletionItem("Get-Process", "Get-Process"),
				new CompletionItem("Get-Service", "Get-Service")
			];
			replaceStart = 0;
			replaceLength = context.CaretOffset;
			return true;
		}

		#endregion
	}

	private sealed class OffsetReplaceSource : ICompletionSource
	{
		#region Fields

		private readonly int _replaceStart;

		#endregion

		#region Constructors

		public OffsetReplaceSource(int replaceStart)
		{
			_replaceStart = replaceStart;
		}

		#endregion

		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items =
			[
				new CompletionItem("Get-Process", "Get-Process"),
				new CompletionItem("Get-Service", "Get-Service")
			];
			replaceStart = _replaceStart;
			replaceLength = context.CaretOffset < _replaceStart ? 0 : context.CaretOffset - _replaceStart;
			return true;
		}

		#endregion
	}

	private sealed class PeriodTriggerSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return key == Key.OemPeriod;
		}

		public bool ShouldTriggerText(string text, out bool silent)
		{
			silent = false;
			return text == ".";
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items = [new CompletionItem("WriteLine", "WriteLine")];
			replaceStart = context.CaretOffset;
			replaceLength = 0;
			return true;
		}

		#endregion
	}

	private sealed class PrefixReplaceSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = key == Key.Tab;
			return key == Key.Tab;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items =
			[
				new CompletionItem("Get-Process", "Get-Process"),
				new CompletionItem("Get-Service", "Get-Service")
			];
			replaceStart = 0;
			replaceLength = context.CaretOffset;
			return true;
		}

		#endregion
	}

	private sealed class QuotedPathMissingCloserSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			var text = context.Text ?? string.Empty;
			var quote = text.LastIndexOf('"');
			var open = text.LastIndexOf('"', quote - 1);
			items = [new CompletionItem("AMD", "\"C:\\AMD\"")];
			replaceStart = open;
			replaceLength = quote - open;
			return true;
		}

		#endregion
	}

	private sealed class QuotedPathSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			var text = context.Text ?? string.Empty;
			var quote = text.LastIndexOf('"');
			var open = text.LastIndexOf('"', quote - 1);
			items = [new CompletionItem("AMD", "\"C:\\AMD\"")];
			replaceStart = open;
			replaceLength = (quote - open) + 1;
			return true;
		}

		#endregion
	}

	private sealed class RejectAllEditsProvider : IReadOnlySectionProvider
	{
		#region Methods

		public bool CanModify(int offset)
		{
			return false;
		}

		public IEnumerable<IRange> GetDeletableSegments(IRange range)
		{
			return [];
		}

		#endregion
	}

	private sealed class SingleItemReplaceSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = key == Key.Tab;
			return key == Key.Tab;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items = [new CompletionItem("Get-Process", "Get-Process")];
			replaceStart = 0;
			replaceLength = context.CaretOffset;
			return true;
		}

		#endregion
	}

	private sealed class ThrowingCompletionSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			throw new InvalidOperationException();
		}

		public bool ShouldTriggerText(string text, out bool silent)
		{
			throw new InvalidOperationException();
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}

	private sealed class UnterminatedQuotedPathSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items = [new CompletionItem("AMD", "\"C:\\AMD\\\"", "", -1, 0, true)];
			replaceStart = context.Text.LastIndexOf('C');
			replaceLength = context.CaretOffset - replaceStart;
			return true;
		}

		#endregion
	}

	#endregion
}