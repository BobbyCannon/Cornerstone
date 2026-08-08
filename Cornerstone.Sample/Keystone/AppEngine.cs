#region References

using Cornerstone.Keystone;
using Cornerstone.Presentation;
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
		ApplicationArguments applicationArguments,
		IDateTimeProvider dateTimeProvider,
		IDispatcher dispatcher,
		IRuntimeInformation runtimeInformation,
		AgentProcessor agentProcessor
	) : base(bus, state)
	{
		ApplicationArguments = applicationArguments;
		DateTimeProvider = dateTimeProvider;
		Dispatcher = dispatcher;
		RuntimeInformation = runtimeInformation;

		Agent = Track(agentProcessor);
	}

	#endregion

	#region Properties

	public AgentProcessor Agent { get; }
	public ApplicationArguments ApplicationArguments { get; }
	public IDateTimeProvider DateTimeProvider { get; }
	public IDispatcher Dispatcher { get; }
	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}