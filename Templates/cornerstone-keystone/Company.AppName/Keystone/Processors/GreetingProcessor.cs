#region References

using Company.AppName.Keystone.Channels;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Keystone.Processors;

/// <summary>
/// Writes <see cref="AppState.Greeting" /> when the home view publishes a greet message.
/// </summary>
[SourceReflection]
[ChannelHandlers]
public partial class GreetingProcessor : KeystoneProcessor<AppBus, AppState>
{
	#region Fields

	private int _greetings;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public GreetingProcessor(AppBus bus, AppState state)
		: base(bus, state)
	{
	}

	#endregion

	#region Methods

	private void OnGreet(GreetingChannel.GreetMessage _)
	{
		_greetings++;
		State.Greeting = $"Cornerstone is listening. Greetings: {_greetings}.";
	}

	#endregion
}
