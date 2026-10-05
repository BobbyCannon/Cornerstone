#region References

using System;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Server ISyncClientProvider that speaks the WebSyncClient protocol in-process.
/// GetSyncableDatabase still opens the server SQL database for counts and tests.
/// </summary>
public class SampleWebServerSyncClientProvider : ISyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly Func<AccountEntity> _getAuthenticatedAccount;

	#endregion

	#region Constructors

	public SampleWebServerSyncClientProvider(
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		Func<AccountEntity> getAuthenticatedAccount = null
	)
	{
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
		_getAuthenticatedAccount = getAuthenticatedAccount;
	}

	#endregion

	#region Methods

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _databaseProvider.GetSyncableDatabase();
	}

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		var inner = new SampleServerSyncClient(
			"Server",
			_databaseProvider,
			_dateTimeProvider,
			syncStatistics,
			syncClientProfiler
		)
		{
			AuthenticatedAccount = _getAuthenticatedAccount?.Invoke()
		};
		return new WebSyncClient(
			"Server",
			_dateTimeProvider,
			_databaseProvider,
			new SampleWebClient(inner),
			profiler: syncClientProfiler
		);
	}

	#endregion
}