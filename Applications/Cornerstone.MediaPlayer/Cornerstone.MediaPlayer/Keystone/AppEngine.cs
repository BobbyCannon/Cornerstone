#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.MediaPlayer.Keystone;

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
