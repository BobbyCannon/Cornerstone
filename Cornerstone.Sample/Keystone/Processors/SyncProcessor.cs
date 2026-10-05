#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Diagnostics;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.Channels;
using Cornerstone.Sample.Keystone.State;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Keystone.Processors;

[SourceReflection]
[DependencyInjected]
public partial class SyncProcessor : AppProcessor
{
	#region Fields

	private readonly SampleClientSyncDatabaseSwitcher _clientSwitcher;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly SampleServerSyncDatabaseSwitcher _serverSwitcher;
	private readonly SampleSyncManager _syncManager;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public SyncProcessor(
		AppBus bus,
		AppState state,
		SampleSyncManager syncManager,
		SampleClientSyncDatabaseSwitcher clientSwitcher,
		SampleServerSyncDatabaseSwitcher serverSwitcher,
		IDateTimeProvider dateTimeProvider
	)
		: base(bus, state)
	{
		_dateTimeProvider = dateTimeProvider;
		_syncManager = Track(syncManager);
		_clientSwitcher = clientSwitcher;
		_serverSwitcher = serverSwitcher;
		Track(clientSwitcher.SqliteMapper);
		Track(serverSwitcher.SqliteMapper);
	}

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		Bus.Sync.SubscribeToSyncRequest(OnSyncRequest);
		Bus.Sync.SubscribeToAddClientAccount(OnAddClientAccount);
		Bus.Sync.SubscribeToApplyDatabaseKinds(OnApplyDatabaseKinds);
		Bus.Sync.SubscribeToInjectAccountBatch(OnInjectAccountBatch);
		Bus.Sync.SubscribeToModifyAccountBatch(OnModifyAccountBatch);
		_syncManager.SyncCompleted += SyncManagerOnSyncCompleted;
		base.InitializeLifecycle();
	}

	public override void StartLifecycle()
	{
		State.Sync.IsAvailable = true;
		SetBusy(false);
		RefreshCounts();
		State.Sync.LastStatus = State.RuntimeInformation.DevicePlatform.HasFlag(DevicePlatform.Browser)
			? "Ready (in-memory SQLite)."
			: "Ready. Pick client and server databases, then Apply.";
		base.StartLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		Bus.Sync.UnsubscribeToSyncRequest(OnSyncRequest);
		Bus.Sync.UnsubscribeToAddClientAccount(OnAddClientAccount);
		Bus.Sync.UnsubscribeToApplyDatabaseKinds(OnApplyDatabaseKinds);
		Bus.Sync.UnsubscribeToInjectAccountBatch(OnInjectAccountBatch);
		Bus.Sync.UnsubscribeToModifyAccountBatch(OnModifyAccountBatch);
		_syncManager.SyncCompleted -= SyncManagerOnSyncCompleted;
		base.UninitializeLifecycle();
	}

	private static void Migrate(ISyncableDatabaseProvider provider)
	{
		using var database = provider.GetSyncableDatabase();
		database.Migrate();
	}

	private void OnAddClientAccount(SyncChannel.AddClientAccountMessage message)
	{
		if (!State.Sync.CanRunWork)
		{
			return;
		}

		using var database = (ISampleDatabase) _clientSwitcher.GetSyncableDatabase();
		var now = _dateTimeProvider.UtcNow;
		var customer = new CustomerEntity
		{
			CreatedOn = now,
			ModifiedOn = now,
			Name = "Sample",
			SyncId = Guid.NewGuid()
		};
		database.Customers.Add(customer);
		database.SaveChanges();
		var storedCustomer = database.Customers.Read(customer.SyncId) as CustomerEntity;

		var address = new AddressEntity
		{
			City = "Atlanta",
			CreatedOn = now,
			CustomerId = storedCustomer?.Id,
			CustomerSyncId = customer.SyncId,
			Line1 = "1 Sample St",
			ModifiedOn = now,
			Postal = "30301",
			State = "GA",
			SyncId = Guid.NewGuid()
		};
		database.Addresses.Add(address);
		database.SaveChanges();

		var storedAddress = database.Addresses.Read(address.SyncId) as AddressEntity;
		database.Accounts.Add(new AccountEntity
		{
			AddressId = storedAddress?.Id,
			AddressSyncId = address.SyncId,
			CreatedOn = now,
			CustomerId = storedCustomer?.Id,
			CustomerSyncId = customer.SyncId,
			EmailAddress = message.Email,
			Name = message.Name,
			ModifiedOn = now,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		});
		database.SaveChanges();
		RefreshCounts();
		State.Sync.LastStatus = $"Added {message.Name} and address on the client.";
	}

	private void OnApplyDatabaseKinds(SyncChannel.ApplyDatabaseKindsMessage _)
	{
		if (!State.Sync.CanRunWork)
		{
			return;
		}

		SetBusy(true);
		State.Sync.LastStatus = "Applying databases...";
		var clientKind = State.Sync.ClientDatabaseKind;
		var serverKind = State.Sync.ServerDatabaseKind;
		RunWork(() =>
		{
			var watch = Stopwatch.StartNew();
			_clientSwitcher.Use(clientKind);
			_serverSwitcher.Use(serverKind);
			Migrate(_clientSwitcher);
			Migrate(_serverSwitcher);
			watch.Stop();
			return $"Client {clientKind}, server {serverKind} ready in {watch.Elapsed}.";
		});
	}

	private void OnInjectAccountBatch(SyncChannel.InjectAccountBatchMessage _)
	{
		if (!State.Sync.CanRunWork)
		{
			return;
		}

		var count = State.Sync.InjectCount;
		if (count < 1)
		{
			return;
		}

		var side = State.Sync.WorkSide;
		SetBusy(true);
		State.Sync.LastStatus = $"Injecting {count} accounts on {side}...";
		RunWork(() =>
		{
			var watch = Stopwatch.StartNew();
			var provider = ProviderFor(side);
			using var database = (ISampleDatabase) provider.GetSyncableDatabase();
			database.Migrate();
			var now = _dateTimeProvider.UtcNow;
			var customer = new CustomerEntity
			{
				CreatedOn = now,
				ModifiedOn = now,
				Name = "Load Test",
				SyncId = Guid.NewGuid()
			};
			database.Customers.Add(customer);
			database.SaveChanges();
			var storedCustomer = database.Customers.Read(customer.SyncId) as CustomerEntity;

			for (var i = 0; i < count; i++)
			{
				var addressSyncId = Guid.NewGuid();
				database.Addresses.Add(new AddressEntity
				{
					City = "Atlanta",
					CreatedOn = now,
					CustomerId = storedCustomer?.Id,
					CustomerSyncId = customer.SyncId,
					Line1 = $"Street {i}",
					ModifiedOn = now,
					Postal = "30301",
					State = "GA",
					SyncId = addressSyncId
				});
				database.Accounts.Add(new AccountEntity
				{
					AddressSyncId = addressSyncId,
					CreatedOn = now,
					CustomerId = storedCustomer?.Id,
					CustomerSyncId = customer.SyncId,
					EmailAddress = $"user{i}@sample.local",
					Name = $"Account {i}",
					ModifiedOn = now,
					Roles = ",,",
					SyncId = Guid.NewGuid()
				});
			}

			database.SaveChanges();
			watch.Stop();
			return $"Injected {count} accounts on {side} in {watch.Elapsed}.";
		});
	}

	private void OnModifyAccountBatch(SyncChannel.ModifyAccountBatchMessage _)
	{
		if (!State.Sync.CanRunWork)
		{
			return;
		}

		var count = State.Sync.ModifyCount;
		if (count < 1)
		{
			return;
		}

		var side = State.Sync.WorkSide;
		SetBusy(true);
		State.Sync.LastStatus = $"Modifying {count} accounts on {side}...";
		RunWork(() =>
		{
			var watch = Stopwatch.StartNew();
			var provider = ProviderFor(side);
			using var database = (ISampleDatabase) provider.GetSyncableDatabase();
			var keys = database.Accounts.ReadAllKeys().Keys.ToList();
			if (keys.Count == 0)
			{
				watch.Stop();
				return $"No accounts on {side} to modify.";
			}

			var take = Math.Min(count, keys.Count);
			Shuffle(keys);
			var now = _dateTimeProvider.UtcNow;
			for (var i = 0; i < take; i++)
			{
				if (database.Accounts.Read(keys[i]) is not AccountEntity account)
				{
					continue;
				}

				var stamp = Random.Shared.Next(100000, 999999);
				account.Name = $"Account {stamp}";
				account.EmailAddress = $"user{stamp}@sample.local";
				account.LastLoginDate = now;
				account.ModifiedOn = now;
			}

			database.SaveChanges();
			watch.Stop();
			return $"Modified {take} accounts on {side} in {watch.Elapsed}.";
		});
	}

	private void OnSyncRequest(SyncChannel.SyncRequestMessage request)
	{
		if (!State.Sync.CanRunWork)
		{
			return;
		}

		SetBusy(true);
		State.Sync.LastStatus = "Syncing...";
		_syncManager.SyncAsync(request.SyncType, waitFor: TimeSpan.FromMinutes(10));
	}

	private ISyncableDatabaseProvider ProviderFor(SampleSyncWorkSide side)
	{
		return side == SampleSyncWorkSide.Server ? _serverSwitcher : _clientSwitcher;
	}

	private void RefreshCounts()
	{
		using (var database = (ISampleDatabase) _clientSwitcher.GetSyncableDatabase())
		{
			State.Sync.ClientAccountCount = database.Accounts.ReadAllKeys().Count;
			State.Sync.ClientAddressCount = database.Addresses.ReadAllKeys().Count;
		}

		using (var database = (ISampleDatabase) _serverSwitcher.GetSyncableDatabase())
		{
			State.Sync.ServerAccountCount = database.Accounts.ReadAllKeys().Count;
			State.Sync.ServerAddressCount = database.Addresses.ReadAllKeys().Count;
		}
	}

	private void RunWork(Func<string> work)
	{
		Task.Run(() =>
		{
			string status;
			TimeSpan elapsed;
			try
			{
				var watch = Stopwatch.StartNew();
				status = work();
				watch.Stop();
				elapsed = watch.Elapsed;
			}
			catch (Exception ex)
			{
				status = ex.Message;
				elapsed = TimeSpan.Zero;
			}

			State.Sync.LastElapsed = elapsed;
			State.Sync.LastStatus = status;
			try
			{
				RefreshCounts();
			}
			catch (Exception ex)
			{
				State.Sync.LastStatus = $"{status} Count refresh failed: {ex.Message}";
			}

			SetBusy(false);
		});
	}

	private void SetBusy(bool busy)
	{
		State.Sync.IsRunning = busy;
		State.Sync.CanRunWork = State.Sync.IsAvailable && !busy;
	}

	private static void Shuffle(IList<Guid> keys)
	{
		for (var i = keys.Count - 1; i > 0; i--)
		{
			var j = Random.Shared.Next(i + 1);
			(keys[i], keys[j]) = (keys[j], keys[i]);
		}
	}

	private static void AddScopeMilliseconds(Profiler profiler, string name, SeriesDataProvider series)
	{
		long ticks = 0;
		foreach (var stats in profiler)
		{
			if (stats.Name == name)
			{
				ticks = stats.TotalTicks;
				break;
			}
		}

		series.Add(ticks / (double) TimeSpan.TicksPerMillisecond);
	}

	private static void SnapshotProfiler(Profiler profiler, SpeedyList<ProfilerScopeModel> destination, TimeSpan elapsed)
	{
		destination.Clear();
		var totalTicks = elapsed.Ticks;
		foreach (var stats in profiler
			.Where(x => !string.IsNullOrEmpty(x.Name) && ((x.Count > 0) || (x.TotalTicks > 0)))
			.OrderByDescending(x => x.TotalTicks))
		{
			var percent = totalTicks <= 0 ? 0 : ((double) stats.TotalTicks / totalTicks) * 100;
			destination.Add(new ProfilerScopeModel(stats.Name, stats.Count, stats.TotalTicks, percent));
		}
	}

	private void SnapshotSyncProfilers(SyncSession session)
	{
		var client = session.SyncClientProfilerForClient;
		var server = session.SyncClientProfilerForServer;
		var elapsed = session.Elapsed;
		SnapshotProfiler(client, State.Sync.ClientProfilerScopes, elapsed);
		SnapshotProfiler(server, State.Sync.ServerProfilerScopes, elapsed);
		AddScopeMilliseconds(client, "ProcessSyncObjectsSaveDatabase", State.Sync.ClientSaveChangesData);
		AddScopeMilliseconds(client, "ProcessSyncObject", State.Sync.ClientProcessSyncObjectData);
		AddScopeMilliseconds(client, "GetChanges", State.Sync.ClientGetChangesData);
		AddScopeMilliseconds(client, "ApplyChanges", State.Sync.ClientApplyChangesData);
		AddScopeMilliseconds(server, "ProcessSyncObjectsSaveDatabase", State.Sync.ServerSaveChangesData);
		AddScopeMilliseconds(server, "ProcessSyncObject", State.Sync.ServerProcessSyncObjectData);
		AddScopeMilliseconds(server, "GetChanges", State.Sync.ServerGetChangesData);
		AddScopeMilliseconds(server, "ApplyChanges", State.Sync.ServerApplyChangesData);
		client.Refresh();
		server.Refresh();
	}

	private void SyncManagerOnSyncCompleted(object sender, SyncSession session)
	{
		Bus.Sync.SyncCompleted(session);
		State.Sync.LastSyncedOn = session.StoppedOn;
		State.Sync.LastElapsed = session.Elapsed;
		State.Sync.LastStatus = session.SyncSuccessful
			? $"Sync succeeded in {session.Elapsed}."
			: $"Sync finished with {session.SyncIssues.Count} issue(s) in {session.Elapsed}.";
		try
		{
			SnapshotSyncProfilers(session);
		}
		catch (Exception ex)
		{
			State.Sync.LastStatus = $"{State.Sync.LastStatus} Profiler snapshot failed: {ex.Message}";
		}

		try
		{
			RefreshCounts();
		}
		catch (Exception ex)
		{
			State.Sync.LastStatus = $"{State.Sync.LastStatus} Count refresh failed: {ex.Message}";
		}

		SetBusy(false);
	}

	#endregion
}
