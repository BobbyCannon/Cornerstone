#region References

using Company.AppName.Keystone.Channels;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Keystone;

/// <summary>
/// Message / channel layer. Add channels here as the app grows.
/// </summary>
[SourceReflection]
public class AppBus : KeystoneBus
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppBus(GreetingChannel greetingChannel)
	{
		Greeting = Track(greetingChannel);
	}

	#endregion

	#region Properties

	public GreetingChannel Greeting { get; private set; }

	#endregion
}
