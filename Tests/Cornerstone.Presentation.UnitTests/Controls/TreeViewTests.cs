#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Markup.Xaml.Templates;
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
public class TreeViewTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddingNodeToRemovedAndReAddedParentShouldNotCrash()
	{
		// Issue #2985
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var parent = data[0];
		var node = parent.Children[1];

		parent.Children.Remove(node);
		parent.Children.Add(node);

		// #2985 causes ArgumentException here.
		node.Children.Add(new Node());
	}

	[PresentationTestMethod]
	public void AutoExpandingInStyleShouldNotBreakRangeSelection()
	{
		// Issue #2980.
		using var app = Start();

		var data = new List<Node>
		{
			new() { Value = "Root1" },
			new() { Value = "Root2" }
		};

		var style = new Style(x => x.OfType<TreeViewItem>())
		{
			Setters =
			{
				new Setter(TreeViewItem.IsExpandedProperty, true)
			}
		};

		var target = CreateTarget(data, styles: new[] { style }, multiSelect: true);

		_mouse.Click(GetItem(target, 0));
		_mouse.Click(GetItem(target, 1), modifiers: KeyModifiers.Shift);
	}

	[PresentationTestMethod]
	public void BoundSelectedItemShouldNotBeClearedwhenChangingSelection()
	{
		using var app = Start();
		var dataContext = new TestDataContext();
		var target = CreateTarget();

		target.DataContext = dataContext;
		target.Bind(TreeView.ItemsSourceProperty, new Binding("Items"));
		target.Bind(TreeView.SelectedItemProperty, new Binding("SelectedItem"));

		var selectedValues = new List<object>();

		dataContext.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(TestDataContext.SelectedItem))
			{
				selectedValues.Add(dataContext.SelectedItem);
			}
		};

		selectedValues.Add(dataContext.SelectedItem);

		_mouse.Click(target.Presenter!.Panel!.Children[0], MouseButton.Left);
		_mouse.Click(target.Presenter!.Panel!.Children[2], MouseButton.Left);

		CornerstoneTest.AreEqual(3, selectedValues.Count);
		CornerstoneTest.AreEqual(new[] { null, "Item 0", "Item 2" }, selectedValues.ToArray());
	}

	[PresentationTestMethod]
	public void CanBindInitialSelectedStateViaItemContainerTheme()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var selected = new[] { data[0], data[0].Children[1] };

		foreach (var node in selected)
		{
			node.IsSelected = true;
		}

		var itemTheme = new ControlTheme(typeof(TreeViewItem))
		{
			BasedOn = CreateTreeViewItemControlTheme(),
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(data, itemContainerTheme: itemTheme, multiSelect: true);

		AssertDataSelection(data, selected);
		AssertContainerSelection(target, selected);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(selected, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void CanBindInitialSelectedStateViaStyle()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var selected = new[] { data[0], data[0].Children[1] };

		foreach (var node in selected)
		{
			node.IsSelected = true;
		}

		var style = new Style(x => x.OfType<TreeViewItem>())
		{
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(data, multiSelect: true, styles: new[] { style });

		AssertDataSelection(data, selected);
		AssertContainerSelection(target, selected);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(selected, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void CanUseDerivedTreeViewItem()
	{
		var tree = CreateTestTreeData();
		var target = new DerivedTreeViewWithDerivedTreeViewItems
		{
			Template = CreateTreeViewTemplate(),
			ItemsSource = tree
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
		ExpandAll(target);

		// Verify that all items are DerivedTreeViewItem
		foreach (var container in target.GetRealizedTreeContainers())
		{
			CornerstoneTest.IsType<DerivedTreeViewItem>(container);
		}
	}

	[PresentationTestMethod]
	public void ClearingChildItemsShouldClearSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0].Children[1];

		target.SelectedItem = item;

		data[0].Children.Clear();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Empty(target.SelectedItems);
	}

	[PresentationTestMethod]
	public void ClearingTreeViewItemsClearsIndex()
	{
		// Issue #3551
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var root = CornerstoneTest.IsType<TestRoot>(target.GetVisualRoot());
		var rootNode = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(rootNode));

		CornerstoneTest.IsNotNull(container);

		root.Child = null;

		data.Clear();

		CornerstoneTest.Empty(target.GetRealizedContainers());
	}

	[PresentationTestMethod]
	public void ClickingFirstItemOfSelectedItemsShouldSelectOnlyIt()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children.Last();
		var to = rootNode.Children[0];
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		ClickContainer(fromContainer, KeyModifiers.None);
		ClickContainer(toContainer, KeyModifiers.Shift);
		AssertAllChildContainersSelected(target, rootNode);

		ClickContainer(fromContainer, KeyModifiers.None);
		CornerstoneTest.IsTrue(fromContainer.IsSelected);

		foreach (var child in rootNode.Children)
		{
			if (child == from)
			{
				continue;
			}

			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(child));
			CornerstoneTest.IsFalse(container.IsSelected);
		}
	}

	[PresentationTestMethod]
	public void ClickingItemShouldSelectIt()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		var item = data[0].Children[1].Children[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		_mouse.Click(container);

		CornerstoneTest.AreEqual(item, target.SelectedItem);
		CornerstoneTest.IsTrue(container.IsSelected);
	}

	[PresentationTestMethod]
	public void ClickingWithControlModifierNotSelectedItemShouldSelectIt()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		var item1 = data[0].Children[1].Children[0];
		var container1 = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item1));

		var item2 = data[0].Children[1];
		var container2 = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item2));

		target.SelectedItem = item1;

		CornerstoneTest.IsTrue(container1.IsSelected);

		_mouse.Click(container2, modifiers: KeyModifiers.Control);

		CornerstoneTest.AreEqual(item2, target.SelectedItem);
		CornerstoneTest.IsFalse(container1.IsSelected);
		CornerstoneTest.IsTrue(container2.IsSelected);
	}

	[PresentationTestMethod]
	public void ClickingWithControlModifierSelectedItemShouldDeselectAndRemoveFromSelectedItems()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var item1 = rootNode.Children[0];
		var item2 = rootNode.Children.Last();
		var item1Container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item1));
		var item2Container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item2));

		ClickContainer(item1Container, KeyModifiers.Control);
		CornerstoneTest.IsTrue(item1Container.IsSelected);

		ClickContainer(item2Container, KeyModifiers.Control);
		CornerstoneTest.IsTrue(item2Container.IsSelected);

		CornerstoneTest.AreEqual(new[] { item1, item2 }, target.SelectedItems.OfType<Node>());

		ClickContainer(item1Container, KeyModifiers.Control);
		CornerstoneTest.IsFalse(item1Container.IsSelected);

		CornerstoneTest.DoesNotContain(target.SelectedItems.OfType<Node>(), item1);
	}

	[PresentationTestMethod]
	public void ClickingWithControlModifierSelectedItemShouldDeselectIt()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		var item = data[0].Children[1].Children[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		target.SelectedItem = item;

		CornerstoneTest.IsTrue(container.IsSelected);

		_mouse.Click(container, modifiers: KeyModifiers.Control);

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.IsFalse(container.IsSelected);
	}

	[PresentationTestMethod]
	public void ClickingWithShiftModifierDownDirectionShouldSelectRangeOfItems()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children[0];
		var to = rootNode.Children.Last();
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		ClickContainer(fromContainer, KeyModifiers.None);
		CornerstoneTest.IsTrue(fromContainer.IsSelected);

		ClickContainer(toContainer, KeyModifiers.Shift);
		AssertAllChildContainersSelected(target, rootNode);
	}

	[PresentationTestMethod]
	public void ClickingWithShiftModifierUpDirectionShouldSelectRangeOfItems()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children.Last();
		var to = rootNode.Children[0];
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		ClickContainer(fromContainer, KeyModifiers.None);
		CornerstoneTest.IsTrue(fromContainer.IsSelected);

		ClickContainer(toContainer, KeyModifiers.Shift);
		AssertAllChildContainersSelected(target, rootNode);
	}

	[PresentationTestMethod]
	public void CollapseEventCanBeCapturedByTreeViewWhenCollapsingTreeViewItem()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		var raised = false;
		object source = null;
		target.AddHandler(TreeViewItem.CollapsedEvent, (o, e) =>
		{
			raised = true;
			source = e.Source;
		});
		container.IsExpanded = false;
		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual(container, source);
	}

	[PresentationTestMethod]
	public void CollapseEventShouldBeRaisedWhenCollapsingTreeViewItem()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		var raised = false;
		object source = null;
		container.AddHandler(TreeViewItem.CollapsedEvent, (o, e) =>
		{
			raised = true;
			source = e.Source;
		});
		container.IsExpanded = false;
		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual(container, source);
	}

	[PresentationTestMethod]
	public void ControlItemShouldNotBeNameScope()
	{
		using var app = Start();
		var target = CreateTarget(null);
		var item = new TreeViewItem();

		target.Items!.Add(item);

		CornerstoneTest.Same(item, target.LogicalChildren[0]);
		CornerstoneTest.IsNull(NameScope.GetNameScope(item));
	}

	[PresentationTestMethod]
	public void CtrlRightClickShouldNotSelectMultiple()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children[0];
		var to = rootNode.Children[1];
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		_mouse.Click(fromContainer);
		_mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Control);

		CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
	}

	[PresentationTestMethod]
	public void DataContextsShouldBeCorrectlySet()
	{
		using var app = Start();
		var items = new object[]
		{
			"Foo",
			new Node { Value = "Bar" },
			new TextBlock { Text = "Baz" },
			new TreeViewItem { Header = "Qux" }
		};

		var target = CreateTarget();
		var root = CornerstoneTest.IsType<TestRoot>(target.GetVisualRoot());

		root.DataTemplates.Add(new FuncDataTemplate<Node>((x, _) => new Button { Content = x }));
		target.DataContext = "Base";
		target.ItemsSource = items;

		var dataContexts = target.Presenter!.Panel!.Children
			.Select(x => x.DataContext)
			.ToList();

		CornerstoneTest.AreEqual(new[] { items[0], items[1], "Base", "Base" }, dataContexts);
	}

	[PresentationTestMethod]
	public void DoubleClickingItemHeaderShouldCollapseIt()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0].Children[1];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsTrue(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		_mouse.DoubleClick(header);

		CornerstoneTest.IsFalse(container.IsExpanded);
	}

	[PresentationTestMethod]
	public void DoubleClickingItemHeaderShouldExpandIt()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CollapseAll(target);

		var item = data[0].Children[1];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsFalse(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		_mouse.DoubleClick(header);

		CornerstoneTest.IsTrue(container.IsExpanded);
	}

	[PresentationTestMethod]
	public void DoubleClickingItemHeaderWithNoChildrenDoesNotExpandIt()
	{
		using var app = Start();
		{
			var data = CreateTestTreeData();
			var target = CreateTarget(data);

			CollapseAll(target);

			var item = data[0].Children[1].Children[0];
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
			var header = container.HeaderPresenter?.Child;

			CornerstoneTest.IsFalse(container.IsExpanded);
			CornerstoneTest.IsNotNull(header);

			_mouse.DoubleClick(header);

			CornerstoneTest.IsFalse(container.IsExpanded);
		}
	}

	[PresentationTestMethod]
	public void EnterKeyShouldCollapseTreeViewItem()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsTrue(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter
		});

		CornerstoneTest.IsFalse(container.IsExpanded);
	}

	[PresentationTestMethod]
	public void EnterKeyShouldExpandTreeViewItem()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CollapseAll(target);

		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsFalse(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter
		});

		CornerstoneTest.IsTrue(container.IsExpanded);
	}

	[PresentationTestMethod]
	public void EnterPlusCtrlKeyShouldCollapseTreeViewItemRecursively()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsTrue(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter,
			KeyModifiers = KeyModifiers.Control
		});

		CornerstoneTest.IsFalse(container.IsExpanded);

		AssertEachItemWithChildrenIsCollapsed(item);

		void AssertEachItemWithChildrenIsCollapsed(Node node)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(node));

			if (node.Children?.Count > 0)
			{
				CornerstoneTest.IsFalse(container.IsExpanded);
				foreach (var c in node.Children)
				{
					AssertEachItemWithChildrenIsCollapsed(c);
				}
			}
			else
			{
				CornerstoneTest.IsTrue(container.IsExpanded);
			}
		}
	}

	[PresentationTestMethod]
	public void EnterPlusCtrlKeyShouldExpandTreeViewItemRecursively()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CollapseAll(target);

		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsFalse(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter,
			KeyModifiers = KeyModifiers.Control
		});

		CornerstoneTest.IsTrue(container.IsExpanded);

		AssertEachItemWithChildrenIsExpanded(item);

		void AssertEachItemWithChildrenIsExpanded(Node node)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(node));

			if (node.Children?.Count > 0)
			{
				CornerstoneTest.IsTrue(container.IsExpanded);
				foreach (var c in node.Children)
				{
					AssertEachItemWithChildrenIsExpanded(c);
				}
			}
			else
			{
				CornerstoneTest.IsFalse(container.IsExpanded);
			}
		}
	}

	[PresentationTestMethod]
	public void ExpandingSelectedItemToBeVisibleShouldResultInSelectedContainer()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, false);

		target.SelectedItem = data[0].Children[1];

		var rootItem = CornerstoneTest.IsType<TreeViewItem>(target.ContainerFromIndex(0));
		rootItem.IsExpanded = true;
		Layout(target);

		var container = CornerstoneTest.IsType<TreeViewItem>(rootItem.ContainerFromIndex(1));
		CornerstoneTest.IsTrue(container.IsSelected);
	}

	[PresentationTestMethod]
	public void FindsCorrectDataTemplateWhenApplicationDataTemplateIsPresent()
	{
		// #10398
		using var app = Start();

		Application.Current!.DataTemplates.Add(new FuncDataTemplate<object>((x, _) => new Canvas()));
		PresentationLocator.CurrentMutable.Bind<IGlobalDataTemplates>().ToConstant(Application.Current);

		var target = CreateTarget();

		CornerstoneTest.AreEqual(new[] { "Root" }, ExtractItemHeader(target, 0));
		CornerstoneTest.AreEqual(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
		CornerstoneTest.AreEqual(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));
	}

	[PresentationTestMethod]
	public void ItemsShouldBeCreated()
	{
		using var app = Start();
		var target = CreateTarget();

		CornerstoneTest.AreEqual(new[] { "Root" }, ExtractItemHeader(target, 0));
		CornerstoneTest.AreEqual(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
		CornerstoneTest.AreEqual(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));
	}

	[PresentationTestMethod]
	public void ItemsShouldBeCreatedUsingItemConatinerThemeIfPresent()
	{
		var theme = CreateTreeViewItemControlTheme();
		var itemTemplate = new FuncTreeDataTemplate<Node>(
			(_, _) => new Canvas(),
			x => x.Children);

		var target = CreateTarget(
			itemContainerTheme: theme,
			itemTemplate: itemTemplate);

		var items = target.GetRealizedTreeContainers()
			.OfType<TreeViewItem>()
			.ToList();

		CornerstoneTest.AreEqual(5, items.Count);
		CornerstoneTest.All(items, x => CornerstoneTest.Same(theme, x.ItemContainerTheme));
	}

	[PresentationTestMethod]
	public void ItemsShouldBeCreatedUsingItemTemplateIfPresent()
	{
		using var app = Start();
		var itemTemplate = new FuncTreeDataTemplate<Node>(
			(_, _) => new Canvas(),
			x => x.Children);
		var target = CreateTarget(itemTemplate: itemTemplate);

		var items = target.GetRealizedTreeContainers()
			.OfType<TreeViewItem>()
			.ToList();

		CornerstoneTest.AreEqual(5, items.Count);
		CornerstoneTest.All(items, x => CornerstoneTest.IsType<Canvas>(x.HeaderPresenter?.Child));
	}

	[PresentationTestMethod]
	public void ItemsShouldBeCreatedUsingItemTemplateIfPresent2()
	{
		using var app = Start();
		var itemTemplate = new TreeDataTemplate
		{
			Content = (IServiceProvider _) => new TemplateResult<Control>(new Canvas(), new NameScope()),
			ItemsSource = new Binding("Children")
		};
		var target = CreateTarget(itemTemplate: itemTemplate);

		var items = target.GetRealizedTreeContainers()
			.OfType<TreeViewItem>()
			.ToList();

		CornerstoneTest.AreEqual(5, items.Count);
		CornerstoneTest.All(items, x => CornerstoneTest.IsType<Canvas>(x.HeaderPresenter?.Child));
	}

	[PresentationTestMethod]
	public void KeyboardNavigationShouldMoveToLastSelectedNode()
	{
		using var app = Start();
		var navigation = PresentationLocator.Current.GetRequiredService<IKeyboardNavigationHandler>();
		var data = CreateTestTreeData();

		var target = new TreeView
		{
			ItemsSource = data
		};

		var button = new Button();

		var root = CreateRoot(new StackPanel
		{
			Children = { target, button }
		});
		var focus = root.FocusManager;

		root.LayoutManager.ExecuteInitialLayoutPass();
		ExpandAll(target);

		var item = data[0].Children[0];
		var node = target.TreeContainerFromItem(item);
		CornerstoneTest.IsNotNull(node);

		target.SelectedItem = item;
		node.Focus();
		CornerstoneTest.Same(node, focus.GetFocusedElement());

		navigation.Move(focus.GetFocusedElement()!, NavigationDirection.Next);
		CornerstoneTest.Same(button, focus.GetFocusedElement());

		navigation.Move(focus.GetFocusedElement()!, NavigationDirection.Next);
		CornerstoneTest.Same(node, focus.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void KeyboardNavigationShouldNotCrashIfSelectedItemIsnotInTree()
	{
		using var app = Start();
		var data = CreateTestTreeData();

		var selectedNode = new Node { Value = "Out of Tree Selected Item" };

		var target = new TreeView
		{
			Template = CreateTreeViewTemplate(),
			ItemsSource = data,
			SelectedItem = selectedNode
		};

		var button = new Button();

		var root = CreateRoot(new StackPanel
		{
			Children = { target, button }
		});
		var focus = root.FocusManager;

		root.LayoutManager.ExecuteInitialLayoutPass();
		ExpandAll(target);

		var item = data[0].Children[0];
		var node = target.TreeContainerFromItem(item);
		CornerstoneTest.IsNotNull(node);

		target.SelectedItem = selectedNode;
		node.Focus();
		CornerstoneTest.Same(node, focus.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void LogicalChildrenShouldBeSet()
	{
		using var app = Start();
		var target = CreateTarget(null);

		target.ItemsSource = new[] { "Foo", "Bar", "Baz " };
		Layout(target);

		var result = target.GetLogicalChildren()
			.OfType<TreeViewItem>()
			.Select(x => x.HeaderPresenter?.Child)
			.OfType<TextBlock>()
			.Select(x => x.Text)
			.ToList();

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz " }, result);
	}

	[PresentationTestMethod]
	public void NumpadSlashShouldCollapseAllChildrenRecursively()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		CornerstoneTest.IsNotNull(container);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Divide
		});

		AssertEachItemWithChildrenIsCollapsed(item);

		void AssertEachItemWithChildrenIsCollapsed(Node node)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
			CornerstoneTest.IsNotNull(container);
			if (node.Children?.Count > 0)
			{
				CornerstoneTest.IsFalse(container.IsExpanded);
				foreach (var c in node.Children)
				{
					AssertEachItemWithChildrenIsCollapsed(c);
				}
			}
			else
			{
				CornerstoneTest.IsTrue(container.IsExpanded);
			}
		}
	}

	[PresentationTestMethod]
	public void NumpadStarShouldExpandAllChildrenRecursively()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CollapseAll(target);

		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		CornerstoneTest.IsNotNull(container);
		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Multiply
		});

		AssertEachItemWithChildrenIsExpanded(item);

		void AssertEachItemWithChildrenIsExpanded(Node node)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
			CornerstoneTest.IsNotNull(container);
			if (node.Children?.Count > 0)
			{
				CornerstoneTest.IsTrue(container.IsExpanded);
				foreach (var c in node.Children)
				{
					AssertEachItemWithChildrenIsExpanded(c);
				}
			}
			else
			{
				CornerstoneTest.IsFalse(container.IsExpanded);
			}
		}
	}

	[PresentationTestMethod]
	public void PressingSelectAllGestureShouldSelectAllNodes()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
		var selectAllGesture = keymap.SelectAll.First();

		var keyEvent = new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = selectAllGesture.Key,
			KeyModifiers = selectAllGesture.KeyModifiers
		};

		target.RaiseEvent(keyEvent);

		AssertAllChildContainersSelected(target, rootNode);
	}

	[PresentationTestMethod]
	public void PressingSelectAllGestureWithDownwardRangeSelectedShouldSelectAllNodes()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children[0];
		var to = rootNode.Children.Last();
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		ClickContainer(fromContainer, KeyModifiers.None);
		ClickContainer(toContainer, KeyModifiers.Shift);

		var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
		var selectAllGesture = keymap.SelectAll.First();

		var keyEvent = new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = selectAllGesture.Key,
			KeyModifiers = selectAllGesture.KeyModifiers
		};

		target.RaiseEvent(keyEvent);

		AssertAllChildContainersSelected(target, rootNode);
	}

	[PresentationTestMethod]
	public void PressingSelectAllGestureWithUpwardRangeSelectedShouldSelectAllNodes()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children.Last();
		var to = rootNode.Children[0];
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		ClickContainer(fromContainer, KeyModifiers.None);
		ClickContainer(toContainer, KeyModifiers.Shift);

		var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
		var selectAllGesture = keymap.SelectAll.First();

		var keyEvent = new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = selectAllGesture.Key,
			KeyModifiers = selectAllGesture.KeyModifiers
		};

		target.RaiseEvent(keyEvent);

		AssertAllChildContainersSelected(target, rootNode);
	}

	[PresentationTestMethod]
	public void RemovingSelectedChildItemShouldClearSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0].Children[1];

		target.SelectedItem = item;

		data[0].Children.RemoveAt(1);

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Empty(target.SelectedItems);
	}

	[PresentationTestMethod]
	public void RemovingSelectedRootItemShouldClearSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];

		target.SelectedItem = item;

		data.RemoveAt(0);

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Empty(target.SelectedItems);
	}

	[PresentationTestMethod]
	public void RemovingTreeViewFromRootShouldPreserveTreeViewItems()
	{
		// Issue #3328
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var root = CornerstoneTest.IsType<TestRoot>(target.GetVisualRoot());

		CornerstoneTest.AreEqual(5, target.GetRealizedTreeContainers().Count());

		root.Child = null;

		CornerstoneTest.AreEqual(5, target.GetRealizedTreeContainers().Count());
		CornerstoneTest.AreEqual(1, target.Presenter!.Panel!.Children.Count);

		var rootNode = CornerstoneTest.IsType<TreeViewItem>(target.Presenter.Panel.Children[0]);
		CornerstoneTest.AreEqual(3, rootNode.GetRealizedContainers().Count());
		CornerstoneTest.AreEqual(3, rootNode.Presenter!.Panel!.Children.Count);

		var child2Node = CornerstoneTest.IsType<TreeViewItem>(rootNode.Presenter.Panel.Children[1]);
		CornerstoneTest.AreEqual(1, child2Node.GetRealizedContainers().Count());
		CornerstoneTest.AreEqual(1, child2Node.Presenter!.Panel!.Children.Count);
	}

	[PresentationTestMethod]
	public void ReplacingSelectedChildItemShouldClearSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0].Children[1];

		target.SelectedItem = item;

		data[0].Children[1] = new Node();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Empty(target.SelectedItems);
	}

	[PresentationTestMethod]
	public void ResettingRootItemsShouldClearSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];

		target.SelectedItem = item;

		data.Clear();

		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Empty(target.SelectedItems);
	}

	[PresentationTestMethod]
	public void RightClickOnSelectedItemShouldNotClearExistingSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);

		target.SelectAll();

		AssertAllChildContainersSelected(target, data[0]);
		CornerstoneTest.AreEqual(5, target.SelectedItems.Count);

		_mouse.Click(target.Presenter!.Panel!.Children[0], MouseButton.Right);

		CornerstoneTest.AreEqual(5, target.SelectedItems.Count);
	}

	[PresentationTestMethod]
	public void RightClickOnUnselectedItemShouldClearExistingSelection()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var to = rootNode.Children[0];
		var then = rootNode.Children[1];
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(rootNode));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));
		var thenContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(then));

		ClickContainer(fromContainer, KeyModifiers.None);
		ClickContainer(toContainer, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(2, target.SelectedItems.Count);

		_mouse.Click(thenContainer, MouseButton.Right);

		CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
	}

	[PresentationTestMethod]
	public void RootItemContainerGeneratorContainersShouldBeRootContainers()
	{
		using var app = Start();
		var target = CreateTarget();

		var container = (TreeViewItem) target.GetRealizedContainers().Single();
		var header = CornerstoneTest.IsType<TextBlock>(container.HeaderPresenter?.Child);
		CornerstoneTest.AreEqual("Root", header?.Text);
	}

	[PresentationTestMethod]
	public void RootTreeContainerFromItemShouldReturnDescendantItem()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		var container = target.TreeContainerFromItem(data[0].Children[1].Children[0]);

		CornerstoneTest.IsNotNull(container);

		var header = ((TreeViewItem) container).HeaderPresenter!;
		var headerContent = ((TextBlock) header.Child!).Text;

		CornerstoneTest.AreEqual("Grandchild2a", headerContent);
	}

	[PresentationTestMethod]
	public void SelectedItemShouldBeValidWhenSelectedItemChangedEventRaised()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		var item = data[0].Children[1].Children[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		CornerstoneTest.IsNotNull(container);

		var called = false;
		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.Same(item, e.AddedItems[0]);
			CornerstoneTest.Same(item, target.SelectedItem);
			called = true;
		};

		_mouse.Click(container);

		CornerstoneTest.AreEqual(item, target.SelectedItem);
		CornerstoneTest.IsTrue(container.IsSelected);
		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void SelectionStateIsUpdatedViaIsSelectedBinding()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var selected = new[] { data[0], data[0].Children[1] };

		selected[0].IsSelected = true;

		var itemTheme = new ControlTheme(typeof(TreeViewItem))
		{
			BasedOn = CreateTreeViewItemControlTheme(),
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(data, itemContainerTheme: itemTheme, multiSelect: true);

		selected[1].IsSelected = true;

		AssertDataSelection(data, selected);
		AssertContainerSelection(target, selected);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(selected, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectionStateIsUpdatedViaIsSelectedBindingOnExpand()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var selected = new[] { data[0], data[0].Children[1] };

		foreach (var node in selected)
		{
			node.IsSelected = true;
		}

		var itemTheme = new ControlTheme(typeof(TreeViewItem))
		{
			BasedOn = CreateTreeViewItemControlTheme(),
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(
			data,
			false,
			itemTheme,
			multiSelect: true);

		var rootContainer = CornerstoneTest.IsType<TreeViewItem>(target.ContainerFromIndex(0));

		// Root TreeViewItem isn't expanded so selection for child won't have been picked
		// up by IsSelected binding yet.
		AssertContainerSelection(target, selected[0]);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { selected[0] }, target.SelectedItems);

		rootContainer.IsExpanded = true;
		Layout(target);

		// Root is expanded so now all expected items will be selected.
		AssertDataSelection(data, selected);
		AssertContainerSelection(target, selected);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(selected, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectionStateIsUpdatedViaIsSelectedBindingOnExpandSingleSelect()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var selected = new[] { data[0], data[0].Children[1] };

		foreach (var node in selected)
		{
			node.IsSelected = true;
		}

		var itemTheme = new ControlTheme(typeof(TreeViewItem))
		{
			BasedOn = CreateTreeViewItemControlTheme(),
			Setters =
			{
				new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected"))
			}
		};

		var target = CreateTarget(
			data,
			false,
			itemTheme);

		var rootContainer = CornerstoneTest.IsType<TreeViewItem>(target.ContainerFromIndex(0));

		// Root TreeViewItem isn't expanded so selection for child won't have been picked
		// up by IsSelected binding yet.
		AssertContainerSelection(target, selected[0]);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(new[] { selected[0] }, target.SelectedItems);

		rootContainer.IsExpanded = true;
		Layout(target);

		// Root is expanded and newly revealed selected node will replace current selection
		// given that we're in SelectionMode == Single.
		selected = new[] { selected[1] };
		AssertDataSelection(data, selected);
		AssertContainerSelection(target, selected);
		CornerstoneTest.AreEqual(selected[0], target.SelectedItem);
		CornerstoneTest.AreEqual(selected, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemShouldRaiseSelectedItemChangedEvent()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0].Children[1].Children[0];

		var called = false;
		target.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.Empty(e.RemovedItems);
			CornerstoneTest.AreEqual(1, e.AddedItems.Count);
			CornerstoneTest.Same(item, e.AddedItems[0]);
			called = true;
		};

		target.SelectedItem = item;
		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemShouldSetContainerSelected()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0].Children[1].Children[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

		CornerstoneTest.IsNotNull(container);

		target.SelectedItem = item;

		CornerstoneTest.IsTrue(container.IsSelected);
	}

	[PresentationTestMethod]
	public void ShiftRightClickShouldNotSelectMultiple()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data, multiSelect: true);
		var rootNode = data[0];
		var from = rootNode.Children[0];
		var to = rootNode.Children[1];
		var fromContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
		var toContainer = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

		_mouse.Click(fromContainer);
		_mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Shift);

		CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
	}

	[PresentationTestMethod]
	public void ShouldReactToChildrenChanging()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CornerstoneTest.AreEqual(new[] { "Root" }, ExtractItemHeader(target, 0));
		CornerstoneTest.AreEqual(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
		CornerstoneTest.AreEqual(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));

		// Make sure that the binding to Node.Children does not get collected.
		GC.Collect();

		data[0].Children = new OldPresentationList<Node>
		{
			new()
			{
				Value = "NewChild1"
			}
		};

		Layout(target);

		CornerstoneTest.AreEqual(new[] { "Root" }, ExtractItemHeader(target, 0));
		CornerstoneTest.AreEqual(new[] { "NewChild1" }, ExtractItemHeader(target, 1));
	}

	[PresentationTestMethod]
	public void SpaceKeyShouldCollapseTreeViewItem()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsTrue(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter
		});

		CornerstoneTest.IsFalse(container.IsExpanded);
	}

	[PresentationTestMethod]
	public void SpaceKeyShouldExpandTreeViewItem()
	{
		using var app = Start();
		{
			var data = CreateTestTreeData();
			var target = CreateTarget(data);

			CollapseAll(target);

			var item = data[0];
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
			var header = container.HeaderPresenter?.Child;

			CornerstoneTest.IsFalse(container.IsExpanded);
			CornerstoneTest.IsNotNull(header);

			container.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});

			CornerstoneTest.IsTrue(container.IsExpanded);
		}
	}

	[PresentationTestMethod]
	public void SpacePlusCtrlKeyShouldCollapseTreeViewItemRecursively()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);
		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsTrue(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter,
			KeyModifiers = KeyModifiers.Control
		});

		CornerstoneTest.IsFalse(container.IsExpanded);

		AssertEachItemWithChildrenIsCollapsed(item);

		void AssertEachItemWithChildrenIsCollapsed(Node node)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
			CornerstoneTest.IsNotNull(container);
			if (node.Children?.Count > 0)
			{
				CornerstoneTest.IsFalse(container.IsExpanded);
				foreach (var c in node.Children)
				{
					AssertEachItemWithChildrenIsCollapsed(c);
				}
			}
			else
			{
				CornerstoneTest.IsTrue(container.IsExpanded);
			}
		}
	}

	[PresentationTestMethod]
	public void SpaceplusCtrlKeyShouldExpandTreeViewItemRecursively()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CollapseAll(target);

		var item = data[0];
		var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
		var header = container.HeaderPresenter?.Child;

		CornerstoneTest.IsFalse(container.IsExpanded);
		CornerstoneTest.IsNotNull(header);

		container.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter,
			KeyModifiers = KeyModifiers.Control
		});

		CornerstoneTest.IsTrue(container.IsExpanded);

		AssertEachItemWithChildrenIsExpanded(item);

		void AssertEachItemWithChildrenIsExpanded(Node node)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
			CornerstoneTest.IsNotNull(container);
			if (node.Children?.Count > 0)
			{
				CornerstoneTest.IsTrue(container.IsExpanded);
				foreach (var c in node.Children)
				{
					AssertEachItemWithChildrenIsExpanded(c);
				}
			}
			else
			{
				CornerstoneTest.IsFalse(container.IsExpanded);
			}
		}
	}

	[PresentationTestMethod]
	public void SwipingOntoChildShouldNotSelect()
	{
		using (ConfigureHitTestTreeView(out var target, out var parent, out var child))
		{
			_mouse.Down(parent);

			// The pointer is captured on mouse down, so we don't want to issue a mouse up event on child itself
			// but on parent at a position ABOVE child, which is nested within the parent.
			_mouse.Up(parent, position: child.GetTransformedBounds()!.Value.Bounds.Center);

			CornerstoneTest.IsNull(target.SelectedItem);
		}
	}

	[PresentationTestMethod]
	public void SwipingOntoParentShouldNotSelect()
	{
		using (ConfigureHitTestTreeView(out var target, out var parent, out var child))
		{
			_mouse.Down(child);

			// When swiping from a child to a parent, behaviour is a little different: first there is a PointerReleased event
			// on the child, then on the parent. This is because the PointerPressed event went unhandled and so was seen by the parent.
			var mouseUpPosition = parent.HeaderPresenter!.GetTransformedBounds()!.Value.Bounds.Center;
			_mouse.Up(child, position: mouseUpPosition);
			_mouse.Up(parent, position: mouseUpPosition);

			CornerstoneTest.IsNull(target.SelectedItem);
		}
	}

	[PresentationTestMethod]
	public void TreeViewItemsLevelShouldBeSet()
	{
		using var app = Start();
		var data = CreateTestTreeData();
		var target = CreateTarget(data);

		CornerstoneTest.AreEqual(0, GetItem(target, 0).Level);
		CornerstoneTest.AreEqual(1, GetItem(target, 0, 0).Level);
		CornerstoneTest.AreEqual(1, GetItem(target, 0, 1).Level);
		CornerstoneTest.AreEqual(1, GetItem(target, 0, 2).Level);
		CornerstoneTest.AreEqual(2, GetItem(target, 0, 1, 0).Level);
	}

	[PresentationTestMethod]
	public void TreeViewItemsLevelShouldBeSetForDerivedTreeView()
	{
		using var app = Start();
		var tree = CreateTestTreeData();
		var target = new DerivedTreeView
		{
			Template = CreateTreeViewTemplate(),
			ItemsSource = tree
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
		ExpandAll(target);

		CornerstoneTest.AreEqual(0, GetItem(target, 0).Level);
		CornerstoneTest.AreEqual(1, GetItem(target, 0, 0).Level);
		CornerstoneTest.AreEqual(1, GetItem(target, 0, 1).Level);
		CornerstoneTest.AreEqual(1, GetItem(target, 0, 2).Level);
		CornerstoneTest.AreEqual(2, GetItem(target, 0, 1, 0).Level);
	}

	private void AssertAllChildContainersSelected(TreeView treeView, Node node)
	{
		CornerstoneTest.IsNotNull(node.Children);

		foreach (var child in node.Children)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(treeView.TreeContainerFromItem(child));
			CornerstoneTest.IsTrue(container.IsSelected);
		}
	}

	private void AssertContainerSelection(TreeView treeView, params Node[] expected)
	{
		static void Evaluate(Control container, HashSet<Node> remaining)
		{
			var treeViewItem = CornerstoneTest.IsType<TreeViewItem>(container);
			var node = (Node) container.DataContext!;

			CornerstoneTest.AreEqual(remaining.Contains(node), treeViewItem.IsSelected);
			remaining.Remove(node);

			foreach (var child in treeViewItem.GetRealizedContainers())
			{
				Evaluate(child, remaining);
			}
		}

		var remaining = expected.ToHashSet();
		foreach (var container in treeView.GetRealizedContainers())
		{
			Evaluate(container, remaining);
		}
		CornerstoneTest.Empty(remaining);
	}

	private void AssertDataSelection(IEnumerable<Node> data, params Node[] expected)
	{
		static void Evaluate(Node rootNode, HashSet<Node> remaining)
		{
			CornerstoneTest.AreEqual(remaining.Contains(rootNode), rootNode.IsSelected);
			remaining.Remove(rootNode);

			if (rootNode.Children is null)
			{
				return;
			}

			foreach (var child in rootNode.Children)
			{
				Evaluate(child, remaining);
			}
		}

		var remaining = expected.ToHashSet();
		foreach (var node in data)
		{
			Evaluate(node, remaining);
		}
		CornerstoneTest.Empty(remaining);
	}

	private void ClickContainer(Control container, KeyModifiers modifiers)
	{
		_mouse.Click(container, modifiers: modifiers);
	}

	private static void CollapseAll(TreeView tree)
	{
		foreach (var i in tree.GetRealizedContainers())
		{
			tree.CollapseSubTree((TreeViewItem) i);
		}
	}

	private static CompositorTestServices ConfigureHitTestTreeView(out TreeView target, out TreeViewItem parent, out TreeViewItem child)
	{
		var services = new CompositorTestServices();
		var data = CreateTestTreeData();
		target = CreateTarget(data, true,
			embeddableRoot: services.TopLevel, styles:
			[
				new(x => x.OfType<TreeViewItem>())
				{
					Setters =
					{
						new Setter(InputElement.IsHoldingEnabledProperty, true),
						new Setter(InputElement.IsHoldWithMouseEnabledProperty, true)
					}
				}
			]);

		services.RunJobs();
		target.SelectedItem = null;

		parent = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(data[0]));
		child = CornerstoneTest.IsType<TreeViewItem>(target.TreeContainerFromItem(data[0].Children[0]));
		return services;
	}

	private static void ConfigureRoot(Control root)
	{
		root.Resources.Add(typeof(TreeView), CreateTreeViewControlTheme());
		root.Resources.Add(typeof(TreeViewItem), CreateTreeViewItemControlTheme());
		root.DataTemplates.Add(new TreeDataTemplate
		{
			DataType = typeof(Node),
			ItemsSource = new Binding(nameof(Node.Children)),
			Content = (IServiceProvider _) => new TemplateResult<Control>(
				new TextBlock
				{
					[!TextBlock.TextProperty] = new Binding(nameof(Node.Value))
				},
				new NameScope())
		});
	}

	private static TestRoot CreateRoot(Control child)
	{
		var root = new TestRoot();
		ConfigureRoot(root);
		root.Child = child;
		return root;
	}

	private static TreeView CreateTarget(Optional<IList<Node>> data = default,
		bool expandAll = true,
		ControlTheme itemContainerTheme = null,
		IDataTemplate itemTemplate = null,
		bool multiSelect = false,
		IEnumerable<Style> styles = null,
		EmbeddableControlRoot embeddableRoot = null)
	{
		var target = new TreeView
		{
			ItemContainerTheme = itemContainerTheme,
			ItemsSource = data.HasValue ? data.Value : CreateTestTreeData(),
			ItemTemplate = itemTemplate,
			SelectionMode = multiSelect ? SelectionMode.Multiple : SelectionMode.Single
		};

		if (embeddableRoot == null)
		{
			var root = CreateRoot(target);

			if (styles is not null)
			{
				root.Styles.AddRange(styles);
			}

			root.LayoutManager.ExecuteInitialLayoutPass();
		}
		else
		{
			ConfigureRoot(embeddableRoot);
			embeddableRoot.Styles.AddRange(styles ?? []);
			embeddableRoot.Content = target;
			embeddableRoot.LayoutManager.ExecuteInitialLayoutPass();
		}

		if (expandAll)
		{
			ExpandAll(target);
		}

		return target;
	}

	private static OldPresentationList<Node> CreateTestTreeData()
	{
		return new OldPresentationList<Node>
		{
			new()
			{
				Value = "Root",
				Children = new OldPresentationList<Node>
				{
					new()
					{
						Value = "Child1"
					},
					new()
					{
						Value = "Child2",
						Children = new OldPresentationList<Node>
						{
							new()
							{
								Value = "Grandchild2a"
							}
						}
					},
					new()
					{
						Value = "Child3"
					}
				}
			}
		};
	}

	private static ControlTheme CreateTreeViewControlTheme()
	{
		return new ControlTheme(typeof(TreeView))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateTreeViewTemplate())
			}
		};
	}

	private static ControlTheme CreateTreeViewItemControlTheme()
	{
		return new ControlTheme(typeof(TreeViewItem))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateTreeViewItemTemplate())
			}
		};
	}

	private static IControlTemplate CreateTreeViewItemTemplate()
	{
		return new FuncControlTemplate<TreeViewItem>((parent, scope) => new Panel
		{
			Children =
			{
				new Border
				{
					Name = "PART_Header",
					Background = Brushes.Transparent,
					Child = new ContentPresenter
					{
						Name = "PART_HeaderPresenter",
						[~ContentPresenter.ContentProperty] = parent[~TreeViewItem.HeaderProperty],
						[~ContentPresenter.ContentTemplateProperty] = parent[~TreeViewItem.HeaderTemplateProperty]
					}.RegisterInNameScope(scope)
				}.RegisterInNameScope(scope),
				new ItemsPresenter
				{
					Name = "PART_ItemsPresenter",
					[~ItemsPresenter.IsVisibleProperty] = parent[~TreeViewItem.IsExpandedProperty]
				}.RegisterInNameScope(scope)
			}
		});
	}

	private static IControlTemplate CreateTreeViewTemplate()
	{
		return new FuncControlTemplate<TreeView>((parent, scope) => new ItemsPresenter
		{
			Name = "PART_ItemsPresenter"
		}.RegisterInNameScope(scope));
	}

	private static void ExpandAll(TreeView tree)
	{
		foreach (var i in tree.GetRealizedContainers())
		{
			tree.ExpandSubTree((TreeViewItem) i);
		}
	}

	private static IEnumerable<TreeViewItem> ExtractItemContent(Panel panel, int currentLevel, int level)
	{
		if (panel is null)
		{
			yield break;
		}

		foreach (var c in panel.Children)
		{
			var container = CornerstoneTest.IsType<TreeViewItem>(c);

			if (currentLevel == level)
			{
				yield return container;
			}
			else if (container.Presenter?.Panel is { } childPanel)
			{
				foreach (var child in ExtractItemContent(childPanel, currentLevel + 1, level))
				{
					yield return child;
				}
			}
		}
	}

	private static List<string> ExtractItemHeader(TreeView tree, int level)
	{
		return ExtractItemContent(tree.Presenter?.Panel, 0, level)
			.Select(x => x.HeaderPresenter?.Child)
			.OfType<TextBlock>()
			.Select(x => x.Text)
			.ToList();
	}

	private static TreeViewItem GetItem(TreeView target, params int[] indexes)
	{
		var c = (ItemsControl) target;

		foreach (var index in indexes)
		{
			var item = c.ItemsView[index]!;
			c = (ItemsControl) target.TreeContainerFromItem(item)!;
		}

		return (TreeViewItem) c;
	}

	private void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private IDisposable Start()
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

	#endregion

	#region Classes

	private class DerivedTreeView : TreeView
	{
	}

	private class DerivedTreeViewItem : TreeViewItem
	{
	}

	private class DerivedTreeViewWithDerivedTreeViewItems : TreeView
	{
		#region Methods

		protected internal override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
		{
			return new DerivedTreeViewItem();
		}

		#endregion
	}

	private class Node : NotifyingBase
	{
		#region Fields

		private IOldPresentationList<Node> _children = new OldPresentationList<Node>();
		private bool _isSelected;

		#endregion

		#region Properties

		public IOldPresentationList<Node> Children
		{
			get => _children;
			set
			{
				_children = value;
				RaisePropertyChanged(nameof(Children));
			}
		}

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

		public string Value { get; set; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return Value ?? string.Empty;
		}

		#endregion
	}

	private class TestDataContext : INotifyPropertyChanged
	{
		#region Fields

		private string _selectedItem;

		#endregion

		#region Constructors

		public TestDataContext()
		{
			Items = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item {i}"));
		}

		#endregion

		#region Properties

		public ObservableCollection<string> Items { get; }

		public string SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}