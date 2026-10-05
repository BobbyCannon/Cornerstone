#region References

using Cornerstone.Presentation;
using Cornerstone.RemoteLink;
using Cornerstone.RemoteLink.AirPlay;
using Cornerstone.RemoteLink.Android;
using Cornerstone.RemoteLink.Keystone;
using Cornerstone.RemoteLink.Keystone.State;
using Cornerstone.RemoteLink.Vnc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.RemoteLink;

[TestClass]
public class AppViewModelTests : RemoteLinkUnitTest
{
	#region Methods

	[TestMethod]
	public void DoesNotAdvertiseUntilStart()
	{
		var service = new AirPlayMirrorService(Dispatcher);
		var viewModel = new AppViewModel(
			new AppBus(),
			new AppState(new AppSettings()),
			service,
			new AndroidMirrorService(Dispatcher),
			GetInstance<VncConnectionService>(),
			GetInstance<ClipboardService>(),
			this,
			Dispatcher,
			RuntimeInformation);
		IsFalse(service.IsAdvertising);
		viewModel.InitializeLifecycle();
		viewModel.LoadLifecycle();
		IsFalse(service.IsAdvertising);
		viewModel.StartLifecycle();
		IsTrue(service.IsAdvertising);
		viewModel.UninitializeLifecycle();
		IsFalse(service.IsAdvertising);
	}

	#endregion
}