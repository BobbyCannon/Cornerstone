#region References

using System;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

public class SampleSyncClientProvider : ISyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly Func<AccountEntity> _getAuthenticatedAccount;
	private readonly string _name;

	#endregion

	#region Constructors

	public SampleSyncClientProvider(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		Func<AccountEntity> getAuthenticatedAccount = null
	)
	{
		_name = name;
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
		_getAuthenticatedAccount = getAuthenticatedAccount;
	}

	#endregion

	#region Methods

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		if (_name.StartsWith("Server", StringComparison.OrdinalIgnoreCase))
		{
			return new SampleServerSyncClient(_name, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler)
			{
				AuthenticatedAccount = _getAuthenticatedAccount?.Invoke()
			};
		}

		return new SampleSyncClient(_name, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
	}

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _databaseProvider.GetSyncableDatabase();
	}

	#endregion
}