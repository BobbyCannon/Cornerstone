#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlSyncableDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddDoesNotWriteUntilSaveChanges()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			const int count = 600;
			for (var i = 0; i < count; i++)
			{
				repository.Add(CreateAccount($"User{i}", StartDateTime));
			}

			AreEqual(0, database.GetRepository<AccountEntity>().Count());
			var commandsAfterAdds = database.CommandCount;
			AreEqual(count, database.SaveChanges());
			var maxRows = SqlGenerator.GetMaxBatchRows(SqlProvider.Sqlite, 15);
			var expectedCommands = (count + maxRows - 1) / maxRows;
			AreEqual(commandsAfterAdds + expectedCommands, database.CommandCount);
			AreEqual(count, database.GetRepository<AccountEntity>().Count());
		});
	}

	[TestMethod]
	public void AddSaveChangesAndReadBySyncId()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			var entity = CreateAccount("John", StartDateTime);
			repository.Add(entity);
			AreEqual(1, database.SaveChanges());

			var read = repository.Read(entity.SyncId) as AccountEntity;
			IsNotNull(read);
			AreEqual("John", read.Name);
			AreEqual(entity.SyncId, read.SyncId);
		});
	}

	[TestMethod]
	public void GetChangesByModifiedWindow()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			repository.Add(CreateAccount("Old", StartDateTime.AddDays(-2)));
			repository.Add(CreateAccount("InWindow", StartDateTime));
			database.SaveChanges();

			var since = StartDateTime.AddDays(-1);
			var until = StartDateTime.AddDays(1);
			AreEqual(1, repository.GetChangeCount(since, until, null));
			var changes = repository.GetChanges(since, until, 0, 10, null).Cast<AccountEntity>().ToList();
			AreEqual(1, changes.Count);
			AreEqual("InWindow", changes[0].Name);
		});
	}

	[TestMethod]
	public void GetChangesIncludesCreatedOnInWindow()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			var entity = CreateAccount("CreatedInWindow", StartDateTime);
			entity.CreatedOn = StartDateTime;
			entity.ModifiedOn = StartDateTime.AddDays(-2);
			repository.Add(entity);
			database.SaveChanges();

			var since = StartDateTime.AddDays(-1);
			var until = StartDateTime.AddDays(1);
			AreEqual(1, repository.GetChangeCount(since, until, null));
			var changes = repository.GetChanges(since, until, 0, 10, null).Cast<AccountEntity>().ToList();
			AreEqual(1, changes.Count);
			AreEqual("CreatedInWindow", changes[0].Name);
		});
	}

	[TestMethod]
	public void GetChangesIncludesUpdateAfterFirstWindow()
	{
		var provider = new SampleSqlDatabaseProvider(
			$"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;",
			SqlProvider.Sqlite,
			this
		);
		using var database = provider.GetSyncableDatabase();
		database.Migrate();

		var address = new AddressEntity
		{
			City = "City",
			CreatedOn = UtcNow,
			Line1 = "Original",
			ModifiedOn = UtcNow,
			Postal = "29640",
			State = "SC",
			SyncId = Guid.NewGuid()
		};
		database.Addresses.Add(address);
		IncrementTime(seconds: 1);
		database.SaveChanges();
		IncrementTime(seconds: 1);

		var firstUntil = UtcNow;
		var firstCount = database.Addresses.GetChangeCount(DateTime.MinValue, firstUntil, null);
		AreEqual(1, firstCount);

		IncrementTime(seconds: 1);
		var stored = (AddressEntity) database.Addresses.Read(address.SyncId);
		stored.Line1 = "Updated";
		stored.ModifiedOn = UtcNow;
		database.Addresses.Add(stored);
		IncrementTime(seconds: 1);
		AreEqual(1, database.SaveChanges());
		IncrementTime(seconds: 1);

		var secondCount = database.Addresses.GetChangeCount(firstUntil, UtcNow, null);
		AreEqual(1, secondCount, () =>
		{
			var reread = (AddressEntity) database.Addresses.Read(address.SyncId);
			return $"line1={reread.Line1} modified={reread.ModifiedOn:o} firstUntil={firstUntil:o} now={UtcNow:o}";
		});
		var changes = database.Addresses.GetChanges(firstUntil, UtcNow, 0, 10, null).Cast<AddressEntity>().ToList();
		AreEqual("Updated", changes[0].Line1);
	}

	[TestMethod]
	public void GetChangesOrdersByModifiedOnNotId()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			var first = CreateAccount("A", StartDateTime);
			repository.Add(first);
			database.SaveChanges();
			IncrementTime(seconds: 1);

			var second = CreateAccount("B", UtcNow);
			repository.Add(second);
			database.SaveChanges();
			IncrementTime(seconds: 1);

			var stored = (AccountEntity) repository.Read(first.SyncId);
			stored.Name = "A-updated";
			stored.ModifiedOn = UtcNow;
			database.SaveChanges();

			var since = StartDateTime.AddMinutes(-1);
			var until = UtcNow.AddMinutes(1);
			var changes = repository.GetChanges(since, until, 0, 10, null).Cast<AccountEntity>().ToList();
			AreEqual(2, changes.Count);
			AreEqual("B", changes[0].Name);
			AreEqual("A-updated", changes[1].Name);
			IsTrue(first.Id < second.Id);
		});
	}

	[TestMethod]
	public void GetChangesSkipReturnsLaterRows()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			repository.Add(CreateAccount("First", StartDateTime));
			repository.Add(CreateAccount("Second", StartDateTime));
			repository.Add(CreateAccount("Third", StartDateTime));
			database.SaveChanges();

			var since = StartDateTime.AddMinutes(-1);
			var until = StartDateTime.AddMinutes(1);
			var first = repository.GetChanges(since, until, 0, 1, null).Cast<AccountEntity>().ToList();
			AreEqual(1, first.Count);
			AreEqual("First", first[0].Name);

			var rest = repository.GetChanges(since, until, 1, 10, null).Cast<AccountEntity>().ToList();
			AreEqual(2, rest.Count);
			AreEqual("Second", rest[0].Name);
			AreEqual("Third", rest[1].Name);
		});
	}

	[TestMethod]
	public void ReadAllKeysAndRemove()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			var entity = CreateAccount("John", StartDateTime);
			repository.Add(entity);
			database.SaveChanges();

			var keys = repository.ReadAllKeys();
			AreEqual(1, keys.Count);
			IsTrue(keys.ContainsKey(entity.SyncId));

			var stored = repository.Read(entity.SyncId);
			repository.Remove(stored);
			database.SaveChanges();
			AreEqual(0, repository.ReadAllKeys().Count);
		});
	}

	[TestMethod]
	public void ReadDoesNotQueueUpsert()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			var entity = CreateAccount("John", StartDateTime);
			repository.Add(entity);
			database.SaveChanges();

			var stored = (AccountEntity) repository.Read(entity.SyncId);
			IsNotNull(stored);
			var modifiedOn = stored.ModifiedOn;
			AreEqual(0, database.SaveChanges());

			var reread = (AccountEntity) repository.Read(entity.SyncId);
			AreEqual(modifiedOn, reread.ModifiedOn);
		});
	}

	[TestMethod]
	public void SaveChangesRollsBackOnFailure()
	{
		ForEach(database =>
		{
			database.GetRepository<AccountEntity>().EnsureTableCreated();
			var repository = database.GetRepository<AccountEntity>();
			try
			{
				database.ExecuteInTransaction(() =>
				{
					repository.Add(CreateAccount("John", StartDateTime));
					database.SaveChanges();
					throw new InvalidOperationException("rollback");
				});
			}
			catch (InvalidOperationException)
			{
			}

			AreEqual(0, repository.Count());
		});
	}

	[TestMethod]
	public void SaveChangesUpdatesTrackedEntity()
	{
		ForEach(database =>
		{
			var repository = database.GetSyncableRepository(typeof(AccountEntity));
			var entity = CreateAccount("John", StartDateTime);
			repository.Add(entity);
			database.SaveChanges();

			var stored = (AccountEntity) repository.Read(entity.SyncId);
			stored.Name = "Jane";
			stored.ModifiedOn = StartDateTime.AddMinutes(1);
			AreEqual(1, database.SaveChanges());

			var updated = (AccountEntity) repository.Read(entity.SyncId);
			AreEqual("Jane", updated.Name);
		});
	}

	private AccountEntity CreateAccount(string name, DateTime modifiedOn)
	{
		return new AccountEntity
		{
			CreatedOn = modifiedOn,
			EmailAddress = $"{name.ToLowerInvariant()}@domain.com",
			Name = name,
			ModifiedOn = modifiedOn,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		};
	}

	private void ForEach(Action<SqlSyncableDatabase> action)
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		var settings = new DatabaseSettings();
		settings.MaintainCreatedOn = false;
		settings.MaintainModifiedOn = false;
		using var database = new SqlSyncableDatabase(
			connectionString,
			SqlProvider.Sqlite,
			settings,
			null,
			this,
			typeof(AddressEntity),
			typeof(AccountEntity));
		database.GetRepository<AddressEntity>().EnsureTableCreated();
		action(database);
	}

	#endregion
}