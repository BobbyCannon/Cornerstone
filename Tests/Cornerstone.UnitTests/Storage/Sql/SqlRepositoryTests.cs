#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlRepositoryTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddDoesNotWriteUntilSaveChanges()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			database.GetRepository<AddressEntity>().EnsureTableCreated();
			repository.EnsureTableCreated();

			const int count = 600;
			for (var i = 0; i < count; i++)
			{
				repository.Add(CreateAccount($"User{i}", $"user{i}@domain.com", Guid.NewGuid(), StartDateTime));
			}

			AreEqual(0, repository.Count());
			var commandsAfterAdds = database.CommandCount;
			AreEqual(count, database.SaveChanges());
			var maxRows = SqlGenerator.GetMaxBatchRows(provider, 15);
			var expectedCommands = (count + maxRows - 1) / maxRows;
			AreEqual(commandsAfterAdds + expectedCommands, database.CommandCount);
			AreEqual(count, repository.Count());
			IsTrue(repository.Where(x => x.Name == "User0").Query().Single().Id > 0);
			IsTrue(repository.Where(x => x.Name == "User599").Query().Single().Id > 0);
		});
	}

	[TestMethod]
	public void DeleteBatchRemovesRows()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			database.GetRepository<AddressEntity>().EnsureTableCreated();
			repository.EnsureTableCreated();

			var first = CreateAccount("John", "john@domain.com", Guid.NewGuid(), StartDateTime);
			var second = CreateAccount("Jane", "jane@domain.com", Guid.NewGuid(), StartDateTime);
			var third = CreateAccount("Bob", "bob@domain.com", Guid.NewGuid(), StartDateTime);
			repository.Add(first);
			repository.Add(second);
			repository.Add(third);
			AreEqual(0, repository.Count());
			AreEqual(2, repository.Delete([first, second]));
			database.SaveChanges();
			AreEqual(1, repository.Count());
			AreEqual("Bob", GetName(repository, third.SyncId));
		});
	}

	[TestMethod]
	public void GetByIdAndAny()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			database.GetRepository<AddressEntity>().EnsureTableCreated();
			repository.EnsureTableCreated();

			IsFalse(repository.Any());
			var account = CreateAccount("John", "john@domain.com", Guid.NewGuid(), StartDateTime);
			repository.Add(account);
			IsFalse(repository.Any());
			AreEqual(0, account.Id);
			database.SaveChanges();
			IsTrue(account.Id > 0);
			IsTrue(repository.Any());
			IsTrue(repository.Any(x => x.Name == "John"));
			IsFalse(repository.Any(x => x.Name == "Missing"));

			var loaded = repository.GetById(account.Id);
			IsNotNull(loaded);
			AreEqual("John", loaded.Name);
			IsNull(repository.GetById(999));
		});
	}

	[TestMethod]
	public void UpsertSyncBatchInsertsAndAssignsIds()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			database.GetRepository<AddressEntity>().EnsureTableCreated();
			repository.EnsureTableCreated();

			var first = CreateAccount("John", "john@domain.com", Guid.NewGuid(), StartDateTime);
			var second = CreateAccount("Jane", "jane@domain.com", Guid.NewGuid(), StartDateTime);
			AreEqual(2, repository.UpsertSync([first, second]));
			IsTrue(first.Id > 0);
			IsTrue(second.Id > 0);
			AreEqual(2, repository.Count());

			second.Name = "Janet";
			second.ModifiedOn = StartDateTime.AddMinutes(1);
			AreEqual(1, repository.UpsertSync([first, second]));
			AreEqual("Janet", GetName(repository, second.SyncId));
			AreEqual("John", GetName(repository, first.SyncId));
		});
	}

	[TestMethod]
	public void UpsertSyncInsertsUpdatesAndSkipsOlder()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			database.GetRepository<AddressEntity>().EnsureTableCreated();
			repository.EnsureTableCreated();

			var syncId = Guid.NewGuid();
			var original = CreateAccount("John", "john@domain.com", syncId, StartDateTime);

			AreEqual(1, repository.UpsertSync(original));
			AreEqual(1, repository.Count());
			AreEqual("John", GetName(repository, syncId));

			var older = CreateAccount("Older", "older@domain.com", syncId, StartDateTime.AddMinutes(-1));
			AreEqual(0, repository.UpsertSync(older));
			AreEqual(1, repository.Count());
			AreEqual("John", GetName(repository, syncId));

			var olderTombstone = CreateAccount("Tombstone", "tombstone@domain.com", syncId, StartDateTime.AddMinutes(-1));
			olderTombstone.IsDeleted = true;
			AreEqual(0, repository.UpsertSync(olderTombstone));
			AreEqual(1, repository.UpsertSync(olderTombstone, false));
			AreEqual(1, repository.Count());
			var stored = repository.Where(x => x.SyncId == syncId).Query().Single();
			AreEqual("Tombstone", stored.Name);
			IsTrue(stored.IsDeleted);

			var newer = CreateAccount("Jane", "jane@domain.com", syncId, StartDateTime.AddMinutes(1));
			AreEqual(1, repository.UpsertSync(newer));
			AreEqual(1, repository.Count());
			AreEqual("Jane", GetName(repository, syncId));
		});
	}

	private AccountEntity CreateAccount(string name, string email, Guid syncId, DateTime modifiedOn)
	{
		return new AccountEntity
		{
			CreatedOn = StartDateTime,
			EmailAddress = email,
			Name = name,
			ModifiedOn = modifiedOn,
			Roles = ",,",
			SyncId = syncId
		};
	}

	private void ForEach(Action<string, SqlProvider> action)
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		action(connectionString, SqlProvider.Sqlite);
	}

	private static string GetName(SqlRepository<AccountEntity> repository, Guid syncId)
	{
		return repository.Where(x => x.SyncId == syncId).Query().Single().Name;
	}

	#endregion
}