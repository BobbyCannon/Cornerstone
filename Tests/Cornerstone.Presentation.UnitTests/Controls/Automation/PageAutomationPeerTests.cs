#region References

using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class PageAutomationPeerTests
{
	#region Classes

	[TestClass]
	public class CarouselPagePeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ControlTypeIsPane()
		{
			var page = new CarouselPage();
			var peer = (CarouselPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(AutomationControlType.Pane, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesCarouselPageAutomationPeer()
		{
			var page = new CarouselPage();
			var peer = ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsType<CarouselPageAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void NameIsEmptyWhenNoHeader()
		{
			var page = new CarouselPage();
			var peer = (CarouselPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsTrue(string.IsNullOrEmpty(peer.GetName()));
		}

		[PresentationTestMethod]
		public void NameReturnsStringHeader()
		{
			var page = new CarouselPage { Header = "Photos" };
			var peer = (CarouselPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Photos", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameReturnsToStringForNonStringHeader()
		{
			var page = new CarouselPage { Header = 7 };
			var peer = (CarouselPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("7", peer.GetName());
		}

		#endregion
	}

	[TestClass]
	public class ContentPagePeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ControlTypeIsPane()
		{
			var page = new ContentPage();
			var peer = (ContentPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(AutomationControlType.Pane, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesContentPageAutomationPeer()
		{
			var page = new ContentPage();
			var peer = ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsType<ContentPageAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void NameIsEmptyWhenNoHeader()
		{
			var page = new ContentPage();
			var peer = (ContentPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsTrue(string.IsNullOrEmpty(peer.GetName()));
		}

		[PresentationTestMethod]
		public void NameReturnsStringHeader()
		{
			var page = new ContentPage { Header = "Settings" };
			var peer = (ContentPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Settings", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameReturnsToStringForNonStringHeader()
		{
			var page = new ContentPage { Header = 42 };
			var peer = (ContentPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("42", peer.GetName());
		}

		#endregion
	}

	[TestClass]
	public class DrawerPagePeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CollapseSetsIsOpenFalse()
		{
			var page = new DrawerPage { IsOpen = true };
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			peer.Collapse();

			CornerstoneTest.IsFalse(page.IsOpen);
		}

		[PresentationTestMethod]
		public void ControlTypeIsPane()
		{
			var page = new DrawerPage();
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(AutomationControlType.Pane, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesDrawerPageAutomationPeer()
		{
			var page = new DrawerPage();
			var peer = ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsType<DrawerPageAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void ExpandCollapseStateCollapsedWhenClosed()
		{
			var page = new DrawerPage { IsOpen = false };
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
		}

		[PresentationTestMethod]
		public void ExpandCollapseStateExpandedWhenOpen()
		{
			var page = new DrawerPage { IsOpen = true };
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, peer.ExpandCollapseState);
		}

		[PresentationTestMethod]
		public void ExpandSetsIsOpenTrue()
		{
			var page = new DrawerPage { IsOpen = false };
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			peer.Expand();

			CornerstoneTest.IsTrue(page.IsOpen);
		}

		[PresentationTestMethod]
		public void ImplementsIExpandCollapseProvider()
		{
			var page = new DrawerPage();
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsAssignableFrom<IExpandCollapseProvider>(peer);
		}

		[PresentationTestMethod]
		public void NameIsEmptyWhenNoHeader()
		{
			var page = new DrawerPage();
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsTrue(string.IsNullOrEmpty(peer.GetName()));
		}

		[PresentationTestMethod]
		public void NameReturnsStringHeader()
		{
			var page = new DrawerPage { Header = "Menu" };
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Menu", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameReturnsToStringForNonStringHeader()
		{
			var page = new DrawerPage { Header = 42 };
			var peer = (DrawerPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("42", peer.GetName());
		}

		#endregion
	}

	[TestClass]
	public class NavigationPagePeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ControlTypeIsPane()
		{
			var page = new NavigationPage();
			var peer = (NavigationPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(AutomationControlType.Pane, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesNavigationPageAutomationPeer()
		{
			var page = new NavigationPage();
			var peer = ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsType<NavigationPageAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void NameFallsBackToCurrentPageHeader()
		{
			var inner = new ContentPage { Header = "Details" };
			var page = new NavigationPage();
			page.SetCurrentValue(Page.CurrentPageProperty, inner);

			var peer = (NavigationPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Details", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameIsEmptyWhenNoHeaderAndNoCurrentPage()
		{
			var page = new NavigationPage();
			var peer = (NavigationPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsTrue(string.IsNullOrEmpty(peer.GetName()));
		}

		[PresentationTestMethod]
		public void NamePrioritizesOwnHeaderOverCurrentPageHeader()
		{
			var inner = new ContentPage { Header = "Details" };
			var page = new NavigationPage { Header = "Navigation" };
			page.SetCurrentValue(Page.CurrentPageProperty, inner);

			var peer = (NavigationPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Navigation", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameReturnsOwnHeaderWhenSet()
		{
			var page = new NavigationPage { Header = "Navigation" };
			var peer = (NavigationPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Navigation", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameReturnsToStringForNonStringHeader()
		{
			var page = new NavigationPage { Header = 42 };
			var peer = (NavigationPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("42", peer.GetName());
		}

		#endregion
	}

	[TestClass]
	public class TabbedPagePeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ControlTypeIsPane()
		{
			var page = new TabbedPage();
			var peer = (TabbedPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual(AutomationControlType.Pane, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesTabbedPageAutomationPeer()
		{
			var page = new TabbedPage();
			var peer = ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsType<TabbedPageAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void NameIsEmptyWhenNoHeader()
		{
			var page = new TabbedPage();
			var peer = (TabbedPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.IsTrue(string.IsNullOrEmpty(peer.GetName()));
		}

		[PresentationTestMethod]
		public void NameReturnsStringHeader()
		{
			var page = new TabbedPage { Header = "Main" };
			var peer = (TabbedPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("Main", peer.GetName());
		}

		[PresentationTestMethod]
		public void NameReturnsToStringForNonStringHeader()
		{
			var page = new TabbedPage { Header = 42 };
			var peer = (TabbedPageAutomationPeer) ControlAutomationPeer.CreatePeerForElement(page);

			CornerstoneTest.AreEqual("42", peer.GetName());
		}

		#endregion
	}

	#endregion
}