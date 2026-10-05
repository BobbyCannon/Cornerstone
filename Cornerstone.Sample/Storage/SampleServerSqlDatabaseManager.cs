#region References

using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Storage.Sql;

#endregion

namespace Cornerstone.Sample.Storage;

[SourceReflection]
[DependencyInjected]
public class SampleServerSqlDatabaseManager : SampleSqlDatabaseManager
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleServerSqlDatabaseManager(
		IDateTimeProvider dateTimeProvider,
		Profiler profiler,
		IRuntimeInformation runtimeInformation
	)
		: base(
			dateTimeProvider,
			profiler,
			GetSqliteConnectionString(runtimeInformation, "SampleServer.db"),
			SqlProvider.Sqlite
		)
	{
	}

	#endregion
}
