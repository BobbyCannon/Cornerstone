#region References

using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class EmbeddableControlRootAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void IRootProviderPlatformImplReturnsOwnerPlatformImpl()
	{
		using var services = new CompositorTestServices();
		var peer = ControlAutomationPeer.CreatePeerForElement(services.TopLevel);

		var rootProvider = peer.GetProvider<IRootProvider>();

		CornerstoneTest.IsNotNull(rootProvider);
		CornerstoneTest.Same(services.TopLevel.PlatformImpl, rootProvider!.PlatformImpl);
	}

	[PresentationTestMethod]
	public void PeerProvidesIRootProvider()
	{
		using var services = new CompositorTestServices();
		var peer = ControlAutomationPeer.CreatePeerForElement(services.TopLevel);

		var rootProvider = peer.GetProvider<IRootProvider>();

		CornerstoneTest.IsNotNull(rootProvider);
		CornerstoneTest.Same(peer, rootProvider);
	}

	[PresentationTestMethod]
	public void PeerStillProvidesIEmbeddedRootProvider()
	{
		using var services = new CompositorTestServices();
		var peer = ControlAutomationPeer.CreatePeerForElement(services.TopLevel);

		var embeddedRootProvider = peer.GetProvider<IEmbeddedRootProvider>();

		CornerstoneTest.IsNotNull(embeddedRootProvider);
	}

	#endregion
}