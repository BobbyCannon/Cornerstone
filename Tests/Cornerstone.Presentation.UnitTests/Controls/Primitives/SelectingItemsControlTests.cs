#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Markup;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public partial class SelectingItemsControlTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _helper = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddingItemBeforeSelectedItemShouldUpdateSelectedIndex()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectedIndex = 1
		};

		Prepare(target);

		items.Insert(0, "Qux");

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual("Bar", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void AddingSelectedItemShouldUpdateSelection()
	{
		var items = new OldPresentationList<Item>(new Item(), new Item());

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		items.Add(new Item { IsSelected = true });

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[2], target.SelectedItem);
	}

	[PresentationTestMethod]
	public void AnchorIndexCanAccessSelectionDuringInit()
	{
		using var _ = Start();

		var target = new ListBox();
		target.BeginInit();

		target.Selection = new SelectionModel<ItemModel>
		{
			AnchorIndex = 42
		};

		CornerstoneTest.AreEqual(42, target.GetAnchorIndex());
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemCausesScrollToInitialSelectedItem()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items
		};

		var raised = false;

		target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) => raised = true);
		target.SelectedIndex = 2;
		Prepare(target);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemCausesScrollToSelectedItem()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items
		};

		var raised = false;

		Prepare(target);
		target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) => raised = true);
		target.SelectedIndex = 2;
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemCausesScrollWhenTurnedOn()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			AutoScrollToSelectedItem = false
		};

		Prepare(target);

		var raised = false;
		target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) => raised = true);
		target.SelectedIndex = 2;
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.IsFalse(raised);

		target.AutoScrollToSelectedItem = true;
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemDoesntScrollIfReattachedToVisualTreeWithNoSelectionChange()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectedIndex = 2
		};

		var raised = false;

		Prepare(target);

		var root = (TestRoot) target.Parent!;

		target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) => raised = true);

		root.Child = null;
		root.Child = target;

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemOnResetWorks()
	{
		// Issue #3148
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = new ResettingCollection(100);

			var target = new ListBox
			{
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((x, _) =>
					new TextBlock
					{
						Text = x,
						Width = 100,
						Height = 10
					}),
				AutoScrollToSelectedItem = true
			};

			var root = new TestRoot(true, target);
			root.Measure(new Size(100, 100));
			root.Arrange(new Rect(0, 0, 100, 100));

			var panel = target.Presenter!.Panel!;
			CornerstoneTest.IsTrue(panel.Children.Count > 0);
			CornerstoneTest.IsTrue(panel.Children.Count < 100);

			target.SelectedItem = "Item99";

			// #3148 triggered here.
			items.Reset(new[] { "Item99" });
			Layout(target);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
			CornerstoneTest.AreEqual(1, panel.Children.Count(x => x.IsVisible));
		}
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemScrollsSynchronouslyWhenLaidOut()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items
		};

		var raised = false;

		Prepare(target);
		target.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);
		target.SelectedIndex = 2;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemScrollsWhenReattachedToVisualTreeIfSelectionChangedWhileDetachedFromVisualTree()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectedIndex = 2
		};

		var raised = false;

		Prepare(target);

		var root = (TestRoot) target.Parent!;

		target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) => raised = true);

		root.Child = null;
		target.SelectedIndex = 1;
		root.Child = target;
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void BindingSelectedIndexSelectsCorrectItem()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		// Issue #4496 (part 2)
		var items = new ObservableCollection<string>();

		var other = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectionMode = SelectionMode.AlwaysSelected
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			[!ListBox.SelectedIndexProperty] = other[!ListBox.SelectedIndexProperty]
		};

		Prepare(other);
		Prepare(target);

		items.Add("Foo");

		CornerstoneTest.AreEqual(0, other.SelectedIndex);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void BindingSelectedItemSelectsCorrectItem()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		// Issue #4496 (part 2)
		var items = new ObservableCollection<string>();

		var other = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectionMode = SelectionMode.AlwaysSelected
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			[!ListBox.SelectedItemProperty] = other[!ListBox.SelectedItemProperty]
		};

		Prepare(target);
		other.ApplyTemplate();
		other.Presenter!.ApplyTemplate();

		items.Add("Foo");

		CornerstoneTest.AreEqual(0, other.SelectedIndex);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void BindingWithDelayedBindingAndInitializationWhereDataContextIsRootWorks()
	{
		// Test for #1932.
		var root = new RootWithItems();

		root.BeginInit();
		root.DataContext = root;

		var target = new ListBox();
		target.BeginInit();
		root.Child = target;

		DelayedBinding.Add(target, ItemsControl.ItemsSourceProperty, new Binding(nameof(RootWithItems.Items)));
		DelayedBinding.Add(target, ListBox.SelectedItemProperty, new Binding(nameof(RootWithItems.Selected)));
		target.EndInit();
		root.EndInit();

		CornerstoneTest.AreEqual("b", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void CanSetBothSelectedItemAndSelectedItemsDuringInitialization()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		// Issue #2969.
		var target = new ListBox();
		var selectedItems = new List<object>();

		target.BeginInit();
		target.Template = Template();
		target.ItemsSource = new[] { "Foo", "Bar", "Baz" };
		target.SelectedItems = selectedItems;
		target.SelectedItem = "Bar";
		target.EndInit();

		Prepare(target);

		CornerstoneTest.AreEqual("Bar", target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.Same(selectedItems, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { "Bar" }, selectedItems);
	}

	[PresentationTestMethod]
	public void ChangingDataContextRespectsAlwaysSelected()
	{
		// Issue #12733
		var target = new ListBox
		{
			DataContext = Enumerable.Range(0, 10).ToList(),
			SelectionMode = SelectionMode.AlwaysSelected,
			Template = Template(),
			[!ListBox.ItemsSourceProperty] = new Binding()
		};

		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.DataContext = Enumerable.Range(10, 10).ToList();

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void ChangingDataContextShouldNotClearNestedViewModelSelectedItem()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var vm = new MasterViewModel
		{
			Child = new ChildViewModel
			{
				Items = items,
				SelectedItem = items[1]
			}
		};

		var target = new SelectingItemsControl { DataContext = vm };
		var itemsBinding = new Binding("Child.Items");
		var selectedBinding = new Binding("Child.SelectedItem");

		target.Bind(SelectingItemsControl.ItemsSourceProperty, itemsBinding);
		target.Bind(SelectingItemsControl.SelectedItemProperty, selectedBinding);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.Same(vm.Child.SelectedItem, target.SelectedItem);

		items = new[]
		{
			new Item { Value = "Item1" },
			new Item { Value = "Item2" },
			new Item { Value = "Item3" }
		};

		vm = new MasterViewModel
		{
			Child = new ChildViewModel
			{
				Items = items,
				SelectedItem = items[2]
			}
		};

		target.DataContext = vm;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.Same(vm.Child.SelectedItem, target.SelectedItem);
	}

	[PresentationTestMethod]
	public void ChangingItemsSourceDuringSelectionChangedWhenSelectionLostDoesNotThrow()
	{
		// Issue #7536.
		var target = new SelectingItemsControl
		{
			ItemsSource = new ObservableCollection<Item> { new(), new(), new() },
			SelectedIndex = 0
		};
		var raised = 0;

		target.SelectionChanged += (s, e) =>
		{
			target.ItemsSource = new ObservableCollection<Item> { new(), new() };
			++raised;
		};

		target.SelectedIndex = -1;

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ClearingIsSelectedAndRaisingIsSelectedChangedOnItemShouldUpdateSelection()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedItem = items[1];

		CornerstoneTest.IsFalse(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);

		items[1].IsSelected = false;
		items[1].RaiseEvent(new RoutedEventArgs(SelectingItemsControl.IsSelectedChangedEvent));

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void ClearingSelectedIndexShouldRaiseSelectionChangedEvent()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template(),
			SelectedIndex = 1
		};

		Prepare(target);

		var called = false;

		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.Same(items[1], e.RemovedItems.Cast<object>().Single());
			CornerstoneTest.Empty(e.AddedItems);
			called = true;
		};

		target.SelectedIndex = -1;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void DoesNotWriteToBoundSelectedItemWhenDataContextChanges()
	{
		// Issue #9438.
		var vm1 = new SelectionViewModel();
		vm1.Items.Add("foo");
		vm1.Items.Add("bar");
		vm1.SelectedItem = "bar";

		var vm2 = new SelectionViewModel();
		vm2.Items.Add("foo");
		vm2.Items.Add("bar");
		vm2.SelectedItem = "bar";

		var target = new SelectingItemsControl
		{
			DataContext = vm1,
			[!ItemsControl.ItemsSourceProperty] = new Binding("Items"),
			[!SelectingItemsControl.SelectedItemProperty] = new Binding("SelectedItem"),
			Template = Template()
		};

		CornerstoneTest.AreEqual("bar", target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		var selectedItemChangedRaised = 0;
		vm2.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(vm2.SelectedItem))
			{
				++selectedItemChangedRaised;
			}
		};

		target.DataContext = vm2;

		CornerstoneTest.AreEqual(0, selectedItemChangedRaised);
	}

	[PresentationTestMethod]
	public void DoesTheBestItCanWithAutoSelectingViewModel()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

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
		var vm = new SelectionViewModel();

		vm.Items.CollectionChanged += (s, e) =>
		{
			if ((vm.SelectedIndex == -1) && (vm.Items.Count > 0))
			{
				vm.SelectedIndex = 0;
			}
		};

		var target = new ListBox
		{
			[!ListBox.ItemsSourceProperty] = new Binding("Items"),
			[!ListBox.SelectedIndexProperty] = new Binding("SelectedIndex"),
			DataContext = vm
		};

		Prepare(target);

		vm.Items.Add("foo");
		vm.Items.Add("bar");

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { 0 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { "foo" }, target.SelectedItems);
	}

	public static IEnumerable<object[]> GetSelectionFieldPermutationParameters()
	{
		return Enum.GetValues<SelectionField>().Permutations().Select(fields => new object[] { fields });
	}

	[PresentationTestMethod]
	public void HandlesRemovingLastItemInControlsWithBoundSelectedItem()
	{
		var items = new ObservableCollection<string> { "foo" };

		// Simulates problem with TabStrip and Carousel with bound SelectedItem.
		var tabStrip = new TestSelector
		{
			ItemsSource = items,
			SelectionMode = SelectionMode.AlwaysSelected
		};

		var carousel = new TestSelector
		{
			ItemsSource = items,
			[!Carousel.SelectedItemProperty] = tabStrip[!TabStrip.SelectedItemProperty]
		};

		var tabStripRaised = 0;
		var carouselRaised = 0;

		tabStrip.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "foo" }, e.RemovedItems);
			CornerstoneTest.Empty(e.AddedItems);
			++tabStripRaised;
		};

		carousel.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "foo" }, e.RemovedItems);
			CornerstoneTest.Empty(e.AddedItems);
			++carouselRaised;
		};

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(1, tabStripRaised);
		CornerstoneTest.AreEqual(1, carouselRaised);
	}

	[PresentationTestMethod]
	public void HandlesRemovingLastItemInTwoControlsWithBoundSelectedIndex()
	{
		var items = new ObservableCollection<string> { "foo" };

		// Simulates problem with TabStrip and Carousel with bound SelectedIndex.
		var tabStrip = new TestSelector
		{
			ItemsSource = items,
			SelectionMode = SelectionMode.AlwaysSelected
		};

		var carousel = new TestSelector
		{
			ItemsSource = items,
			[!Carousel.SelectedIndexProperty] = tabStrip[!TabStrip.SelectedIndexProperty]
		};

		var tabStripRaised = 0;
		var carouselRaised = 0;

		tabStrip.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "foo" }, e.RemovedItems);
			CornerstoneTest.Empty(e.AddedItems);
			++tabStripRaised;
		};

		carousel.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new[] { "foo" }, e.RemovedItems);
			CornerstoneTest.Empty(e.AddedItems);
			++carouselRaised;
		};

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(1, tabStripRaised);
		CornerstoneTest.AreEqual(1, carouselRaised);
	}

	[PresentationTestMethod]
	public void ItemIsSelectedShouldInitiallyBeFalse()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);

		CornerstoneTest.IsFalse(items[0].IsSelected);
		CornerstoneTest.IsFalse(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void ModeForSelectedIndexIsTwoWayByDefault()
	{
		var items = new[]
		{
			new Item(),
			new Item(),
			new Item()
		};

		var vm = new MasterViewModel
		{
			Child = new ChildViewModel
			{
				Items = items,
				SelectedIndex = 1
			}
		};

		var target = new SelectingItemsControl { DataContext = vm };
		var itemsBinding = new Binding("Child.Items");
		var selectedIndBinding = new Binding("Child.SelectedIndex");

		target.Bind(SelectingItemsControl.ItemsSourceProperty, itemsBinding);
		target.Bind(SelectingItemsControl.SelectedIndexProperty, selectedIndBinding);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		target.SelectedIndex = 2;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual(2, vm.Child.SelectedIndex);
	}

	[PresentationTestMethod(Timeout = 2000)]
	public Task MoveSelectionDoesNotHangWhenAllItemsAreNonFocusableAndWeMoveToFirstItem()
	{
		// Keep the case bounded so a hung dispatcher cannot stall the suite.
		return ThreadRunHelper.RunOnDedicatedThread(() =>
		{
			using var _ = UnitTestApplication.Start();
			var target = new TestSelector
			{
				Template = Template(),
				Items =
				{
					new ListBoxItem { Focusable = false },
					new ListBoxItem { Focusable = false }
				}
			};

			target.Measure(new Size(100, 100));
			target.Arrange(new Rect(0, 0, 100, 100));

			target.MoveSelection(NavigationDirection.First, true);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		});
	}

	[PresentationTestMethod(Timeout = 2000)]
	public Task MoveSelectionDoesNotHangWhenAllItemsAreNonFocusableAndWeMoveToLastItem()
	{
		return ThreadRunHelper.RunOnDedicatedThread(() =>
		{
			using var _ = UnitTestApplication.Start();
			var target = new TestSelector
			{
				Template = Template(),
				Items =
				{
					new ListBoxItem { Focusable = false },
					new ListBoxItem { Focusable = false }
				}
			};

			target.Measure(new Size(100, 100));
			target.Arrange(new Rect(0, 0, 100, 100));

			target.MoveSelection(NavigationDirection.Last, true);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		});
	}

	[PresentationTestMethod]
	public void MoveSelectionDoesSelectDisabledControls()
	{
		// Issue #3426.
		var target = new TestSelector
		{
			Template = Template(),
			Items =
			{
				new ListBoxItem(),
				new ListBoxItem { IsEnabled = false }
			},
			SelectedIndex = 0
		};

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));
		target.MoveSelection(NavigationDirection.Next, true);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void MoveSelectionSkipsNonFocusableControlsWhenMovingToFirstItem()
	{
		var target = new TestSelector
		{
			Template = Template(),
			Items =
			{
				new ListBoxItem { Focusable = false },
				new ListBoxItem()
			}
		};

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));
		target.MoveSelection(NavigationDirection.Last, true);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void MoveSelectionSkipsNonFocusableControlsWhenMovingToLastItem()
	{
		var target = new TestSelector
		{
			Template = Template(),
			Items =
			{
				new ListBoxItem(),
				new ListBoxItem { Focusable = false }
			}
		};

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));
		target.MoveSelection(NavigationDirection.Last, true);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod(Timeout = 2000)]
	public Task MoveSelectionWrapDoesNotHangWithNoFocusableControls()
	{
		// Keep the case bounded so a hung dispatcher cannot stall the suite.
		return ThreadRunHelper.RunOnDedicatedThread(() =>
		{
			using var _ = UnitTestApplication.Start();

			// Issue #3094.
			var target = new TestSelector
			{
				Template = Template(),
				Items =
				{
					new ListBoxItem { Focusable = false },
					new ListBoxItem { Focusable = false }
				},
				SelectedIndex = 0
			};

			target.Measure(new Size(100, 100));
			target.Arrange(new Rect(0, 0, 100, 100));

			target.MoveSelection(NavigationDirection.Next, true);
		});
	}

	[PresentationTestMethod]
	public void MovingSelectedContainerShouldNotClearSelection()
	{
		var items = new OldPresentationList<Item>
		{
			new(),
			new()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		var receivedArgs = new List<SelectionChangedEventArgs>();

		target.SelectionChanged += (_, args) => receivedArgs.Add(args);

		var moved = items[1];
		items.Move(1, 0);

		// Because the moved container is still marked as selected on the insert part of the
		// move, it will remain selected.
		CornerstoneTest.Same(moved, target.SelectedItem);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.AreEqual(2, receivedArgs.Count);
		CornerstoneTest.AreEqual(new[] { moved }, receivedArgs[0].RemovedItems);
		CornerstoneTest.AreEqual(new[] { moved }, receivedArgs[1].AddedItems);
		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsFalse(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void MovingSelectedItemShouldClearSelection()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar" };
		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		SelectionChangedEventArgs receivedArgs = null;

		target.SelectionChanged += (_, args) => receivedArgs = args;

		var removed = items[1];
		items.Move(1, 0);

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.Empty(receivedArgs.AddedItems);
		CornerstoneTest.AreEqual(new[] { removed }, receivedArgs.RemovedItems);
	}

	[PresentationTestMethod]
	public void NestedListBoxDoesNotChangeParentSelectedIndex()
	{
		SelectingItemsControl nested;

		var root = new SelectingItemsControl
		{
			Template = Template(),
			ItemsSource = new Control[]
			{
				new Border(),
				nested = new ListBox
				{
					Template = Template(),
					ItemsSource = new[] { "foo", "bar" },
					SelectedIndex = 1
				}
			},
			SelectedIndex = 0
		};

		root.ApplyTemplate();
		root.Presenter!.ApplyTemplate();
		nested.ApplyTemplate();
		nested.Presenter!.ApplyTemplate();

		CornerstoneTest.AreEqual(0, root.SelectedIndex);
		CornerstoneTest.AreEqual(1, nested.SelectedIndex);

		nested.SelectedIndex = 0;

		CornerstoneTest.AreEqual(0, root.SelectedIndex);
	}

	[PresentationTestMethod]
	public void OrderOfSettingItemsAndSelectedIndexDuringInitializationShouldNotMatter()
	{
		using var app = Start();
		var items = new[] { "Foo", "Bar" };
		var target = new SelectingItemsControl();

		((ISupportInitialize) target).BeginInit();
		target.SelectedIndex = 1;
		target.ItemsSource = items;
		((ISupportInitialize) target).EndInit();

		Prepare(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual("Bar", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void OrderOfSettingItemsAndSelectedItemDuringInitializationShouldNotMatter()
	{
		using var app = Start();
		var items = new[] { "Foo", "Bar" };
		var target = new SelectingItemsControl();

		((ISupportInitialize) target).BeginInit();
		target.SelectedItem = "Bar";
		target.ItemsSource = items;
		((ISupportInitialize) target).EndInit();

		Prepare(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual("Bar", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void PreSelectingItemShouldSetSelectionAfterItWasAddedWhenAlwaysSelected()
	{
		var target = new TestSelector(SelectionMode.AlwaysSelected)
		{
			Template = Template()
		};

		var second = new Item { IsSelected = true };

		var items = new OldPresentationList<object>
		{
			new Item(),
			second
		};

		target.ItemsSource = items;

		Prepare(target);

		CornerstoneTest.AreEqual(second, target.SelectedItem);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void PreservesInitialSelectedItemsWhenBound()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		// Issue #4272 (there are two issues there, this addresses the second one).
		var vm = new SelectionViewModel
		{
			Items = { "foo", "bar", "baz" },
			SelectedItems = { "bar" }
		};

		var target = new ListBox
		{
			[!ListBox.ItemsSourceProperty] = new Binding("Items"),
			[!ListBox.SelectedItemsProperty] = new Binding("SelectedItems"),
			DataContext = vm
		};

		Prepare(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual(new[] { 1 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual("bar", target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { "bar" }, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void PreservesSelectedItemWhenItemsChanged()
	{
		// Issue #4048
		using var app = Start();
		var target = new SelectingItemsControl
		{
			ItemsSource = new[] { "foo", "bar", "baz" },
			SelectedItem = "bar"
		};

		Prepare(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual("bar", target.SelectedItem);

		target.ItemsSource = new[] { "qux", "foo", "bar" };

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual("bar", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void RaisingIsSelectedChangedOnItemShouldUpdateSelection()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedItem = items[1];

		CornerstoneTest.IsFalse(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);

		items[0].IsSelected = true;
		items[0].RaiseEvent(new RoutedEventArgs(SelectingItemsControl.IsSelectedChangedEvent));

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.IsTrue(items[0].IsSelected);
		CornerstoneTest.IsFalse(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void RaisingIsSelectedChangedOnSomeoneElsesItemShouldNotUpdateSelection()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = items[1];

		var notChild = new Item
		{
			IsSelected = true
		};

		target.RaiseEvent(new RoutedEventArgs
		{
			RoutedEvent = SelectingItemsControl.IsSelectedChangedEvent,
			Source = notChild
		});

		CornerstoneTest.AreEqual(target.SelectedItem, items[1]);
	}

	[PresentationTestMethod]
	public void RemovingItemBeforeSelectedItemShouldUpdateSelectedIndex()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectedIndex = 1
		};

		Prepare(target);

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("Bar", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void RemovingItemBeforeSelectionShouldRaisePropertyChangedEvents()
	{
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		var selectedIndexRaised = 0;
		var selectedItemRaised = 0;
		target.SelectedIndex = 1;

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == SelectingItemsControl.SelectedIndexProperty)
			{
				CornerstoneTest.AreEqual(1, e.OldValue);
				CornerstoneTest.AreEqual(0, e.NewValue);
				++selectedIndexRaised;
			}
			else if (e.Property == SelectingItemsControl.SelectedItemProperty)
			{
				++selectedItemRaised;
			}
		};

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(1, selectedIndexRaised);
		CornerstoneTest.AreEqual(0, selectedItemRaised);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItem0ShouldRaisePropertyChangedEventsWithAlwaysSelected()
	{
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template(),
			SelectionMode = SelectionMode.AlwaysSelected
		};

		var selectedIndexRaised = 0;
		var selectedItemRaised = 0;
		target.SelectedIndex = 0;

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == SelectingItemsControl.SelectedIndexProperty)
			{
				++selectedIndexRaised;
			}
			else if (e.Property == SelectingItemsControl.SelectedItemProperty)
			{
				CornerstoneTest.AreEqual("foo", e.OldValue);
				CornerstoneTest.AreEqual("bar", e.NewValue);
				++selectedItemRaised;
			}
		};

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(0, selectedIndexRaised);
		CornerstoneTest.AreEqual(1, selectedItemRaised);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItem1ShouldRaisePropertyChangedEventsWithAlwaysSelected()
	{
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template(),
			SelectionMode = SelectionMode.AlwaysSelected
		};

		var selectedIndexRaised = 0;
		var selectedItemRaised = 0;
		target.SelectedIndex = 1;

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == SelectingItemsControl.SelectedIndexProperty)
			{
				CornerstoneTest.AreEqual(1, e.OldValue);
				CornerstoneTest.AreEqual(0, e.NewValue);
				++selectedIndexRaised;
			}
			else if (e.Property == SelectingItemsControl.SelectedItemProperty)
			{
				CornerstoneTest.AreEqual("bar", e.OldValue);
				CornerstoneTest.AreEqual("foo", e.NewValue);
			}
		};

		items.RemoveAt(1);

		CornerstoneTest.AreEqual(1, selectedIndexRaised);
		CornerstoneTest.AreEqual(0, selectedItemRaised);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemShouldClearSelection()
	{
		var items = new OldPresentationList<Item>
		{
			new(),
			new()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		SelectionChangedEventArgs receivedArgs = null;

		target.SelectionChanged += (_, args) => receivedArgs = args;

		var removed = items[1];

		items.RemoveAt(1);

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.Empty(receivedArgs.AddedItems);
		CornerstoneTest.AreEqual(new[] { removed }, receivedArgs.RemovedItems);
		CornerstoneTest.IsFalse(items.Single().IsSelected);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemShouldClearSelectionWithBeginInit()
	{
		var items = new OldPresentationList<Item>
		{
			new(),
			new()
		};

		var target = new SelectingItemsControl();
		target.BeginInit();
		target.ItemsSource = items;
		target.Template = Template();
		target.EndInit();

		Prepare(target);
		target.SelectedIndex = 0;

		CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		SelectionChangedEventArgs receivedArgs = null;

		target.SelectionChanged += (_, args) => receivedArgs = args;

		var removed = items[0];

		items.RemoveAt(0);

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.Empty(receivedArgs.AddedItems);
		CornerstoneTest.AreEqual(new[] { removed }, receivedArgs.RemovedItems);
		CornerstoneTest.IsFalse(items.Single().IsSelected);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemShouldClearTabOnceActiveElement()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new ObservableCollection<string>(new[] { "Foo", "Bar", "Baz " });

			var target = new ListBox
			{
				Template = Template(),
				ItemsSource = items
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);

			var panel = target.Presenter!.Panel!;
			_helper.Down(panel.Children[1]);

			items.RemoveAt(1);

			CornerstoneTest.IsNull(KeyboardNavigation.GetTabOnceActiveElement(panel));
		}
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemShouldRaisePropertyChangedEvents()
	{
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		var selectedIndexRaised = 0;
		var selectedItemRaised = 0;
		target.SelectedIndex = 1;

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == SelectingItemsControl.SelectedIndexProperty)
			{
				CornerstoneTest.AreEqual(1, e.OldValue);
				CornerstoneTest.AreEqual(-1, e.NewValue);
				++selectedIndexRaised;
			}
			else if (e.Property == SelectingItemsControl.SelectedItemProperty)
			{
				CornerstoneTest.AreEqual("bar", e.OldValue);
				CornerstoneTest.IsNull(e.NewValue);
			}
		};

		items.RemoveAt(1);

		CornerstoneTest.AreEqual(1, selectedIndexRaised);
		CornerstoneTest.AreEqual(0, selectedItemRaised);
	}

	[PresentationTestMethod]
	public void RemovingSelectedItemShouldUpdateSelectionWithAlwaysSelected()
	{
		var item0 = new Item();
		var item1 = new Item();
		var items = new OldPresentationList<Item>
		{
			item0,
			item1
		};

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template(),
			SelectionMode = SelectionMode.AlwaysSelected
		};

		Prepare(target);
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		SelectionChangedEventArgs receivedArgs = null;

		target.SelectionChanged += (_, args) => receivedArgs = args;

		items.RemoveAt(1);

		CornerstoneTest.Same(item0, target.SelectedItem);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.AreEqual(new[] { item0 }, receivedArgs.AddedItems);
		CornerstoneTest.AreEqual(new[] { item1 }, receivedArgs.RemovedItems);
		CornerstoneTest.IsTrue(items.Single().IsSelected);
	}

	[PresentationTestMethod]
	public void ReplacingSelectedItemShouldClearSelection()
	{
		var items = new OldPresentationList<Item>
		{
			new(),
			new()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		SelectionChangedEventArgs receivedArgs = null;

		target.SelectionChanged += (_, args) => receivedArgs = args;

		var removed = items[1];
		items[1] = new Item();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNotNull(receivedArgs);
		CornerstoneTest.Empty(receivedArgs.AddedItems);
		CornerstoneTest.AreEqual(new[] { removed }, receivedArgs.RemovedItems);
		CornerstoneTest.All(items, x => CornerstoneTest.IsFalse(x.IsSelected));
	}

	[PresentationTestMethod]
	public void ReplacingSelectedItemShouldUpdateSelectedItem()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"Baz"
		};

		var target = new ListBox
		{
			Template = Template(),
			ItemsSource = items,
			SelectedIndex = 1
		};

		Prepare(target);

		items[1] = "Qux";

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void ResettingItemsCollectionShouldClearSelection()
	{
		// Need to use ObservableCollection here as OldPresentationList signals a Clear as an
		// add + remove.
		var items = new ObservableCollection<Item>
		{
			new(),
			new()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		items.Clear();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void ResettingItemsCollectionShouldRaiseSelectionChanged()
	{
		var items = new ObservableCollection<Item>
		{
			new(),
			new(),
			new()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);
		target.SelectedIndex = 1;

		var selectedItem = items[1];

		var receivedArgs = new List<SelectionChangedEventArgs>();
		target.SelectionChanged += (_, args) => receivedArgs.Add(args);

		items.Clear();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.Single(receivedArgs);
		CornerstoneTest.Empty(receivedArgs[0].AddedItems);
		CornerstoneTest.AreEqual(new[] { selectedItem }, receivedArgs[0].RemovedItems);
	}

	[PresentationTestMethod]
	public void ResettingItemsCollectionShouldRetainSelection()
	{
		var itemsMock = new ResettableList { "Foo", "Bar", "Baz" };
		var target = new SelectingItemsControl
		{
			ItemsSource = itemsMock
		};

		target.SelectedIndex = 1;

		itemsMock.RaiseReset();

		CornerstoneTest.IsTrue(target.SelectedIndex == 1);
	}

	[PresentationTestMethod]
	public void ResettingItemsToEmptyWithMultipleSelectionShouldRaiseSelectionChanged()
	{
		var items = new ObservableCollection<Item>
		{
			new(),
			new(),
			new()
		};

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template(),
			SelectionMode = SelectionMode.Multiple
		};

		Prepare(target);
		target.SelectedIndex = 0;
		target.Selection.Select(2);

		var selected0 = items[0];
		var selected2 = items[2];

		var receivedArgs = new List<SelectionChangedEventArgs>();
		target.SelectionChanged += (_, args) => receivedArgs.Add(args);

		items.Clear();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.Single(receivedArgs);
		CornerstoneTest.Empty(receivedArgs[0].AddedItems);
		CornerstoneTest.AreEqual(2, receivedArgs[0].RemovedItems.Count);
		CornerstoneTest.Contains(receivedArgs[0].RemovedItems.Cast<object>(), selected0);
		CornerstoneTest.Contains(receivedArgs[0].RemovedItems.Cast<object>(), selected2);
	}

	[PresentationTestMethod]
	public void ResettingItemsWithPreservedSelectionShouldNotReportDeselection()
	{
		var items = new ResettingCollection(3);

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 1;

		var receivedArgs = new List<SelectionChangedEventArgs>();
		target.SelectionChanged += (_, args) => receivedArgs.Add(args);

		items.Reset(new[] { "Item2", "Item0", "Item1" });

		CornerstoneTest.AreEqual("Item1", target.SelectedItem);
		CornerstoneTest.Single(receivedArgs);
		CornerstoneTest.Empty(receivedArgs[0].RemovedItems);
	}

	[PresentationTestMethod]
	[TestData(nameof(GetSelectionFieldPermutationParameters))]
	public void SelectedIndexAndSelectionPropertiesWorkInAnyOrderWhenInitializing(SelectionField[] fields)
	{
		TestSelectionFields(vm => vm.SelectedIndex = 2, fields);
	}

	[PresentationTestMethod]
	public void SelectedIndexCanAccessSelectionDuringInit()
	{
		using var _ = Start();

		var target = new ListBox();
		target.BeginInit();

		target.Selection = new SelectionModel<ItemModel>
		{
			SelectedIndex = 42
		};

		CornerstoneTest.AreEqual(42, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexItemIsUpdatedAsItemsRemovedWhenLastItemIsSelected()
	{
		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"FooBar"
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = items[2];

		CornerstoneTest.AreEqual(items[2], target.SelectedItem);
		CornerstoneTest.AreEqual(2, target.SelectedIndex);

		items.RemoveAt(0);

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexShouldBe0AfterInitializeWithAlwaysSelected()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new ListBox();
		target.BeginInit();
		target.SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected;
		target.ItemsSource = items;
		target.Template = Template();
		target.EndInit();

		Prepare(target);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexShouldBeMinus1AfterInitialize()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new ListBox();
		target.BeginInit();
		target.ItemsSource = items;
		target.Template = Template();
		target.EndInit();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexShouldBeMinus1WithoutInitialize()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new ListBox();
		target.ItemsSource = items;
		target.Template = Template();
		target.DataContext = new object();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexShouldInitiallyBeMinus1()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	[TestData(nameof(GetSelectionFieldPermutationParameters))]
	public void SelectedItemAndSelectionPropertiesWorkInAnyOrderWhenInitializing(SelectionField[] fields)
	{
		TestSelectionFields(vm => vm.SelectedItem = vm.Items[2], fields);
	}

	[PresentationTestMethod]
	public void SelectedItemCanAccessSelectionDuringInit()
	{
		using var _ = Start();

		var target = new ListBox();
		target.BeginInit();

		var item = new ItemModel();

		target.Selection = new SelectionModel<ItemModel>
		{
			SelectedItem = item
		};

		CornerstoneTest.AreEqual(item, target.SelectedItem);
	}

	[PresentationTestMethod]
	[TestData(nameof(GetSelectionFieldPermutationParameters))]
	public void SelectedValueAndSelectionPropertiesWorkInAnyOrderWhenInitializing(SelectionField[] fields)
	{
		TestSelectionFields(vm => vm.SelectedValue = 12, fields);
	}

	[PresentationTestMethod]
	public void SettingIsTextSearchEnabledEnablesOrDisablesTextSearch()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow.With()))
		{
			var items = new[]
			{
				new Item { [TextSearch.TextProperty] = "Foo" },
				new Item { [TextSearch.TextProperty] = "Bar" }
			};

			var target = new SelectingItemsControl
			{
				ItemsSource = items,
				Template = Template(),
				IsTextSearchEnabled = false
			};

			Prepare(target);

			target.RaiseEvent(new TextInputEventArgs
			{
				RoutedEvent = InputElement.TextInputEvent,
				Text = "Foo"
			});

			CornerstoneTest.IsNull(target.SelectedItem);

			target.IsTextSearchEnabled = true;

			target.RaiseEvent(new TextInputEventArgs
			{
				RoutedEvent = InputElement.TextInputEvent,
				Text = "Foo"
			});

			CornerstoneTest.AreEqual(items[0], target.SelectedItem);
		}
	}

	[PresentationTestMethod]
	public void SettingItemsToNullShouldClearSelection()
	{
		var items = new OldPresentationList<Item>
		{
			new(),
			new()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		target.ItemsSource = null;

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexBeforeApplyTemplateShouldSetItemIsSelectedTrue()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.SelectedIndex = 1;
		Prepare(target);

		CornerstoneTest.IsFalse(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexBeforeInitializeShouldRetainSelection()
	{
		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "foo", "bar", "baz" },
			SelectedIndex = 1
		};

		listBox.BeginInit();

		listBox.EndInit();

		CornerstoneTest.AreEqual(1, listBox.SelectedIndex);
		CornerstoneTest.AreEqual("bar", listBox.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexBeforeInitializeWithAlwaysSelectedShouldRetainSelection()
	{
		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected,

			ItemsSource = new[] { "foo", "bar", "baz" },
			SelectedIndex = 1
		};

		listBox.BeginInit();

		listBox.EndInit();

		CornerstoneTest.AreEqual(1, listBox.SelectedIndex);
		CornerstoneTest.AreEqual("bar", listBox.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexDuringInitializeShouldSelectItemWhenAlwaysSelectedIsUsed()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected
		};

		listBox.BeginInit();

		listBox.SelectedIndex = 1;
		var items = new OldPresentationList<string>();
		listBox.ItemsSource = items;
		items.Add("A");
		items.Add("B");
		items.Add("C");

		listBox.EndInit();

		Prepare(listBox);

		CornerstoneTest.AreEqual("B", listBox.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexDuringInitializeShouldTakePriorityOverPreviousValue()
	{
		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "foo", "bar", "baz" },
			SelectedIndex = 2
		};

		listBox.BeginInit();

		listBox.SelectedIndex = 1;

		listBox.EndInit();

		CornerstoneTest.AreEqual(1, listBox.SelectedIndex);
		CornerstoneTest.AreEqual("bar", listBox.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexOutOfBoundsWithItemsSourceShouldClearSelection()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 2;

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexOutOfBoundsWithoutItemsSourceShouldKeepSelectionUntilItemsSourceIsSet()
	{
		var target = new SelectingItemsControl
		{
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 2;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);

		target.ItemsSource = Array.Empty<Item>();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexShouldRaisePropertyChangedEvents()
	{
		var items = new ObservableCollection<string> { "foo", "bar", "baz" };

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		var selectedIndexRaised = 0;
		var selectedItemRaised = 0;

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == SelectingItemsControl.SelectedIndexProperty)
			{
				CornerstoneTest.AreEqual(-1, e.OldValue);
				CornerstoneTest.AreEqual(1, e.NewValue);
				++selectedIndexRaised;
			}
			else if (e.Property == SelectingItemsControl.SelectedItemProperty)
			{
				CornerstoneTest.IsNull(e.OldValue);
				CornerstoneTest.AreEqual("bar", e.NewValue);
				++selectedItemRaised;
			}
		};

		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(1, selectedIndexRaised);
		CornerstoneTest.AreEqual(1, selectedItemRaised);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexShouldRaiseSelectionChangedEvent()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		var called = false;

		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.Same(items[1], e.AddedItems.Cast<object>().Single());
			CornerstoneTest.Empty(e.RemovedItems);
			called = true;
		};

		target.SelectedIndex = 1;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexShouldSetSelectedItem()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexWithoutItemsSourceShouldKeepSelectionIfIndexExistsWhenItemsSourceIsSet()
	{
		var target = new SelectingItemsControl
		{
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 2;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);

		var items = new Item[] { new(), new(), new(), new() };
		target.ItemsSource = items;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.Same(items[2], target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemBeforeApplyTemplateShouldSetItemIsSelectedTrue()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.SelectedItem = items[1];
		Prepare(target);

		CornerstoneTest.IsFalse(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemBeforeInitializeShouldRetainSelection()
	{
		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "foo", "bar", "baz" },
			SelectedItem = "bar"
		};

		listBox.BeginInit();

		listBox.EndInit();

		CornerstoneTest.AreEqual(1, listBox.SelectedIndex);
		CornerstoneTest.AreEqual("bar", listBox.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemShouldSetItemIsSelectedTrue()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		Prepare(target);

		target.SelectedItem = items[1];

		CornerstoneTest.IsFalse(items[0].IsSelected);
		CornerstoneTest.IsTrue(items[1].IsSelected);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemShouldSetSelectedIndex()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = items[1];

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemToNonExistentItemWithItemsSourceShouldClearSelection()
	{
		var target = new SelectingItemsControl
		{
			ItemsSource = Array.Empty<Item>(),
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = new Item();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemToNonExistentItemWithoutItemsSourceShouldKeepSelectionUntilItemsSourceIsSet()
	{
		var item = new Item();

		var target = new SelectingItemsControl
		{
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = item;

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.Same(item, target.SelectedItem);

		target.ItemsSource = Array.Empty<Item>();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemToNotPresentItemShouldClearSelection()
	{
		var items = new[]
		{
			new Item(),
			new Item()
		};

		var target = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = items[1];

		CornerstoneTest.AreEqual(items[1], target.SelectedItem);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		target.SelectedItem = new Item();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemWithPointerShouldSetTabOnceActiveElement()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = Template(),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);

			var container = target.ContainerFromIndex(1)!;
			_helper.Down(container);

			var panel = target.Presenter!.Panel;

			CornerstoneTest.Same(container, KeyboardNavigation.GetTabOnceActiveElement(target));
		}
	}

	[PresentationTestMethod]
	public void SettingSelectedItemWithoutItemsSourceShouldKeepSelectionIfItemExistsWhenItemsSourceIsSet()
	{
		var item = new Item();

		var target = new SelectingItemsControl
		{
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedItem = item;

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.Same(item, target.SelectedItem);

		target.ItemsSource = new[] { new(), new(), item, new() };

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.Same(item, target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemsBeforeInitializeShouldRetainSelection()
	{
		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Multiple,
			ItemsSource = new[] { "foo", "bar", "baz" }
		};

		var selected = new[] { "foo", "bar" };

		foreach (var v in selected)
		{
			listBox.SelectedItems!.Add(v);
		}

		listBox.BeginInit();

		listBox.EndInit();

		CornerstoneTest.AreEqual(selected, listBox.SelectedItems);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemsDuringInitializeShouldTakePriorityOverPreviousValue()
	{
		var listBox = new ListBox
		{
			SelectionMode = SelectionMode.Multiple,
			ItemsSource = new[] { "foo", "bar", "baz" }
		};

		var selected = new[] { "foo", "bar" };

		foreach (var v in new[] { "bar", "baz" })
		{
			listBox.SelectedItems!.Add(v);
		}

		listBox.BeginInit();

		listBox.SelectedItems = new OldPresentationList<object>(selected);

		listBox.EndInit();

		CornerstoneTest.AreEqual(selected, listBox.SelectedItems);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemsRaisesPropertyChanged()
	{
		using var app = Start();
		var target = new TestSelector
		{
			ItemsSource = new[] { "foo", "bar", "baz" }
		};

		var raised = 0;
		var newValue = new OldPresentationList<object>();

		Prepare(target);

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == ListBox.SelectedItemsProperty)
			{
				CornerstoneTest.IsNull(e.OldValue);
				CornerstoneTest.Same(newValue, e.NewValue);
				++raised;
			}
		};

		target.SelectedItems = newValue;

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void SettingSelectionModeShouldUpdateSelectionModel()
	{
		var target = new TestSelector();
		var model = target.Selection;

		CornerstoneTest.IsTrue(model.SingleSelect);

		target.SelectionMode = SelectionMode.Multiple;

		CornerstoneTest.IsFalse(model.SingleSelect);
	}

	[PresentationTestMethod]
	public void SettingSelectionRaisesSelectedItemsPropertyChanged()
	{
		using var app = Start();
		var target = new TestSelector
		{
			ItemsSource = new[] { "foo", "bar", "baz" }
		};

		var raised = 0;
		var oldValue = target.SelectedItems;

		Prepare(target);

		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == ListBox.SelectedItemsProperty)
			{
				CornerstoneTest.Same(oldValue, e.OldValue);
				CornerstoneTest.IsNull(e.NewValue);
				++raised;
			}
		};

		target.Selection = new SelectionModel<int>();

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ShouldApplySelectedPseudoclassToCorrectItemWhenDuplicateItemsArePresent()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = Template(),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			_helper.Down(target.Presenter!.Panel!.Children[3]);

			CornerstoneTest.AreEqual(new[] { ":pressed", ":selected" }, target.Presenter.Panel.Children[3].Classes);
		}
	}

	[PresentationTestMethod]
	public void ShouldFirstRaisePropertyChangedNotificationThenFireSelectionChangedEvent()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		// Issue #11006
		var items = new ObservableCollection<string>();

		var vm = new SelectionViewModel
		{
			SelectedItem = ""
		};

		var theListBox = new ListBox
		{
			DataContext = vm,
			Template = Template(),
			ItemsSource = items,
			SelectionMode = SelectionMode.AlwaysSelected,
			[!ListBox.SelectedItemProperty] = new Binding("SelectedItem")
		};

		var target = new TextBox
		{
			Text = ""
		};

		Prepare(theListBox);

		items.Add("Default");
		items.Add("First");
		items.Add("Second");
		items.Add("Third");

		theListBox.SelectionChanged += (s, e) => { target.Text = (string) vm.SelectedItem; };

		theListBox.SelectedIndex = 1;
		CornerstoneTest.AreEqual("First", target.Text);

		theListBox.SelectedIndex = 2;
		CornerstoneTest.AreEqual("Second", target.Text);

		theListBox.SelectedIndex = 3;
		CornerstoneTest.AreEqual("Third", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldSelectCorrectItemWhenDuplicateItemsArePresent()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = Template(),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			_helper.Down(target.Presenter!.Panel!.Children[3]);

			CornerstoneTest.AreEqual(3, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void TabOnceActiveElementShouldBeInitializedWithSelectedItem()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = Template(),
				ItemsSource = new[] { "Foo", "Bar", "Baz " },
				SelectedIndex = 1
			};

			Prepare(target);

			var container = target.ContainerFromIndex(1)!;
			CornerstoneTest.Same(container, KeyboardNavigation.GetTabOnceActiveElement(target));
		}
	}

	private static void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static void Prepare(SelectingItemsControl target)
	{
		var root = new TestRoot
		{
			Child = target,
			Width = 100,
			Height = 100,
			Styles =
			{
				new Style(x => x.Is<SelectingItemsControl>())
				{
					Setters =
					{
						new Setter(ListBox.TemplateProperty, Template())
					}
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.StyledWindow);
	}

	private static FuncControlTemplate Template()
	{
		return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
			new ItemsPresenter
			{
				Name = "itemsPresenter",
				[~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty]
			}.RegisterInNameScope(scope));
	}

	private void TestSelectionFields(Action<FullSelectionViewModel> setItem2, SelectionField[] fields)
	{
		using var _ = Start();

		var vm = new FullSelectionViewModel
		{
			Items =
			{
				new ItemModel { Id = 10, Name = "Item0" },
				new ItemModel { Id = 11, Name = "Item1" },
				new ItemModel { Id = 12, Name = "Item2" },
				new ItemModel { Id = 13, Name = "Item3" }
			}
		};

		setItem2(vm);

		var root = new TestRoot
		{
			Width = 100,
			Height = 100
		};

		// Match the Begin/EndInit sequence emitted by the XAML compiler
		root.BeginInit();
		var target = new ListBox();
		target.BeginInit();
		root.Child = target;
		target.DataContext = vm;

		foreach (var field in fields)
		{
			switch (field)
			{
				case SelectionField.ItemsSource:
					target.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(FullSelectionViewModel.Items)));
					break;
				case SelectionField.SelectedItem:
					target.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(FullSelectionViewModel.SelectedItem)));
					break;
				case SelectionField.SelectedIndex:
					target.Bind(SelectingItemsControl.SelectedIndexProperty, new Binding(nameof(FullSelectionViewModel.SelectedIndex)));
					break;
				case SelectionField.SelectedValue:
					target.Bind(SelectingItemsControl.SelectedValueProperty, new Binding(nameof(FullSelectionViewModel.SelectedValue)));
					break;
				case SelectionField.SelectedValueBinding:
					target.SelectedValueBinding = new Binding(nameof(ItemModel.Id));
					break;
				default:
					throw new InvalidOperationException($"Unknown field {field}");
			}
		}

		target.EndInit();
		root.EndInit();

		CornerstoneTest.AreEqual(vm.Items[2], target.SelectedItem);
		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual(12, target.SelectedValue);

		CornerstoneTest.AreEqual(vm.Items[2], vm.SelectedItem);
		CornerstoneTest.AreEqual(2, vm.SelectedIndex);
		CornerstoneTest.AreEqual(12, vm.SelectedValue);
	}

	#endregion

	#region Classes

	private class ChildViewModel : NotifyingBase
	{
		#region Properties

		public IList<Item> Items { get; set; } = [];
		public int SelectedIndex { get; set; }
		public Item SelectedItem { get; set; }

		#endregion
	}

	private sealed class FullSelectionViewModel : NotifyingBase
	{
		#region Fields

		private int _selectedIndex = -1;
		private ItemModel _selectedItem;
		private int? _selectedValue;

		#endregion

		#region Properties

		public ObservableCollection<ItemModel> Items { get; } = new();

		public int SelectedIndex
		{
			get => _selectedIndex;
			set => SetField(ref _selectedIndex, value);
		}

		public ItemModel SelectedItem
		{
			get => _selectedItem;
			set => SetField(ref _selectedItem, value);
		}

		public int? SelectedValue
		{
			get => _selectedValue;
			set => SetField(ref _selectedValue, value);
		}

		#endregion
	}

	private class Item : Control, ISelectable
	{
		#region Properties

		public bool IsSelected
		{
			get => SelectingItemsControl.GetIsSelected(this);
			set => SelectingItemsControl.SetIsSelected(this, value);
		}

		public string Value { get; set; }

		#endregion
	}

	private sealed class ItemModel : NotifyingBase
	{
		#region Fields

		private int _id;
		private string _name;

		#endregion

		#region Properties

		public int Id
		{
			get => _id;
			set => SetField(ref _id, value);
		}

		public string Name
		{
			get => _name;
			set => SetField(ref _name, value);
		}

		#endregion
	}

	private class MasterViewModel : NotifyingBase
	{
		#region Fields

		private ChildViewModel _child;

		#endregion

		#region Properties

		public ChildViewModel Child
		{
			get => _child;
			set
			{
				_child = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	private sealed class ResettableList : List<string>, INotifyCollectionChanged
	{
		#region Methods

		public void RaiseReset()
		{
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion
	}

	private class ResettingCollection : List<string>, INotifyCollectionChanged
	{
		#region Constructors

		public ResettingCollection(int itemCount)
		{
			AddRange(Enumerable.Range(0, itemCount).Select(x => $"Item{x}"));
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

	private class RootWithItems : TestRoot
	{
		#region Properties

		public List<string> Items { get; set; } = new() { "a", "b", "c", "d", "e" };
		public string Selected { get; set; } = "b";

		#endregion
	}

	private class SelectionViewModel : NotifyingBase
	{
		#region Fields

		private int _selectedIndex = -1;
		private object _selectedItem;

		#endregion

		#region Constructors

		public SelectionViewModel()
		{
			Items = new ObservableCollection<string>();
			SelectedItems = new ObservableCollection<string>();
		}

		#endregion

		#region Properties

		public ObservableCollection<string> Items { get; }

		public int SelectedIndex
		{
			get => _selectedIndex;
			set
			{
				_selectedIndex = value;
				RaisePropertyChanged();
			}
		}

		public object SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<string> SelectedItems { get; }

		#endregion
	}

	private class TestSelector : SelectingItemsControl
	{
		#region Fields

		public new static readonly DirectProperty<SelectingItemsControl, IList> SelectedItemsProperty =
			SelectingItemsControl.SelectedItemsProperty;

		#endregion

		#region Constructors

		public TestSelector()
		{
		}

		public TestSelector(SelectionMode selectionMode)
		{
			SelectionMode = selectionMode;
		}

		#endregion

		#region Properties

		public new IList SelectedItems
		{
			get => base.SelectedItems;
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

		public bool MoveSelection(NavigationDirection direction, bool wrap)
		{
			return base.MoveSelection(direction, wrap);
		}

		#endregion
	}

	#endregion

	#region Enumerations

	public enum SelectionField
	{
		ItemsSource,
		SelectedItem,
		SelectedIndex,
		SelectedValue,
		SelectedValueBinding
	}

	#endregion
}