#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Client;

#endregion

namespace Sample.Shared;

public class SampleSyncManager : SyncManager
{
	#region Fields

	private readonly Dictionary<string, Func<bool>> _testUntil;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public SampleSyncManager(ISyncClientProvider clientSyncClientProvider, IServerSyncClientProvider serverSyncClientProvider,
		IRuntimeInformation runtimeInformation, IDateTimeProvider timeProvider, IDispatcher dispatcher)
		: base(clientSyncClientProvider, serverSyncClientProvider, runtimeInformation,
			timeProvider, dispatcher, "All", "Accounts", "Addresses")
	{
		_testUntil = new Dictionary<string, Func<bool>>();

		foreach (var type in SupportedSyncTypes)
		{
			GetOrAddSyncTimer(type);
		}
	}

	#endregion

	#region Methods

	public SyncSession SyncAccounts(Func<bool> testUntil = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Process("Accounts", options =>
			{
				options.ResetFilters();
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientAccount>());

				_testUntil.AddOrUpdate(options.SyncType, testUntil);
			},
			waitFor,
			postAction);
	}

	public Task<SyncSession> SyncAccountsAsync(Func<bool> testUntil = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Task.Run(() => SyncAccounts(testUntil, waitFor, postAction));
	}

	public SyncSession SyncAddresses(Func<bool> testUntil = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Process("Addresses", options =>
			{
				options.ResetFilters();
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientAddress>());

				_testUntil.AddOrUpdate(options.SyncType, testUntil);
			},
			waitFor,
			postAction);
	}

	public Task<SyncSession> SyncAddressesAsync(Func<bool> testUntil = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Task.Run(() => SyncAddresses(testUntil, waitFor, postAction));
	}

	public SyncSession SyncAll(Func<bool> testUntil = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Process("All", options =>
			{
				options.ResetFilters();
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientAccount>());
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientAddress>());
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientLogEvent>());

				_testUntil.AddOrUpdate(options.SyncType, testUntil);
			},
			waitFor,
			postAction);
	}

	public Task<SyncSession> SyncAllAsync(Func<bool> testUntil = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Task.Run(() => SyncAll(testUntil, waitFor, postAction));
	}

	/// <inheritdoc />
	protected override void OnSyncCompleted(SyncSession session)
	{
		this.DispatchAsync(() => SyncClientProfilerForClient.UpdateWith(session.SyncClientProfilerForClient));
		this.DispatchAsync(() => SyncClientProfilerForServer.UpdateWith(session.SyncClientProfilerForServer));

		base.OnSyncCompleted(session);
	}

	/// <inheritdoc />
	protected override void OnSyncRunning(SyncSession session)
	{
		if (_testUntil.TryGetValue(session.SyncType, out var testUntil))
		{
			ProcessTestUntil(session, testUntil);
			_testUntil.Remove(session.SyncType);
		}
		base.OnSyncRunning(session);
	}

	private void ProcessTestUntil(SyncSession session, Func<bool> testUntil)
	{
		if (testUntil == null)
		{
			return;
		}

		while (!testUntil.Invoke() && !session.SyncCancelled)
		{
			// Delay while getting options
			Thread.Sleep(1);
		}
	}

	#endregion
}