#region References

using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Keystone.Processors;

[SourceReflection]
[DependencyInjected]
public partial class AgentProcessor : AppProcessor
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AgentProcessor(AppBus bus, AppState state) : base(bus, state)
	{
	}

	#endregion

	#region Methods

	public async Task ProcessAsync(string prompt, CancellationToken cancellationToken = default)
	{
	}

	#endregion
}