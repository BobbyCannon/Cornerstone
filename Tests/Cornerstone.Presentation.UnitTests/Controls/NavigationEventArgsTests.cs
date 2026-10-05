#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class NavigationEventArgsTests
{
	#region Classes

	[TestClass]
	public class ModalPoppedEventArgsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertiesRoundTrip()
		{
			var modal = new ContentPage();
			var args = new ModalPoppedEventArgs(modal);
			CornerstoneTest.Same(modal, args.Modal);
		}

		#endregion
	}

	[TestClass]
	public class ModalPushedEventArgsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertiesRoundTrip()
		{
			var modal = new ContentPage();
			var args = new ModalPushedEventArgs(modal);
			CornerstoneTest.Same(modal, args.Modal);
		}

		#endregion
	}

	[TestClass]
	public class NavigatedFromEventArgsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertiesRoundTrip()
		{
			var dest = new ContentPage { Header = "Dest" };
			var args = new NavigatedFromEventArgs(dest, NavigationType.Pop);
			CornerstoneTest.Same(dest, args.DestinationPage);
			CornerstoneTest.AreEqual(NavigationType.Pop, args.NavigationType);
		}

		#endregion
	}

	[TestClass]
	public class NavigatedToEventArgsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void NullPreviousPageIsAllowed()
		{
			var args = new NavigatedToEventArgs(null, NavigationType.PopToRoot);
			CornerstoneTest.IsNull(args.PreviousPage);
		}

		[PresentationTestMethod]
		public void PropertiesRoundTrip()
		{
			var prev = new ContentPage { Header = "Prev" };
			var args = new NavigatedToEventArgs(prev, NavigationType.Push);
			CornerstoneTest.Same(prev, args.PreviousPage);
			CornerstoneTest.AreEqual(NavigationType.Push, args.NavigationType);
		}

		#endregion
	}

	[TestClass]
	public class NavigatingFromEventArgsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CancelCanBeSetTrue()
		{
			var args = new NavigatingFromEventArgs(null, NavigationType.Push) { Cancel = true };
			CornerstoneTest.IsTrue(args.Cancel);
		}

		[PresentationTestMethod]
		public void CancelDefaultIsFalse()
		{
			var args = new NavigatingFromEventArgs(null, NavigationType.Push);
			CornerstoneTest.IsFalse(args.Cancel);
		}

		#endregion
	}

	[TestClass]
	public class NavigationEventArgsConstructionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertiesRoundTrip()
		{
			var page = new ContentPage();
			var args = new NavigationEventArgs(page, NavigationType.Push);
			CornerstoneTest.Same(page, args.Page);
			CornerstoneTest.AreEqual(NavigationType.Push, args.NavigationType);
		}

		#endregion
	}

	[TestClass]
	public class NavigationTypeEnumTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void AllValuesAreDefined()
		{
			var values = Enum.GetValues<NavigationType>();
			CornerstoneTest.Contains(values, NavigationType.Push);
			CornerstoneTest.Contains(values, NavigationType.Pop);
			CornerstoneTest.Contains(values, NavigationType.PopToRoot);
			CornerstoneTest.Contains(values, NavigationType.Insert);
			CornerstoneTest.Contains(values, NavigationType.Remove);
			CornerstoneTest.Contains(values, NavigationType.Replace);
		}

		#endregion
	}

	[TestClass]
	public class PageSelectionChangedEventArgsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PropertiesRoundTrip()
		{
			var prev = new ContentPage { Header = "Tab 1" };
			var current = new ContentPage { Header = "Tab 2" };
			var args = new PageSelectionChangedEventArgs(SelectingMultiPage.SelectionChangedEvent, prev, current);
			CornerstoneTest.Same(prev, args.PreviousPage);
			CornerstoneTest.Same(current, args.CurrentPage);
		}

		#endregion
	}

	#endregion
}