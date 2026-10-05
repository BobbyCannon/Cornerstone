#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Folding;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class FoldingManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ApplyCollapsedFoldsDistinguishesDuplicateHintsByPath()
	{
		var viewModel = CreateRegionDocument("""
											#region Left
											#region Shared
											a
											#endregion
											#endregion
											#region Right
											#region Shared
											b
											#endregion
											#endregion
											""");
		AreEqual(4, viewModel.FoldingManager.AllFoldings.Count);
		viewModel.FoldingManager.AllFoldings[3].IsFolded = true;

		var snapshots = viewModel.FoldingManager.CaptureCollapsedFolds();
		AreEqual("Right/Shared", snapshots[0].Path);

		viewModel.FoldingManager.ExpandAll();
		viewModel.FoldingManager.ApplyCollapsedFolds(snapshots);

		IsFalse(viewModel.FoldingManager.AllFoldings[1].IsFolded);
		IsTrue(viewModel.FoldingManager.AllFoldings[3].IsFolded);
	}

	[TestMethod]
	public void ApplyCollapsedFoldsSkipsStaleOffsetWhenHintDoesNotMatch()
	{
		var viewModel = CreateDocument();
		AddNamedFold(viewModel, 1, 2, "Alpha");
		AddNamedFold(viewModel, 3, 4, "Beta");

		var snapshots = new[]
		{
			new CollapsedFoldSnapshot
			{
				Offset = viewModel.FoldingManager.AllFoldings[0].StartOffset,
				Line = 1,
				Hint = "Beta",
				Path = "Beta"
			}
		};

		viewModel.FoldingManager.ApplyCollapsedFolds(snapshots);

		IsFalse(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsTrue(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void ApplyCollapsedFoldsUsesUniquePathWhenShiftExceedsNearbyWindow()
	{
		var viewModel = CreateRegionDocument("""
											#region Outer
											#region Inner
											body
											#endregion
											#endregion
											""");
		viewModel.FoldingManager.AllFoldings[1].IsFolded = true;
		var snapshots = viewModel.FoldingManager.CaptureCollapsedFolds();
		AreEqual("Outer/Inner", snapshots[0].Path);

		var padding = new string('x', FoldingManager.NearbyRestoreLineWindow + 5);
		var padded = string.Join("\r\n", Enumerable.Repeat(padding, FoldingManager.NearbyRestoreLineWindow + 5))
			+ "\r\n#region Outer\r\n#region Inner\r\nbody\r\n#endregion\r\n#endregion";
		viewModel.Load(padded);
		viewModel.FoldingManager.Strategy = new RegionFoldingStrategy();

		viewModel.FoldingManager.ApplyCollapsedFolds(snapshots);

		AreEqual(2, viewModel.FoldingManager.AllFoldings.Count);
		IsFalse(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsTrue(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void ApplyCollapsedStartOffsetsExpandsUnlistedSections()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.CreateFoldingForLines(1, 2);
		viewModel.FoldingManager.CreateFoldingForLines(3, 4);
		viewModel.FoldingManager.CollapseAll();

		viewModel.FoldingManager.ApplyCollapsedStartOffsets([]);

		IsFalse(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsFalse(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void CaptureAndApplyCollapsedFoldsUsesHintWhenOffsetShiftsNearby()
	{
		var viewModel = CreateDocument();
		AddNamedFold(viewModel, 1, 2, "Alpha");
		AddNamedFold(viewModel, 3, 4, "Beta");
		viewModel.FoldingManager.AllFoldings[1].IsFolded = true;

		var snapshots = viewModel.FoldingManager.CaptureCollapsedFolds();
		AreEqual(1, snapshots.Length);
		AreEqual("Beta", snapshots[0].Hint);

		viewModel.Load("inserted\r\none\r\ntwo\r\nthree\r\nfour");
		AddNamedFold(viewModel, 2, 3, "Alpha");
		AddNamedFold(viewModel, 4, 5, "Beta");

		viewModel.FoldingManager.ApplyCollapsedFolds(snapshots);

		IsFalse(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsTrue(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void CaptureAndApplyCollapsedStartOffsets()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.CreateFoldingForLines(1, 2);
		viewModel.FoldingManager.CreateFoldingForLines(3, 4);
		viewModel.FoldingManager.AllFoldings[0].IsFolded = true;

		var offsets = viewModel.FoldingManager.CaptureCollapsedStartOffsets();
		AreEqual(1, offsets.Length);
		AreEqual(viewModel.FoldingManager.AllFoldings[0].StartOffset, offsets[0]);

		viewModel.FoldingManager.ExpandAll();
		viewModel.FoldingManager.ApplyCollapsedStartOffsets(offsets);

		IsTrue(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsFalse(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void CaretSkipsCollapsedInnerLines()
	{
		var viewModel = CreateDocument();
		var section = viewModel.FoldingManager.CreateFoldingForLines(1, 3);
		section.IsFolded = true;
		viewModel.Lines.Measure(new Size(400, 400), false);

		viewModel.Caret.Move(0);
		viewModel.Caret.Move(viewModel.Lines[1].StartOffset);
		AreEqual(viewModel.Lines[0].EndOffset - viewModel.Lines[0].LineEndingLength, viewModel.Caret.Offset);

		viewModel.Caret.MoveToLineEnd();
		viewModel.Caret.MoveRight();
		AreEqual(viewModel.Lines[2].EndOffset, viewModel.Caret.Offset);
	}

	[TestMethod]
	public void CollapseAllFoldsEverySection()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.CreateFoldingForLines(1, 2);
		viewModel.FoldingManager.CreateFoldingForLines(3, 4);

		viewModel.FoldingManager.CollapseAll();

		IsTrue(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsTrue(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void CollapseLeavesCaretOnHeader()
	{
		var viewModel = CreateDocument();
		var headerOffset = viewModel.Lines[0].StartOffset;
		var section = viewModel.FoldingManager.CreateFoldingForLines(1, 3);
		viewModel.Caret.Move(headerOffset);

		section.IsFolded = true;

		AreEqual(headerOffset, viewModel.Caret.Offset);
	}

	[TestMethod]
	public void CollapseMovesCaretFromInnerLineToHeader()
	{
		var viewModel = CreateDocument();
		var section = viewModel.FoldingManager.CreateFoldingForLines(1, 3);
		viewModel.Caret.Move(viewModel.Lines[1].StartOffset);
		AreEqual(viewModel.Lines[1].StartOffset, viewModel.Caret.Offset);

		section.IsFolded = true;

		AreEqual(viewModel.Lines[0].EndOffset - viewModel.Lines[0].LineEndingLength, viewModel.Caret.Offset);
	}

	[TestMethod]
	public void CollapsedInnerLinesHaveZeroHeight()
	{
		var viewModel = CreateDocument();
		var section = viewModel.FoldingManager.CreateFoldingForLines(1, 3);
		section.IsFolded = true;
		var size = viewModel.Lines.Measure(new Size(400, 400), false);

		AreEqual(20, viewModel.Lines[0].VisualLayout.Height);
		AreEqual(0, viewModel.Lines[1].VisualLayout.Height);
		AreEqual(0, viewModel.Lines[2].VisualLayout.Height);
		AreEqual(20, viewModel.Lines[3].VisualLayout.Height);
		AreEqual(40, size.Height);
	}

	[TestMethod]
	public void ExpandAllUnfoldsEverySection()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.CreateFoldingForLines(1, 2);
		viewModel.FoldingManager.CreateFoldingForLines(3, 4);
		viewModel.FoldingManager.CollapseAll();

		viewModel.FoldingManager.ExpandAll();

		IsFalse(viewModel.FoldingManager.AllFoldings[0].IsFolded);
		IsFalse(viewModel.FoldingManager.AllFoldings[1].IsFolded);
	}

	[TestMethod]
	public void ExpandDoesNotRestoreInnerCaret()
	{
		var viewModel = CreateDocument();
		var section = viewModel.FoldingManager.CreateFoldingForLines(1, 3);
		viewModel.Caret.Move(viewModel.Lines[1].StartOffset);
		section.IsFolded = true;
		var headerEnd = viewModel.Lines[0].EndOffset - viewModel.Lines[0].LineEndingLength;

		section.IsFolded = false;

		AreEqual(headerEnd, viewModel.Caret.Offset);
	}

	[TestMethod]
	public void ExpandRestoresHeight()
	{
		var viewModel = CreateDocument();
		var section = viewModel.FoldingManager.CreateFoldingForLines(1, 3);
		section.IsFolded = true;
		viewModel.Lines.Measure(new Size(400, 400), false);
		section.IsFolded = false;
		var size = viewModel.Lines.Measure(new Size(400, 400), false);

		AreEqual(20, viewModel.Lines[1].VisualLayout.Height);
		AreEqual(80, size.Height);
	}

	[TestMethod]
	public void GetFoldingStartingOnLineFindsHeaderAmongSeveralSections()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.CreateFoldingForLines(1, 2);
		viewModel.FoldingManager.CreateFoldingForLines(3, 4);

		var first = viewModel.FoldingManager.GetFoldingStartingOnLine(viewModel.Lines[0]);
		var second = viewModel.FoldingManager.GetFoldingStartingOnLine(viewModel.Lines[2]);
		var inner = viewModel.FoldingManager.GetFoldingStartingOnLine(viewModel.Lines[1]);

		IsNotNull(first);
		AreEqual(viewModel.Lines[0].StartOffset, first.StartOffset);
		IsNotNull(second);
		AreEqual(viewModel.Lines[2].StartOffset, second.StartOffset);
		IsNull(inner);

		var next = viewModel.FoldingManager.GetNextFolding(viewModel.Lines[1].StartOffset);
		AreEqual(second, next);
	}

	[TestMethod]
	public void InsertBeforeFoldShiftsOffsets()
	{
		var viewModel = CreateDocument();
		var start = viewModel.Lines[1].StartOffset;
		var end = viewModel.Lines[2].EndOffset;
		viewModel.FoldingManager.CreateFolding(start, end);

		viewModel.Insert(0, "Z");

		AreEqual(start + 1, viewModel.FoldingManager.AllFoldings[0].StartOffset);
		AreEqual(end + 1, viewModel.FoldingManager.AllFoldings[0].EndOffset);
	}

	[TestMethod]
	public void LoadSameTextPreservesCollapsedFoldsWhenStrategyIsSet()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.Strategy = new HeaderToLastLineStrategy();
		viewModel.FoldingManager.AllFoldings[0].IsFolded = true;

		viewModel.Load("one\r\ntwo\r\nthree\r\nfour");

		AreEqual(1, viewModel.FoldingManager.AllFoldings.Count);
		IsTrue(viewModel.FoldingManager.AllFoldings[0].IsFolded);
	}

	[TestMethod]
	public void SettableStrategyRefreshesFolds()
	{
		var viewModel = CreateDocument();
		viewModel.FoldingManager.Strategy = new HeaderToLastLineStrategy();

		AreEqual(1, viewModel.FoldingManager.AllFoldings.Count);
		AreEqual(viewModel.Lines[0].StartOffset, viewModel.FoldingManager.AllFoldings[0].StartOffset);
		AreEqual(viewModel.Lines[^1].EndOffset, viewModel.FoldingManager.AllFoldings[0].EndOffset);
	}

	[TestMethod]
	public void UpdateFoldingsPreservesCollapsedState()
	{
		var viewModel = CreateDocument();
		var section = viewModel.FoldingManager.CreateFolding(viewModel.Lines[0].StartOffset, viewModel.Lines[2].EndOffset);
		section.IsFolded = true;

		viewModel.FoldingManager.UpdateFoldings(
		[
			new NewFolding(viewModel.Lines[0].StartOffset, viewModel.Lines[2].EndOffset)
		]);

		AreEqual(1, viewModel.FoldingManager.AllFoldings.Count);
		IsTrue(viewModel.FoldingManager.AllFoldings[0].IsFolded);
	}

	private static void AddNamedFold(TextEditorViewModel viewModel, int startLineNumber, int endLineNumber, string name)
	{
		var startLine = viewModel.Lines[startLineNumber - 1];
		var endLine = viewModel.Lines[endLineNumber - 1];
		var section = viewModel.FoldingManager.CreateFolding(startLine.StartOffset, endLine.EndOffset);
		section.Title = name;
	}

	private static TextEditorViewModel CreateDocument()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		viewModel.Load("one\r\ntwo\r\nthree\r\nfour");
		viewModel.Lines.Measure(new Size(400, 400), false);
		return viewModel;
	}

	private static TextEditorViewModel CreateRegionDocument(string text)
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		viewModel.Load(text.Replace("\n", "\r\n").Replace("\r\r\n", "\r\n"));
		viewModel.FoldingManager.Strategy = new RegionFoldingStrategy();
		viewModel.Lines.Measure(new Size(400, 400), false);
		return viewModel;
	}

	#endregion

	#region Classes

	private sealed class HeaderToLastLineStrategy : IFoldingStrategy
	{
		#region Methods

		public IEnumerable<NewFolding> CreateNewFoldings(TextEditorViewModel viewModel)
		{
			if (viewModel.Lines.Count < 2)
			{
				yield break;
			}

			yield return new NewFolding(viewModel.Lines[0].StartOffset, viewModel.Lines[^1].EndOffset);
		}

		#endregion
	}

	#endregion
}