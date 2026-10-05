#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Navigator.Keystone;

/// <summary>
/// Message / channel layer. Add channels here as the app grows.
/// </summary>
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