#region References

using Avalonia.Input;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Completion;
using Cornerstone.Avalonia.Text.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class CompletionManagerTests : CornerstoneUnitTest
{
	#region Methods

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
	public void BackgroundQueryDoesNotBlockThenOpens()
	{
		var started = new System.Threading.ManualResetEventSlim(false);
		var release = new System.Threading.ManualResetEventSlim(false);
		var done = new System.Threading.ManualResetEventSlim(false);
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
		var firstStarted = new System.Threading.ManualResetEventSlim(false);
		var firstRelease = new System.Threading.ManualResetEventSlim(false);
		var secondStarted = new System.Threading.ManualResetEventSlim(false);
		var done = new System.Threading.ManualResetEventSlim(false);
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

	#endregion

	#region Classes

	private sealed class ImmediateBackgroundSource : ICompletionSource
	{
		private readonly string _name;
		private readonly System.Threading.ManualResetEventSlim _started;

		public ImmediateBackgroundSource(System.Threading.ManualResetEventSlim started, string name)
		{
			_started = started;
			_name = name;
		}

		public bool QueryOnBackgroundThread => true;

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = true;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out System.Collections.Generic.IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			_started.Set();
			items = [new CompletionItem(_name, _name)];
			replaceStart = 0;
			replaceLength = 0;
			return true;
		}
	}

	private sealed class BlockingBackgroundSource : ICompletionSource
	{
		private readonly string _name;
		private readonly System.Threading.ManualResetEventSlim _release;
		private readonly System.Threading.ManualResetEventSlim _started;

		public BlockingBackgroundSource(
			System.Threading.ManualResetEventSlim started,
			System.Threading.ManualResetEventSlim release,
			string name = "Get-Process")
		{
			_started = started;
			_release = release;
			_name = name;
		}

		public bool QueryOnBackgroundThread => true;

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = true;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out System.Collections.Generic.IReadOnlyList<CompletionItem> items,
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
	}

	private sealed class SingleItemReplaceSource : ICompletionSource
	{
		public bool QueryOnBackgroundThread => false;

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = key == Key.Tab;
			return key == Key.Tab;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out System.Collections.Generic.IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items = [new CompletionItem("Get-Process", "Get-Process")];
			replaceStart = 0;
			replaceLength = context.CaretOffset;
			return true;
		}
	}

	private sealed class PrefixReplaceSource : ICompletionSource
	{
		public bool QueryOnBackgroundThread => false;

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = key == Key.Tab;
			return key == Key.Tab;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out System.Collections.Generic.IReadOnlyList<CompletionItem> items,
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
	}

	#endregion
}
