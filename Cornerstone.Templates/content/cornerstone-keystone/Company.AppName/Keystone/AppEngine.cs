#region References

using Company.AppName.Keystone.Processors;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Keystone;

/// <summary>
/// Host for processors that mutate <see cref="AppState" /> via the bus.
/// </summary>
[SourceReflection]
public class AppEngine : KeystoneEngine<AppBus, AppState>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppEngine(AppBus bus, AppState state, GreetingProcessor greetingProcessor)
		: base(bus, state)
	{
		Greeting = Track(greetingProcessor);
	}

	#endregion

	#region Properties

	public GreetingProcessor Greeting { get; }

	#endregion
}
