#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink.Keystone;

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