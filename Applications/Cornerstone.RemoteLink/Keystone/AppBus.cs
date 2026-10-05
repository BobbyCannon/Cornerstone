#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppBus : KeystoneBus
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppBus()
	{
	}

	#endregion
}