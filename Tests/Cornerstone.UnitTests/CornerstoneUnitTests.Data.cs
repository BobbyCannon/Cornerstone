#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Cornerstone.EntityFramework;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Testing;
using Cornerstone.Text.Buffers;
using Cornerstone.UnitTests.Resources;
using Microsoft.EntityFrameworkCore;
using Sample.Client.Data;
using Sample.Client.Data.Sql;
using Sample.Client.Data.Sqlite;
using Sample.Server.Data;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;
using Sample.Shared.Sync;

#endregion

namespace Cornerstone.UnitTests;

public partial class CornerstoneUnitTest
{
	#region Methods

	protected void ForEachBuffers(Action<IStringBuffer> action, string value)
	{
		var buffers = GetStringBuffers(value);
		foreach (var buffer in buffers)
		{
			buffer.GetType().FullName.Dump();
			action(buffer);
		}
	}

	protected void ForEachClientDatabaseProvider(Action<IDatabaseProvider<IClientDatabase>> test)
	{
		var providers = GetClientDatabaseProviders();

		foreach (var provider in providers)
		{
			using var database = provider.GetDatabase();

			if (database is EntityFrameworkDatabase efDatabase)
			{
				if (efDatabase.Database.IsSqlServer())
				{
					efDatabase.Database.Migrate();
					efDatabase.Database.ExecuteSqlRaw(ClearDatabaseQuery);
				}

				if (efDatabase.Database.IsSqlite())
				{
					efDatabase.Database.EnsureDeleted();
					efDatabase.Database.EnsureCreated();
				}
			}

			provider.GetType().Name.Dump(Environment.NewLine);

			test(provider);
		}
	}

	protected AccountSync GetAccountSync(Action<AccountSync> update = null)
	{
		var response = new AccountSync
		{
			AddressSyncId = Guid.Empty,
			Name = "John Doe",
			EmailAddress = "john.doe@domain.com",
			Roles = ",,",
			SyncId = new Guid("1A07C107-E7B2-4AF0-9B50-FF4CEAC719D6")
		};
		update?.Invoke(response);
		return response;
	}

	protected AddressSync GetAddressSync(Action<AddressSync> update = null)
	{
		var response = new AddressSync
		{
			Line1 = "Line 1",
			Line2 = "Line 2",
			City = "City",
			State = "SC",
			Postal = "123456-7890",
			SyncId = new Guid("CFA64191-199B-4374-AB1C-BFB026DC92D9")
		};
		update?.Invoke(response);
		return response;
	}

	protected ClientAccount GetClientAccount(string name, Guid? syncId = null, Action<ClientAccount> update = null)
	{
		var response = new ClientAccount
		{
			Name = name,
			EmailAddress = $"{name}@domain.com",
			Roles = string.Empty,
			SyncId = syncId ?? Guid.NewGuid()
		};
		update?.Invoke(response);
		return response;
	}

	protected ISyncableDatabaseProvider<IClientDatabase> GetClientDatabaseProvider()
	{
		return new ClientMemoryDatabaseProvider();
	}

	protected MemoryLogListener GetLogListener(EventLevel level = EventLevel.Verbose)
	{
		return MemoryLogListener.CreateSession(Guid.Empty, level);
	}

	protected T GetModel<T>(Action<T> update = null) where T : new()
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
			case SampleAccount sValue:
			{
				sValue.EmailAddress = "john@doe.com";
				sValue.FirstName = "John";
				sValue.LastName = "Doe";
				sValue.LastClientUpdate = DateTime.MinValue;
				break;
			}
		}

		update?.Invoke(response);

		return response;
	}

	protected Profiler GetProfiler(string name = "Profiler",
		IDateTimeProvider timeProvider = null,
		IDispatcher dispatcher = null)
	{
		dispatcher ??= GetDispatcher();

		return new Profiler(
			name ?? "Profiler",
			timeProvider ?? this,
			dispatcher
		);
	}

	protected ISyncableDatabaseProvider<IServerDatabase> GetServerDatabaseProvider()
	{
		return new ServerMemoryDatabaseProvider();
	}

	protected T GetService<T>()
	{
		return Dependencies.GetInstance<T>();
	}

	protected IEnumerable<IStringBuffer> GetStringBuffers(string text)
	{
		yield return new StringGapBuffer(text);
		yield return new StringRopeBuffer(text);
	}

	protected virtual void SetupDependencyInjection()
	{
		Dependencies.AddTransient(GetRuntimeInformation());
		Dependencies.AddSingleton<IDateTimeProvider>(this);
		Dependencies.AddSingleton<IDispatcher>(() => null);
	}

	private IEnumerable<IDatabaseProvider<IClientDatabase>> GetClientDatabaseProviders()
	{
		yield return GetClientMemoryDatabaseProvider();
		yield return GetClientSqlDatabaseProvider();
		yield return GetClientSqliteDatabaseProvider();
	}

	private IDatabaseProvider<IClientDatabase> GetClientMemoryDatabaseProvider()
	{
		return new ClientMemoryDatabaseProvider();
	}

	private IDatabaseProvider<IClientDatabase> GetClientSqlDatabaseProvider()
	{
		return new ClientSqlDatabaseProvider("server=localhost;database=CornerstoneClient;integrated security=true;encrypt=false");
	}

	private IDatabaseProvider<IClientDatabase> GetClientSqliteDatabaseProvider()
	{
		return new ClientSqliteDatabaseProvider("Data Source=CornerstoneClient.db");
	}

	#endregion
}