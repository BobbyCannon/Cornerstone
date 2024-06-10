#region References

using System;
using System.Diagnostics.Tracing;
using Cornerstone.Logging;
using Cornerstone.Sync;
using Sample.Client.Data;
using Sample.Server.Data;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;
using Sample.Shared.Sync;

#endregion

namespace Cornerstone.UnitTests;

public partial class CornerstoneUnitTest
{
	#region Methods

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

	protected LogListener GetLogListener()
	{
		return MemoryLogListener.CreateSession(Guid.NewGuid(), EventLevel.Verbose);
	}

	protected ISyncableDatabaseProvider<IServerDatabase> GetServerDatabaseProvider()
	{
		return new ServerMemoryDatabaseProvider();
	}

	protected virtual void SetupDependencyInjection()
	{
		DependencyInjector.AddTransient(RuntimeInformation);
	}

	#endregion
}