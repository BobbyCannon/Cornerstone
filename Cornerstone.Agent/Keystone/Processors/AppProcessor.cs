#region References

using Cornerstone.Keystone;

#endregion

namespace Cornerstone.Agent.Keystone.Processors;

public abstract class AppProcessor : KeystoneProcessor<AppBus, AppState>
{
	#region Constructors

	protected AppProcessor(AppBus bus, AppState state)
		: base(bus, state)
	{
	}

	#endregion
}