#region References

using System;
using System.Collections;
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
public class InternalSelectionModelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingToWritableSelectedItemsSelectsOnModel()
	{
		var target = CreateTarget();

		target.SelectRange(1, 2);
		target.WritableSelectedItems.Add("foo");

		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, target.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { "bar", "baz", "foo" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void AddsNullWritableSelectedItemsWhenSourceIsNull()
	{
		var target = CreateTarget(nullSource: true);

		target.SelectRange(1, 2);
		CornerstoneTest.AreEqual(new object[] { null, null }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void ClearingWritableSelectedItemsUpdatesModel()
	{
		var target = CreateTarget();

		target.WritableSelectedItems.Clear();

		CornerstoneTest.Empty(target.SelectedIndexes);
		CornerstoneTest.Empty(target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void DeselectingDuplicateOnModelRemovesSelectedItem()
	{
		var target = CreateTarget(source: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

		target.SelectRange(1, 2);
		target.Select(4);
		target.Deselect(4);

		CornerstoneTest.AreEqual(new[] { "baz", "bar" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void DeselectingOnModelRemovesSelectedItem()
	{
		var target = CreateTarget();

		target.SelectRange(1, 2);
		target.Deselect(1);

		CornerstoneTest.AreEqual(new[] { "baz" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void DoesNotAcceptFixedSizeItems()
	{
		var target = CreateTarget();

		Assert.Throws<NotSupportedException>(() =>
			target.WritableSelectedItems = new[] { "foo", "bar", "baz" });
	}

	[PresentationTestMethod]
	public void PreservesSelectedItemOnItemsReset()
	{
		var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
		var target = CreateTarget(source: items);

		target.SelectedItem = "foo";

		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		items.Reset(["baz", "foo", "bar"]);

		CornerstoneTest.AreEqual("foo", target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "foo" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void PreservesSelectionOnSourceChanged()
	{
		var target = CreateTarget();

		target.SelectedIndex = 1;
		target.Source = new[] { "baz", "foo", "bar" };

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void RaisesSelectionChangedOnItemsReset()
	{
		var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
		var target = CreateTarget(source: items);

		target.SelectedIndex = 1;

		var changed = new List<string>();

		target.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

		var oldSelectedIndex = target.SelectedIndex;
		var oldSelectedItem = target.SelectedItem;

		items.Reset(new string[0]);

		CornerstoneTest.AreNotEqual(oldSelectedIndex, target.SelectedIndex);
		CornerstoneTest.AreNotEqual(oldSelectedItem, target.SelectedItem);

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.AreEqual(null, target.SelectedItem);
		CornerstoneTest.Empty(target.WritableSelectedItems);

		CornerstoneTest.Contains(changed, nameof(target.SelectedIndex));
		CornerstoneTest.Contains(changed, nameof(target.SelectedItem));
	}

	[PresentationTestMethod]
	public void RemovingFromWritableSelectedItemsDeselectsOnModel()
	{
		var target = CreateTarget();

		target.SelectRange(1, 2);
		target.WritableSelectedItems.Remove("baz");

		CornerstoneTest.AreEqual(new[] { 1 }, target.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void ReplacingSelectedItemUpdatesModel()
	{
		var target = CreateTarget();

		target.SelectRange(1, 2);
		target.WritableSelectedItems[0] = "foo";

		CornerstoneTest.AreEqual(new[] { 0, 2 }, target.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { "foo", "baz" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void RestoresSelectionOnItemsReset()
	{
		var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
		var target = CreateTarget(source: items);

		target.SelectedIndex = 1;
		items.Reset(new[] { "baz", "foo", "bar" });

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void SelectingDuplicateOnModelAddsToWritableSelectedItems()
	{
		var target = CreateTarget(source: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

		target.SelectRange(1, 4);

		CornerstoneTest.AreEqual(new[] { "bar", "baz", "foo", "bar" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void SelectingItemAddsToWritableSelectedItems()
	{
		var target = CreateTarget();

		target.Select(0);

		CornerstoneTest.AreEqual(new[] { "foo" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void SettingItemsToNullClearsSelection()
	{
		var target = CreateTarget();

		target.SelectRange(1, 2);
		target.WritableSelectedItems = null;

		CornerstoneTest.Empty(target.SelectedIndexes);
		CornerstoneTest.Empty(target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void SettingItemsToNullCreatesEmptyItems()
	{
		var target = CreateTarget();
		var oldItems = target.WritableSelectedItems;

		target.WritableSelectedItems = null;

		CornerstoneTest.IsNotNull(target.WritableSelectedItems);
		CornerstoneTest.NotSame(oldItems, target.WritableSelectedItems);
		CornerstoneTest.IsType<OldPresentationList<object>>(target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void SettingWritableSelectedItemsUpdatesModel()
	{
		var target = CreateTarget();
		var oldItems = target.WritableSelectedItems;

		var newItems = new OldPresentationList<string> { "foo", "baz" };
		target.WritableSelectedItems = newItems;

		CornerstoneTest.AreEqual(new[] { 0, 2 }, target.SelectedIndexes);
		CornerstoneTest.Same(newItems, target.WritableSelectedItems);
		CornerstoneTest.NotSame(oldItems, target.WritableSelectedItems);
		CornerstoneTest.AreEqual(new[] { "foo", "baz" }, newItems);
	}

	[PresentationTestMethod]
	public void UpdatesWritableSelectedItemsWhenSourceChangesFromNull()
	{
		var target = CreateTarget(nullSource: true);

		target.SelectRange(1, 2);
		CornerstoneTest.AreEqual(new object[] { null, null }, target.WritableSelectedItems);

		target.Source = new[] { "foo", "bar", "baz" };
		CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void UpdatesWritableSelectedItemsWhenSourceChangesToNull()
	{
		var target = CreateTarget();

		target.SelectRange(1, 2);
		CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.WritableSelectedItems);

		target.Source = null;
		CornerstoneTest.AreEqual(new object[] { null, null }, target.WritableSelectedItems);
	}

	[PresentationTestMethod]
	public void WritableSelectedItemsCanBeSetBeforeSource()
	{
		var target = CreateTarget(nullSource: true);
		var items = new OldPresentationList<string> { "foo", "bar", "baz" };
		var WritableSelectedItems = new OldPresentationList<string> { "bar" };

		target.WritableSelectedItems = WritableSelectedItems;
		target.Source = items;

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.WritableSelectedItems);
	}

	private static InternalSelectionModel CreateTarget(
		bool singleSelect = false,
		IList source = null,
		bool nullSource = false)
	{
		source ??= !nullSource ? new[] { "foo", "bar", "baz" } : null;

		var result = new InternalSelectionModel
		{
			SingleSelect = singleSelect
		};

		((ISelectionModel) result).Source = source;
		return result;
	}

	#endregion

	#region Classes

	private class ResettingCollection : List<string>, INotifyCollectionChanged
	{
		#region Constructors

		public ResettingCollection(IEnumerable<string> items)
		{
			AddRange(items);
		}

		#endregion

		#region Methods

		public void Reset(IEnumerable<string> items)
		{
			Clear();
			AddRange(items);
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