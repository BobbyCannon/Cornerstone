#region References

using System.Collections.ObjectModel;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ListBoxVirtualizationIssueTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingItemsAtTopShouldNotCreateGhostItems()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			ObservableCollection<Item> items = new();
			for (var i = 0; i < 100; i++)
			{
				items.Add(new Item(i));
			}

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = items,
				Height = 100, // Show 2 items
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel())
			};

			Prepare(target);

			// Scroll to some position
			var scrollViewer = (ScrollViewer) target.VisualChildren[0];
			scrollViewer.Offset = new Vector(0, 500); // Scrolled down
			target.UpdateLayout();

			// Add items at the top multiple times
			for (var i = 0; i < 5; i++)
			{
				for (var j = 0; j < 10; j++)
				{
					items.Insert(0, new Item(1000 + (i * 100) + j));
				}

				target.UpdateLayout();

				// Randomly select something
				target.SelectedIndex = items.Count - 1;
				target.UpdateLayout();

				// Scroll a bit
				scrollViewer.ScrollToEnd();
				scrollViewer.ScrollToEnd();
				target.UpdateLayout();

				// Check for ghost items during the process
				var p = target.Presenter!.Panel!;
				var visibleChildren = p.Children.Where(c => c.IsVisible).ToList();
				var realizedContainers = target.GetRealizedContainers()
					.Cast<ListBoxItem>()
					.ToList();

				// Only visible children should be considered. Invisible children may be recycled items kept for reuse.
				CornerstoneTest.AreEqual(realizedContainers.Count, visibleChildren.Count);
				foreach (var child in visibleChildren)
				{
					CornerstoneTest.Contains(realizedContainers, child);
				}

				var realizedItems = realizedContainers
					.Select(x => x.Content)
					.Cast<Item>()
					.ToList();

				// Check for duplicates in realized items
				var duplicateIds = realizedItems.GroupBy(x => x.Id).Where(g => g.Count() > 1).Select(g => g.Key)
					.ToList();
				CornerstoneTest.Empty(duplicateIds);

				// Check if all realized items are actually in the items source
				foreach (var item in realizedItems)
				{
					CornerstoneTest.Contains(items, item);
				}

				// Check if realized items are in the correct order
				var lastIndex = -1;
				foreach (var item in realizedItems)
				{
					var currentIndex = items.IndexOf(item);
					CornerstoneTest.IsTrue(currentIndex > lastIndex, $"Item {item.Id} is at index {currentIndex}, but previous item was at index {lastIndex}");
					lastIndex = currentIndex;
				}

				// New check: verify that all visual children of the panel are accounted for in realizedContainers
				var panel = target.Presenter!.Panel!;
				var visualChildren = panel.Children.ToList();

				// Realized containers should match exactly the visual children of the panel
				// (VirtualizingStackPanel manages its children such that they should be the realized containers)
				// We also check if all children are visible, if not they might be "ghosts"
				foreach (var child in visualChildren)
				{
					CornerstoneTest.IsTrue(child.IsVisible, $"Child {((ListBoxItem) child).Content} should be visible");
				}

				CornerstoneTest.AreEqual(realizedContainers.Count, visualChildren.Count);
				foreach (var child in visualChildren)
				{
					CornerstoneTest.Contains(realizedContainers, child);
				}
			}
		}
	}

	[PresentationTestMethod]
	public void GhostItemTestFocusManagement()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new ObservableCollection<string>(Enumerable.Range(0, 100).Select(i => $"Item {i}"));

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
				Height = 100, // Show 2 items
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 })
			};

			Prepare(target);

			// 1. Get the first container and focus it
			var container = (ListBoxItem) target.Presenter!.Panel!.Children[0];
			KeyboardNavigation.SetTabOnceActiveElement(target, container);

			// 2. Scroll down so the first item is recycled
			target.ScrollIntoView(10);
			target.UpdateLayout();

			// 3. Verify it is now _focusedElement in the panel
			var panel = (VirtualizingStackPanel) target.Presenter!.Panel!;

			var realizedContainers = target.GetRealizedContainers().ToList();

			// The focused container should still be in Children, but NOT in realizedContainers
			CornerstoneTest.Contains(panel.Children, container);
			CornerstoneTest.DoesNotContain(realizedContainers, container);

			// Now scroll back to top.
			target.ScrollIntoView(0);
			target.UpdateLayout();

			// Check if we have two containers for the same item or other weirdness
			var visibleChildren = panel.Children.Where(c => c.IsVisible).ToList();

			// If it was a ghost, it might still be there or we might have two items for the same thing
			CornerstoneTest.AreEqual(target.GetRealizedContainers().Count(), visibleChildren.Count);

			// 4. Test: Re-insert at top might cause issues if _focusedElement is not updated correctly
			items.Insert(0, "New Item");
			target.UpdateLayout();

			visibleChildren = panel.Children.Where(c => c.IsVisible).ToList();
			CornerstoneTest.AreEqual(target.GetRealizedContainers().Count(), visibleChildren.Count);

			// 5. Remove the focused item while it's recycled
			target.ScrollIntoView(10);
			target.UpdateLayout();
			CornerstoneTest.Contains(panel.Children, container);

			items.RemoveAt(1); // Item 0 was at index 1 because of Insert(0, "New Item")
			target.UpdateLayout();

			// container should be removed from children because RecycleElementOnItemRemoved is called
			CornerstoneTest.DoesNotContain(panel.Children, container);
			CornerstoneTest.IsFalse(container.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void GhostItemTestScrollToManagement()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new ObservableCollection<string>(Enumerable.Range(0, 100).Select(i => $"Item {i}"));

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
				Height = 100, // Show 2 items
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 })
			};

			Prepare(target);

			// 1. ScrollIntoView to trigger _scrollToElement
			// We use a high index and don't call UpdateLayout immediately if we want to catch it in between
			// Actually ScrollIntoView calls layout internally.
			target.ScrollIntoView(50);

			var panel = (VirtualizingStackPanel) target.Presenter!.Panel!;

			// 2. Remove the item we just scrolled to
			items.RemoveAt(50);
			target.UpdateLayout();

			// If it was kept in _scrollToElement and not recycled, it might be a ghost.
			var visibleChildren = panel.Children.Where(c => c.IsVisible).ToList();
			CornerstoneTest.AreEqual(target.GetRealizedContainers().Count(), visibleChildren.Count);
		}
	}

	[PresentationTestMethod]
	public void OpeningSplitViewPaneAfterScrollingKeepsTabOnceContainerIndexed()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));

		var target = CreateSizedListBox();
		var (window, splitView) = CreateSplitViewWindow(target);

		ScrollWhilePaneIsClosed(target, window);

		var tabOnceContainer = CornerstoneTest.IsType<ListBoxItem>(target.ContainerFromIndex(0));
		KeyboardNavigation.SetTabOnceActiveElement(target, tabOnceContainer);

		OpenPane(splitView, target, window);

		var activeContainer = CornerstoneTest.IsType<ListBoxItem>(KeyboardNavigation.GetTabOnceActiveElement(target));
		var activeIndex = target.IndexFromContainer(activeContainer);

		CornerstoneTest.InRange(activeIndex, 0, 5);
		CornerstoneTest.Same(activeContainer, target.ContainerFromIndex(activeIndex));
	}

	[PresentationTestMethod]
	public void OpeningSplitViewPaneAfterScrollingListBoxDoesNotShowUnrealizedContainers()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));

		var items = new[]
		{
			new SizedItem(196, 331),
			new SizedItem(186, 258),
			new SizedItem(196, 321),
			new SizedItem(186, 296),
			new SizedItem(150, 340),
			new SizedItem(196, 319)
		};
		var target = new ListBox
		{
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<SizedItem>((item, _) => new Border
			{
				Width = item?.Width ?? 0,
				Height = item?.Height ?? 0
			}),
			ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel()),
			SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected
		};

		var splitView = new SplitView
		{
			DisplayMode = SplitViewDisplayMode.CompactInline,
			CompactPaneLength = 0,
			OpenPaneLength = 300,
			Pane = target,
			Content = new TextBlock()
		};
		var window = new Window
		{
			Width = 800,
			Height = 804,
			Content = new Grid
			{
				RowDefinitions = new RowDefinitions("30,*"),
				Children = { new TextBlock(), splitView }
			}
		};
		Grid.SetRow(splitView, 1);
		window.Show();

		CornerstoneTest.AreEqual(0, target.Bounds.Width);

		for (var index = 1; index <= 3; ++index)
		{
			target.SelectedIndex = index;
			window.LayoutManager.ExecuteLayoutPass();
		}

		var paneRoot = splitView.GetVisualDescendants()
			.OfType<Panel>()
			.Single(x => x.Name == "PART_PaneRoot");
		paneRoot.Transitions = null;

		splitView.IsPaneOpen = true;
		window.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(300, target.Bounds.Width);

		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(target.Presenter!.Panel);
		var realized = target.GetRealizedContainers().ToHashSet();
		var visibleChildren = panel.Children.Where(x => x.IsVisible).ToList();

		CornerstoneTest.All(visibleChildren, child =>
		{
			CornerstoneTest.AreNotEqual(-1, target.IndexFromContainer(child));
			CornerstoneTest.Contains(realized, child);
		});

		var selectedContainer = CornerstoneTest.IsType<ListBoxItem>(target.ContainerFromIndex(target.SelectedIndex));
		CornerstoneTest.Contains(realized, selectedContainer);
		CornerstoneTest.IsTrue(selectedContainer.Bounds.Bottom > target.Scroll!.Offset.Y);
		CornerstoneTest.IsTrue(selectedContainer.Bounds.Top < (target.Scroll.Offset.Y + target.Scroll.Viewport.Height));
	}

	[PresentationTestMethod]
	public void OpeningSplitViewPaneAfterScrollingOwnContainerItemsDoesNotShowUnrealizedContainers()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));

		var target = new ListBox
		{
			ItemsSource = new[]
			{
				new ListBoxItem { Content = new Border { Width = 196, Height = 331 } },
				new ListBoxItem { Content = new Border { Width = 186, Height = 258 } },
				new ListBoxItem { Content = new Border { Width = 196, Height = 321 } },
				new ListBoxItem { Content = new Border { Width = 186, Height = 296 } },
				new ListBoxItem { Content = new Border { Width = 150, Height = 340 } },
				new ListBoxItem { Content = new Border { Width = 196, Height = 319 } }
			},
			ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel()),
			SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected
		};
		var (window, splitView) = CreateSplitViewWindow(target);

		ScrollWhilePaneIsClosed(target, window);
		OpenPane(splitView, target, window);

		AssertVisibleChildrenAreRealized(target);
	}

	[PresentationTestMethod]
	public void RealizedContainersShouldOnlyIncludeVisibleItemsWithCacheLengthZero()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var letters = "ABCDEFGHIJ".Select(c => c.ToString()).ToList();

			var target = new ListBox
			{
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 }),
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = letters,
				ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
				Height = 100 // Show 2 items (100 / 50 = 2)
			};

			Prepare(target);

			// At the top, only 2 items should be visible (items at index 0 and 1)
			var realizedContainers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();

			// With CacheLength = 0, we should only have the visible items realized
			CornerstoneTest.AreEqual(2, realizedContainers.Count);
			CornerstoneTest.AreEqual("A", realizedContainers[0].Content?.ToString());
			CornerstoneTest.AreEqual("B", realizedContainers[1].Content?.ToString());
		}
	}

	[PresentationTestMethod]
	public void RemovingFirstItemAfterScrollingToEndShouldAllowScrollingToStart()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new ObservableCollection<int>(Enumerable.Range(0, 100));
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<int>((_, _) => new TextBlock { Height = 50 }),
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel())
			};

			Prepare(target);
			target.ScrollIntoView(99);

			items.RemoveAt(0);
			target.ScrollIntoView(0);

			var firstContainer = CornerstoneTest.IsType<ListBoxItem>(target.ContainerFromIndex(0));
			CornerstoneTest.AreEqual(1, firstContainer.Content);
		}
	}

	[PresentationTestMethod]
	public void ReplacedItemsSourceShouldNotShowOldSelectedItemWhenScrolledBack()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var letters = "ABCDEFGHIJ".Select(c => c.ToString()).ToList();
			var numbers = "0123456789".Select(c => c.ToString()).ToList();

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = letters,
				ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
				Height = 100, // Show 2 items
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 })
			};

			Prepare(target);

			// 1. Select a ListBoxItem
			target.SelectedIndex = 0;
			CornerstoneTest.IsTrue(((ListBoxItem) target.Presenter!.Panel!.Children[0]).IsSelected);

			// 2. Scroll until the selected ListBoxItem is no longer visible
			target.ScrollIntoView(letters.Count - 1); // Scroll down to the last item

			// Verify that the first item is no longer realized
			var realizedContainers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();
			CornerstoneTest.DoesNotContain(realizedContainers, x => x.Content as string == "A");

			// 3. Change the ItemsSource
			target.ItemsSource = numbers;

			// 4. Scroll to the top
			target.ScrollIntoView(0);

			// 5. The previously selected ListBoxItem should NOT appear in the ListBox
			var realizedItems = target.GetRealizedContainers()
				.Cast<ListBoxItem>()
				.Select(x => x.Content?.ToString())
				.ToList();

			CornerstoneTest.All(realizedItems, item => CornerstoneTest.DoesNotContain(letters, item));
			CornerstoneTest.AreEqual("0", realizedItems[0]);
		}
	}

	private static void AssertVisibleChildrenAreRealized(ListBox listBox)
	{
		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(listBox.Presenter!.Panel);
		var realized = listBox.GetRealizedContainers().ToHashSet();

		CornerstoneTest.All(panel.Children.Where(x => x.IsVisible), child =>
		{
			CornerstoneTest.AreNotEqual(-1, listBox.IndexFromContainer(child));
			CornerstoneTest.Contains(realized, child);
		});
	}

	private Control CreateListBoxTemplate(TemplatedControl parent, INameScope scope)
	{
		return new ScrollViewer
		{
			Name = "PART_ScrollViewer",
			Template = new FuncControlTemplate(CreateScrollViewerTemplate),
			Content = new ItemsPresenter
			{
				Name = "PART_ItemsPresenter",
				[~ItemsPresenter.ItemsPanelProperty] =
					((ListBox) parent).GetObservable(ItemsControl.ItemsPanelProperty).ToBinding()
			}.RegisterInNameScope(scope)
		}.RegisterInNameScope(scope);
	}

	private Control CreateScrollViewerTemplate(TemplatedControl parent, INameScope scope)
	{
		return new ScrollContentPresenter
		{
			Name = "PART_ContentPresenter",
			[~ContentPresenter.ContentProperty] =
				parent.GetObservable(ContentControl.ContentProperty).ToBinding()
		}.RegisterInNameScope(scope);
	}

	private static ListBox CreateSizedListBox()
	{
		var items = new[]
		{
			new SizedItem(196, 331),
			new SizedItem(186, 258),
			new SizedItem(196, 321),
			new SizedItem(186, 296),
			new SizedItem(150, 340),
			new SizedItem(196, 319)
		};

		return new ListBox
		{
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<SizedItem>((item, _) => new Border
			{
				Width = item?.Width ?? 0,
				Height = item?.Height ?? 0
			}),
			ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel()),
			SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected
		};
	}

	private static (Window Window, SplitView SplitView) CreateSplitViewWindow(ListBox listBox)
	{
		var splitView = new SplitView
		{
			DisplayMode = SplitViewDisplayMode.CompactInline,
			CompactPaneLength = 0,
			OpenPaneLength = 300,
			Pane = listBox,
			Content = new TextBlock()
		};
		var window = new Window
		{
			Width = 800,
			Height = 804,
			Content = new Grid
			{
				RowDefinitions = new RowDefinitions("30,*"),
				Children = { new TextBlock(), splitView }
			}
		};
		Grid.SetRow(splitView, 1);
		window.Show();
		return (window, splitView);
	}

	private static void OpenPane(SplitView splitView, ListBox listBox, Window window)
	{
		var paneRoot = splitView.GetVisualDescendants()
			.OfType<Panel>()
			.Single(x => x.Name == "PART_PaneRoot");
		paneRoot.Transitions = null;

		splitView.IsPaneOpen = true;
		window.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(300, listBox.Bounds.Width);
	}

	private static void Prepare(ListBox target)
	{
		target.Width = target.Height = 100;
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
	}

	private static void ScrollWhilePaneIsClosed(ListBox listBox, Window window)
	{
		CornerstoneTest.AreEqual(0, listBox.Bounds.Width);

		for (var index = 1; index <= 3; ++index)
		{
			listBox.SelectedIndex = index;
			window.LayoutManager.ExecuteLayoutPass();
		}
	}

	#endregion

	#region Classes

	private class Item
	{
		#region Constructors

		public Item(int id)
		{
			Id = id;
		}

		#endregion

		#region Properties

		public int Id { get; }

		#endregion
	}

	private class SizedItem
	{
		#region Constructors

		public SizedItem(double width, double height)
		{
			Width = width;
			Height = height;
		}

		#endregion

		#region Properties

		public double Height { get; }

		public double Width { get; }

		#endregion
	}

	#endregion
}