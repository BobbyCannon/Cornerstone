#region References

using System;
using System.IO;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage;

[TestClass]
[DoNotParallelize]
public class SampleDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddAndReadOnEntityFramework()
	{
		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
		try
		{
			ISyncableDatabaseProvider provider = new SampleEntityFrameworkDatabaseProvider(
				$"Data Source={path}",
				SqlProvider.Sqlite,
				DateTimeProvider.RealTime
			);
			AddAndRead(provider);
		}
		finally
		{
			SqliteConnection.ClearAllPools();
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[TestMethod]
	public void AddAndReadOnSql()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		ISyncableDatabaseProvider provider = new SampleSqlDatabaseProvider(
			connectionString,
			SqlProvider.Sqlite,
			DateTimeProvider.RealTime
		);
		AddAndRead(provider);
	}

	private void AddAndRead(ISyncableDatabaseProvider provider)
	{
		using var database = (ISampleDatabase) provider.GetSyncableDatabase();
		database.Migrate();

		var entity = new AccountEntity
		{
			CreatedOn = StartDateTime,
			EmailAddress = "john@domain.com",
			Name = "John",
			ModifiedOn = StartDateTime,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		};

		database.Accounts.Add(entity);
		AreEqual(1, database.SaveChanges());

		var read = database.Accounts.Read(entity.SyncId) as AccountEntity;
		IsNotNull(read);
		AreEqual("John", read.Name);
		AreEqual(entity.SyncId, read.SyncId);
	}

	#endregion
}