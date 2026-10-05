#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Five in-memory SQLite databases and the eight links between them.
/// </summary>
public class MeshSyncGraph : IDisposable
{
	#region Fields

	public static readonly Guid AccountSyncId = Guid.Parse("33333333-3333-3333-3333-333333333333");

	public static readonly Guid AddressSyncId = Guid.Parse("55555555-5555-5555-5555-555555555555");

	public static readonly Guid CustomerSyncId = Guid.Parse("44444444-4444-4444-4444-444444444444");

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly Dictionary<MeshLink, SampleSyncManager> _links;
	private readonly Dictionary<MeshNode, SampleSqlDatabaseManager> _nodes;
	private readonly MeshLink[] _syncAllOrder;
	private bool _disposed;

	#endregion

	#region Constructors

	public MeshSyncGraph(
		IDateTimeProvider dateTimeProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher
	)
	{
		_dateTimeProvider = dateTimeProvider;
		_nodes = new Dictionary<MeshNode, SampleSqlDatabaseManager>();
		_links = new Dictionary<MeshLink, SampleSyncManager>();

		AddNode(MeshNode.Center, runtimeInformation);
		AddNode(MeshNode.North, runtimeInformation);
		AddNode(MeshNode.East, runtimeInformation);
		AddNode(MeshNode.South, runtimeInformation);
		AddNode(MeshNode.West, runtimeInformation);

		AddLink(MeshLink.NorthToCenter, MeshNode.North, MeshNode.Center, true, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.EastToCenter, MeshNode.East, MeshNode.Center, true, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.SouthToCenter, MeshNode.South, MeshNode.Center, true, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.WestToCenter, MeshNode.West, MeshNode.Center, true, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.NorthToEast, MeshNode.North, MeshNode.East, false, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.EastToSouth, MeshNode.East, MeshNode.South, false, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.SouthToWest, MeshNode.South, MeshNode.West, false, dateTimeProvider, runtimeInformation, dispatcher);
		AddLink(MeshLink.WestToNorth, MeshNode.West, MeshNode.North, false, dateTimeProvider, runtimeInformation, dispatcher);
		_syncAllOrder =
		[
			MeshLink.WestToCenter,
			MeshLink.NorthToCenter,
			MeshLink.EastToCenter,
			MeshLink.SouthToCenter,
			MeshLink.NorthToEast,
			MeshLink.EastToSouth,
			MeshLink.SouthToWest,
			MeshLink.WestToNorth
		];
	}

	#endregion

	#region Properties

	public IReadOnlyList<MeshLink> SyncAllOrder => _syncAllOrder;

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		foreach (var node in _nodes.Values)
		{
			node.UninitializeLifecycle();
		}
	}

	public MeshAccountCard ReadCard(MeshNode node)
	{
		using var database = Open(node);
		var account = ReadAccount(database);
		return new MeshAccountCard(account.Name, account.ModifiedOn);
	}

	public void Prepare()
	{
		foreach (var node in _nodes.Values)
		{
			using var database = (ISampleDatabase) node.GetSyncableDatabase();
			database.Migrate();
		}

		var now = _dateTimeProvider.UtcNow;
		foreach (var node in _nodes.Keys)
		{
			using var database = Open(node);
			if (database.Accounts.Read(AccountSyncId) is AccountEntity)
			{
				continue;
			}

			var customer = new CustomerEntity
			{
				CreatedOn = now,
				ModifiedOn = now,
				Name = "Mesh",
				SyncId = CustomerSyncId
			};
			database.Customers.Add(customer);
			database.SaveChanges();
			var storedCustomer = database.Customers.Read(CustomerSyncId) as CustomerEntity;

			var address = new AddressEntity
			{
				City = "Atlanta",
				CreatedOn = now,
				CustomerId = storedCustomer?.Id,
				CustomerSyncId = CustomerSyncId,
				Line1 = "1 Mesh St",
				ModifiedOn = now,
				Postal = "30301",
				State = "GA",
				SyncId = AddressSyncId
			};
			database.Addresses.Add(address);
			database.SaveChanges();
			var storedAddress = database.Addresses.Read(AddressSyncId) as AddressEntity;

			database.Accounts.Add(new AccountEntity
			{
				AddressId = storedAddress?.Id,
				AddressSyncId = AddressSyncId,
				CreatedOn = now,
				CustomerId = storedCustomer?.Id,
				CustomerSyncId = CustomerSyncId,
				EmailAddress = "mesh@cornerstone.local",
				ModifiedOn = now,
				Name = "Account",
				Roles = ",,",
				SyncId = AccountSyncId
			});
			database.SaveChanges();
		}
	}

	public void SaveName(MeshNode node, string name)
	{
		using var database = Open(node);
		var account = ReadAccount(database);
		account.Name = name ?? string.Empty;
		account.ModifiedOn = _dateTimeProvider.UtcNow;
		database.SaveChanges();
	}

	public SyncSession Sync(MeshLink link)
	{
		var manager = _links[link];
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		return manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromMinutes(2));
	}

	private void AddLink(
		MeshLink link,
		MeshNode client,
		MeshNode server,
		bool serverIsHub,
		IDateTimeProvider dateTimeProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher
	)
	{
		var manager = new SampleSyncManager(
			new MeshSyncClientProvider(client.ToString(), _nodes[client], dateTimeProvider, false),
			new MeshSyncClientProvider(server.ToString(), _nodes[server], dateTimeProvider, serverIsHub),
			new SyncSession(dateTimeProvider),
			runtimeInformation,
			dateTimeProvider,
			dispatcher
		);
		_links.Add(link, manager);
	}

	private void AddNode(MeshNode node, IRuntimeInformation runtimeInformation)
	{
		var manager = new SampleSqlDatabaseManager(
			_dateTimeProvider,
			new Profiler("Mesh" + node),
			SampleSqlDatabaseManager.GetSqliteConnectionString(runtimeInformation, "Mesh" + node + ".db"),
			SqlProvider.Sqlite
		);
		_nodes.Add(node, manager);
	}

	private ISampleDatabase Open(MeshNode node)
	{
		return (ISampleDatabase) _nodes[node].GetSyncableDatabase();
	}

	private static AccountEntity ReadAccount(ISampleDatabase database)
	{
		if (database.Accounts.Read(AccountSyncId) is not AccountEntity account)
		{
			throw new InvalidOperationException("Mesh account is missing.");
		}

		return account;
	}

	#endregion
}
