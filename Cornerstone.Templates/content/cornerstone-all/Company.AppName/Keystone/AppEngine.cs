#region References

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
	public AppEngine(AppBus bus, AppState state)
		: base(bus, state)
	{
	}

	#endregion
}
