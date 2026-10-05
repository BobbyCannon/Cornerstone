#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class PageNavigationHostTests
{
	#region Classes

	[TestClass]
	public class LifecycleEventTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void InitialLayoutWithExistingPageDoesNotThrowWhenContentPresenterChildIsAssigned()
		{
			var page = new ContentPage { Header = "Home" };
			var host = new PageNavigationHost { Page = page };
			var root = new TestRoot { Child = host };

			var exception = Record.Exception(() => root.LayoutManager.ExecuteInitialLayoutPass());

			CornerstoneTest.IsNull(exception);
			CornerstoneTest.IsNotNull(host.Presenter);
			CornerstoneTest.Same(page, host.Presenter!.Child);
		}

		[PresentationTestMethod]
		public void PageChangedFiresLifecycleEventsInOrder()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };

			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = first;

			var order = new List<string>();
			first.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
			second.NavigatedTo += (_, _) => order.Add("NavigatedTo");

			host.Page = second;

			CornerstoneTest.AreEqual(2, order.Count);
			CornerstoneTest.AreEqual("NavigatedFrom", order[0]);
			CornerstoneTest.AreEqual("NavigatedTo", order[1]);
		}

		[PresentationTestMethod]
		public void PageChangedFiresNavigatedFromOnOldPage()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };

			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = first;

			NavigatedFromEventArgs args = null;
			first.NavigatedFrom += (_, e) => args = e;

			host.Page = second;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
			CornerstoneTest.Same(second, args!.DestinationPage);
		}

		[PresentationTestMethod]
		public void PageChangedFiresNavigatedToOnNewPage()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };

			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = first;

			NavigatedToEventArgs args = null;
			second.NavigatedTo += (_, e) => args = e;

			host.Page = second;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
			CornerstoneTest.Same(first, args!.PreviousPage);
		}

		[PresentationTestMethod]
		public void PageSetFiresNavigatedTo()
		{
			var page = new ContentPage { Header = "Home" };
			var fired = false;
			page.NavigatedTo += (_, _) => fired = true;

			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = page;

			CornerstoneTest.IsTrue(fired);
		}

		[PresentationTestMethod]
		public void PageSetNavigatedToNavigationTypeIsReplace()
		{
			var page = new ContentPage { Header = "Home" };
			NavigatedToEventArgs args = null;
			page.NavigatedTo += (_, e) => args = e;

			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = page;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
		}

		[PresentationTestMethod]
		public void PageSetNavigatedToPreviousPageIsNull()
		{
			var page = new ContentPage { Header = "Home" };
			NavigatedToEventArgs args = null;
			page.NavigatedTo += (_, e) => args = e;

			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = page;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.IsNull(args!.PreviousPage);
		}

		[PresentationTestMethod]
		public void PageSetToNullFiresNavigatedFromOnOldPage()
		{
			var page = new ContentPage { Header = "Home" };
			var host = new PageNavigationHost();
			var root = new TestRoot { Child = host };
			host.Page = page;

			NavigatedFromEventArgs args = null;
			page.NavigatedFrom += (_, e) => args = e;

			host.Page = null;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.IsNull(args!.DestinationPage);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
		}

		[PresentationTestMethod]
		public void ReplacingPageResetsOldPresenterChildSafeAreaPadding()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };
			var host = new PageNavigationHost { Page = first };
			var root = new TestRoot { Child = host };

			root.LayoutManager.ExecuteInitialLayoutPass();
			first.SafeAreaPadding = new Thickness(1, 2, 3, 4);

			var exception = Record.Exception(() => host.Page = second);

			CornerstoneTest.IsNull(exception);
			CornerstoneTest.AreEqual(default, first.SafeAreaPadding);
			CornerstoneTest.IsNotNull(host.Presenter);
			CornerstoneTest.Same(second, host.Presenter!.Child);
		}

		#endregion
	}

	[TestClass]
	public class SystemBackButtonTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BackRequestedForwardsToNestedCurrentPageOnce()
		{
			using var app = UnitTestApplication.Start(TestServices.StyledWindow);
			var child = new ContentPage { Header = "Child" };
			var parent = new CarouselPage
			{
				Pages = new OldPresentationList<Page> { child }
			};
			var host = new PageNavigationHost { Page = parent };
			var window = new Window
			{
				Width = 400,
				Height = 300,
				Content = host
			};
			var raiseCount = 0;
			child.PageNavigationSystemBackButtonPressed += (_, e) =>
			{
				raiseCount++;
				e.Handled = true;
			};

			window.Show();

			CornerstoneTest.Same(child, parent.CurrentPage);
			CornerstoneTest.Same(parent, host.Presenter?.Child);

			var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
			window.RaiseEvent(args);

			CornerstoneTest.AreEqual(1, raiseCount);
			CornerstoneTest.IsTrue(args.Handled);
		}

		#endregion
	}

	#endregion
}