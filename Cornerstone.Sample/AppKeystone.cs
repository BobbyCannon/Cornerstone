#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;

#endregion

namespace Cornerstone.Sample;

[SourceReflection]
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