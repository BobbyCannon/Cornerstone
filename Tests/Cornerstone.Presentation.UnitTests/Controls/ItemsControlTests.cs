#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ItemsControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddedContainerShouldHaveLogicalParentSetToItemsControl()
	{
		using var app = Start();
		var items = new ObservableCollection<Border>();
		var target = CreateTarget(itemsSource: items);

		var item = new Border();
		items.Add(item);

		CornerstoneTest.AreEqual(target, item.Parent);
	}

	[PresentationTestMethod]
	public void AddingControlItemShouldMakeControlAppearInLogicalChildren()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child }, performLayout: false);

		// Should appear both before and after applying template.
		CornerstoneTest.AreEqual(new ILogical[] { child }, target.GetLogicalChildren());

		Layout(target);

		CornerstoneTest.AreEqual(new ILogical[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void AddingItemsShouldFireLogicalChildrenCollectionChanged()
	{
		using var app = Start();
		var target = CreateTarget();
		var called = false;

		target.Template = CreateItemsControlTemplate();
		target.ApplyTemplate();

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Add;

		var child = new Control();
		target.Items.Add(child);

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void AddingStringItemShouldMakeContentPresenterAppearInLogicalChildren()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "Foo " });
		var logical = (ILogical) target;

		CornerstoneTest.AreEqual(1, logical.LogicalChildren.Count);
		CornerstoneTest.IsType<ContentPresenter>(logical.LogicalChildren[0]);
	}

	[PresentationTestMethod]
	public void AssigningItemsSourceShouldNotFireLogicalChildrenCollectionChangedBeforeApplyTemplate()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(itemsSource: new[] { child }, performLayout: false);
		var called = false;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) => called = true;

		var list = new OldPresentationList<Control>(child);
		target.ItemsSource = list;

		CornerstoneTest.IsFalse(called);
	}

	[PresentationTestMethod]
	public void CannotModifyItemsWhenItemsSourceSet()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: Array.Empty<string>());

		Assert.Throws<InvalidOperationException>(() => target.Items.Add("foo"));
	}

	[PresentationTestMethod]
	public void CannotSetBothDisplayMemberBindingAndItemTemplate1()
	{
		using var app = Start();
		var target = CreateTarget(
			displayMemberBinding: new Binding("Length"));

		Assert.Throws<InvalidOperationException>(() =>
			target.ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock()));
	}

	[PresentationTestMethod]
	public void CannotSetBothDisplayMemberBindingAndItemTemplate2()
	{
		using var app = Start();
		var target = CreateTarget(
			itemTemplate: new FuncDataTemplate<string>((_, _) => new TextBlock()));

		Assert.Throws<InvalidOperationException>(() => target.DisplayMemberBinding = new Binding("Length"));
	}

	[PresentationTestMethod]
	public void CannotSetItemsSourceWithItemsPresent()
	{
		using var app = Start();
		var target = CreateTarget();
		target.Items.Add("foo");

		Assert.Throws<InvalidOperationException>(() => target.ItemsSource = new[] { "baz" });
	}

	[PresentationTestMethod]
	public void ChangingItemsSourceShouldNotFireLogicalChildrenCollectionChangedBeforeApplyTemplate()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(itemsSource: new[] { child }, performLayout: false);
		var called = false;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) => called = true;

		var list = new OldPresentationList<Control>();
		target.ItemsSource = list;
		list.Add(child);

		CornerstoneTest.IsFalse(called);
	}

	[PresentationTestMethod]
	public void ClearingItemsShouldClearChildControlsParent()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child });

		target.Items.Clear();

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(((ILogical) child).LogicalParent);
	}

	[PresentationTestMethod]
	public void ClearingItemsShouldClearChildControlsParentBeforeApplyTemplate()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child }, performLayout: false);

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		CornerstoneTest.Empty(target.GetVisualChildren());
		CornerstoneTest.Single(target.GetLogicalChildren());

		target.Items.Clear();

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(child.GetLogicalParent());
	}

	[PresentationTestMethod]
	public void ClearingItemsShouldFireLogicalChildrenCollectionChanged()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child });
		var called = false;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Remove;

		target.Items.Clear();

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ContainerClearingIsRaisedWhenItemRemoved()
	{
		using var app = Start();
		var target = CreateTarget(items: new[] { "Foo", "Bar", "Baz" });
		var expected = target.ContainerFromIndex(1);
		var raised = 0;

		target.ContainerClearing += (s, e) =>
		{
			CornerstoneTest.Same(expected, e.Container);
			++raised;
		};

		target.Items.RemoveAt(1);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ContainerClearingIsRaisedWhenItemsSourceIsCleared()
	{
		using var app = Start();
		var itemsSource = new ObservableCollection<object> { "Foo", "Bar", "Baz" };
		var target = CreateTarget(itemsSource: itemsSource);

		var expectedContainers = itemsSource.Select(x => target.ContainerFromItem(x)).ToArray();
		var actualContainers = new List<Control>();
		var raised = 0;

		target.ContainerClearing += (s, e) =>
		{
			actualContainers.Add(e.Container);
			++raised;
		};

		itemsSource.Clear();

		CornerstoneTest.AreEqual(3, raised);
		CornerstoneTest.AreEqual(expectedContainers, actualContainers);
	}

	[PresentationTestMethod]
	public void ContainerIndexChangedIsRaisedWhenItemAdded()
	{
		using var app = Start();
		var target = CreateTarget(items: new[] { "Foo", "Bar", "Baz" });
		var result = new List<Control>();
		var index = 1;

		target.ContainerIndexChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.OldIndex);
			CornerstoneTest.AreEqual(index, e.NewIndex);
			result.Add(e.Container);
		};

		target.Items.Insert(1, "Qux");

		CornerstoneTest.AreEqual(2, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers().Skip(2), result);
	}

	[PresentationTestMethod]
	public void ContainerPreparedIsRaisedForEachControlItemContainer()
	{
		using var app = Start();
		var items = new OldPresentationList<string>();
		var target = CreateTarget();
		var result = new List<Control>();
		var index = 0;

		target.ContainerPrepared += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.Index);
			result.Add(e.Container);
		};

		target.Items.Add(new Button());
		target.Items.Add(new Button());
		target.Items.Add(new Button());

		CornerstoneTest.AreEqual(3, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers(), result);
	}

	[PresentationTestMethod]
	public void ContainerPreparedIsRaisedForEachItemContainer()
	{
		using var app = Start();
		var items = new OldPresentationList<string>();
		var target = CreateTarget();
		var result = new List<Control>();
		var index = 0;

		target.ContainerPrepared += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.Index);
			result.Add(e.Container);
		};

		target.Items.Add("Foo");
		target.Items.Add("Bar");
		target.Items.Add("Baz");

		CornerstoneTest.AreEqual(3, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers(), result);
	}

	[PresentationTestMethod]
	public void ContainerPreparedIsRaisedForEachItemsSourceItemContainerOnLayout()
	{
		using var app = Start();
		var items = new OldPresentationList<string>();
		var target = CreateTarget(itemsSource: items);
		var result = new List<Control>();
		var index = 0;

		target.ContainerPrepared += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.Index);
			result.Add(e.Container);
		};

		items.AddRange(new[] { "Foo", "Bar", "Baz" });

		CornerstoneTest.AreEqual(3, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers(), result);
	}

	[PresentationTestMethod]
	public void ContainerShouldHaveLogicalParentSetToItemsControl()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = new ItemsControl();
		var root = CreateRoot(target);
		var templatedParent = new Button();

		target.TemplatedParent = templatedParent;
		target.Template = CreateItemsControlTemplate();
		target.ItemsSource = new[] { "Foo" };

		root.LayoutManager.ExecuteInitialLayoutPass();

		var container = GetContainer(target);

		CornerstoneTest.AreEqual(target, container.Parent);
	}

	[PresentationTestMethod]
	public void ContainerShouldHaveTemplatedParentSetToNull()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "Foo" });

		var container = GetContainer(target);

		CornerstoneTest.IsNull(container.TemplatedParent);
	}

	[PresentationTestMethod]
	public void ContainerShouldHaveThemeSetToItemContainerTheme()
	{
		using var app = Start();
		var theme = new ControlTheme { TargetType = typeof(ContentPresenter) };
		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemContainerTheme: theme);

		var container = GetContainer(target);

		CornerstoneTest.Same(container.Theme, theme);
	}

	[PresentationTestMethod]
	public void ContainerShouldHaveThemeSetToItemContainerThemeWithBaseTargetType()
	{
		using var app = Start();
		var theme = new ControlTheme { TargetType = typeof(Control) };
		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemContainerTheme: theme);

		var container = GetContainer(target);

		CornerstoneTest.Same(container.Theme, theme);
	}

	[PresentationTestMethod]
	public void ControlItemCanBeRemovedFromLogicalChildrenBeforeApplyTemplate()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child }, performLayout: false);

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		CornerstoneTest.Empty(target.GetVisualChildren());
		CornerstoneTest.Single(target.GetLogicalChildren());

		target.Items.RemoveAt(0);

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(child.GetLogicalParent());
		CornerstoneTest.Empty(target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ControlItemShouldBeLogicalChildAfterLayout()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child });

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.Single(target.GetVisualChildren());
		CornerstoneTest.AreEqual(target, child.Parent);
		CornerstoneTest.AreEqual(target, child.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ControlItemShouldBeLogicalChildBeforeApplyTemplate()
	{
		using var app = Start();
		var child = new Control();
		var target = CreateTarget(items: new[] { child }, performLayout: false);

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		CornerstoneTest.Empty(target.GetVisualChildren());
		CornerstoneTest.AreEqual(child.Parent, target);
		CornerstoneTest.AreEqual(child.GetLogicalParent(), target);
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ControlItemShouldBeRemovedFromLogicalChildren()
	{
		using var app = Start();
		var item = new Border();

		var items = new ObservableCollection<Control>();
		var target = CreateTarget(itemsSource: items);

		items.Add(item);
		items.Remove(item);

		CornerstoneTest.Empty(target.LogicalChildren);
	}

	[PresentationTestMethod]
	public void ControlItemShouldBeRemovedFromLogicalChildrenVirtualizing()
	{
		using var app = Start();
		var item = new Border();

		var items = new ObservableCollection<Control>();
		var itemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel());
		var target = CreateTarget(
			itemsPanel: itemsPanel,
			itemsSource: items);

		items.Add(item);
		Layout(target);

		items.Remove(item);

		CornerstoneTest.Empty(target.LogicalChildren);
	}

	[PresentationTestMethod]
	public void ControlItemShouldNotBeNameScope()
	{
		using var app = Start();
		var items = new object[] { new TextBlock() };
		var target = CreateTarget(itemsSource: items);
		var item = target.LogicalChildren[0];

		CornerstoneTest.IsNull(NameScope.GetNameScope((TextBlock) item));
	}

	[PresentationTestMethod]
	public void DataContextsShouldBeCorrectlySet()
	{
		using var app = Start();
		var items = new object[]
		{
			"Foo",
			new Item("Bar"),
			new TextBlock { Text = "Baz" },
			new ListBoxItem { Content = "Qux" }
		};
		var dataTemplate = new FuncDataTemplate<Item>((x, __) => new Button { Content = x });
		var target = CreateTarget(
			"Base",
			itemsSource: items,
			itemTemplate: dataTemplate);
		var panel = CornerstoneTest.IsAssignableFrom<Panel>(target.ItemsPanelRoot);
		var dataContexts = panel.Children
			.Do(x => (x as ContentPresenter)?.UpdateChild())
			.Select(x => x.DataContext)
			.ToList();

		CornerstoneTest.AreEqual(new[] { items[0], items[1], "Base", "Base" }, dataContexts);
	}

	[PresentationTestMethod]
	public void DetachingThenReattachingToLogicalTreeTwiceDoesNotThrow()
	{
		// # Issue 3487
		using var app = Start();
		var target = CreateTarget(
			itemsSource: new[] { "foo", "bar" },
			itemTemplate: new FuncDataTemplate<string>((_, __) => new Canvas()));

		var root = CornerstoneTest.IsType<TestRoot>(target.GetVisualRoot());

		root.Child = null;
		root.Child = target;

		root.LayoutManager.ExecuteLayoutPass();

		root.Child = null;
		root.Child = target;
	}

	[PresentationTestMethod]
	public void DisplayMemberBindingCanBeChanged()
	{
		using var app = Start();
		var target = CreateTarget(
			itemsSource: new[] { new Item("Foo", "Bar") },
			displayMemberBinding: new Binding("Value"));

		var container = GetContainer(target);
		var textBlock = CornerstoneTest.IsType<TextBlock>(container.Child);

		CornerstoneTest.AreEqual(textBlock.Text, "Bar");

		target.DisplayMemberBinding = new Binding("Caption");
		Layout(target);

		container = GetContainer(target);
		textBlock = CornerstoneTest.IsType<TextBlock>(container.Child);

		CornerstoneTest.AreEqual(textBlock.Text, "Foo");
	}

	[PresentationTestMethod]
	public void DoesNotFocusNonFocusableItemOnKeyDown()
	{
		using var app = Start();
		var items = new object[]
		{
			new Button(),
			new Button { Focusable = false },
			new Button()
		};

		var target = CreateTarget(itemsSource: items);
		GetContainer<Button>(target).Focus();

		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Down
		});

		var panel = CornerstoneTest.IsAssignableFrom<Panel>(target.ItemsPanelRoot);
		var focusManager = ((IInputRoot) target.VisualRoot!).FocusManager;

		CornerstoneTest.AreEqual(panel.Children[2], focusManager?.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void EmptyClassShouldBeClearedWhenItemsAdded()
	{
		using var app = Start();
		var target = CreateTarget(items: new[] { 1, 2, 3 }, performLayout: false);

		CornerstoneTest.DoesNotContain(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void EmptyClassShouldBeClearedWhenItemsSourceItemsAdded()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { 1, 2, 3 }, performLayout: false);

		CornerstoneTest.DoesNotContain(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void EmptyClassShouldBeSetWhenItemsCollectionCleared()
	{
		using var app = Start();
		var items = new ObservableCollection<int> { 1, 2, 3 };
		var target = CreateTarget(itemsSource: items);

		items.Clear();

		CornerstoneTest.Contains(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void EmptyClassShouldBeSetWhenItemsSourceCollectionCleared()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { 1, 2, 3 });

		target.ItemsSource = new int[0];

		CornerstoneTest.Contains(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void EmptyClassShouldInitiallyBeApplied()
	{
		using var app = Start();
		var target = CreateTarget(performLayout: false);

		CornerstoneTest.Contains(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void EmptyClassShouldNotBeSetWhenItemsSourceCollectionCleared()
	{
		using var app = Start();
		var items = new ObservableCollection<int> { 1, 2, 3 };
		var target = CreateTarget(itemsSource: items);

		items.Clear();

		CornerstoneTest.DoesNotContain(target.Classes, ":singleitem");
	}

	[PresentationTestMethod]
	public void EmptyClassShouldNotBeSetWhenItemsSourceCollectionCountIncreases()
	{
		using var app = Start();
		var items = new ObservableCollection<int>();
		var target = CreateTarget(itemsSource: items);

		items.Add(1);

		CornerstoneTest.DoesNotContain(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void FocusesNextItemOnKeyDown()
	{
		using var app = Start();
		var items = new object[]
		{
			new Button(),
			new Button()
		};

		var target = CreateTarget(itemsSource: items);
		GetContainer<Button>(target).Focus();

		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Down
		});

		var panel = CornerstoneTest.IsAssignableFrom<Panel>(target.ItemsPanelRoot);
		var focusManager = ((IInputRoot) target.VisualRoot!).FocusManager;

		CornerstoneTest.AreEqual(panel.Children[1], focusManager?.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void HandlesRecyclingControlItemsInsideContainers()
	{
		// Issue #10825
		using var app = Start();

		// The items must be controls but not of the container type.
		var items = Enumerable.Range(0, 100).Select(x => new TextBlock
		{
			Text = $"Item {x}",
			Width = 100,
			Height = 100
		}).ToList();

		// Virtualization is required
		var itemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel());

		// Create an ItemsControl which uses containers, and provide a scroll viewer.
		var target = CreateTarget<ItemsControlWithContainer>(
			items: items,
			itemsPanel: itemsPanel,
			scrollViewer: true);
		var scroll = target.FindAncestorOfType<ScrollViewer>();

		CornerstoneTest.IsNotNull(scroll);
		CornerstoneTest.AreEqual(10, target.GetRealizedContainers().Count());

		// Scroll so that half a container is visible: an extra container is generated.
		scroll.Offset = new(0, 2050);
		Layout(target);

		// Scroll so that the extra container is no longer needed and recycled.
		scroll.Offset = new(0, 2100);
		Layout(target);

		// Scroll back: issue #10825 triggered.
		scroll.Offset = new(0, 2000);
		Layout(target);
	}

	[PresentationTestMethod]
	public void ItemContainerThemeCanBeChanged()
	{
		using var app = Start();

		var theme1 = new ControlTheme
		{
			TargetType = typeof(ContentPresenter),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
		};

		var theme2 = new ControlTheme
		{
			TargetType = typeof(ContentPresenter),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }
		};

		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemContainerTheme: theme1);

		var container = GetContainer(target);

		CornerstoneTest.Same(container.Theme, theme1);
		CornerstoneTest.AreEqual(container.Background, Brushes.Red);

		target.ItemContainerTheme = theme2;

		container = GetContainer(target);
		CornerstoneTest.Same(container.Theme, theme2);
		CornerstoneTest.AreEqual(container.Background, Brushes.Green);
	}

	[PresentationTestMethod]
	public void ItemContainerThemeCanBeChangedVirtualizing()
	{
		using var app = Start();

		var theme1 = new ControlTheme
		{
			TargetType = typeof(ContentPresenter),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
		};

		var theme2 = new ControlTheme
		{
			TargetType = typeof(ContentPresenter),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }
		};

		var itemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel());
		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemContainerTheme: theme1,
			itemsPanel: itemsPanel);

		var container = GetContainer(target);

		CornerstoneTest.Same(container.Theme, theme1);
		CornerstoneTest.AreEqual(container.Background, Brushes.Red);

		target.ItemContainerTheme = theme2;
		Layout(target);

		container = GetContainer(target);
		CornerstoneTest.Same(container.Theme, theme2);
		CornerstoneTest.AreEqual(container.Background, Brushes.Green);
	}

	[PresentationTestMethod]
	public void ItemContainerThemeCanBeCleared()
	{
		using var app = Start();

		var theme = new ControlTheme
		{
			TargetType = typeof(ContentPresenter),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
		};

		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemContainerTheme: theme);

		var container = GetContainer(target);

		CornerstoneTest.Same(container.Theme, theme);
		CornerstoneTest.AreEqual(container.Background, Brushes.Red);

		target.ItemContainerTheme = null;

		container = GetContainer(target);
		CornerstoneTest.IsNull(container.Theme);
		CornerstoneTest.IsNull(container.Background);
	}

	[PresentationTestMethod]
	public void ItemContainerThemeShouldNotOverrideLocalValueTheme()
	{
		using var app = Start();

		var theme1 = new ControlTheme
		{
			TargetType = typeof(ContentPresenter),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
		};

		var theme2 = new ControlTheme
		{
			TargetType = typeof(Control),
			Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }
		};

		var items = new object[]
		{
			new ContentPresenter(),
			new ContentPresenter
			{
				Theme = theme2
			}
		};

		var target = CreateTarget(
			itemsSource: items,
			itemContainerTheme: theme1);

		CornerstoneTest.Same(theme1, GetContainer(target, 0).Theme);
		CornerstoneTest.Same(theme2, GetContainer(target, 1).Theme);

		target.ItemContainerTheme = null;

		CornerstoneTest.IsNull(GetContainer(target, 0).Theme);
		CornerstoneTest.Same(theme2, GetContainer(target, 1).Theme);
	}

	[PresentationTestMethod]
	public void ItemCountShouldBeSetWhenItemsChanged()
	{
		using var app = Start();
		var items = new ObservableCollection<int> { 1, 2, 3 };
		var target = CreateTarget(items: new[] { 1, 2, 3 });

		target.Items.Add(4);

		CornerstoneTest.AreEqual(4, target.ItemCount);

		target.Items.Clear();

		CornerstoneTest.AreEqual(0, target.ItemCount);
	}

	[PresentationTestMethod]
	public void ItemCountShouldBeSetWhenItemsSourceItemsChanged()
	{
		using var app = Start();
		var items = new ObservableCollection<int> { 1, 2, 3 };
		var target = CreateTarget(itemsSource: items);

		items.Add(4);

		CornerstoneTest.AreEqual(4, target.ItemCount);

		items.Clear();

		CornerstoneTest.AreEqual(0, target.ItemCount);
	}

	[PresentationTestMethod]
	public void ItemCountShouldBeSetWhenItemsSourceSet()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { 1, 2, 3 });

		CornerstoneTest.AreEqual(3, target.ItemCount);
	}

	[PresentationTestMethod]
	public void ItemIsOwnContainerContentShouldNotBeClearedWhenRemoved()
	{
		// Issue #11128.
		using var app = Start();
		var item = new ContentPresenter { Content = "foo" };
		var target = CreateTarget(items: new[] { item });

		target.Items.RemoveAt(0);

		CornerstoneTest.AreEqual("foo", item.Content);
	}

	[PresentationTestMethod]
	public void ItemTemplateCanBeChanged()
	{
		using var app = Start();
		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemTemplate: new FuncDataTemplate<string>((_, __) => new Canvas()));
		var container = GetContainer(target);

		CornerstoneTest.IsType<Canvas>(container.Child);

		target.ItemTemplate = new FuncDataTemplate<string>((_, __) => new Border());
		Layout(target);

		container = GetContainer(target);

		CornerstoneTest.IsType<Border>(container.Child);
	}

	[PresentationTestMethod]
	public void LogicalChildrenShouldNotChangeInstanceWhenTemplateChanged()
	{
		using var app = Start();
		var target = CreateTarget();
		var before = ((ILogical) target).LogicalChildren;

		target.Template = null;
		target.Template = CreateItemsControlTemplate();
		Layout(target);

		var after = ((ILogical) target).LogicalChildren;

		CornerstoneTest.IsNotNull(before);
		CornerstoneTest.IsNotNull(after);
		CornerstoneTest.Same(before, after);
	}

	[PresentationTestMethod]
	public void PanelShouldHaveItemsHostSetToTrue()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "Foo" });

		CornerstoneTest.IsTrue(target.ItemsPanelRoot?.IsItemsHost);
	}

	[PresentationTestMethod]
	public void PanelShouldHaveTemplatedParentSetToItemsControl()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "Foo" });

		CornerstoneTest.AreEqual(target, target.ItemsPanelRoot?.TemplatedParent);
	}

	[PresentationTestMethod]
	public void RemovingItemsSourceItemsShouldNotFireLogicalChildrenCollectionChangedBeforeApplyTemplate()
	{
		using var app = Start();
		var items = new OldPresentationList<string> { "Foo", "Bar" };
		var target = CreateTarget(itemsSource: items, performLayout: false);
		var called = false;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) => called = true;

		items.Remove("Bar");

		CornerstoneTest.IsFalse(called);
	}

	[PresentationTestMethod]
	public void SettingItemsSourceShouldPopulateItems()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

		CornerstoneTest.NotSame(target.ItemsSource, target.Items);
		CornerstoneTest.AreEqual(target.ItemsSource, target.Items);
	}

	[PresentationTestMethod]
	public void ShouldClearContainersWhenItemsPresenterChanges()
	{
		using var app = Start();
		var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
		var panel = CornerstoneTest.IsAssignableFrom<Panel>(target.Presenter?.Panel);

		CornerstoneTest.AreEqual(2, panel.Children.Count());

		target.Template = CreateItemsControlTemplate();
		target.ApplyTemplate();

		CornerstoneTest.Empty(panel.Children);
	}

	[PresentationTestMethod]
	public void ShouldUseDisplayMemberBinding()
	{
		using var app = Start();
		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			displayMemberBinding: new Binding("Length"));

		var container = GetContainer(target);
		var textBlock = CornerstoneTest.IsType<TextBlock>(container.Child);

		CornerstoneTest.AreEqual(textBlock.Text, "3");
	}

	[PresentationTestMethod]
	public void ShouldUseItemTemplateToCreateControl()
	{
		using var app = Start();
		var target = CreateTarget(
			itemsSource: new[] { "Foo" },
			itemTemplate: new FuncDataTemplate<string>((_, __) => new Canvas()));
		var container = GetContainer(target);

		CornerstoneTest.IsType<Canvas>(container.Child);
	}

	[PresentationTestMethod]
	public void SingleItemClassShouldBeSetWhenItemsSourceCollectionCountIncreasesToOne()
	{
		using var app = Start();
		var items = new ObservableCollection<int>();
		var target = CreateTarget(itemsSource: items);

		items.Add(1);

		CornerstoneTest.Contains(target.Classes, ":singleitem");
	}

	[PresentationTestMethod]
	public void SingleItemClassShouldNotBeSetWhenItemsCollectionCountIncreasesBeyondOne()
	{
		using var app = Start();
		var items = new ObservableCollection<int> { 1 };
		var target = CreateTarget(itemsSource: items);

		items.Add(2);

		CornerstoneTest.DoesNotContain(target.Classes, ":singleitem");
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

	private static FuncControlTemplate CreateContentControlTemplate()
	{
		return new FuncControlTemplate<ContentControl>((parent, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
				[!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty]
			}.RegisterInNameScope(scope));
	}

	private static ControlTheme CreateContentControlTheme()
	{
		return new ControlTheme(typeof(ContentControl))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateContentControlTemplate())
			}
		};
	}

	private static FuncControlTemplate CreateItemsControlTemplate()
	{
		return new FuncControlTemplate<ItemsControl>((parent, scope) =>
		{
			return new Border
			{
				Background = new SolidColorBrush(0xffffffff),
				Child = new ItemsPresenter
				{
					Name = "PART_ItemsPresenter",
					[~ItemsPresenter.ItemsPanelProperty] = parent[~ItemsControl.ItemsPanelProperty]
				}.RegisterInNameScope(scope)
			};
		});
	}

	private static ControlTheme CreateItemsControlTheme()
	{
		return new ControlTheme(typeof(ItemsControl))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateItemsControlTemplate())
			}
		};
	}

	private static TestRoot CreateRoot(Control child)
	{
		return new TestRoot
		{
			Resources =
			{
				{ typeof(ContentControl), CreateContentControlTheme() },
				{ typeof(ItemsControl), CreateItemsControlTheme() },
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

	private static ItemsControl CreateTarget(
		object dataContext = null,
		BindingBase displayMemberBinding = null,
		IList items = null,
		IList itemsSource = null,
		ControlTheme itemContainerTheme = null,
		IDataTemplate itemTemplate = null,
		ITemplate<Panel> itemsPanel = null,
		IEnumerable<IDataTemplate> dataTemplates = null,
		bool performLayout = true)
	{
		return CreateTarget<ItemsControl>(
			dataContext,
			displayMemberBinding,
			items,
			itemsSource,
			itemContainerTheme,
			itemTemplate,
			itemsPanel,
			dataTemplates,
			performLayout);
	}

	private static T CreateTarget<T>(
		object dataContext = null,
		BindingBase displayMemberBinding = null,
		IList items = null,
		IList itemsSource = null,
		ControlTheme itemContainerTheme = null,
		IDataTemplate itemTemplate = null,
		ITemplate<Panel> itemsPanel = null,
		IEnumerable<IDataTemplate> dataTemplates = null,
		bool performLayout = true,
		bool scrollViewer = false)
		where T : ItemsControl, new()
	{
		var target = new T
		{
			DataContext = dataContext,
			DisplayMemberBinding = displayMemberBinding,
			ItemContainerTheme = itemContainerTheme,
			ItemTemplate = itemTemplate,
			ItemsSource = itemsSource
		};

		if (items is not null)
		{
			foreach (var item in items)
			{
				target.Items.Add(item);
			}
		}

		if (itemsPanel is not null)
		{
			target.ItemsPanel = itemsPanel;
		}

		var scroll = scrollViewer ? new ScrollViewer { Content = target } : null;
		var root = CreateRoot(scroll ?? (Control) target);

		if (dataTemplates is not null)
		{
			foreach (var dataTemplate in dataTemplates)
			{
				root.DataTemplates.Add(dataTemplate);
			}
		}

		if (performLayout)
		{
			root.LayoutManager.ExecuteInitialLayoutPass();
		}

		return target;
	}

	private static ContentPresenter GetContainer(ItemsControl target, int index = 0)
	{
		return CornerstoneTest.IsType<ContentPresenter>(target.GetRealizedContainers().ElementAt(index));
	}

	private static T GetContainer<T>(ItemsControl target, int index = 0)
	{
		return CornerstoneTest.IsType<T>(target.GetRealizedContainers().ElementAt(index));
	}

	private static void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
	}

	#endregion

	#region Classes

	private class ContainerControl : ContentControl
	{
		#region Properties

		protected override Type StyleKeyOverride => typeof(ContentControl);

		#endregion
	}

	private class ItemsControlWithContainer : ItemsControl
	{
		#region Properties

		protected override Type StyleKeyOverride => typeof(ItemsControl);

		#endregion

		#region Methods

		protected internal override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
		{
			return new ContainerControl();
		}

		protected internal override bool NeedsContainerOverride(object item, int index, out object recycleKey)
		{
			return NeedsContainer<ContainerControl>(item, out recycleKey);
		}

		#endregion
	}

	#endregion

	#region Records

	private record Item(string Caption, string Value = null);

	#endregion
}