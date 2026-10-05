#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Selection;

[TestClass]
public class SelectionModelTestsMultiple : ScopedTestBase
{
	#region Methods

	private static SelectionModel<string> CreateTarget(bool createData = true)
	{
		var result = new SelectionModel<string> { SingleSelect = false };

		if (createData)
		{
			result.Source = new OldPresentationList<string>
			{
				"foo",
				"bar",
				"baz",
				"qux",
				"quux",
				"corge",
				"grault",
				"garply",
				"waldo",
				"fred",
				"plugh",
				"xyzzy",
				"thud"
			};
		}

		return result;
	}

	#endregion

	#region Classes

	[TestClass]
	public class AnchorIndex : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DeselectDoesntClearAnchorIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.Select(0);
			target.Select(1);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.AnchorIndex))
				{
					++raised;
				}
			};

			target.Deselect(1);

			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void SelectRangeDoesntOverwriteAnchorIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.AnchorIndex = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.AnchorIndex))
				{
					++raised;
				}
			};

			target.SelectRange(1, 2);

			CornerstoneTest.AreEqual(0, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void SelectSetsAnchorIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.AnchorIndex))
				{
					++raised;
				}
			};

			target.Select(1);

			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SettingSelectedIndexSetsAnchorIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.AnchorIndex))
				{
					++raised;
				}
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SettingSelectedIndexToMinus1DoesntClearAnchorIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 1;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.AnchorIndex))
				{
					++raised;
				}
			};

			target.SelectedIndex = -1;

			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, raised);
		}

		#endregion
	}

	[TestClass]
	public class BatchUpdate : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CorrectlyBatchesClearSelect()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(2, 3);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 3 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "qux" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.Clear();
				target.Select(2);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesClearSelectedIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(2, 3);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 3 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "qux" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.Clear();
				target.SelectedIndex = 2;
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesDeselectSelect()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(2, 8);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 2, 3 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.Deselect(2);
				target.Deselect(3);
				target.Deselect(4);
				target.Select(4);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesDeselectSelectRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(2, 8);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 2, 3 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.DeselectRange(2, 6);
				target.SelectRange(4, 8);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesSelectDeselect()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 2, 3 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux" }, e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.Select(2);
				target.Select(3);
				target.Select(4);
				target.Deselect(4);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesSelectDeselectRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 2, 3 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux" }, e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.SelectRange(2, 6);
				target.DeselectRange(4, 8);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesSelectRanges()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 2, 3, 5, 6 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux", "corge", "grault" }, e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.SelectRange(2, 3);
				target.SelectRange(5, 6);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesSelects()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 2, 3 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux" }, e.SelectedItems);
				++raised;
			};

			using (target.BatchUpdate())
			{
				target.Select(2);
				target.Select(3);
			}

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class Clear : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ClearRaisesSelectionChanged()
		{
			var target = CreateTarget();
			var raised = 0;

			target.Select(1);
			target.Select(2);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1, 2 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar", "baz" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.Clear();

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class CollectionChanges : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void AddingItemAfterSelectedDoesntRaiseEvents()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var raised = 0;

			target.SelectedIndex = 1;

			target.PropertyChanged += (s, e) => ++raised;
			target.SelectionChanged += (s, e) => ++raised;
			target.IndexesChanged += (s, e) => ++raised;

			data.Insert(2, "new");

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void AddingItemAtBeginningOfSelectedRangeUpdatesIndexes()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var indexesChangedraised = 0;

			target.SelectRange(4, 8);

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;

			target.IndexesChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(4, e.StartIndex);
				CornerstoneTest.AreEqual(2, e.Delta);
				++indexesChangedraised;
			};

			data.InsertRange(4, new[] { "frank", "tank" });

			CornerstoneTest.AreEqual(6, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 6, 7, 8, 9, 10 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("quux", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "quux", "corge", "grault", "garply", "waldo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(6, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, indexesChangedraised);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
		}

		[PresentationTestMethod]
		public void AddingItemAtEndOfSelectedRangeUpdatesIndexes()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var indexesChangedraised = 0;

			target.SelectRange(4, 8);

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;

			target.IndexesChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(8, e.StartIndex);
				CornerstoneTest.AreEqual(2, e.Delta);
				++indexesChangedraised;
			};

			data.InsertRange(8, new[] { "frank", "tank" });

			CornerstoneTest.AreEqual(4, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 4, 5, 6, 7, 10 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("quux", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "quux", "corge", "grault", "garply", "waldo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(4, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, indexesChangedraised);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
		}

		[PresentationTestMethod]
		public void AddingItemBeforeSelectedItemUpdatesIndexes()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var indexesChangedraised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;

			target.IndexesChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(0, e.StartIndex);
				CornerstoneTest.AreEqual(1, e.Delta);
				++indexesChangedraised;
			};

			data.Insert(0, "new");

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(2, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, indexesChangedraised);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
		}

		[PresentationTestMethod]
		public void AddingItemInMiddleOfSelectedRangeUpdatesIndexes()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var indexesChangedraised = 0;

			target.SelectRange(4, 8);

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;

			target.IndexesChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(6, e.StartIndex);
				CornerstoneTest.AreEqual(2, e.Delta);
				++indexesChangedraised;
			};

			data.InsertRange(6, new[] { "frank", "tank" });

			CornerstoneTest.AreEqual(4, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 4, 5, 8, 9, 10 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("quux", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "quux", "corge", "grault", "garply", "waldo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(4, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, indexesChangedraised);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
		}

		[PresentationTestMethod]
		public void HandlesSelectionMadeInCollectionChanged()
		{
			// Tests the following scenario:
			//
			// - Items changes from empty to having 2 items
			// - ViewModel auto-selects range 0..1 in CollectionChanged
			// - SelectionModel receives CollectionChanged
			// - And so adjusts the selected item from 0..1 to 2..4, which is past the end of
			//   the items.
			//
			// There's not much we can do about this situation because the order in which
			// CollectionChanged handlers are called can't be known (the problem also exists with
			// WPF). The best we can do is not select an invalid index.
			var target = CreateTarget(false);
			var data = new OldPresentationList<string>();

			data.CollectionChanged += (s, e) => { target.SelectRange(0, 1); };

			target.Source = data;
			data.AddRange(new[] { "foo", "bar" });

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0, 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("foo", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "foo", "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, target.AnchorIndex);
		}

		[PresentationTestMethod]
		public void MovingSelectedItemUpdatesState()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;
			var selectedItemRaised = 0;
			var indexesChangedRaised = 0;

			target.Source = data;
			target.SelectRange(1, 4);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}

				if (e.PropertyName == nameof(target.SelectedItem))
				{
					++selectedItemRaised;
				}
			};

			target.IndexesChanged += (s, e) => ++indexesChangedRaised;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.Move(1, 0);

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2, 3, 4 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("baz", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "baz", "qux", "quux" }, target.SelectedItems);
			CornerstoneTest.AreEqual(2, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
			CornerstoneTest.AreEqual(1, selectedItemRaised);
			CornerstoneTest.AreEqual(0, indexesChangedRaised);
		}

		[PresentationTestMethod]
		public void RemovingItemAfterSelectedDoesntRaiseEvents()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var raised = 0;

			target.SelectedIndex = 1;

			target.PropertyChanged += (s, e) => ++raised;
			target.SelectionChanged += (s, e) => ++raised;
			target.IndexesChanged += (s, e) => ++raised;

			data.RemoveAt(2);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void RemovingItemBeforeSelectedItemUpdatesIndexes()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var indexesChangedraised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;

			target.IndexesChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(0, e.StartIndex);
				CornerstoneTest.AreEqual(-1, e.Delta);
				++indexesChangedraised;
			};

			data.RemoveAt(0);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, indexesChangedraised);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
		}

		[PresentationTestMethod]
		public void RemovingPartialSelectedRangeRaisesEvents1()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;

			target.Source = data;
			target.SelectRange(4, 8);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "quux", "corge", "grault" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.RemoveRange(0, 7);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0, 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("garply", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "garply", "waldo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
		}

		[PresentationTestMethod]
		public void RemovingPartialSelectedRangeRaisesEvents2()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;

			target.Source = data;
			target.SelectRange(4, 8);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "garply", "waldo" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.RemoveRange(7, 3);

			CornerstoneTest.AreEqual(4, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 4, 5, 6 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("quux", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "quux", "corge", "grault" }, target.SelectedItems);
			CornerstoneTest.AreEqual(4, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(0, selectedIndexRaised);
		}

		[PresentationTestMethod]
		public void RemovingPartialSelectedRangeRaisesEvents3()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;

			target.Source = data;
			target.SelectRange(4, 8);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "corge", "grault", "garply" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.RemoveRange(5, 3);

			CornerstoneTest.AreEqual(4, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 4, 5 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("quux", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "quux", "waldo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(4, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(0, selectedIndexRaised);
		}

		[PresentationTestMethod]
		public void RemovingSelectedItemUpdatesState()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;

			target.Source = data;
			target.Select(1);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.RemoveAt(1);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(-1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
		}

		[PresentationTestMethod]
		public void RemovingSelectedRangeRaisesEvents()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;

			target.Source = data;
			target.SelectRange(4, 8);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "quux", "corge", "grault", "garply", "waldo" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.RemoveRange(4, 5);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(-1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
		}

		[PresentationTestMethod]
		public void ReplacingSelectedItemUpdatesState()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;
			var selectedItemRaised = 0;
			var indexesChangedRaised = 0;

			target.Source = data;
			target.SelectRange(1, 4);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}

				if (e.PropertyName == nameof(target.SelectedItem))
				{
					++selectedItemRaised;
				}
			};

			target.IndexesChanged += (s, e) => ++indexesChangedRaised;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data[1] = "new";

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2, 3, 4 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("baz", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "baz", "qux", "quux" }, target.SelectedItems);
			CornerstoneTest.AreEqual(2, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
			CornerstoneTest.AreEqual(1, selectedItemRaised);
			CornerstoneTest.AreEqual(0, indexesChangedRaised);
		}

		[PresentationTestMethod]
		public void ResettingSourceUpdatesState()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;
			var resetRaised = 0;

			target.Source = data;
			target.Select(1);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;
			target.SourceReset += (s, e) => ++resetRaised;

			data.Clear();

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(-1, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, resetRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
		}

		#endregion
	}

	[TestClass]
	public class Deselect : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DeselectClearsSelectedItem()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;
			target.Select(1);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.Deselect(1);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("foo", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "foo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void DeselectUpdatesSelectedItemToFirstSelectedItem()
		{
			var target = CreateTarget();

			target.SelectRange(3, 5);
			target.Deselect(3);

			CornerstoneTest.AreEqual(4, target.SelectedIndex);
		}

		#endregion
	}

	[TestClass]
	public class DeselectRange : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DeselectRangeClearsIdenticalRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(1, 2);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1, 2 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar", "baz" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.DeselectRange(1, 2);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void DeselectRangeClearsIntersectingRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(1, 2);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.DeselectRange(0, 1);

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("baz", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void DeselectRangeDoesNothingForNonintersectingRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;
			target.SelectionChanged += (s, e) => ++raised;
			target.DeselectRange(1, 2);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("foo", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "foo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		#endregion
	}

	[TestClass]
	public class LostSelection : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void LostSelectionCalledOnClear()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 0 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "foo" }, e.SelectedItems);
				++raised;
			};

			target.LostSelection += (s, e) => { target.Select(0); };

			target.Clear();

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void LostSelectionCalledWhenSelectionRemoved()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var raised = 0;

			target.SelectRange(1, 3);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar", "baz", "qux" }, e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 0 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "quux" }, e.SelectedItems);
				++raised;
			};

			target.LostSelection += (s, e) => { target.Select(0); };

			data.RemoveRange(0, 4);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class NoSource : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CanSelectMultipleItemsBeforeSourceAssigned()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				var index = raised switch
				{
					0 => 5,
					1 => 10,
					2 => 100,
					_ => throw new NotSupportedException()
				};

				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { index }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new string[] { null }, e.SelectedItems);
				++raised;
			};

			target.SelectedIndex = 5;
			target.Select(10);
			target.Select(100);

			CornerstoneTest.AreEqual(5, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 5, 10, 100 }, target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.AreEqual(new string[] { null, null, null }, target.SelectedItems);
			CornerstoneTest.AreEqual(3, raised);
		}

		[PresentationTestMethod]
		public void ChangingSourceToNullRaisesSelectedItemsPropertyChanged()
		{
			var target = CreateTarget();
			var selectedItemRaised = 0;
			var selectedItemsRaised = 0;

			target.Select(1);
			target.Select(2);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedItem))
				{
					++selectedItemRaised;
				}
				else if (e.PropertyName == nameof(target.SelectedItems))
				{
					++selectedItemsRaised;
				}
			};

			target.Source = null;

			CornerstoneTest.AreEqual(1, selectedItemRaised);
			CornerstoneTest.AreEqual(1, selectedItemsRaised);
		}

		[PresentationTestMethod]
		public void InitializingSourceCoercesSelectedIndex()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectedIndex = 100;
			target.Select(2);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++raised;
				}
			};

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("baz", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void InitializingSourceDoesntRaiseSelectionChangedIfSelectionValid()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.Select(1);
			target.Select(2);

			target.SelectionChanged += (s, e) => { ++raised; };

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void InitializingSourceRaisesSelectedItemsPropertyChanged()
		{
			var target = CreateTarget(false);
			var selectedItemRaised = 0;
			var selectedItemsRaised = 0;

			target.Select(1);
			target.Select(2);

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedItem))
				{
					++selectedItemRaised;
				}
				else if (e.PropertyName == nameof(target.SelectedItems))
				{
					++selectedItemsRaised;
				}
			};

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, selectedItemRaised);
			CornerstoneTest.AreEqual(1, selectedItemsRaised);
		}

		[PresentationTestMethod]
		public void InitializingSourceRespectsRangeSourceItemOrder()
		{
			var target = CreateTarget(false);

			target.SelectRange(2, 2);
			target.SelectedItem = "bar";

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
		}

		[PresentationTestMethod]
		public void InitializingSourceRespectsSourceItemRangeOrder()
		{
			var target = CreateTarget(false);

			target.SelectedItem = "baz";
			target.SelectRange(1, 1);

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
		}

		[PresentationTestMethod]
		public void InitializingSourceRetainsValidSelectionAndRemovesInvalid()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectedIndex = 1;
			target.Select(2);
			target.Select(10);
			target.Select(100);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 10, 100 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new string[] { null, null }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1, 2 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class Select : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void SelectAddsToSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 1 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.SelectedItems);
				++raised;
			};

			target.Select(1);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0, 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("foo", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "foo", "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SelectSetsSelectedIndexIfPreviouslyUnset()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++raised;
				}
			};

			target.Select(1);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SelectWithInvalidIndexDoesNothing()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

			target.PropertyChanged += (s, e) => ++raised;
			target.SelectionChanged += (s, e) => ++raised;

			target.Select(15);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("foo", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "foo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void SelectingAlreadySelectedItemDoesntRaiseSelectionChanged()
		{
			var target = CreateTarget();
			var raised = 0;

			target.Select(2);
			target.SelectionChanged += (s, e) => ++raised;
			target.Select(2);

			CornerstoneTest.AreEqual(0, raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectRange : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void SelectRangeDoesNothingForNonIntersectingRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) => ++raised;

			target.SelectRange(18, 30);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.AreEqual(-1, target.AnchorIndex);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void SelectRangeIgnoresOutOfBoundsItems()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 11, 12 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "xyzzy", "thud" }, e.SelectedItems);
				++raised;
			};

			target.SelectRange(11, 20);

			CornerstoneTest.AreEqual(11, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 11, 12 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("xyzzy", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "xyzzy", "thud" }, target.SelectedItems);
			CornerstoneTest.AreEqual(11, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SelectRangeSelectsItems()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 1, 2 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar", "baz" }, e.SelectedItems);
				++raised;
			};

			target.SelectRange(1, 2);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1, 2 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectedIndex : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void NegativeSelectedIndexIsCoercedToMinus1()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) => ++raised;

			target.SelectedIndex = -5;

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void PropertyChangedIsRaised()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++raised;
				}
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SelectedIndexLargerThanSourceClearsSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.SelectedIndex = 15;

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SettingSelectedIndexClearsOldSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 0 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "foo" }, e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 1 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.SelectedItems);
				++raised;
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectedIndexes : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertyChangedIsRaisedWhenSelectedIndexChanges()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndexes))
				{
					++raised;
				}
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectedItem : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertyChangedIsRaisedWhenSelectedIndexChanges()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedItem))
				{
					++raised;
				}
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectedItems : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertyChangedIsRaisedWhenSelectedIndexChanges()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedItems))
				{
					++raised;
				}
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SingleSelect : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ConvertingToSingleSelectionRemovesMultipleSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectRange(1, 3);

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 2, 3 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz", "qux" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.SingleSelect = true;

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void RaisesPropertyChanged()
		{
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SingleSelect))
				{
					++raised;
				}
			};

			target.SingleSelect = true;

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SourceReset : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CanRestoreSelectionInSourceResetEvent()
		{
			var data = new ResettingList<string> { "foo", "bar", "baz" };
			var target = CreateTarget(false);
			var sourceResetRaised = 0;
			var selectionChangedRaised = 0;

			target.Source = data;
			target.SelectedIndex = 1;

			target.SourceReset += (s, e) =>
			{
				target.SelectedIndex = data.IndexOf("bar");
				++sourceResetRaised;
			};

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 3 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.SelectedItems);
				++selectionChangedRaised;
			};

			data.Reset(new[] { "qux", "foo", "quux", "bar", "baz" });

			CornerstoneTest.AreEqual(3, target.SelectedIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, sourceResetRaised);
		}

		#endregion
	}

	private class ResettingList<T> : List<T>, INotifyCollectionChanged
	{
		#region Methods

		public void Reset(IEnumerable<T> items = null)
		{
			if (items != null)
			{
				Clear();
				AddRange(items);
			}

			CollectionChanged?.Invoke(
				this,
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion
	}

	#endregion
}