#region References

using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Storage.Sql;

#endregion

namespace Cornerstone.Sample.Storage;

[SourceReflection]
[DependencyInjected]
public class SampleClientSqlDatabaseManager : SampleSqlDatabaseManager
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleClientSqlDatabaseManager(
		IDateTimeProvider dateTimeProvider,
		Profiler profiler,
		IRuntimeInformation runtimeInformation
	)
		: base(
			dateTimeProvider,
			profiler,
			GetSqliteConnectionString(runtimeInformation, "SampleClient.db"),
			SqlProvider.Sqlite
		)
	{
	}

	#endregion
}
