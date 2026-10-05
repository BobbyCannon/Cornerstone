#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Mixins;
using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class SelectingItemsControlTestsMultiple : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingFirstSelectedItemShouldRaiseSelectedIndexSelectedItemChanged()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
		var indexRaised = false;
		var itemRaised = false;

		target.PropertyChanged += (s, e) =>
		{
			indexRaised |= (e.Property.Name == "SelectedIndex") &&
				((int) e.OldValue! == -1) &&
				((int) e.NewValue! == 1);
			itemRaised |= (e.Property.Name == "SelectedItem") &&
				((string) e.OldValue == null) &&
				((string) e.NewValue == "bar");
		};

		target.SelectedItems.Add("bar");

		CornerstoneTest.IsTrue(indexRaised);
		CornerstoneTest.IsTrue(itemRaised);
	}

	[PresentationTestMethod]
	public void AddingItemBeforeSelectedItemsShouldUpdateSelection()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };
		var target = CreateTarget(itemsSource: items);

		target.SelectAll();
		items.Insert(0, "qux");
		Layout(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { "foo", "bar", "baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 1, 2, 3 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void AddingSelectedItemContainersShouldUpdateSelection()
	{
		using var app = Start();
		var items = new[]
		{
			new TestContainer(),
			new TestContainer()
		};

		var target = CreateTarget(items: items);

		target.Items.Add(new TestContainer { IsSelected = true });
		target.Items.Add(new TestContainer { IsSelected = true });

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual(target.Items[2], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { target.Items[2], target.Items[3] }, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void AddingSelectedItemsShouldSetItemIsSelected()
	{
		using var app = Start();
		var items = new[]
		{
			new ListBoxItem(),
			new ListBoxItem(),
			new ListBoxItem()
		};

		var target = CreateTarget(items: items);

		target.SelectedItems.Add(target.Items[0]);
		target.SelectedItems.Add(target.Items[1]);

		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
		CornerstoneTest.IsFalse(items[2].IsSelected);
	}

	[PresentationTestMethod]
	public void AddingSelectedItemsShouldSetSelectedIndex()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedItems.Add("bar");

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void AddingSubsequentSelectedItemsShouldNotRaiseSelectedIndexSelectedItemChanged()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedItems.Add("foo");

		var raised = false;
		target.PropertyChanged += (s, e) =>
			raised |= (e.Property.Name == "SelectedIndex") ||
				(e.Property.Name == "SelectedItem");

		target.SelectedItems.Add("bar");

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void AddingToSelectedItemsShouldRaiseSelectionChanged()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });
		var called = false;

		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "bar" }, e.AddedItems.Cast<object>().ToList());
			CornerstoneTest.Empty(e.RemovedItems);
			called = true;
		};

		target.SelectedItems.Add("bar");

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void AddingToSelectionShouldSetSelectedIndex()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedItems.Add("bar");

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void AssigningMultipleSelectedItemsShouldSetSelectedIndex()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		target.SelectedItems = new OldPresentationList<string>("foo", "bar", "baz");

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "foo", "bar", "baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void AssigningMultipleSelectedItemsToSelectionShouldSetSelectedIndex()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });
		var selection = new SelectionModel<string> { SingleSelect = false };

		selection.SelectRange(0, 2);
		target.Selection = selection;

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "foo", "bar", "baz" }, target.Selection.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void AssigningNullToSelectionShouldCreateNewSelectionModel()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
		var oldSelection = target.Selection;

		target.Selection = null!;

		CornerstoneTest.IsNotNull(target.Selection);
		CornerstoneTest.NotSame(oldSelection, target.Selection);
	}

	[PresentationTestMethod]
	public void AssigningSelectedItemsShouldRaiseSelectionChanged()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		target.SelectedItem = "bar";

		var called = false;

		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
			CornerstoneTest.AreEqual(new[] { "bar" }, e.RemovedItems.Cast<object>());
			called = true;
		};

		target.SelectedItems = new OldPresentationList<object>("foo", "baz");

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void AssigningSelectedItemsShouldSetItemIsSelected()
	{
		using var app = Start();
		var items = new[]
		{
			new ListBoxItem(),
			new ListBoxItem(),
			new ListBoxItem()
		};

		var target = CreateTarget(items: items);

		target.SelectedItems = new OldPresentationList<object> { items[0], items[1] };

		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
		CornerstoneTest.IsFalse(items[2].IsSelected);
	}

	[PresentationTestMethod]
	public void AssigningSelectionModelWithDifferentSourceToSelectionShouldFail()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
		var selection = new SelectionModel<string> { Source = new[] { "baz" } };

		Assert.Throws<ArgumentException>(() => target.Selection = selection);
	}

	[PresentationTestMethod]
	public void AssigningSelectionModelWithNullSourceToSelectionShouldSetSource()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
		var selection = new SelectionModel<string>();

		target.Selection = selection;

		CornerstoneTest.Same(target.ItemsSource, selection.Source);
	}

	[PresentationTestMethod]
	public void AssigningSelectionShouldRaiseSelectionChanged()
	{
		using var app = Start();
		var items = new[] { "foo", "bar", "baz" };
		var target = CreateTarget(itemsSource: items);
		var raised = 0;

		target.SelectedItem = "bar";

		target.SelectionChanged += (s, e) =>
		{
			if (raised == 0)
			{
				CornerstoneTest.Empty(e.AddedItems.Cast<object>());
				CornerstoneTest.AreEqual(new[] { "bar" }, e.RemovedItems.Cast<object>());
			}
			else
			{
				CornerstoneTest.AreEqual(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
				CornerstoneTest.Empty(e.RemovedItems.Cast<object>());
			}

			++raised;
		};

		var selection = new SelectionModel<string> { Source = items, SingleSelect = false };
		selection.Select(0);
		selection.Select(2);
		target.Selection = selection;

		CornerstoneTest.AreEqual(2, raised);
	}

	[PresentationTestMethod]
	public void AssigningSelectionShouldSetItemIsSelected()
	{
		using var app = Start();
		var items = new[]
		{
			new ListBoxItem(),
			new ListBoxItem(),
			new ListBoxItem()
		};

		var target = CreateTarget(items: items);
		var selection = new SelectionModel<object> { SingleSelect = false };

		selection.SelectRange(0, 1);
		target.Selection = selection;

		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
		CornerstoneTest.IsFalse(items[2].IsSelected);
	}

	[PresentationTestMethod]
	public void AssigningSingleSelectedItemToSelectionShouldSetSelectedIndex()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
		var selection = new SelectionModel<string> { SingleSelect = false };

		selection.Select(1);
		target.Selection = selection;

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.Selection.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 1 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void AssigningSingleSelectedItemsShouldSetSelectedIndex()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedItems = new OldPresentationList<object>("bar");

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 1 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void CanBindInitialSelectedStateViaItemContainerTheme()
	{
		using var app = Start();
		var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
		var itemTheme = new ControlTheme(typeof(ContentPresenter))
		{
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme);

		CornerstoneTest.AreEqual(new[] { 0, 2 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 0, 2 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[0], items[2] }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void CanBindInitialSelectedStateViaStyle()
	{
		using var app = Start();
		var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
		var style = new Style(x => x.OfType<ContentPresenter>())
		{
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(itemsSource: items, styles: new[] { style });

		CornerstoneTest.AreEqual(new[] { 0, 2 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 0, 2 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[0], items[2] }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void CanChangeSelectionForContainersOutsideOfViewport()
	{
		// Issue #11119
		using var app = Start();
		var items = Enumerable.Range(0, 100).Select(x => new TestContainer
		{
			Content = $"Item {x}",
			Height = 100
		}).ToList();

		// Create a SelectingItemsControl with a virtualizing stack panel.
		var target = CreateTarget(itemsSource: items, virtualizing: true);
		target.AutoScrollToSelectedItem = false;

		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
		var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

		// Select item 1.
		target.SelectedIndex = 1;

		// Scroll item 1 and 2 out of view.
		scroll.Offset = new(0, 1000);
		Layout(target);

		CornerstoneTest.AreEqual(10, panel.FirstRealizedIndex);
		CornerstoneTest.AreEqual(19, panel.LastRealizedIndex);

		// Select item 2 now that items 1 and 2 are both unrealized.
		target.SelectedIndex = 2;

		// The selection should be updated.
		CornerstoneTest.Empty(SelectedContainers(target));
		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.Same(items[2], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 2 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[2] }, target.Selection.SelectedItems);

		// Scroll selected item back into view.
		scroll.Offset = new(0, 0);

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == SelectingItemsControl.SelectedIndexProperty)
			{
			}
		};

		Layout(target);

		// The selection should be preserved.
		CornerstoneTest.AreEqual(new[] { 2 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.Same(items[2], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 2 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[2] }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void CanSetSelectedIndexToAnotherSelectedItem()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		target.SelectedItems.Add("foo");
		target.SelectedItems.Add("bar");

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));

		var raised = false;
		target.SelectionChanged += (s, e) =>
		{
			raised = true;
			CornerstoneTest.Empty(e.AddedItems);
			CornerstoneTest.AreEqual(new[] { "foo" }, e.RemovedItems);
		};

		target.SelectedIndex = 1;

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 1 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void RangeSelectBackwardsShouldSelectRange()
	{
		using var app = Start();
		var items = new[]
		{
			"foo",
			"bar",
			"baz",
			"qux",
			"qiz",
			"lol"
		};

		var target = CreateTarget(items: items);

		target.SelectedIndex = 3;
		target.SelectRange(1);

		CornerstoneTest.AreEqual(new[] { "qux", "bar", "baz" }, target.SelectedItems.Cast<object>().ToList());
	}

	[PresentationTestMethod]
	public void RangeSelectShouldSelectRange()
	{
		using var app = Start();
		var items = new[]
		{
			"foo",
			"bar",
			"baz",
			"qux",
			"qiz",
			"lol"
		};

		var target = CreateTarget(items: items);

		target.SelectedIndex = 1;
		target.SelectRange(3);

		CornerstoneTest.AreEqual(new[] { "bar", "baz", "qux" }, target.SelectedItems.Cast<object>().ToList());
	}

	[PresentationTestMethod]
	public void ReassigningSelectedItemsShouldClearSelection()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedItems.Add("bar");
		target.SelectedItems = new OldPresentationList<object>();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void ReassigningSelectedItemsShouldNotClearItemIsSelected()
	{
		using var app = Start();
		var items = new[]
		{
			new ListBoxItem(),
			new ListBoxItem(),
			new ListBoxItem()
		};

		var target = CreateTarget(items: items);

		target.SelectedItems.Add(target.Items[0]);
		target.SelectedItems.Add(target.Items[1]);
		target.SelectedItems = new OldPresentationList<object> { items[0], items[1] };

		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
		CornerstoneTest.IsFalse(items[2].IsSelected);
	}

	[PresentationTestMethod]
	public void ReassigningSelectionShouldClearSelection()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.Selection.Select(1);
		target.Selection = new SelectionModel<string>();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void RemovingFromSelectedItemsShouldRaiseSelectionChanged()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });
		var called = false;

		target.SelectedItem = "bar";
		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "bar" }, e.RemovedItems.Cast<object>().ToList());
			CornerstoneTest.Empty(e.AddedItems);
			called = true;
		};

		target.SelectedItems.Remove("bar");

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void RemovingItemBeforeSelectedItemShouldUpdateSelection()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };
		var target = CreateTarget(itemsSource: items);

		target.SelectedIndex = 1;
		target.SelectRange(2);

		CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.SelectedItems);

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("bar", target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void RemovingLastSelectedItemShouldRaiseSelectedIndexChanged()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedItems.Add("foo");

		var raised = false;
		target.PropertyChanged += (s, e) =>
			raised |= (e.Property.Name == "SelectedIndex") &&
				((int) e.OldValue! == 0) &&
				((int) e.NewValue! == -1);

		target.SelectedItems.RemoveAt(0);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemWithMultipleSelectionActiveShouldUpdateSelection()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };
		var target = CreateTarget(itemsSource: items);

		target.SelectAll();
		items.RemoveAt(0);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("bar", target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { "bar", "baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemsShouldClearItemIsSelected()
	{
		using var app = Start();
		var items = new[]
		{
			new ListBoxItem(),
			new ListBoxItem(),
			new ListBoxItem()
		};

		var target = CreateTarget(items: items);

		target.SelectedItems.Add(items[0]);
		target.SelectedItems.Add(items[1]);
		target.SelectedItems.Remove(items[1]);

		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsFalse(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void ReplacingSelectedItemShouldUpdateSelectedItems()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };
		var target = CreateTarget(itemsSource: items);

		target.SelectAll();
		items[1] = "qux";

		CornerstoneTest.AreEqual(new[] { "foo", "baz" }, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void SecondRangeSelectBackwardsShouldSelectFromOriginalSelection()
	{
		using var app = Start();
		var items = new[]
		{
			"foo",
			"bar",
			"baz",
			"qux",
			"qiz",
			"lol"
		};

		var target = CreateTarget(items: items);

		target.SelectedIndex = 2;
		target.SelectRange(5);
		target.SelectRange(4);

		CornerstoneTest.AreEqual(new[] { "baz", "qux", "qiz" }, target.SelectedItems.Cast<object>().ToList());
	}

	[PresentationTestMethod]
	public void SelectAllHandlesDuplicateItems()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

		target.SelectAll();

		CornerstoneTest.AreEqual(new[] { "foo", "bar", "baz", "foo", "bar", "baz" }, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectAllRaisesSelectionChangedEvent()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		SelectionChangedEventArgs receivedArgs = null;

		target.SelectionChanged += (_, args) => receivedArgs = args;

		target.SelectAll();

		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.AreEqual(target.ItemsSource, receivedArgs.AddedItems);
		CornerstoneTest.Empty(receivedArgs.RemovedItems);
	}

	[PresentationTestMethod]
	public void SelectAllSetsSelectedIndexAndSelectedItem()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		target.SelectAll();

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SelectedItemsShouldBeMarkedWhenPanelCreatedAfterSelectedItemsIsSet()
	{
		// Issue #2565.
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" }, performLayout: false);

		CornerstoneTest.IsNull(target.ItemsPanelRoot);
		target.SelectedItems = new OldPresentationList<string>("foo", "bar", "baz");

		var root = CornerstoneTest.IsType<TestRoot>(target.GetVisualRoot());
		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { "foo", "bar", "baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void SelectionIsNotClearedOnRecyclingContainers()
	{
		using var app = Start();
		var items = Enumerable.Range(0, 100).Select(x => new ItemViewModel($"Item {x}", false)).ToList();

		// Create a SelectingItemsControl that creates containers that raise IsSelectedChanged,
		// with a virtualizing stack panel.
		var target = CreateTarget<TestSelectorWithContainers>(
			itemsSource: items,
			virtualizing: true);
		target.AutoScrollToSelectedItem = false;

		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
		var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

		// Select item 1.
		target.SelectedIndex = 1;

		// Scroll item 1 out of view.
		scroll.Offset = new(0, 1000);
		Layout(target);

		CornerstoneTest.AreEqual(10, panel.FirstRealizedIndex);
		CornerstoneTest.AreEqual(19, panel.LastRealizedIndex);

		// The selection should be preserved.
		CornerstoneTest.AreEqual(new[] { 1 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.Same(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 1 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[1] }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectionIsUpdatedOnContainerRealizationWithIsSelectedBinding()
	{
		using var app = Start();
		var items = Enumerable.Range(0, 100).Select(x => new ItemViewModel($"Item {x}", false)).ToList();
		items[0].IsSelected = true;
		items[15].IsSelected = true;

		var itemTheme = new ControlTheme(typeof(ContentPresenter))
		{
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
				new Setter(Control.HeightProperty, 100.0)
			}
		};

		// Create a SelectingItemsControl with a virtualizing stack panel.
		var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme, virtualizing: true);
		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
		var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

		// The SelectingItemsControl does not yet know anything about item 15's selection state.
		CornerstoneTest.AreEqual(new[] { 0 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 0 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[0] }, target.Selection.SelectedItems);

		// Scroll item 15 into view.
		scroll.Offset = new(0, 1000);
		Layout(target);

		CornerstoneTest.AreEqual(10, panel.FirstRealizedIndex);
		CornerstoneTest.AreEqual(19, panel.LastRealizedIndex);

		// The final selection should be in place.
		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[15].IsSelected);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 0, 15 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[0], items[15] }, target.Selection.SelectedItems);

		// Although item 0 is selected, it's not realized.
		CornerstoneTest.AreEqual(new[] { 15 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void SelectionStateChangeOnUnrealizedItemIsRespectedWithIsSelectedBinding()
	{
		using var app = Start();
		var items = Enumerable.Range(0, 100).Select(x => new ItemViewModel($"Item {x}", false)).ToList();
		var itemTheme = new ControlTheme(typeof(ContentPresenter))
		{
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
				new Setter(Control.HeightProperty, 100.0)
			}
		};

		// Create a SelectingItemsControl with a virtualizing stack panel.
		var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme, virtualizing: true);
		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
		var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

		// Scroll item 1 out of view.
		scroll.Offset = new(0, 1000);
		Layout(target);

		CornerstoneTest.AreEqual(10, panel.FirstRealizedIndex);
		CornerstoneTest.AreEqual(19, panel.LastRealizedIndex);

		// Select item 1 now it's unrealized.
		items[1].IsSelected = true;

		// The SelectingItemsControl does not yet know anything about the selection change.
		CornerstoneTest.Empty(SelectedContainers(target));
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Empty(target.Selection.SelectedIndexes);
		CornerstoneTest.Empty(target.Selection.SelectedItems);

		// Scroll item 1 back into view.
		scroll.Offset = new(0, 0);
		Layout(target);

		// The item and container should be marked as selected.
		CornerstoneTest.IsTrue(items[1].IsSelected);
		CornerstoneTest.AreEqual(new[] { 1 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 1 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[1] }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectionStateIsUpdatedViaIsSelectedBinding()
	{
		using var app = Start();
		var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
		var itemTheme = new ControlTheme(typeof(TestContainer))
		{
			BasedOn = CreateTestContainerTheme(),
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		// For the container selection state to be communicated back to the SelectingItemsControl
		// we need a container which raises the SelectingItemsControl.IsSelectedChangedEvent when
		// the IsSelected property changes.
		var target = CreateTarget<TestSelectorWithContainers>(
			itemsSource: items,
			itemContainerTheme: itemTheme);

		items[1].IsSelected = true;

		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[0], items[1], items[2] }, target.Selection.SelectedItems);

		items[0].IsSelected = false;

		CornerstoneTest.AreEqual(new[] { 1, 2 }, SelectedContainers(target));
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { 1, 2 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { items[1], items[2] }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectionStateIsWrittenBackToItemViaIsSelectedBinding()
	{
		using var app = Start();
		var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
		var itemTheme = new ControlTheme(typeof(ContentPresenter))
		{
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme);
		var container0 = CornerstoneTest.IsAssignableFrom<Control>(target.ContainerFromIndex(0));
		var container1 = CornerstoneTest.IsAssignableFrom<Control>(target.ContainerFromIndex(1));

		SelectingItemsControl.SetIsSelected(container1, true);

		CornerstoneTest.IsTrue(items[1].IsSelected);

		SelectingItemsControl.SetIsSelected(container0, false);

		CornerstoneTest.IsFalse(items[0].IsSelected);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexAfterRangeShouldUnmarkPreviouslySelectedContainers()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz", "qux" });

		target.SelectRange(2);

		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));

		target.SelectedIndex = 3;

		CornerstoneTest.AreEqual(new[] { 3 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexShouldAddToSelectedItems()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems.Cast<object>().ToList());
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexShouldUnmarkPreviouslySelectedContainers()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		target.SelectedItems.Add("foo");
		target.SelectedItems.Add("bar");

		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));

		target.SelectedIndex = 2;

		CornerstoneTest.AreEqual(new[] { 2 }, SelectedContainers(target));
	}

	/// <summary>
	/// Tests a problem discovered with ListBox with selection.
	/// </summary>
	/// <remarks>
	/// - Items is bound to DataContext first, followed by say SelectedIndex
	/// - When the ListBox is removed from the visual tree, DataContext becomes null (as it's
	/// inherited)
	/// - This changes Items to null, which changes SelectedIndex to null as there are no
	/// longer any items
	/// - However, the news that DataContext is now null hasn't yet reached the SelectedItems
	/// binding and so the unselection is sent back to the ViewModel
	/// 
	/// This is a similar problem to that tested by XamlBindingTest.Should_Not_Write_To_Old_DataContext.
	/// However, that tests a general property binding problem: here we are writing directly
	/// to the SelectedItems collection - not via a binding - so it's something that the
	/// binding system cannot solve. Instead we solve it by not clearing SelectedItems when
	/// DataContext is in the process of changing.
	/// </remarks>
	[PresentationTestMethod]
	public void ShouldNotWriteSelectedItemsToOldDataContext()
	{
		using var app = Start();
		var vm = new OldDataContextViewModel();
		var target = CreateTarget();

		var itemsBinding = new Binding
		{
			Path = "Items",
			Mode = BindingMode.OneWay
		};

		var selectedItemsBinding = new Binding
		{
			Path = "SelectedItems",
			Mode = BindingMode.OneWay
		};

		// Bind ItemsSource and SelectedItems to the VM.
		target.Bind(TestSelector.ItemsSourceProperty, itemsBinding);
		target.Bind(TestSelector.SelectedItemsProperty, selectedItemsBinding);

		// Set DataContext and SelectedIndex
		target.DataContext = vm;
		target.SelectedIndex = 1;

		// Make sure SelectedItems are written back to VM.
		CornerstoneTest.AreEqual(new[] { "bar" }, vm.SelectedItems);

		// Clear DataContext and ensure that SelectedItems is still set in the VM.
		target.DataContext = null;
		CornerstoneTest.AreEqual(new[] { "bar" }, vm.SelectedItems);

		// Ensure target's SelectedItems is now clear.
		CornerstoneTest.Empty(target.SelectedItems);
	}

	/// <summary>
	/// See <see cref="Should_Not_Write_SelectedItems_To_Old_DataContext" />.
	/// </summary>
	[PresentationTestMethod]
	public void ShouldNotWriteSelectionModelToOldDataContext()
	{
		using var app = Start();
		var vm = new OldDataContextViewModel();
		var target = CreateTarget();

		var itemsBinding = new Binding
		{
			Path = "Items",
			Mode = BindingMode.OneWay
		};

		var selectionBinding = new Binding
		{
			Path = "Selection",
			Mode = BindingMode.OneWay
		};

		// Bind ItemsSource and Selection to the VM.
		target.Bind(TestSelector.ItemsSourceProperty, itemsBinding);
		target.Bind(TestSelector.SelectionProperty, selectionBinding);

		// Set DataContext and SelectedIndex
		target.DataContext = vm;
		target.SelectedIndex = 1;

		// Make sure selection is written to selection model
		CornerstoneTest.AreEqual(1, vm.Selection.SelectedIndex);

		// Clear DataContext and ensure that selection is still set in model.
		target.DataContext = null;
		CornerstoneTest.AreEqual(1, vm.Selection.SelectedIndex);

		// Ensure target's SelectedItems is now clear.
		CornerstoneTest.Empty(target.SelectedItems);
	}

	public static IDisposable Start()
	{
		return UnitTestApplication.Start(
			TestServices.MockThreadingInterface.With(
				fontManagerImpl: new HeadlessFontManagerStub(),
				keyboardDevice: () => new KeyboardDevice(),
				keyboardNavigation: () => new KeyboardNavigationHandler(),
				inputManager: new InputManager(),
				renderInterface: new HeadlessPlatformRenderInterface(),
				textShaperImpl: new HarfBuzzTextShaper(),
				assetLoader: new StandardAssetLoader()));
	}

	[PresentationTestMethod]
	public void SupriousSelectedIndexChangesShouldNotBeTriggered()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		var selectedIndexes = new List<int>();
		target.GetObservable(TestSelector.SelectedIndexProperty).Subscribe(x => selectedIndexes.Add(x));

		target.SelectedItems = new OldPresentationList<object> { "bar", "baz" };
		target.SelectedItem = "foo";

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { -1, 1, 0 }, selectedIndexes);
	}

	[PresentationTestMethod]
	public void TogglingSelectionAfterRangeShouldWork()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

		target.SelectRange(3);

		CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3 }, SelectedContainers(target));

		target.Toggle(4);

		CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3, 4 }, SelectedContainers(target));
	}

	[PresentationTestMethod]
	public void UnboundSelectedItemsShouldBeClearedWhenDataContextCleared()
	{
		using var app = Start();
		var data = new
		{
			Items = new[] { "foo", "bar", "baz" }
		};

		var target = CreateTarget(data);
		var itemsBinding = new Binding { Path = "Items" };
		target.Bind(TestSelector.ItemsSourceProperty, itemsBinding);

		CornerstoneTest.Same(data.Items, target.ItemsSource);

		target.SelectedItems.Add("bar");
		target.DataContext = null;

		CornerstoneTest.Empty(target.SelectedItems);
	}

	[PresentationTestMethod]
	public void UnselectAllClearsSelectedIndexAndSelectedItem()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

		target.SelectedIndex = 0;
		target.UnselectAll();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.AreEqual(null, target.SelectedItem);
	}

	private static TestRoot CreateRoot(Control child)
	{
		return new TestRoot
		{
			Resources =
			{
				{ typeof(TestSelector), CreateTestSelectorControlTheme() },
				{ typeof(TestContainer), CreateTestContainerTheme() },
				{ typeof(ScrollViewer), CreateScrollViewerTheme() }
			},
			Child = child
		};
	}

	private static FuncControlTemplate CreateScrollViewerTemplate()
	{
		return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
			new Panel
			{
				Children =
				{
					new ScrollContentPresenter
					{
						Name = "PART_ContentPresenter"
					}.RegisterInNameScope(scope),
					new ScrollBar
					{
						Name = "verticalScrollBar"
					}
				}
			});
	}

	private static ControlTheme CreateScrollViewerTheme()
	{
		return new ControlTheme(typeof(ScrollViewer))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateScrollViewerTemplate())
			}
		};
	}

	private static TestSelector CreateTarget(
		object dataContext = null,
		IList items = null,
		IList itemsSource = null,
		ControlTheme itemContainerTheme = null,
		IDataTemplate itemTemplate = null,
		IEnumerable<Style> styles = null,
		bool performLayout = true,
		bool virtualizing = false)
	{
		return CreateTarget<TestSelector>(
			dataContext,
			items,
			itemsSource,
			itemContainerTheme,
			itemTemplate,
			styles,
			performLayout,
			virtualizing);
	}

	private static T CreateTarget<T>(
		object dataContext = null,
		IList items = null,
		IList itemsSource = null,
		ControlTheme itemContainerTheme = null,
		IDataTemplate itemTemplate = null,
		IEnumerable<Style> styles = null,
		bool performLayout = true,
		bool virtualizing = false)
		where T : TestSelector, new()
	{
		var target = new T
		{
			DataContext = dataContext,
			ItemContainerTheme = itemContainerTheme,
			ItemTemplate = itemTemplate,
			ItemsSource = itemsSource,
			SelectionMode = SelectionMode.Multiple
		};

		if (items is not null)
		{
			foreach (var item in items)
			{
				target.Items.Add(item);
			}
		}

		if (virtualizing)
		{
			target.ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel());
		}

		var root = CreateRoot(target);

		if (styles is not null)
		{
			foreach (var style in styles)
			{
				root.Styles.Add(style);
			}
		}

		if (performLayout)
		{
			root.LayoutManager.ExecuteInitialLayoutPass();
		}

		return target;
	}

	private static FuncControlTemplate CreateTestContainerTemplate()
	{
		return new FuncControlTemplate<TestContainer>((parent, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = parent[!TestContainer.ContentProperty],
				[!ContentPresenter.ContentTemplateProperty] = parent[!TestContainer.ContentTemplateProperty]
			}.RegisterInNameScope(scope));
	}

	private static ControlTheme CreateTestContainerTheme()
	{
		return new ControlTheme(typeof(TestContainer))
		{
			Setters =
			{
				new Setter(TestContainer.TemplateProperty, CreateTestContainerTemplate()),
				new Setter(TestContainer.HeightProperty, 100.0)
			}
		};
	}

	private static ControlTheme CreateTestSelectorControlTheme()
	{
		return new ControlTheme(typeof(TestSelector))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateTestSelectorTemplate())
			}
		};
	}

	private static FuncControlTemplate CreateTestSelectorTemplate()
	{
		return new FuncControlTemplate<ItemsControl>((parent, scope) =>
		{
			return new Border
			{
				Background = new SolidColorBrush(0xffffffff),
				Child = new ScrollViewer
				{
					Name = "PART_ScrollViewer",
					Content = new ItemsPresenter
					{
						Name = "PART_ItemsPresenter",
						[~ItemsPresenter.ItemsPanelProperty] = parent[~ItemsControl.ItemsPanelProperty]
					}.RegisterInNameScope(scope)
				}.RegisterInNameScope(scope)
			};
		});
	}

	private static void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static IEnumerable<int> SelectedContainers(SelectingItemsControl target)
	{
		CornerstoneTest.IsNotNull(target.ItemsPanel);

		return target.ItemsPanelRoot!.Children
			.Select(x => SelectingItemsControl.GetIsSelected(x) ? target.IndexFromContainer(x) : -1)
			.Where(x => x != -1);
	}

	#endregion

	#region Classes

	private class ItemViewModel : NotifyingBase
	{
		#region Fields

		private bool _isSelected;

		#endregion

		#region Constructors

		public ItemViewModel(string value, bool isSelected = false)
		{
			Value = value;
			_isSelected = isSelected;
		}

		#endregion

		#region Properties

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					RaisePropertyChanged();
				}
			}
		}

		public string Value { get; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return Value;
		}

		#endregion
	}

	private class OldDataContextViewModel
	{
		#region Constructors

		public OldDataContextViewModel()
		{
			Items = new List<string> { "foo", "bar" };
			SelectedItems = new List<string>();
			Selection = new SelectionModel<string>();
		}

		#endregion

		#region Properties

		public List<string> Items { get; }
		public List<string> SelectedItems { get; }
		public SelectionModel<string> Selection { get; }

		#endregion
	}

	private class TestContainer : ContentControl, ISelectable
	{
		#region Fields

		public static readonly StyledProperty<bool> IsSelectedProperty =
			SelectingItemsControl.IsSelectedProperty.AddOwner<TestContainer>();

		#endregion

		#region Constructors

		static TestContainer()
		{
			SelectableMixin.Attach<TestContainer>(SelectingItemsControl.IsSelectedProperty);
		}

		#endregion

		#region Properties

		public bool IsSelected
		{
			get => GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		#endregion
	}

	private class TestSelector : SelectingItemsControl
	{
		#region Fields

		public new static readonly PresentationProperty<IList> SelectedItemsProperty =
			SelectingItemsControl.SelectedItemsProperty;

		public new static readonly DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
			SelectingItemsControl.SelectionProperty;

		#endregion

		#region Constructors

		public TestSelector()
		{
			SelectionMode = SelectionMode.Multiple;
		}

		#endregion

		#region Properties

		public new IList SelectedItems
		{
			get => base.SelectedItems!;
			set => base.SelectedItems = value;
		}

		public new ISelectionModel Selection
		{
			get => base.Selection;
			set => base.Selection = value;
		}

		public new SelectionMode SelectionMode
		{
			get => base.SelectionMode;
			set => base.SelectionMode = value;
		}

		#endregion

		#region Methods

		public void SelectAll()
		{
			Selection.SelectAll();
		}

		public void SelectRange(int index)
		{
			UpdateSelection(index, true, true);
		}

		public void Toggle(int index)
		{
			UpdateSelection(index, true, false, true);
		}

		public void UnselectAll()
		{
			Selection.Clear();
		}

		#endregion
	}

	private class TestSelectorWithContainers : TestSelector
	{
		#region Properties

		protected override Type StyleKeyOverride => typeof(TestSelector);

		#endregion

		#region Methods

		protected internal override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
		{
			return new TestContainer();
		}

		protected internal override bool NeedsContainerOverride(object item, int index, out object recycleKey)
		{
			return NeedsContainer<TestContainer>(item, out recycleKey);
		}

		#endregion
	}

	#endregion
}