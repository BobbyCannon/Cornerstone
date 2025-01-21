#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Server.Data;
using Sample.Server.Data.Sql;
using Sample.Server.Data.Sqlite;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

[TestClass]
public class ServerDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Add()
	{
		ForEachServerDatabaseProvider(provider =>
		{
			var expected = GetModel<AddressEntity>();

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

	public void ForEachServerDatabaseProvider(Action<IDatabaseProvider<IServerDatabase>> test)
	{
		var providers = GetServerDatabaseProviders();

		foreach (var provider in providers)
		{
			provider.GetType().FullName.Dump();
			test(provider);
		}
	}

	private IEnumerable<IDatabaseProvider<IServerDatabase>> GetServerDatabaseProviders()
	{
		yield return GetServerMemoryDatabaseProvider();
		yield return GetServerSqlDatabaseProvider();
		yield return GetServerSqliteDatabaseProvider();
	}

	private IDatabaseProvider<IServerDatabase> GetServerMemoryDatabaseProvider()
	{
		return new ServerMemoryDatabaseProvider(this);
	}

	private IDatabaseProvider<IServerDatabase> GetServerSqlDatabaseProvider()
	{
		return new ServerSqlDatabaseProvider("server=localhost;database=CornerstoneServer;integrated security=true;encrypt=false", this);
	}
	
	private IDatabaseProvider<IServerDatabase> GetServerSqliteDatabaseProvider()
	{
		return new ServerSqliteDatabaseProvider("Data Source=CornerstoneServer.db", this);
	}

	#endregion
}