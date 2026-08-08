#region References

using Cornerstone.Agent.Keystone;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent;

[SourceReflection]
[DependencyInjected]
public class AppKeystone : Keystone<AppBus, AppState, AppEngine, AppViewModel>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppKeystone(AppBus bus, AppState state, AppEngine engine, AppViewModel viewModel, IDateTimeProvider dateTimeProvider)
		: base(bus, state, engine, viewModel, dateTimeProvider)
	{
	}

	#endregion
}