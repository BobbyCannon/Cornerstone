#region References

using Cornerstone.GrokMonitor.GrokUsage.Processors;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppEngine : KeystoneEngine<AppBus, AppState>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppEngine(
		AppBus bus,
		AppState state,
		GrokUsageProcessor grokUsageProcessor
	) : base(bus, state)
	{
		GrokUsage = Track(grokUsageProcessor);
	}

	#endregion

	#region Properties

	public GrokUsageProcessor GrokUsage { get; }

	#endregion
}