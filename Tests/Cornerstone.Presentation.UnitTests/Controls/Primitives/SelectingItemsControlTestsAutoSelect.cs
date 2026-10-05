#region References

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class SelectingItemsControlTestsAutoSelect : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemExecutesWithinLayoutPassWhenBecomingVisible()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList();

		var target = new ListBox
		{
			Template = new FuncControlTemplate(CreateListBoxTemplate),
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
			Height = 100,
			ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 }),
			AutoScrollToSelectedItem = true,
			IsVisible = false
		};

		target.Width = target.Height = 100;
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SelectedIndex = 50;
		target.IsVisible = true;

		target.UpdateLayout();

		var scrollViewer = (ScrollViewer) target.VisualChildren[0];
		CornerstoneTest.InRange(scrollViewer.Offset.Y, 2400, 2500);
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemShouldWorkWhenAncestorBecomesVisible()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList();

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
				Height = 100,
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 }),
				AutoScrollToSelectedItem = true
			};

			target.Width = target.Height = 100;

			var host = new StackPanel
			{
				IsVisible = false,
				Children =
				{
					target
				}
			};

			var root = new TestRoot(host);
			root.LayoutManager.ExecuteInitialLayoutPass();

			target.SelectedIndex = 50;
			CornerstoneTest.IsFalse(target.IsEffectivelyVisible);

			host.IsVisible = true;
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			root.LayoutManager.ExecuteLayoutPass();

			var scrollViewer = (ScrollViewer) target.VisualChildren[0];
			var offset = scrollViewer.Offset.Y;
			CornerstoneTest.InRange(offset, 2400, 2500);
		}
	}

	[PresentationTestMethod]
	public void AutoScrollToSelectedItemShouldWorkWhenBecomingVisible()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList();

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 50 }),
				Height = 100,
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel { CacheLength = 0 }),
				AutoScrollToSelectedItem = true,
				IsVisible = false
			};

			target.Width = target.Height = 100;
			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			// Select item 50
			target.SelectedIndex = 50;

			// Make visible
			target.IsVisible = true;
			target.UpdateLayout();

			// Wait for dispatcher
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			target.UpdateLayout();

			var scrollViewer = (ScrollViewer) target.VisualChildren[0];
			var offset = scrollViewer.Offset.Y;

			// Item 50 is at 50 * 50 = 2500. 
			// ListBox height is 100, so it should be visible if offset is between 2400 and 2500.
			CornerstoneTest.InRange(offset, 2400, 2500);
		}
	}

	[PresentationTestMethod]
	public void CanChangeSelectionFromSelectionChangedWhenAlwaysSelectedReselects()
	{
		// Regression test for the #7536 fix: clearing the selection makes AlwaysSelected
		// reselect the first item (via LostSelection), which raises SelectionChanged. A
		// handler that changes the selection from there must be honoured rather than
		// swallowed by the batch update that wraps the LostSelection handler.
		var target = new TestSelector
		{
			ItemsSource = new[] { "foo", "bar", "baz" },
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 1;

		var raised = 0;
		target.SelectionChanged += (s, e) =>
		{
			if (++raised == 1)
			{
				target.SelectedIndex = 2;
			}
		};

		target.SelectedIndex = -1;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.AreEqual("baz", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void FirstEnabledItemShouldBeSelectedWhenFirstContainerIsDisabled()
	{
		var target = new TestSelector
		{
			ItemsSource = new object[]
			{
				new ListBoxItem { Content = "disabled", IsEnabled = false },
				new ListBoxItem { Content = "enabled" }
			},
			Template = Template()
		};

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void FirstEnabledItemShouldBeSelectedWhenItemsAreAddedToALaidOutControl()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new OldPresentationList<string>();
		var target = new AlwaysSelectedTestSelectorDisablingFirstContainers(1);
		InitWithBeginEndInit(target, items);

		items.Add("item-0");
		items.Add("item-1");

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual("item-1", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void FirstItemShouldBeSelected()
	{
		var target = new TestSelector
		{
			ItemsSource = new[] { "foo", "bar" },
			Template = Template()
		};

		target.ApplyTemplate();

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void FirstItemShouldBeSelectedWhenAdded()
	{
		var items = new OldPresentationList<string>();
		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		items.Add("foo");

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void FirstItemShouldBeSelectedWhenReset()
	{
		var items = new ResetOnAdd();
		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		items.Add("foo");

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void FirstVisibleItemShouldBeSelectedWhenContainerBecomesHiddenDuringPreparation()
	{
		// Regression test for https://github.com/AvaloniaUI/Avalonia/pull/20798
		// Simulates a MVVM scenario where container visibility is set by a binding applied
		// during PrepareContainerForItemOverride (e.g. an ItemContainerTheme). Verifies that
		// selection lands on the first truly visible container rather than an unrealized item.
		var target = new AlwaysSelectedTestSelectorHidingFirstContainers(2)
		{
			ItemsSource = new[] { "item-0", "item-1", "item-2" },
			Template = Template()
		};

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void FirstVisibleItemShouldBeSelectedWhenFirstContainerIsHidden()
	{
		// Uses own-container items (Controls) so that IsVisible can be set before preparation.
		var target = new TestSelector
		{
			ItemsSource = new object[]
			{
				new ListBoxItem { Content = "hidden", IsVisible = false },
				new ListBoxItem { Content = "visible" }
			},
			Template = Template()
		};

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void FirstVisibleItemShouldBeSelectedWhenItemsAreAddedToALaidOutControl()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new OldPresentationList<string>();
		var target = new AlwaysSelectedTestSelectorHidingFirstContainers(1);
		InitWithBeginEndInit(target, items);

		items.Add("item-0");
		items.Add("item-1");

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.AreEqual("item-1", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void ItemShouldBeSelectedWhenSelectionRemoved()
	{
		var items = new OldPresentationList<string>("foo", "bar", "baz", "qux");

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 2;
		items.RemoveAt(2);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void RemovingSelectedFirstItemShouldSelectNextItem()
	{
		var items = new OldPresentationList<string>("foo", "bar");
		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();
		items.RemoveAt(0);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("bar", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SelectionShouldBeClearedWhenAHiddenItemIsAddedToALaidOutControl()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new OldPresentationList<string>();
		var target = new AlwaysSelectedTestSelectorHidingFirstContainers(1);
		InitWithBeginEndInit(target, items);

		items.Add("item-0");

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SelectionShouldBeClearedWhenAllContainersAreHiddenDuringPreparation()
	{
		// When all containers are made invisible during preparation (MVVM binding scenario),
		// SelectedIndex must be -1 rather than the last container's index. Previously the
		// selection would cascade to the last unrealized item and land on an invisible one.
		var target = new AlwaysSelectedTestSelectorHidingFirstContainers(3)
		{
			ItemsSource = new[] { "item-0", "item-1", "item-2" },
			Template = Template()
		};

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectionShouldBeClearedWhenNoItemsLeft()
	{
		var items = new OldPresentationList<string>("foo", "bar");

		var target = new TestSelector
		{
			ItemsSource = items,
			Template = Template()
		};

		target.ApplyTemplate();
		target.SelectedIndex = 1;
		items.RemoveAt(1);
		items.RemoveAt(0);

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SelectionShouldSettleOnTheVisibleItemWhenItemsAreAddedToALaidOutControl()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var items = new OldPresentationList<string>();
		var target = new AlwaysSelectedTestSelectorHidingFirstContainers(1);
		InitWithBeginEndInit(target, items);

		var selectionChanges = new List<object>();
		target.SelectionChanged += (_, _) => selectionChanges.Add(target.SelectedItem);

		items.Add("item-0");
		items.Add("item-1");

		CornerstoneTest.AreEqual("item-1", selectionChanges.Last());
		CornerstoneTest.AreEqual("item-1", target.SelectedItem);
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

	/// <summary>
	/// Initializes and lays out <paramref name="target" /> the way it's done in XAML.
	/// This is important for some tests, as the selection model isn't committed yet while doing so.
	/// </summary>
	private static void InitWithBeginEndInit(SelectingItemsControl target, IEnumerable items)
	{
		target.BeginInit();
		target.Template = Template();
		target.ItemsSource = items;
		target.EndInit();

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
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

	#endregion

	#region Classes

	/// <summary>
	/// A selector with AlwaysSelected that disables the first N containers during preparation.
	/// This simulates a MVVM scenario where an ItemContainerTheme binding sets IsEnabled=false on some containers.
	/// </summary>
	private class AlwaysSelectedTestSelectorDisablingFirstContainers(int disabledCount) : SelectingItemsControl
	{
		#region Constructors

		static AlwaysSelectedTestSelectorDisablingFirstContainers()
		{
			SelectionModeProperty.OverrideDefaultValue<AlwaysSelectedTestSelectorDisablingFirstContainers>(SelectionMode.AlwaysSelected);
		}

		#endregion

		#region Methods

		protected internal override void PrepareContainerForItemOverride(Control container, object item, int index)
		{
			base.PrepareContainerForItemOverride(container, item, index);
			if (index < disabledCount)
			{
				container.IsEnabled = false;
			}
		}

		#endregion
	}

	/// <summary>
	/// A selector with AlwaysSelected that hides the first N containers during preparation.
	/// This simulates a MVVM scenario where an ItemContainerTheme binding sets IsVisible=false on some containers.
	/// </summary>
	private class AlwaysSelectedTestSelectorHidingFirstContainers(int hiddenCount) : SelectingItemsControl
	{
		#region Constructors

		static AlwaysSelectedTestSelectorHidingFirstContainers()
		{
			SelectionModeProperty.OverrideDefaultValue<AlwaysSelectedTestSelectorHidingFirstContainers>(SelectionMode.AlwaysSelected);
		}

		#endregion

		#region Methods

		protected internal override void PrepareContainerForItemOverride(Control container, object item, int index)
		{
			base.PrepareContainerForItemOverride(container, item, index);
			if (index < hiddenCount)
			{
				container.IsVisible = false;
			}
		}

		#endregion
	}

	private class ResetOnAdd : List<string>, INotifyCollectionChanged
	{
		#region Methods

		public new void Add(string item)
		{
			base.Add(item);
			CollectionChanged?.Invoke(
				this,
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion
	}

	private class TestSelector : SelectingItemsControl
	{
		#region Constructors

		static TestSelector()
		{
			SelectionModeProperty.OverrideDefaultValue<TestSelector>(SelectionMode.AlwaysSelected);
		}

		#endregion
	}

	#endregion
}