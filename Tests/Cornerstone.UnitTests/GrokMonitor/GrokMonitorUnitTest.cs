#region References

using Cornerstone.GrokMonitor;
using Cornerstone.Runtime;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor;

public abstract class GrokMonitorUnitTest : CornerstoneTest
{
	#region Methods

	protected override void ResetDependencyInjection()
	{
		base.ResetDependencyInjection();
		App.RegisterServices(this, true);

		// Pin concrete RuntimeInformation (SetSingleton overwrites generator / design-stub bindings).
		var runtime = new RuntimeInformation();
		SetSingleton(runtime);
		SetSingleton<IRuntimeInformation>(runtime);

		AppBootstrap.RegisterAsTests(this);
	}

	#endregion
}
