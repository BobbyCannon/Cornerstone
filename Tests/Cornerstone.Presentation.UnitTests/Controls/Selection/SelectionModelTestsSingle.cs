#region References

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CollectionChangedEventManager = Cornerstone.Presentation.Controls.Utils.CollectionChangedEventManager;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Selection;

[TestClass]
public class SelectionModelTestsSingle
{
	#region Methods

	private static SelectionModel<string> CreateTarget(bool createData = true)
	{
		var result = new SelectionModel<string> { SingleSelect = true };

		if (createData)
		{
			result.Source = new OldPresentationList<string> { "foo", "bar", "baz" };
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
		public void RaisesPropertyChanged()
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

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SelectSetsAnchorIndex()
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
		public void ChangesDoNotTakeEffectUntilEndUpdateCalled()
		{
			var target = CreateTarget();

			target.BeginBatchUpdate();
			target.Select(0);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);

			target.EndBatchUpdate();

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}

		[PresentationTestMethod]
		public void CorrectlyBatchesClearSelectedIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 2;
			target.SelectionChanged += (s, e) => ++raised;

			using (target.BatchUpdate())
			{
				target.Clear();
				target.SelectedIndex = 2;
			}

			CornerstoneTest.AreEqual(0, raised);
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

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
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
		public void AddingItemBeforeSelectedItemUpdatesIndexes()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var indexesChangedRaised = 0;
			var selectedIndexRaised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) => ++selectionChangedRaised;

