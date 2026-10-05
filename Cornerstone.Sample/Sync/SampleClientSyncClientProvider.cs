#region References

using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Storage;

#endregion

namespace Cornerstone.Sample.Sync;

[SourceReflection]
[DependencyInjected]
public class SampleClientSyncClientProvider : SampleSyncClientProvider
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleClientSyncClientProvider(
		SampleClientSyncDatabaseSwitcher databaseProvider,
		IDateTimeProvider dateTimeProvider
	)
		: base("Client", databaseProvider, dateTimeProvider)
	{
	}

	#endregion
}