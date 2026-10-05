#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

[TestClass]
public class ItemsPresenterTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void PanelShouldBeVisualChild()
	{
		var (target, _, _) = CreateTarget();
		var child = target.GetVisualChildren().Single();

		CornerstoneTest.AreEqual(target.Panel, child);
	}

	[PresentationTestMethod]
	public void ShouldRegisterWithHostWhenTemplatedParentSet()
	{
		var host = new ItemsControl();
		var target = new ItemsPresenter();

		CornerstoneTest.IsNull(host.Presenter);

		target.TemplatedParent = host;

		CornerstoneTest.Same(target, host.Presenter);
	}

	private static (ItemsPresenter, ItemsControl, TestRoot) CreateTarget(IReadOnlyList<string> items = null)
	{
		var result = new ItemsPresenter();

		var itemsControl = new ItemsControl
		{
			ItemsSource = items,
			Template = new FuncControlTemplate<ItemsControl>((_, _) => result)
		};

		var root = new TestRoot(itemsControl);
		root.LayoutManager.ExecuteInitialLayoutPass();
		return (result, itemsControl, root);
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
	}

	#endregion

	#region Classes

	[TestClass]
	public class NonVirtualizingPanel : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CreatesContainersForInitialItems()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var (target, _, _) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			AssertContainers(panel, items);
		}

		[PresentationTestMethod]
		public void CreatesContainersForInsertedItems()
		{
			using var app = Start();
			var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
			var (target, _, root) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			items.Insert(1, "foo2");
			root.LayoutManager.ExecuteLayoutPass();

			AssertContainers(panel, items);
		}

		[PresentationTestMethod]
		public void RemovesContainersForRemovedItems()
		{
			using var app = Start();
			var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
			var (target, _, root) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			items.RemoveAt(1);
			root.LayoutManager.ExecuteLayoutPass();

			AssertContainers(panel, items);
		}

		[PresentationTestMethod]
		public void UpdatesContainerForMovedRangeOfItems()
		{
			using var app = Start();
			OldPresentationList<string> items = ["foo", "bar", "baz"];
			var (target, _, root) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			items.MoveRange(0, 2, 2);
			root.LayoutManager.ExecuteLayoutPass();

			AssertContainers(panel, items);
		}

		[PresentationTestMethod]
		public void UpdatesContainersForMovedItems()
		{
			using var app = Start();
			var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
			var (target, _, root) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			items.Move(0, 2);
			root.LayoutManager.ExecuteLayoutPass();

			AssertContainers(panel, items);
		}

		[PresentationTestMethod]
		public void UpdatesContainersForReplacedItems()
		{
			using var app = Start();
			var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
			var (target, _, root) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			items[1] = "bar2";
			root.LayoutManager.ExecuteLayoutPass();

			AssertContainers(panel, items);
		}

		[PresentationTestMethod]
		public void UpdatesContainersOnItemsChanged()
		{
			using var app = Start();
			var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
			var (target, itemsControl, root) = CreateTarget(items);
			var panel = CornerstoneTest.IsType<StackPanel>(target.Panel);

			var newItems = new[] { "qux", "quux", "corge" };
			itemsControl.ItemsSource = newItems;
			root.LayoutManager.ExecuteLayoutPass();

			AssertContainers(panel, newItems);
		}

		private static void AssertContainers(StackPanel panel, IReadOnlyList<string> items)
		{
			CornerstoneTest.AreEqual(items.Count, panel.Children.Count);

			for (var i = 0; i < items.Count; i++)
			{
				var container = CornerstoneTest.IsType<ContentPresenter>(panel.Children[i]);
				CornerstoneTest.AreEqual(items[i], container.DataContext);
				CornerstoneTest.AreEqual(items[i], container.Content);
			}
		}

		#endregion
	}

	#endregion
}