			target.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(target.SelectedIndex))
				{
					++selectedIndexRaised;
				}
			};

			target.IndexesChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(0, e.StartIndex);
				CornerstoneTest.AreEqual(1, e.Delta);
				++indexesChangedRaised;
			};

			data.Insert(0, "new");

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(2, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, indexesChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
			CornerstoneTest.AreEqual(0, selectionChangedRaised);
		}

		[PresentationTestMethod]
		public void HandlesSelectionMadeInCollectionChanged()
		{
			// Tests the following scenario:
			//
			// - Items changes from empty to having 1 item
			// - ViewModel auto-selects item 0 in CollectionChanged
			// - SelectionModel receives CollectionChanged
			// - And so adjusts the selected item from 0 to 1, which is past the end of the items.
			//
			// There's not much we can do about this situation because the order in which
			// CollectionChanged handlers are called can't be known (the problem also exists with
			// WPF). The best we can do is not select an invalid index.
			var target = CreateTarget(false);
			var data = new OldPresentationList<string>();

			data.CollectionChanged += (s, e) => { target.Select(0); };

			target.Source = data;
			data.Add("foo");

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 0 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("foo", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "foo" }, target.SelectedItems);
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

			target.Source = data;
			target.Select(1);

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

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data.Move(1, 0);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(-1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
			CornerstoneTest.AreEqual(1, selectedItemRaised);
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
		public void ReplacingSelectedItemUpdatesState()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var selectionChangedRaised = 0;
			var selectedIndexRaised = 0;
			var selectedItemRaised = 0;

			target.Source = data;
			target.Select(1);

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

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++selectionChangedRaised;
			};

			data[1] = "new";

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(-1, target.AnchorIndex);
			CornerstoneTest.AreEqual(1, selectionChangedRaised);
			CornerstoneTest.AreEqual(1, selectedIndexRaised);
			CornerstoneTest.AreEqual(1, selectedItemRaised);
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

		[PresentationTestMethod]
		public void SelectedItemsIndexerIsCorrect()
		{
			// Issue #7974
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual("bar", e.SelectedItems.First());
				CornerstoneTest.AreEqual("bar", e.SelectedItems[0]);
				++raised;
			};

			target.Select(1);
			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class Deselect : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DeselectClearsCurrentSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 0 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "foo" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.Deselect(0);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void DeselectDoesNothingForNonselectedItem()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 1;
			target.SelectionChanged += (s, e) => ++raised;
			target.Deselect(0);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		#endregion
	}

	[TestClass]
	public class DeselectRange : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DeselectRangeClearsCurrentSelectionForIntersectingRange()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 0 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "foo" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.DeselectRange(0, 2);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
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
		public void LostSelectionCalledWhenSelectedItemRemoved()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var raised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 0 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "foo" }, e.SelectedItems);
				++raised;
			};

			target.LostSelection += (s, e) => { target.Select(0); };

			data.RemoveAt(1);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void LostSelectionIsCalledWhenSourceChangedWhileCollectionChangeInProgress()
		{
			// Issue #12733.
			var data1 = new OldPresentationList<string> { "foo1", "bar1", "baz1" };
			var data2 = new OldPresentationList<string> { "foo1", "bar1", "baz1" };
			var target = new DerivedSelectionModel { Source = data1 };
			var raised = 0;

			target.LostSelection += (s, e) =>
			{
				if (target.Source == data2)
				{
					++raised;
				}
			};

			target.UpdateSource(data2);

			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void LostSelectionNotCalledWithOldSourceWhenChangingSource()
		{
			var target = CreateTarget();
			var data = (OldPresentationList<string>) target.Source!;
			var raised = 0;

			target.LostSelection += (s, e) =>
			{
				if (target.Source == data)
				{
					++raised;
				}
			};

			target.Source = null;

			CornerstoneTest.AreEqual(0, raised);
		}

		#endregion

		#region Classes

		private class DerivedSelectionModel : SelectionModel<string>
		{
			#region Methods

			public void UpdateSource(IEnumerable source)
			{
				OnSourceCollectionChangeStarted();
				Source = source;
				OnSourceCollectionChangeFinished();
			}

			#endregion
		}

		#endregion
	}

	[TestClass]
	public class Select : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void SelectClearsOldSelection()
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

			target.Select(1);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void SelectSetsSelectedIndex()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 0;

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

			target.Select(5);

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
		public void SelectRangeThrows()
		{
			var target = CreateTarget();

			Assert.Throws<InvalidOperationException>(() => target.SelectRange(0, 10));
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

			target.SelectedIndex = 5;

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

		[PresentationTestMethod]
		public void SettingSelectedIndexDuringCollectionChangedResultsInCorrectSelection()
		{
			// Issue #4496
			var data = new OldPresentationList<string>();
			var target = CreateTarget();
			var binding = new MockBinding(target, data);

			target.Source = data;

			data.Add("foo");

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}

		#endregion

		#region Classes

		private class MockBinding : ICollectionChangedListener
		{
			#region Fields

			private readonly SelectionModel<string> _target;

			#endregion

			#region Constructors

			public MockBinding(SelectionModel<string> target, OldPresentationList<string> data)
			{
				_target = target;
				CollectionChangedEventManager.Instance.AddListener(data, this);
			}

			#endregion

			#region Methods

			public void Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
			{
				_target.Select(0);
			}

			public void PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
			{
			}

			public void PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
			{
			}

			#endregion
		}

		#endregion
	}

	[TestClass]
	public class SelectedIndexes : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CollectionChangedIsRaisedWhenSelectedIndexChanges()
		{
			var target = CreateTarget();
			var raised = 0;
			var incc = CornerstoneTest.IsAssignableFrom<INotifyCollectionChanged>(target.SelectedIndexes);

			incc.CollectionChanged += (s, e) =>
			{
				// For the moment, for simplicity, we raise a Reset event when the SelectedIndexes
				// collection changes - whatever the change. This can be improved later if necessary.
				CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Reset, e.Action);
				++raised;
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, raised);
		}

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

		[PresentationTestMethod]
		public void SettingSelectedItemToValidItemUpdatesSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 1 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.SelectedItems);
				++raised;
			};

			target.SelectedItem = "bar";

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectedItems : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CollectionChangedIsRaisedWhenSelectedIndexChanges()
		{
			var target = CreateTarget();
			var raised = 0;
			var incc = CornerstoneTest.IsAssignableFrom<INotifyCollectionChanged>(target.SelectedIndexes);

			incc.CollectionChanged += (s, e) =>
			{
				// For the moment, for simplicity, we raise a Reset event when the SelectedItems
				// collection changes - whatever the change. This can be improved later if necessary.
				CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Reset, e.Action);
				++raised;
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, raised);
		}

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
		public void ConvertingToMultipleSelectionPreservesSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) => ++raised;

			target.SingleSelect = false;

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
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

			target.SingleSelect = false;

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class Source : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CanAssignValueTypeCollectionToSelectionModelOfObject()
		{
			var target = (ISelectionModel) new SelectionModel<object>();

			target.Source = new[] { 1, 2, 3 };
		}

		[PresentationTestMethod]
		public void CanChangeSourceInSelectedItemChangeHandler()
		{
			// Issue #11617
			var target = CreateTarget();
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				if ((e.PropertyName == nameof(target.SelectedItem)) && (raised == 0))
				{
					++raised;
					target.Source = new[] { "foo", "baz", "bar" };
				}
			};

			target.SelectedIndex = 1;

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		}

		[PresentationTestMethod]
		public void CanSelectIndexBeforeSourceAssigned()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 5 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new string[] { null }, e.SelectedItems);
				++raised;
			};

			target.SelectedIndex = 5;

			CornerstoneTest.AreEqual(5, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 5 }, target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.AreEqual(new string[] { null }, target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void CanSelectItemBeforeSourceAssigned()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectionChanged += (s, e) => ++raised;
			target.SelectedItem = "bar";

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void ChangingSourceToNonNullFirstClearsOldSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 2;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 2 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz" }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.Source = new[] { "qux", "quux", "corge" };

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void ChangingSourceToNullDoesntClearSelection()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 2;

			target.SelectionChanged += (s, e) => ++raised;

			target.Source = null;

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 2 }, target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.AreEqual(new string[] { null }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void ChangingSourceToNullRaisesSelectedItemsPropertyChanged()
		{
			var target = CreateTarget();
			var selectedItemRaised = 0;
			var selectedItemsRaised = 0;

			target.Select(1);

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
		public void InitializingSourceRaisesSelectedItemsPropertyChanged()
		{
			var target = CreateTarget(false);
			var selectedItemRaised = 0;
			var selectedItemsRaised = 0;

			target.Select(1);

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
		public void InitializingSourceRemovesInvalidIndexSelection()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectedIndex = 5;

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 5 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new string[] { null }, e.DeselectedItems);
				CornerstoneTest.Empty(e.SelectedIndexes);
				CornerstoneTest.Empty(e.SelectedItems);
				++raised;
			};

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(1, raised);
		}

		[PresentationTestMethod]
		public void InitializingSourceRemovesInvalidItemSelection()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectedItem = "qux";
			target.SelectionChanged += (s, e) => ++raised;
			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
			CornerstoneTest.Empty(target.SelectedIndexes);
			CornerstoneTest.IsNull(target.SelectedItem);
			CornerstoneTest.Empty(target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void InitializingSourceRespectsSourceIndexSourceItemOrder()
		{
			var target = CreateTarget(false);

			target.SelectedIndex = 0;
			target.SelectedItem = "bar";

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
		}

		[PresentationTestMethod]
		public void InitializingSourceRespectsSourceItemSourceIndexOrder()
		{
			var target = CreateTarget(false);

			target.SelectedItem = "foo";
			target.SelectedIndex = 1;

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
		}

		[PresentationTestMethod]
		public void InitializingSourceRetainsValidIndexSelection()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectedIndex = 1;

			target.SelectionChanged += (s, e) => ++raised;

			target.Source = new[] { "foo", "bar", "baz" };

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
			CornerstoneTest.AreEqual("bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(0, raised);
		}

		[PresentationTestMethod]
		public void InitializingSourceRetainsValidItemSelection()
		{
			var target = CreateTarget(false);
			var raised = 0;

			target.SelectedItem = "bar";

			target.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Empty(e.DeselectedIndexes);
				CornerstoneTest.Empty(e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 1 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.SelectedItems);
				++raised;
			};

			target.Source = new[] { "foo", "bar", "baz" };

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
				if (e.PropertyName == nameof(target.Source))
				{
					++raised;
				}
			};

			target.Source = new[] { "qux", "quux", "corge" };

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	[TestClass]
	public class UntypedInterface : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void RaisesUntypedSelectionChangedEvent()
		{
			var target = CreateTarget();
			var raised = 0;

			target.SelectedIndex = 1;

			((ISelectionModel) target).SelectionChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new[] { 1 }, e.DeselectedIndexes);
				CornerstoneTest.AreEqual(new[] { "bar" }, e.DeselectedItems);
				CornerstoneTest.AreEqual(new[] { 2 }, e.SelectedIndexes);
				CornerstoneTest.AreEqual(new[] { "baz" }, e.SelectedItems);
				++raised;
			};

			target.SelectedIndex = 2;

			CornerstoneTest.AreEqual(1, raised);
		}

		#endregion
	}

	#endregion
}