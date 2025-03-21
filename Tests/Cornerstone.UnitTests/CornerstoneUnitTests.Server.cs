#region References

using System;
using System.Collections.Generic;
using Cornerstone.Profiling;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;
using Sample.Server.Data;
using Sample.Server.Data.Sql;
using Sample.Server.Data.Sqlite;
using Sample.Server.Website.Sync;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests;

public partial class CornerstoneUnitTest
{
	#region Properties

	protected string ServerSqlConnectionString { get; }

	protected string ServerSqlConnectionString2 { get; }

	protected string ServerSqliteConnectionString { get; }

	protected string ServerSqliteConnectionString2 { get; }

	#endregion

	#region Methods

	protected SyncableDatabaseProvider<IServerDatabase> GetServerDatabaseProvider(DatabaseType type, bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		return type switch
		{
			DatabaseType.Memory => GetServerMemoryProvider(initializeDatabase, useKeyCache),
			DatabaseType.Sql => GetServerSqlProvider(initializeDatabase, useKeyCache, useSecondConnection),
			DatabaseType.Sqlite => GetServerSqliteProvider(initializeDatabase, useKeyCache, useSecondConnection),
			_ => throw new ArgumentException($"Invalid database type ({type})")
		};
	}

	protected IEnumerable<SyncableDatabaseProvider<IServerDatabase>> GetServerDatabaseProviders(bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		yield return GetServerMemoryProvider(initializeDatabase, useKeyCache);
		yield return GetServerSqlProvider(initializeDatabase, useKeyCache, useSecondConnection);
		yield return GetServerSqliteProvider(initializeDatabase, useKeyCache, useSecondConnection);
	}

	protected ServerMemoryDatabaseProvider GetServerMemoryProvider(bool initializeDatabase = false, bool useKeyCache = false)
	{
		var response = new ServerMemoryDatabaseProvider(this, useKeyCache ? new DatabaseKeyCache() : null);
		var database = response.GetDatabase();

		if (initializeDatabase)
		{
			InitializeDatabase(database, response.KeyCache);
		}

		return response;
	}

	protected T GetServerModel<T>(Action<T> update = null) where T : new()
	{
		var response = new T();

		switch (response)
		{
			case AccountEntity sValue:
			{
				sValue.EmailAddress = "john@doe.com";
				sValue.Name = "John Doe";
				sValue.Roles = ",,";
				break;
			}
			case AddressEntity sValue:
			{
				sValue.City = "City";
				sValue.Line1 = "Line1";
				sValue.Line2 = "Line2";
				sValue.Postal = "Postal";
				sValue.State = "State";
				break;
			}
			case LogEventEntity sValue:
			{
				sValue.Message = "The quick brown fox jumped over the lazy dog.";
				sValue.Level = LogLevel.Information;
				sValue.LoggedOn = UtcNow;
				break;
			}
		}

		update?.Invoke(response);

		return response;
	}

	protected ServerSqliteDatabaseProvider GetServerSqliteProvider(bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		// Do not use the cache during migration and clearing of the database
		var response = new ServerSqliteDatabaseProvider(
			useSecondConnection
				? ServerSqliteConnectionString2
				: ServerSqliteConnectionString,
			this,
			useKeyCache ? new DatabaseKeyCache() : null
		);
		using var database = (ServerSqliteDatabase) response.GetDatabase();
		database.Database.EnsureDeleted();
		database.Database.Migrate();

		if (initializeDatabase)
		{
			InitializeDatabase(database, response.KeyCache);
		}

		return response;
	}

	protected ServerDatabaseProvider GetServerSqlProvider(bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		// Do not use the cache during migration and clearing of the database
		var response = new ServerSqlDatabaseProvider(
			useSecondConnection
				? ServerSqlConnectionString2
				: ServerSqlConnectionString,
			this,
			useKeyCache ? new DatabaseKeyCache() : null
		);
		using var database = (ServerSqlDatabase) response.GetDatabase();
		database.Database.Migrate();
		database.ClearDatabase();

		if (initializeDatabase)
		{
			InitializeDatabase(database, response.KeyCache);
		}

		return response;
	}

	protected IServerSyncClientProvider GetServerSyncClientProvider(string name, DatabaseType type,
		bool useSecondConnection = false, bool useKeyCache = false, bool initializeDatabase = false)
	{
		var provider = new SampleServerSyncClientProvider(name, new AccountEntity(), GetServerDatabaseProvider(type, initializeDatabase, useKeyCache, useSecondConnection), this);
		return provider;
	}

	protected SyncClient GetServerSyncClient(string name, AccountEntity authenticatedAccount, DatabaseType type,
		bool initializeDatabase, bool useKeyCache, bool useSecondConnection)
	{
		name = $"{name}: ({type}{(useKeyCache ? ", cached" : "")})";

		switch (type)
		{
			case DatabaseType.Memory:
			{
				return new SampleServerSyncClient(name, authenticatedAccount, GetServerMemoryProvider(initializeDatabase, useKeyCache),
					this, new SyncStatistics(), new Profiler(name)
				);
			}
			case DatabaseType.Sql:
			{
				return new SampleServerSyncClient(name, authenticatedAccount, GetServerSqlProvider(initializeDatabase, useKeyCache, useSecondConnection),
					this, new SyncStatistics(), new Profiler(name)
				);
			}
			case DatabaseType.Sqlite:
			{
				return new SampleServerSyncClient(name, authenticatedAccount, GetServerSqliteProvider(initializeDatabase, useKeyCache, useSecondConnection),
					this, new SyncStatistics(), new Profiler(name)
				);
			}
			default:
			case DatabaseType.Unknown:
			{
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}
	}

	protected static void InitializeDatabase(IServerDatabase database, DatabaseKeyCache keyCache)
	{
		var address = new AddressEntity
		{
			City = "City",
			Line1 = "Line1",
			Line2 = "Line2",
			Postal = "12345",
			State = "ST",
			SyncId = Guid.Parse("BDCC2C49-BCE5-49B4-8268-1EFD1E434F79")
		};

		database.Accounts.Add(new AccountEntity
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