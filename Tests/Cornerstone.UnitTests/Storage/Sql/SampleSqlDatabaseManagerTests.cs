#region References

using System;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SampleSqlDatabaseManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConnectionStringIsAlwaysMemory()
	{
		var connectionString = SampleSqlDatabaseManager.GetSqliteConnectionString(new RuntimeInformationData(), "SampleClient.db");
		AreEqual("Data Source=SampleClient.db;Mode=Memory;Cache=Shared;", connectionString);
	}

	[TestMethod]
	public void MemoryDatabaseSurvivesDisposedOpens()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		var manager = new SampleSqlDatabaseManager(this, new Profiler("SampleSql"), connectionString, SqlProvider.Sqlite);

		using (var database = manager.GetSyncableDatabase())
		{
			AreEqual(0, database.Accounts.ReadAllKeys().Count);
			database.Accounts.Add(new AccountEntity
			{
				CreatedOn = UtcNow,
				EmailAddress = "a@b.c",
				Name = "Ann",
				ModifiedOn = UtcNow,
				Roles = ",,",
				SyncId = Guid.NewGuid()
			});
			database.SaveChanges();
			AreEqual(1, database.Accounts.ReadAllKeys().Count);
		}

		using (var database = manager.GetSyncableDatabase())
		{
			AreEqual(1, database.Accounts.ReadAllKeys().Count);
		}

		manager.UninitializeLifecycle();
	}

	#endregion
}