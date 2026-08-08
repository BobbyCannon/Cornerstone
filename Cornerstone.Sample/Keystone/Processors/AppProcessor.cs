#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Keystone.Processors;

[SourceReflection]
[DependencyInjected]
public abstract class AppProcessor : KeystoneProcessor<AppBus, AppState>
{
	#region Constructors

	protected AppProcessor(AppBus bus, AppState state)
		: base(bus, state)
	{
	}

	#endregion
}