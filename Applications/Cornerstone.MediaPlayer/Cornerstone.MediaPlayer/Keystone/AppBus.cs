#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.MediaPlayer.Keystone;

[SourceReflection]
public class AppBus : KeystoneBus
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppBus()
	{
	}

	#endregion
}
