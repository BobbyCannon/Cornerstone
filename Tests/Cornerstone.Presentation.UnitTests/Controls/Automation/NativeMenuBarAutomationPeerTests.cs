#region References

using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class NativeMenuBarAutomationPeerTests
{
	#region Methods

	private static FuncControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate((_, ns) =>
			new Menu
			{
				Name = "PART_NativeMenuPresenter",
				Items =
				{
					new MenuItem { Header = "File" },
					new MenuItem { Header = "Edit" }
				}
			}.RegisterInNameScope(ns));
	}

	#endregion

	#region Classes

	[TestClass]
	public class PeerCreation : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ControlTypeIsMenuBar()
		{
			var control = new NativeMenuBar { Template = CreateTemplate() };
			var peer = (NativeMenuBarAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual(AutomationControlType.MenuBar, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesNativeMenuBarAutomationPeer()
		{
			var control = new NativeMenuBar { Template = CreateTemplate() };
			var peer = ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsType<NativeMenuBarAutomationPeer>(peer);
		}

		#endregion
	}

	#endregion
}