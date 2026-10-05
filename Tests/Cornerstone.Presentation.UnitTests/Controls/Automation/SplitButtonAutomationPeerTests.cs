#region References

using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class SplitButtonAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClassNameIsSplitButton()
	{
		var target = new SplitButton();
		var peer = (SplitButtonAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual("SplitButton", peer.GetClassName());
	}

	[PresentationTestMethod]
	public void ControlTypeIsSplitButton()
	{
		var target = new SplitButton();
		var peer = (SplitButtonAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual(AutomationControlType.SplitButton, peer.GetAutomationControlType());
	}

	[PresentationTestMethod]
	public void CreatesSplitButtonAutomationPeer()
	{
		var target = new SplitButton();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsType<SplitButtonAutomationPeer>(peer);
	}

	[PresentationTestMethod]
	public void ExpandCollapseRaisesPropertyChanged()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new SplitButton
			{
				Flyout = new Flyout()
			};
			var window = new Window
			{
				Content = target
			};
			window.Show();

			var peer = (SplitButtonAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);
			AutomationPropertyChangedEventArgs changed = null;
			peer.PropertyChanged += (_, e) =>
			{
				if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
				{
					changed = e;
				}
			};

			peer.Expand();

			CornerstoneTest.IsNotNull(changed);
			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, changed!.OldValue);
			CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, changed.NewValue);
		}
	}

	[PresentationTestMethod]
	public void ExpandCollapseStateTracksFlyout()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new SplitButton
			{
				Flyout = new Flyout()
			};
			var window = new Window
			{
				Content = target
			};
			window.Show();

			var peer = (SplitButtonAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);

			peer.Expand();
			CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, peer.ExpandCollapseState);
			CornerstoneTest.IsTrue(target.Flyout?.IsOpen);

			peer.Collapse();
			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
			CornerstoneTest.IsFalse(target.Flyout!.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ImplementsIExpandCollapseProvider()
	{
		var target = new SplitButton();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsAssignableFrom<IExpandCollapseProvider>(peer);
	}

	[PresentationTestMethod]
	public void ImplementsIInvokeProvider()
	{
		var target = new SplitButton();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsAssignableFrom<IInvokeProvider>(peer);
	}

	[PresentationTestMethod]
	public void InvokeTriggersClick()
	{
		var clicked = 0;
		var target = new SplitButton();
		var peer = (IInvokeProvider) ControlAutomationPeer.CreatePeerForElement(target);

		target.Click += (_, _) => clicked++;
		peer.Invoke();

		CornerstoneTest.AreEqual(1, clicked);
	}

	[PresentationTestMethod]
	public void ShowsMenuIsTrue()
	{
		var target = new SplitButton();
		var peer = (IExpandCollapseProvider) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsTrue(peer.ShowsMenu);
	}

	#endregion
}

[TestClass]
public class ToggleSplitButtonAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClassNameIsToggleSplitButton()
	{
		var target = new ToggleSplitButton();
		var peer = (ToggleSplitButtonAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual("ToggleSplitButton", peer.GetClassName());
	}

	[PresentationTestMethod]
	public void ControlTypeIsSplitButton()
	{
		var target = new ToggleSplitButton();
		var peer = (SplitButtonAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual(AutomationControlType.SplitButton, peer.GetAutomationControlType());
	}

	[PresentationTestMethod]
	public void CreatesToggleSplitButtonAutomationPeer()
	{
		var target = new ToggleSplitButton();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsType<ToggleSplitButtonAutomationPeer>(peer);
	}

	[PresentationTestMethod]
	public void ImplementsIToggleProvider()
	{
		var target = new ToggleSplitButton();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsAssignableFrom<IToggleProvider>(peer);
	}

	[PresentationTestMethod]
	public void ToggleChangesIsCheckedAndFiresClick()
	{
		var clicked = 0;
		var target = new ToggleSplitButton();
		var peer = (IToggleProvider) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual(ToggleState.Off, peer.ToggleState);

		target.Click += (_, _) => clicked++;
		peer.Toggle();

		CornerstoneTest.IsTrue(target.IsChecked);
		CornerstoneTest.AreEqual(ToggleState.On, peer.ToggleState);
		CornerstoneTest.AreEqual(1, clicked);
	}

	#endregion
}