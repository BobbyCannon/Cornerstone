#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.Processors;

#endregion

namespace Cornerstone.Sample.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppEngine : KeystoneEngine<AppBus, AppState>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppEngine(
		AppBus bus,
		AppState state,
		AgentProcessor agentProcessor
	) : base(bus, state)
	{
		Agent = Track(agentProcessor);
	}

	#endregion

	#region Properties

	public AgentProcessor Agent { get; }

	#endregion
}