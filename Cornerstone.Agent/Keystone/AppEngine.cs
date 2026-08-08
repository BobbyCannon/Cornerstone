#region References

using Cornerstone.Agent.Keystone.Processors;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppEngine : KeystoneEngine<AppBus, AppState>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppEngine(
		AppBus bus,
		AppState state,
		AgentProcessor agentProcessor,
		LogProcessor logProcessor,
		ModelsProcessor modelsProcessor
	) : base(bus, state)
	{
		Agent = Track(agentProcessor);
		Log = Track(logProcessor);
		ModelsProcessor = Track(modelsProcessor);
	}

	#endregion

	#region Properties

	public AgentProcessor Agent { get; }
	public LogProcessor Log { get; }
	public ModelsProcessor ModelsProcessor { get; }

	#endregion
}