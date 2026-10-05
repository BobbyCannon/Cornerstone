#region References

using System;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlSyncableDatabaseProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetSyncableDatabaseFromInterface()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		ISyncableDatabaseProvider provider = new SqlSyncableDatabaseProvider(
			connectionString,
			SqlProvider.Sqlite,
			DateTimeProvider.RealTime,
			typeof(AddressEntity),
			typeof(AccountEntity)
		);

		using var database = provider.GetSyncableDatabase();
		IsTrue(database is SqlSyncableDatabase);

		database.GetSyncableRepository(typeof(AddressEntity));
		var repository = database.GetSyncableRepository(typeof(AccountEntity));
		var entity = new AccountEntity
		{
			CreatedOn = StartDateTime,
			EmailAddress = "john@domain.com",
			Name = "John",
			ModifiedOn = StartDateTime,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		};
		repository.Add(entity);
		AreEqual(1, database.SaveChanges());

		var read = repository.Read(entity.SyncId) as AccountEntity;
		IsNotNull(read);
		AreEqual("John", read.Name);
	}

	[TestMethod]
	public void GetSyncableDatabasePassesKeyCache()
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		var keyCache = new DatabaseKeyCache();
		var provider = new SqlSyncableDatabaseProvider(
			connectionString,
			SqlProvider.Sqlite,
			DateTimeProvider.RealTime,
			keyCache,
			new DatabaseSettings(),
			typeof(AccountEntity)
		);

		using var database = provider.GetSyncableDatabase();
		AreEqual(keyCache, database.KeyCache);
	}

	#endregion
}