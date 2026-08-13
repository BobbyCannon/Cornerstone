#region References

using Cornerstone.GrokMonitor.GrokUsage.Channels;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppBus : KeystoneBus
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppBus(GrokUsageChannel grokUsageChannel)
	{
		GrokUsage = Track(grokUsageChannel);
	}

	#endregion

	#region Properties

	public GrokUsageChannel GrokUsage { get; }

	#endregion
}