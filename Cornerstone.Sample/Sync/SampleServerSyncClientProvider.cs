#region References

using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Storage;

#endregion

namespace Cornerstone.Sample.Sync;

[SourceReflection]
[DependencyInjected]
public class SampleServerSyncClientProvider : SampleWebServerSyncClientProvider
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleServerSyncClientProvider(
		SampleServerSyncDatabaseSwitcher databaseProvider,
		IDateTimeProvider dateTimeProvider
	)
		: base(databaseProvider, dateTimeProvider)
	{
	}

	#endregion
}