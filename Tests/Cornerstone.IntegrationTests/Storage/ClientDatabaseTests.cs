#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Client.Data;
using Sample.Client.Data.Sql;
using Sample.Client.Data.Sqlite;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

[TestClass]
public class ClientDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Add()
	{
		ForEachClientDatabaseProvider(provider =>
		{
			var expected = GetModel<ClientAddress>();

			using (var database = provider.GetDatabase())
			{
				database.EnsureMigrated();
				database.Addresses.Add(expected);
				database.SaveChanges();
			}

			using (var database = provider.GetDatabase())
			{
				var actual = database.Addresses.FirstOrDefault(x => x.Id == expected.Id);
				IsNotNull(actual);
				AreEqual(expected, actual.Unwrap());
			}
		});
	}

	public void ForEachClientDatabaseProvider(Action<IDatabaseProvider<IClientDatabase>> test)
	{
		var providers = GetClientDatabaseProviders();

		foreach (var provider in providers)
		{
			test(provider);
		}
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