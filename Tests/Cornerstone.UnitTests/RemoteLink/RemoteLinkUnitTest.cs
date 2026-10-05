#region References

using Cornerstone.Presentation;
using Cornerstone.RemoteLink;
using Cornerstone.Runtime;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.UnitTests.RemoteLink;

public abstract class RemoteLinkUnitTest : CornerstoneTest
{
	#region Methods

	protected override void ResetDependencyInjection()
	{
		base.ResetDependencyInjection();
		App.RegisterServices(this, true);

		var runtime = new RuntimeInformation();
		SetSingleton(runtime);
		SetSingleton<IRuntimeInformation>(runtime);
		SetSingleton<IDispatcher>(Dispatcher);

		AppBootstrap.RegisterAsTests(this);
	}

	#endregion
}