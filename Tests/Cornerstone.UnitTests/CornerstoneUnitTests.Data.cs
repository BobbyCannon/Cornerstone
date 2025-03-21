#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
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
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests;

public partial class CornerstoneUnitTest
{
	#region Constants

	public const string AdministratorEmailAddress = "admin@speedy.local";
	public const int AdministratorId = 1;
	public const string AdministratorPassword = "Password";

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override object CreateCustomTypeFactory(Type type, object[] args)
	{
		if (type == typeof(Geometry))
		{
			return new LineGeometry(new Point(0, 0), new Point(1, 1));
		}

		if (type == typeof(Type))
		{
			return typeof(object);
		}

		if (type == typeof(ICommand))
		{
			return new RelayCommand(_ => { });
		}
		
		if (type == typeof(SyncClient))
		{
			return new SyncClientStub();
		}

		return base.CreateCustomTypeFactory(type, args);
	}

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
		var providers = GetClientDatabaseProviders(false, false, false);

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

	protected Account GetAccountSync(Action<Account> update = null)
	{
		var response = new Account
		{
			AddressSyncId = Guid.Empty,
			Name = "John Doe",
			EmailAddress = "john.doe@domain.com",
			Roles = [],
			SyncId = new Guid("1A07C107-E7B2-4AF0-9B50-FF4CEAC719D6")
		};
		update?.Invoke(response);
		return response;
	}

	protected Address GetAddressSync(Action<Address> update = null)
	{
		var response = new Address
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

	protected ClientAccount GetClientAccount(string name, ClientAddress address = null, Action<ClientAccount> update = null)
	{
		var response = new ClientAccount
		{
			Name = name,
			EmailAddress = $"{name}@domain.com",
			Roles = string.Empty,
			SyncId = Guid.NewGuid(),
			Address = address
		};
		update?.Invoke(response);
		return response;
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

	protected IEnumerable<IStringBuffer> GetStringBuffers(string text)
	{
		yield return new StringGapBuffer(text);
	}

	#endregion
}