#region References

using System;
using System.Collections.Generic;
using Cornerstone.Profiling;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;
using Sample.Client.Data;
using Sample.Client.Data.Sql;
using Sample.Client.Data.Sqlite;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests;

public partial class CornerstoneUnitTest
{
	#region Properties

	protected string ClientSqlConnectionString { get; }

	protected string ClientSqlConnectionString2 { get; }

	protected string ClientSqliteConnectionString { get; }

	protected string ClientSqliteConnectionString2 { get; }

	#endregion

	#region Methods

	protected SyncableDatabaseProvider<IClientDatabase> GetClientDatabaseProvider(DatabaseType type, bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		return type switch
		{
			DatabaseType.Memory => GetClientMemoryProvider(initializeDatabase, useKeyCache),
			DatabaseType.Sql => GetClientSqlProvider(initializeDatabase, useKeyCache, useSecondConnection),
			DatabaseType.Sqlite => GetClientSqliteProvider(initializeDatabase, useKeyCache, useSecondConnection),
			_ => throw new ArgumentException($"Invalid database type ({type})")
		};
	}

	protected IEnumerable<SyncableDatabaseProvider<IClientDatabase>> GetClientDatabaseProviders(bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		yield return GetClientMemoryProvider(initializeDatabase, useKeyCache);
		yield return GetClientSqlProvider(initializeDatabase, useKeyCache, useSecondConnection);
		yield return GetClientSqliteProvider(initializeDatabase, useKeyCache, useSecondConnection);
	}

	protected ClientMemoryDatabaseProvider GetClientMemoryProvider(bool initializeDatabase = false, bool useKeyCache = false)
	{
		var response = new ClientMemoryDatabaseProvider(this, useKeyCache ? new DatabaseKeyCache() : null);
		var database = response.GetDatabase();

		if (initializeDatabase)
		{
			InitializeDatabase(database, response.KeyCache);
		}

		return response;
	}

	protected T GetClientModel<T>(Action<T> update = null) where T : new()
	{
		var response = new T();

		switch (response)
		{
			case ClientAccount sValue:
			{
				sValue.EmailAddress = "john@doe.com";
				sValue.Name = "John Doe";
				sValue.LastClientUpdate = DateTime.MinValue;
				sValue.Roles = ",,";
				break;
			}
			case ClientAddress sValue:
			{
				sValue.City = "City";
				sValue.Line1 = "Line1";
				sValue.Line2 = "Line2";
				sValue.Postal = "Postal";
				sValue.State = "State";
				break;
			}
		}

		update?.Invoke(response);

		return response;
	}

	protected ClientSqliteDatabaseProvider GetClientSqliteProvider(bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		// Do not use the cache during migration and clearing of the database
		var response = new ClientSqliteDatabaseProvider(
			useSecondConnection
				? ClientSqliteConnectionString2
				: ClientSqliteConnectionString,
			this,
			useKeyCache ? new DatabaseKeyCache() : null
		);
		using var database = (ClientSqliteDatabase) response.GetDatabase();
		database.Database.EnsureDeleted();
		database.Database.Migrate();

		if (initializeDatabase)
		{
			InitializeDatabase(database, response.KeyCache);
		}

		return response;
	}

	protected ClientSqlDatabaseProvider GetClientSqlProvider(bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		// Do not use the cache during migration and clearing of the database
		var response = new ClientSqlDatabaseProvider(
			useSecondConnection
				? ClientSqlConnectionString2
				: ClientSqlConnectionString,
			this,
			useKeyCache ? new DatabaseKeyCache() : null
		);
		using var database = (ClientSqlDatabase) response.GetDatabase();
		database.Database.Migrate();
		database.ClearDatabase();

		if (initializeDatabase)
		{
			InitializeDatabase(database, response.KeyCache);
		}

		return response;
	}

	protected SyncClient GetClientSyncClient(string name, DatabaseType type, bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		switch (type)
		{
			case DatabaseType.Memory:
			{
				return new SampleClientSyncClient($"{name}: ({type}{(useKeyCache ? ", cached" : "")})",
					GetClientMemoryProvider(initializeDatabase, useKeyCache), this,
					new SyncStatistics(), new Profiler(name)
				);
			}
			case DatabaseType.Sql:
			{
				return new SampleClientSyncClient($"{name}: ({type}{(useKeyCache ? ", cached" : "")})",
					GetClientSqlProvider(initializeDatabase, useKeyCache, useSecondConnection), this,
					new SyncStatistics(), new Profiler(name)
				);
			}
			case DatabaseType.Sqlite:
			{
				return new SampleClientSyncClient($"{name}: ({type}{(useKeyCache ? ", cached" : "")})",
					GetClientSqliteProvider(initializeDatabase, useKeyCache, useSecondConnection), this,
					new SyncStatistics(), new Profiler(name)
				);
			}
			default:
			case DatabaseType.Unknown:
			{
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}
	}

	protected ISyncClientProvider GetClientSyncClientProvider(string name, DatabaseType type,
		bool initializeDatabase = false, bool useKeyCache = false, bool useSecondConnection = false)
	{
		var provider = new SampleClientSyncClientProvider(name, GetClientDatabaseProvider(type, initializeDatabase, useKeyCache, useSecondConnection), this);
		return provider;
	}

	private static void InitializeDatabase(IClientDatabase database, DatabaseKeyCache keyCache)
	{
		var address = new ClientAddress
		{
			City = "City",
			Line1 = "Line1",
			Line2 = "Line2",
			Postal = "12345",
			State = "ST",
			SyncId = Guid.Parse("BDCC2C49-BCE5-49B4-8268-1EFD1E434F79")
		};

		database.Accounts.Add(new ClientAccount
		{
			Address = address,
			Name = "Administrator",
			EmailAddress = "admin@domain.com",
			//PasswordHash = AccountService.Hash(AdministratorPassword, AdministratorId.ToString()),
			Roles = Account.CombineRoles(AccountRole.Administrator.ToString()),
			SyncId = Guid.Parse("56CF7B5C-4C5A-462C-939D-A1F387A7483C")
		});

		database.SaveChanges();

		keyCache?.InitializeAndLoad(database);
	}

	#endregion
}