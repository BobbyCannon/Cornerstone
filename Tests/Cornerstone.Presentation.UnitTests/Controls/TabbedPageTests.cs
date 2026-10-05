#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TabbedPageTests
{
	#region Classes

	[TestClass]
	public class DataTemplateTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CustomPageTemplateBuildCreatesContentPageWithCorrectHeader()
		{
			var template = new FuncDataTemplate<DataItem>(
				(item, _) => new ContentPage { Header = item!.Name }, true);
			var tp = new TabbedPage { PageTemplate = template };

			var built = tp.PageTemplate!.Build(new DataItem("Electronics")) as ContentPage;

			CornerstoneTest.IsNotNull(built);
			CornerstoneTest.AreEqual("Electronics", built!.Header);
		}

		[PresentationTestMethod]
		public void DefaultPageDataTemplateIsNonNull()
		{
			CornerstoneTest.IsNotNull(new TabbedPage().PageTemplate);
		}

		[PresentationTestMethod]
		public void DefaultPageDataTemplateWrapsNonPageItemInContentPage()
		{
			// Data items must be wrapped in ContentPage, not shown as raw ToString() headers.
			var tp = new TabbedPage();
			var built = tp.PageTemplate!.Build(new DataItem("Test"));
			CornerstoneTest.IsType<ContentPage>(built);
		}

		[PresentationTestMethod]
		public void ItemsSourceReplacedNoPhantomLogicalChildren()
		{
			var first = new ObservableCollection<DataItem>
			{
				new("One"),
				new("Two")
			};
			var second = new ObservableCollection<DataItem>
			{
				new("Three"),
				new("Four")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = first,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			tp.ItemsSource = second;
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var logicals = ((ILogical) tp).LogicalChildren;
			CornerstoneTest.DoesNotContain(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "One"));
			CornerstoneTest.DoesNotContain(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "Two"));
			CornerstoneTest.Contains(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "Three"));
			CornerstoneTest.Contains(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "Four"));
		}

		[PresentationTestMethod]
		public void ItemsSourceSelectedPageIsNotNullAfterContainersRealized()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("First"),
				new("Second")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(tp.SelectedPage);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
			CornerstoneTest.IsType<ContentPage>(tp.SelectedPage);
			CornerstoneTest.AreEqual("First", tp.SelectedPage!.Header?.ToString());
		}

		[PresentationTestMethod]
		public void ItemsSourceSelectionChangedReportsCorrectPage()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("Alpha"),
				new("Beta")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			Page reportedPage = null;
			tp.SelectionChanged += (_, e) => reportedPage = e.CurrentPage;

			tp.SelectedIndex = 1;
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(tp.SelectedPage);
			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
			CornerstoneTest.AreEqual("Beta", tp.SelectedPage!.Header?.ToString());
			CornerstoneTest.IsNotNull(reportedPage);
			CornerstoneTest.AreEqual("Beta", reportedPage!.Header?.ToString());
		}

		[PresentationTestMethod]
		public void NonListItemsSourceSelectedPageAndAutomationNameAreResolved()
		{
			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = EnumerateItems(new("First"), new("Second")),
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(tp.SelectedPage);
			CornerstoneTest.AreEqual("First", tp.SelectedPage!.Header?.ToString());
			CornerstoneTest.AreEqual("Tab 1 of 2: First", new TabbedPageAutomationPeer(tp).GetName());

			tp.SelectedIndex = 1;
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(tp.SelectedPage);
			CornerstoneTest.AreEqual("Second", tp.SelectedPage!.Header?.ToString());
			CornerstoneTest.AreEqual("Tab 2 of 2: Second", new TabbedPageAutomationPeer(tp).GetName());
		}

		[PresentationTestMethod]
		public void PageTemplateChangedAfterContainersRealizedRebuildsExistingContainers()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("X"),
				new("Y")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = "old-" + item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			tp.PageTemplate = new FuncDataTemplate<DataItem>(
				(item, _) => new ContentPage { Header = "new-" + item!.Name }, false);
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var logicals = ((ILogical) tp).LogicalChildren;
			CornerstoneTest.DoesNotContain(logicals, l => l is ContentPage cp && (cp.Header?.ToString()?.StartsWith("old-") == true));
			CornerstoneTest.Contains(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "new-X"));
			CornerstoneTest.Contains(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "new-Y"));
		}

		[PresentationTestMethod]
		public void PageTemplateChangedAfterContainersRealizedUpdatesSelectedPage()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("A"),
				new("B")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = "old-" + item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			tp.PageTemplate = new FuncDataTemplate<DataItem>(
				(item, _) => new ContentPage { Header = "new-" + item!.Name }, false);
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(tp.SelectedPage);
			CornerstoneTest.AreEqual("new-A", tp.SelectedPage!.Header?.ToString());
		}

		[PresentationTestMethod]
		public void PageTemplateSetToNullAfterContainersRealizedClearsGeneratedPages()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("A"),
				new("B")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var originalSelectedPage = tp.SelectedPage;

			tp.PageTemplate = null;
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNull(tp.SelectedPage);
			CornerstoneTest.IsNull(tp.CurrentPage);
			CornerstoneTest.Empty(((ILogical) tp).LogicalChildren);
			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, originalSelectedPage!);
		}

		[PresentationTestMethod]
		public void ReapplyingTemplateDoesNotLeavePhantomLogicalChildren()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("A"),
				new("B")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var firstSelectedPage = tp.SelectedPage;

			tp.Template = CreateTabbedPageTemplate();
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(2, ((ILogical) tp).LogicalChildren.Count);
			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, firstSelectedPage!);
		}

		[PresentationTestMethod]
		public void ViewModelItemsRemovedFromCollectionTemplateCreatedPageRemovedFromLogicalChildren()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("A"),
				new("B")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			items.RemoveAt(1);
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var logicals = ((ILogical) tp).LogicalChildren;
			CornerstoneTest.DoesNotContain(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "B"));
		}

		[PresentationTestMethod]
		public void ViewModelItemsWithPageTemplateBuildContainersAsContentPages()
		{
			var items = new ObservableCollection<DataItem>
			{
				new("Electronics"),
				new("Books")
			};

			var tp = new TabbedPage
			{
				Width = 400, Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => new ContentPage { Header = item!.Name }, false),
				Template = CreateTabbedPageTemplate()
			};

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var logicals = ((ILogical) tp).LogicalChildren;
			CornerstoneTest.Contains(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "Electronics"));
			CornerstoneTest.Contains(logicals, l => l is ContentPage cp && (cp.Header?.ToString() == "Books"));
		}

		private static FuncControlTemplate<TabbedPage> CreateTabbedPageTemplate()
		{
			return new FuncControlTemplate<TabbedPage>((parent, scope) =>
			{
				var tc = new TabControl
				{
					Name = "PART_TabControl",
					Template = new FuncControlTemplate<TabControl>((_, tcScope) =>
						new ItemsPresenter
						{
							Name = "PART_ItemsPresenter"
						}.RegisterInNameScope(tcScope))
				};
				tc.RegisterInNameScope(scope);
				return tc;
			});
		}

		private static IEnumerable<DataItem> EnumerateItems(params DataItem[] items)
		{
			foreach (var item in items)
			{
				yield return item;
			}
		}

		#endregion

		#region Records

		private record DataItem(string Name);

		#endregion
	}

	[TestClass]
	public class FindNextEnabledTabTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void AllDisabledReturnsMinusOne()
		{
			var tp = new TestableTabbedPage();
			var page0 = new ContentPage();
			var page1 = new ContentPage();
			var page2 = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page0, page1, page2 };
			TabbedPage.SetIsTabEnabled(page0, false);
			TabbedPage.SetIsTabEnabled(page1, false);
			TabbedPage.SetIsTabEnabled(page2, false);

			CornerstoneTest.AreEqual(-1, tp.CallFindNextEnabledTab(0, 1));
			CornerstoneTest.AreEqual(-1, tp.CallFindNextEnabledTab(2, -1));
		}

		[PresentationTestMethod]
		public void AllEnabledReturnsStartIndex()
		{
			var tp = new TestableTabbedPage();
			var page0 = new ContentPage();
			var page1 = new ContentPage();
			var page2 = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page0, page1, page2 };

			var result = tp.CallFindNextEnabledTab(1, 1);
			CornerstoneTest.AreEqual(1, result);
		}

		[PresentationTestMethod]
		public void BackwardSkipsDisabled()
		{
			var tp = new TestableTabbedPage();
			var page0 = new ContentPage();
			var page1 = new ContentPage();
			var page2 = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page0, page1, page2 };
			TabbedPage.SetIsTabEnabled(page1, false);

			var result = tp.CallFindNextEnabledTab(1, -1);
			CornerstoneTest.AreEqual(0, result);
		}

		[PresentationTestMethod]
		public void ForwardSkipsDisabled()
		{
			var tp = new TestableTabbedPage();
			var page0 = new ContentPage();
			var page1 = new ContentPage();
			var page2 = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page0, page1, page2 };
			TabbedPage.SetIsTabEnabled(page1, false);

			var result = tp.CallFindNextEnabledTab(1, 1);
			CornerstoneTest.AreEqual(2, result);
		}

		[PresentationTestMethod]
		public void MultipleConsecutiveDisabledSkipsAll()
		{
			var tp = new TestableTabbedPage();
			var page0 = new ContentPage();
			var page1 = new ContentPage();
			var page2 = new ContentPage();
			var page3 = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page0, page1, page2, page3 };
			TabbedPage.SetIsTabEnabled(page1, false);
			TabbedPage.SetIsTabEnabled(page2, false);

			var result = tp.CallFindNextEnabledTab(1, 1);
			CornerstoneTest.AreEqual(3, result);
		}

		[PresentationTestMethod]
		public void NoEnabledTabAheadReturnsMinusOne()
		{
			var tp = new TestableTabbedPage();
			var page0 = new ContentPage();
			var page1 = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page0, page1 };
			TabbedPage.SetIsTabEnabled(page1, false);

			var result = tp.CallFindNextEnabledTab(1, 1);
			CornerstoneTest.AreEqual(-1, result);
		}

		#endregion
	}

	[TestClass]
	public class IsTabEnabledTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void IsTabEnabledDefaultIsTrue()
		{
			var page = new ContentPage();
			CornerstoneTest.IsTrue(TabbedPage.GetIsTabEnabled(page));
		}

		[PresentationTestMethod]
		public void IsTabEnabledSetFalseGetFalse()
		{
			var page = new ContentPage();
			TabbedPage.SetIsTabEnabled(page, false);
			CornerstoneTest.IsFalse(TabbedPage.GetIsTabEnabled(page));
		}

		[PresentationTestMethod]
		public void IsTabEnabledSetTrueGetTrue()
		{
			var page = new ContentPage();
			TabbedPage.SetIsTabEnabled(page, false);
			TabbedPage.SetIsTabEnabled(page, true);
			CornerstoneTest.IsTrue(TabbedPage.GetIsTabEnabled(page));
		}

		#endregion
	}

	[TestClass]
	public class KeyboardNavigationTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void IsKeyboardNavigationEnabledDefaultIsTrue()
		{
			var tp = new TabbedPage();
			CornerstoneTest.IsTrue(tp.IsKeyboardNavigationEnabled);
		}

		[PresentationTestMethod]
		public void IsKeyboardNavigationEnabledFalseCtrlTabIsNotHandled()
		{
			var tp = new TestableTabbedPage { IsKeyboardNavigationEnabled = false };
			tp.Pages = new OldPresentationList<Page> { new ContentPage(), new ContentPage() };
			tp.SelectedIndex = 0;

			var handled = tp.SimulateKeyDownWithModifiersReturnsHandled(Key.Tab, KeyModifiers.Control);

			CornerstoneTest.IsFalse(handled);
		}

		[PresentationTestMethod]
		public void IsKeyboardNavigationEnabledFalseRightKeyIsNotHandled()
		{
			var tp = new TestableTabbedPage { IsKeyboardNavigationEnabled = false };
			tp.Pages = new OldPresentationList<Page> { new ContentPage(), new ContentPage() };
			tp.SelectedIndex = 0;

			var handled = tp.SimulateKeyDownReturnsHandled(Key.Right);

			CornerstoneTest.IsFalse(handled);
		}

		[PresentationTestMethod]
		public void IsKeyboardNavigationEnabledTrueNoTemplateKeyIsNotHandled()
		{
			var tp = new TestableTabbedPage { IsKeyboardNavigationEnabled = true };
			tp.Pages = new OldPresentationList<Page> { new ContentPage(), new ContentPage() };

			var handled = tp.SimulateKeyDownReturnsHandled(Key.Right);

			CornerstoneTest.IsFalse(handled);
		}

		#endregion
	}

	[TestClass]
	public class KeyboardNavigationWithTemplateTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CtrlShiftTabNavigatesToPreviousPage()
		{
			var tp = MakeTabbed(3, 1);
			var handled = tp.SimulateKeyDownWithModifiersReturnsHandled(Key.Tab, KeyModifiers.Control | KeyModifiers.Shift);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
			CornerstoneTest.IsTrue(handled);
		}

		[PresentationTestMethod]
		public void CtrlTabNavigatesToNextPage()
		{
			var tp = MakeTabbed(3, 0);
			var handled = tp.SimulateKeyDownWithModifiersReturnsHandled(Key.Tab, KeyModifiers.Control);
			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
			CornerstoneTest.IsTrue(handled);
		}

		[PresentationTestMethod]
		public void DownKeyWithVerticalPlacementNavigatesToNextPage()
		{
			var tp = MakeTabbed(3, 0, TabPlacement.Left);
			tp.SimulateKeyDown(Key.Down);
			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void LeftKeyAtFirstPageDoesNotNavigate()
		{
			var tp = MakeTabbed(3, 0);
			tp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void LeftKeyNavigatesToPreviousPage()
		{
			var tp = MakeTabbed(3, 1);
			tp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RightKeyAllTabsAheadDisabledDoesNotNavigate()
		{
			var tp = MakeTabbed(3, 0);
			TabbedPage.SetIsTabEnabled((Page) ((IList) tp.Pages!)[1]!, false);
			TabbedPage.SetIsTabEnabled((Page) ((IList) tp.Pages!)[2]!, false);
			tp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RightKeyAtLastPageDoesNotMarkEventHandled()
		{
			var tp = MakeTabbed(3, 2);
			var handled = tp.SimulateKeyDownReturnsHandled(Key.Right);
			CornerstoneTest.IsFalse(handled);
		}

		[PresentationTestMethod]
		public void RightKeyAtLastPageDoesNotNavigate()
		{
			var tp = MakeTabbed(3, 2);
			tp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(2, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RightKeyMarksEventHandled()
		{
			var tp = MakeTabbed(3, 0);
			var handled = tp.SimulateKeyDownReturnsHandled(Key.Right);
			CornerstoneTest.IsTrue(handled);
		}

		[PresentationTestMethod]
		public void RightKeyNavigatesToNextPage()
		{
			var tp = MakeTabbed(3, 0);
			tp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RightKeySkipsDisabledTab()
		{
			var tp = MakeTabbed(3, 0);
			TabbedPage.SetIsTabEnabled((Page) ((IList) tp.Pages!)[1]!, false);
			tp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(2, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RtlFlowDirectionLeftKeyNavigatesToNextPage()
		{
			var tp = MakeTabbed(3, 0);
			tp.FlowDirection = FlowDirection.RightToLeft;
			tp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RtlFlowDirectionRightKeyNavigatesToPreviousPage()
		{
			var tp = MakeTabbed(3, 1);
			tp.FlowDirection = FlowDirection.RightToLeft;
			tp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void UpKeyWithVerticalPlacementNavigatesToPreviousPage()
		{
			var tp = MakeTabbed(3, 1, TabPlacement.Left);
			tp.SimulateKeyDown(Key.Up);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
		}

		// Builds a TabbedPage with a real PART_TabControl wired up so OnKeyDown can navigate.
		private static TestableTabbedPage MakeTabbed(int pageCount, int selectedIndex = 0,
			TabPlacement placement = TabPlacement.Top)
		{
			var tp = new TestableTabbedPage { TabPlacement = placement };
			for (var i = 0; i < pageCount; i++)
			{
				((OldPresentationList<Page>) tp.Pages!).Add(new ContentPage { Header = $"Tab {i}" });
			}

			tp.Template = new FuncControlTemplate<TabbedPage>((parent, scope) =>
				new TabControl
				{
					Name = "PART_TabControl",
					ItemsSource = parent.Pages
				}.RegisterInNameScope(scope));

			_ = new TestRoot { Child = tp };
			tp.ApplyTemplate();
			tp.SelectedIndex = selectedIndex;
			return tp;
		}

		#endregion
	}

	[TestClass]
	public class LifecycleTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CommitSelectionFiresNavigatedFromOnPreviousPage()
		{
			var tp = new TestableTabbedPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			tp.CallCommitSelection(0, page1);

			NavigatedFromEventArgs args = null;
			page1.NavigatedFrom += (_, e) => args = e;
			tp.CallCommitSelection(1, page2);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(page2, args!.DestinationPage);
			CornerstoneTest.AreEqual(NavigationType.Replace, args.NavigationType);
		}

		[PresentationTestMethod]
		public void CommitSelectionFiresNavigatedToOnNewPage()
		{
			var tp = new TestableTabbedPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			tp.CallCommitSelection(0, page1);

			NavigatedToEventArgs args = null;
			page2.NavigatedTo += (_, e) => args = e;
			tp.CallCommitSelection(1, page2);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(page1, args!.PreviousPage);
			CornerstoneTest.AreEqual(NavigationType.Replace, args.NavigationType);
		}

		[PresentationTestMethod]
		public void CommitSelectionFirstPageNavigatedToHasNullPrevious()
		{
			var tp = new TestableTabbedPage();
			var page = new ContentPage { Header = "A" };

			NavigatedToEventArgs navigatedToArgs = null;
			var events = new List<string>();
			page.NavigatedTo += (_, e) =>
			{
				navigatedToArgs = e;
				events.Add("NavigatedTo");
			};
			page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

			tp.CallCommitSelection(0, page);

			CornerstoneTest.AreEqual(new[] { "NavigatedTo" }, events);
			CornerstoneTest.IsNotNull(navigatedToArgs);
			CornerstoneTest.IsNull(navigatedToArgs!.PreviousPage);
			CornerstoneTest.AreEqual(NavigationType.Replace, navigatedToArgs.NavigationType);
		}

		[PresentationTestMethod]
		public void CommitSelectionLifecycleOrderNavigatedFromNavigatedTo()
		{
			var tp = new TestableTabbedPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			tp.CallCommitSelection(0, page1);

			var order = new List<string>();
			page1.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
			page2.NavigatedTo += (_, _) => order.Add("NavigatedTo");
			tp.CallCommitSelection(1, page2);

			CornerstoneTest.AreEqual(new[] { "NavigatedFrom", "NavigatedTo" }, order);
		}

		[PresentationTestMethod]
		public void CommitSelectionSamePageNoLifecycleEvents()
		{
			var tp = new TestableTabbedPage();
			var page = new ContentPage { Header = "A" };
			tp.CallCommitSelection(0, page);

			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");
			page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");
			tp.CallCommitSelection(0, page);

			CornerstoneTest.Empty(events);
		}

		[PresentationTestMethod]
		public void CommitSelectionToNullFiresNavigatedFromWithNullDestination()
		{
			var tp = new TestableTabbedPage();
			var page = new ContentPage { Header = "A" };
			tp.CallCommitSelection(0, page);

			NavigatedFromEventArgs navigatedFromArgs = null;
			var events = new List<string>();
			page.NavigatedFrom += (_, e) =>
			{
				navigatedFromArgs = e;
				events.Add("NavigatedFrom");
			};

			tp.CallCommitSelection(-1, null);

			CornerstoneTest.AreEqual(new[] { "NavigatedFrom" }, events);
			CornerstoneTest.IsNotNull(navigatedFromArgs);
			CornerstoneTest.IsNull(navigatedFromArgs!.DestinationPage);
			CornerstoneTest.AreEqual(NavigationType.Replace, navigatedFromArgs.NavigationType);
		}

		#endregion
	}

	[TestClass]
	public class PageIconTemplateTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DrawerPageDrawerIconTemplateRoundTrips()
		{
			var template = new FuncDataTemplate<object>((_, _) => new Border());
			var dp = new DrawerPage { DrawerIconTemplate = template };
			CornerstoneTest.Same(template, dp.DrawerIconTemplate);
		}

		[PresentationTestMethod]
		public void DrawerPageDrawerIconWithGeometryDoesNotThrow()
		{
			var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
			var dp = new DrawerPage
			{
				DrawerIcon = geometry,
				DrawerIconTemplate = new FuncDataTemplate<object>((_, _) => new PathIcon())
			};
			var root = new TestRoot { Child = dp };

			dp.DrawerIcon = new EllipseGeometry { Rect = new Rect(0, 0, 20, 20) };
			CornerstoneTest.IsNotNull(dp.DrawerIcon);
		}

		[PresentationTestMethod]
		public void PageIconAcceptsControlValue()
		{
			var icon = new PathIcon { Data = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) } };
			var page = new ContentPage { Icon = icon };
			CornerstoneTest.Same(icon, page.Icon);
		}

		[PresentationTestMethod]
		public void PageIconAcceptsNonControlValue()
		{
			var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
			var page = new ContentPage { Icon = geometry };
			CornerstoneTest.Same(geometry, page.Icon);
		}

		[PresentationTestMethod]
		public void PageIconTemplateRoundTrips()
		{
			var template = new FuncDataTemplate<object>((_, _) => new Border());
			var page = new ContentPage { IconTemplate = template };
			CornerstoneTest.Same(template, page.IconTemplate);
		}

		#endregion
	}

	[TestClass]
	public class PagesChangedEventTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PagesChangedAddArgsContainAddedPage()
		{
			var tp = new TabbedPage();
			var pages = new OldPresentationList<Page>();
			tp.Pages = pages;

			NotifyCollectionChangedEventArgs received = null;
			tp.PagesChanged += (_, e) => received = e;

			var page = new ContentPage { Header = "New" };
			pages.Add(page);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.IsNotNull(received!.NewItems);
			CornerstoneTest.IsTrue(received.NewItems!.Contains(page));
		}

		[PresentationTestMethod]
		public void PagesChangedFiresOnAdd()
		{
			var tp = new TabbedPage();
			var pages = new OldPresentationList<Page>();
			tp.Pages = pages;

			NotifyCollectionChangedEventArgs received = null;
			tp.PagesChanged += (_, e) => received = e;

			pages.Add(new ContentPage());

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, received!.Action);
		}

		[PresentationTestMethod]
		public void PagesChangedFiresOnClearWithResetAction()
		{
			var tp = new TabbedPage();
			var pages = new OldPresentationList<Page>
			{
				new ContentPage { Header = "A" },
				new ContentPage { Header = "B" }
			};
			tp.Pages = pages;

			NotifyCollectionChangedEventArgs received = null;
			tp.PagesChanged += (_, e) => received = e;

			pages.Clear();

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Reset, received!.Action);
		}

		[PresentationTestMethod]
		public void PagesChangedFiresOnRemove()
		{
			var tp = new TabbedPage();
			var page = new ContentPage();
			var pages = new OldPresentationList<Page> { page };
			tp.Pages = pages;

			NotifyCollectionChangedEventArgs received = null;
			tp.PagesChanged += (_, e) => received = e;

			pages.Remove(page);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, received!.Action);
		}

		[PresentationTestMethod]
		public void PagesChangedNotFiredAfterPagesReplaced()
		{
			var tp = new TabbedPage();
			var oldPages = new OldPresentationList<Page>();
			tp.Pages = oldPages;
			var fired = false;
			tp.PagesChanged += (_, _) => fired = true;

			tp.Pages = new OldPresentationList<Page>();
			oldPages.Add(new ContentPage());
			CornerstoneTest.IsFalse(fired);
		}

		[PresentationTestMethod]
		public void PagesChangedRemoveArgsContainRemovedPage()
		{
			var tp = new TabbedPage();
			var page = new ContentPage { Header = "ToRemove" };
			var pages = new OldPresentationList<Page> { page };
			tp.Pages = pages;

			NotifyCollectionChangedEventArgs received = null;
			tp.PagesChanged += (_, e) => received = e;

			pages.Remove(page);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.IsNotNull(received!.OldItems);
			CornerstoneTest.IsTrue(received.OldItems!.Contains(page));
		}

		#endregion
	}

	[TestClass]
	public class PagesCollectionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PagesAddMultipleAllBecomeLogicalChildren()
		{
			var tp = new TabbedPage();
			var pages = new OldPresentationList<Page>();
			tp.Pages = pages;

			var list = new List<ContentPage>();
			for (var i = 0; i < 5; i++)
			{
				var p = new ContentPage { Header = $"Tab {i}" };
				list.Add(p);
				pages.Add(p);
			}

			var children = ((ILogical) tp).LogicalChildren;
			foreach (var p in list)
			{
				CornerstoneTest.Contains(children, p);
			}
		}

		[PresentationTestMethod]
		public void PagesAddedBecomeLogicalChildren()
		{
			var tp = new TabbedPage();
			var pages = new OldPresentationList<Page>();
			tp.Pages = pages;

			var page1 = new ContentPage { Header = "Tab 1" };
			var page2 = new ContentPage { Header = "Tab 2" };
			pages.Add(page1);
			pages.Add(page2);

			var children = ((ILogical) tp).LogicalChildren;
			CornerstoneTest.Contains(children, page1);
			CornerstoneTest.Contains(children, page2);
		}

		[PresentationTestMethod]
		public void PagesClearRemovesAllLogicalChildren()
		{
			var tp = new TabbedPage();
			var a = new ContentPage { Header = "A" };
			var b = new ContentPage { Header = "B" };
			var pages = new OldPresentationList<Page> { a, b };
			tp.Pages = pages;

			pages.Clear();

			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, a);
			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, b);
		}

		[PresentationTestMethod]
		public void PagesInitiallyNonNullEmptyList()
		{
			var tp = new TabbedPage();
			CornerstoneTest.IsNotNull(tp.Pages);
		}

		[PresentationTestMethod]
		public void PagesRemovedRemovedFromLogicalChildren()
		{
			var tp = new TabbedPage();
			var page1 = new ContentPage { Header = "Tab 1" };
			var page2 = new ContentPage { Header = "Tab 2" };
			var pages = new OldPresentationList<Page> { page1, page2 };
			tp.Pages = pages;

			pages.Remove(page1);

			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, page1);
			CornerstoneTest.Contains(((ILogical) tp).LogicalChildren, page2);
		}

		[PresentationTestMethod]
		public void PagesReplacedOldLogicalChildrenClearedNewAdded()
		{
			var tp = new TabbedPage();
			var old = new ContentPage { Header = "Old" };
			tp.Pages = new OldPresentationList<Page> { old };

			var fresh = new ContentPage { Header = "Fresh" };
			tp.Pages = new OldPresentationList<Page> { fresh };

			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, old);
			CornerstoneTest.Contains(((ILogical) tp).LogicalChildren, fresh);
		}

		[PresentationTestMethod]
		public void PagesSetNewListUpdatesProperty()
		{
			var tp = new TabbedPage();
			var pages = new OldPresentationList<Page> { new ContentPage { Header = "A" } };
			tp.Pages = pages;
			CornerstoneTest.Same(pages, tp.Pages);
		}

		[PresentationTestMethod]
		public void PagesSetNullClearsCurrentPage()
		{
			var tp = new TestableTabbedPage();
			var page = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page };
			tp.CallCommitSelection(0, page);
			CornerstoneTest.IsNotNull(tp.CurrentPage);
			tp.Pages = null;
			CornerstoneTest.IsNull(tp.CurrentPage);
		}

		[PresentationTestMethod]
		public void PagesSetNullClearsLogicalChildren()
		{
			var tp = new TabbedPage();
			var page = new ContentPage();
			tp.Pages = new OldPresentationList<Page> { page };
			tp.Pages = null;
			CornerstoneTest.DoesNotContain(((ILogical) tp).LogicalChildren, page);
		}

		#endregion
	}

	[TestClass]
	public class PropertyDefaults : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void SelectedIndexInitiallyMinusOne()
		{
			// -1 is the "no selection" sentinel used throughout the selection API.
			var tp = new TabbedPage();
			CornerstoneTest.AreEqual(-1, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void SelectedPageInitiallyNull()
		{
			var tp = new TabbedPage();
			CornerstoneTest.IsNull(tp.SelectedPage);
		}

		[PresentationTestMethod]
		public void TabPlacementDefaultIsAuto()
		{
			// Auto resolves to Bottom on iOS/Android and Top everywhere else.
			var tp = new TabbedPage();
			CornerstoneTest.AreEqual(TabPlacement.Auto, tp.TabPlacement);
		}

		#endregion
	}

	[TestClass]
	public class PropertyRoundTrips : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void IndicatorTemplateCanBeSetToNull()
		{
			var template = new FuncDataTemplate<object>((_, _) => new Border());
			var tp = new TabbedPage { IndicatorTemplate = template };
			tp.IndicatorTemplate = null;
			CornerstoneTest.IsNull(tp.IndicatorTemplate);
		}

		[PresentationTestMethod]
		public void IndicatorTemplateRoundTrips()
		{
			var template = new FuncDataTemplate<object>((_, _) => new Border());
			var tp = new TabbedPage { IndicatorTemplate = template };
			CornerstoneTest.Same(template, tp.IndicatorTemplate);
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void IsKeyboardNavigationEnabledRoundTrips(bool enabled)
		{
			var tp = new TabbedPage { IsKeyboardNavigationEnabled = enabled };
			CornerstoneTest.AreEqual(enabled, tp.IsKeyboardNavigationEnabled);
		}

		[PresentationTestMethod]
		public void PageTemplateCanBeSetToNull()
		{
			var tp = new TabbedPage { PageTemplate = null };
			CornerstoneTest.IsNull(tp.PageTemplate);
		}

		[PresentationTestMethod]
		public void PageTransitionRoundTrips()
		{
			var transition = new CrossFade(TimeSpan.FromMilliseconds(200));
			var tp = new TabbedPage { PageTransition = transition };
			CornerstoneTest.Same(transition, tp.PageTransition);
		}

		[PresentationTestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public void SelectedIndexStoredBeforeTemplateApplied(int index)
		{
			var tp = new TabbedPage();
			tp.SelectedIndex = index;
			CornerstoneTest.AreEqual(index, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		[DataRow(TabPlacement.Auto)]
		[DataRow(TabPlacement.Top)]
		[DataRow(TabPlacement.Bottom)]
		[DataRow(TabPlacement.Left)]
		[DataRow(TabPlacement.Right)]
		public void TabPlacementRoundTrips(TabPlacement placement)
		{
			var tp = new TabbedPage { TabPlacement = placement };
			CornerstoneTest.AreEqual(placement, tp.TabPlacement);
		}

		#endregion
	}

	[TestClass]
	public class SelectingMultiPageTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void SelectedIndexDirectPropertyRaisesChangedEvent()
		{
			var tp = new TestableTabbedPage();
			var raised = false;
			tp.GetObservable(SelectingMultiPage.SelectedIndexProperty)
				.Subscribe(_ => raised = true);
			tp.CallCommitSelection(0, new ContentPage());
			CornerstoneTest.IsTrue(raised);
		}

		[PresentationTestMethod]
		public void SelectedPageDirectPropertyRaisesChangedEvent()
		{
			var tp = new TestableTabbedPage();
			var raised = false;
			tp.GetObservable(SelectingMultiPage.SelectedPageProperty)
				.Subscribe(_ => raised = true);
			tp.CallCommitSelection(0, new ContentPage());
			CornerstoneTest.IsTrue(raised);
		}

		#endregion
	}

	[TestClass]
	public class SelectionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CommitSelectionNullPageSetsCurrentPageToNull()
		{
			var tp = new TestableTabbedPage();
			tp.CallCommitSelection(0, new ContentPage());
			tp.CallCommitSelection(-1, null);

			CornerstoneTest.IsNull(tp.CurrentPage);
			CornerstoneTest.IsNull(tp.SelectedPage);
			CornerstoneTest.AreEqual(-1, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void CommitSelectionRapidChangesTracksFinalState()
		{
			var tp = new TestableTabbedPage();
			var pages = new[]
			{
				new ContentPage { Header = "A" },
				new ContentPage { Header = "B" },
				new ContentPage { Header = "C" }
			};

			for (var i = 0; i < pages.Length; i++)
			{
				tp.CallCommitSelection(i, pages[i]);
			}

			CornerstoneTest.Same(pages[2], tp.CurrentPage);
			CornerstoneTest.Same(pages[2], tp.SelectedPage);
			CornerstoneTest.AreEqual(2, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void CommitSelectionSequentialSelectionsTracksCorrectPages()
		{
			var tp = new TestableTabbedPage();
			var pages = new[]
			{
				new ContentPage { Header = "Feed" },
				new ContentPage { Header = "Explore" },
				new ContentPage { Header = "Profile" }
			};

			var events = new List<(Page prev, Page curr)>();
			tp.SelectionChanged += (_, e) => events.Add((e.PreviousPage, e.CurrentPage));

			tp.CallCommitSelection(0, pages[0]);
			tp.CallCommitSelection(1, pages[1]);
			tp.CallCommitSelection(2, pages[2]);
			tp.CallCommitSelection(0, pages[0]);

			CornerstoneTest.AreEqual(4, events.Count);
			CornerstoneTest.IsNull(events[0].prev);
			CornerstoneTest.Same(pages[0], events[0].curr);
			CornerstoneTest.Same(pages[0], events[1].prev);
			CornerstoneTest.Same(pages[1], events[1].curr);
			CornerstoneTest.Same(pages[1], events[2].prev);
			CornerstoneTest.Same(pages[2], events[2].curr);
			CornerstoneTest.Same(pages[2], events[3].prev);
			CornerstoneTest.Same(pages[0], events[3].curr);
		}

		[PresentationTestMethod]
		public void CommitSelectionUpdatesCurrentPage()
		{
			var tp = new TestableTabbedPage();
			var page = new ContentPage { Header = "X" };
			tp.CallCommitSelection(0, page);

			CornerstoneTest.Same(page, tp.CurrentPage);
			CornerstoneTest.Same(page, tp.SelectedPage);
			CornerstoneTest.AreEqual(0, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void CurrentPageChangedFiresOnCommitSelection()
		{
			var tp = new TestableTabbedPage();
			var count = 0;
			tp.CurrentPageChanged += (_, _) => count++;

			tp.CallCommitSelection(0, new ContentPage());

			CornerstoneTest.AreEqual(1, count);
		}

		[PresentationTestMethod]
		public void CurrentPageChangedNotFiredWhenSamePageCommitted()
		{
			var tp = new TestableTabbedPage();
			var page = new ContentPage();
			tp.CallCommitSelection(0, page);

			var count = 0;
			tp.CurrentPageChanged += (_, _) => count++;

			tp.CallCommitSelection(0, page);

			CornerstoneTest.AreEqual(0, count);
		}

		[PresentationTestMethod]
		public void SelectionChangedFiresWhenSelectionChanges()
		{
			var tp = new TestableTabbedPage();

			PageSelectionChangedEventArgs received = null;
			tp.SelectionChanged += (_, e) => received = e;

			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			tp.CallCommitSelection(0, page1);
			tp.CallCommitSelection(1, page2);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.Same(page1, received!.PreviousPage);
			CornerstoneTest.Same(page2, received!.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectionChangedNotFiredWhenSamePageSelected()
		{
			var tp = new TestableTabbedPage();
			var count = 0;
			tp.SelectionChanged += (_, _) => count++;

			var page = new ContentPage { Header = "A" };
			tp.CallCommitSelection(0, page);
			var countAfterFirst = count;
			tp.CallCommitSelection(0, page);

			CornerstoneTest.AreEqual(1, countAfterFirst); // first commit must fire exactly once
			CornerstoneTest.AreEqual(1, count); // second commit (same page) must not fire again
		}

		#endregion
	}

	[TestClass]
	public class SwipeGestureTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void MouseSwipeAdvancesTab()
		{
			var tp = CreateSwipeReadyTabbedPage();
			var mouse = new MouseTestHelper();

			mouse.Down(tp, position: new Point(200, 100));
			mouse.Move(tp, new Point(160, 100));
			mouse.Up(tp, position: new Point(160, 100));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void NewGestureIdCanAdvanceAgain()
		{
			var tp = CreateSwipeReadyTabbedPage();

			tp.RaiseEvent(new SwipeGestureEventArgs(7, new Vector(20, 0), default));
			tp.RaiseEvent(new SwipeGestureEventArgs(8, new Vector(20, 0), default));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(2, tp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void SameGestureIdOnlyAdvancesOneTab()
		{
			var tp = CreateSwipeReadyTabbedPage();

			var firstSwipe = new SwipeGestureEventArgs(7, new Vector(20, 0), default);
			var repeatedSwipe = new SwipeGestureEventArgs(7, new Vector(20, 0), default);

			tp.RaiseEvent(firstSwipe);
			tp.RaiseEvent(repeatedSwipe);
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsTrue(firstSwipe.Handled);
			CornerstoneTest.IsFalse(repeatedSwipe.Handled);
			CornerstoneTest.AreEqual(1, tp.SelectedIndex);
		}

		private static TabbedPage CreateSwipeReadyTabbedPage()
		{
			var tp = new TabbedPage
			{
				IsGestureEnabled = true,
				Width = 400,
				Height = 300,
				TabPlacement = TabPlacement.Top,
				SelectedIndex = 0,
				Pages = new OldPresentationList<Page>
				{
					new ContentPage { Header = "A" },
					new ContentPage { Header = "B" },
					new ContentPage { Header = "C" }
				},
				Template = new FuncControlTemplate<TabbedPage>((parent, scope) =>
				{
					var tabControl = new TabControl
					{
						Name = "PART_TabControl",
						ItemsSource = parent.Pages
					};
					scope.Register("PART_TabControl", tabControl);
					return tabControl;
				})
			};
			tp.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;

			var root = new TestRoot
			{
				ClientSize = new Size(400, 300),
				Child = tp
			};
			tp.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			return tp;
		}

		#endregion
	}

	[TestClass]
	public class SystemBackButtonTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BackEventIsForwardedToContent()
		{
			var tp = new TestableTabbedPage();
			var page1 = new ContentPage { Header = "Page1" };
			var page2 = new ContentPage { Header = "Page2" };
			var page3 = new ContentPage { Header = "Page3" };
			var pages = new OldPresentationList<Page> { page1, page2, page3 };
			tp.Pages = pages;

			NotifyCollectionChangedEventArgs received = null;
			tp.PagesChanged += (_, e) => received = e;
			bool isRaisedOnPage1 = false, isRaisedOnPage2 = false, isRaisedOnPage3 = false;
			page1.PageNavigationSystemBackButtonPressed += (s, e) => { isRaisedOnPage1 = true; };
			page2.PageNavigationSystemBackButtonPressed += (s, e) => { isRaisedOnPage2 = true; };
			page3.PageNavigationSystemBackButtonPressed += (s, e) => { isRaisedOnPage3 = true; };
			var root = new TestRoot { Child = tp };
			tp.CallCommitSelection(1, page2);

			var args = RaiseBackButton(tp);

			CornerstoneTest.IsFalse(isRaisedOnPage1);
			CornerstoneTest.IsTrue(isRaisedOnPage2);
			CornerstoneTest.IsFalse(isRaisedOnPage3);
			CornerstoneTest.IsFalse(args.Handled);
		}

		private static RoutedEventArgs RaiseBackButton(Page dp)
		{
			var args = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
			dp.RaiseEvent(args);
			return args;
		}

		#endregion
	}

	[TestClass]
	public class VisualTreeLifecycleTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DetachAndReattachCollectionChangedStillFiresPagesChanged()
		{
			var pages = new OldPresentationList<Page>();
			var tp = new TabbedPage { Pages = pages };
			var root = new TestRoot { Child = tp };

			root.Child = null;
			root.Child = tp;

			var fireCount = 0;
			tp.PagesChanged += (_, _) => fireCount++;

			pages.Add(new ContentPage { Header = "A" });
			pages.Add(new ContentPage { Header = "B" });

			CornerstoneTest.AreEqual(2, fireCount);
		}

		#endregion
	}

	private sealed class TestableTabbedPage : TabbedPage
	{
		#region Methods

		public void CallCommitSelection(int index, Page page)
		{
			CommitSelection(index, page);
		}

		public int CallFindNextEnabledTab(int start, int dir)
		{
			return FindNextEnabledTab(start, dir);
		}

		public void SimulateKeyDown(Key key)
		{
			var e = new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = key };
			OnKeyDown(e);
		}

		public bool SimulateKeyDownReturnsHandled(Key key)
		{
			var e = new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = key };
			OnKeyDown(e);
			return e.Handled;
		}

		public bool SimulateKeyDownWithModifiersReturnsHandled(Key key, KeyModifiers modifiers)
		{
			var e = new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = key, KeyModifiers = modifiers };
			OnKeyDown(e);
			return e.Handled;
		}

		protected override void ApplySelectedIndex(int index)
		{
			base.ApplySelectedIndex(index);
		}

		#endregion
	}

	#endregion
}