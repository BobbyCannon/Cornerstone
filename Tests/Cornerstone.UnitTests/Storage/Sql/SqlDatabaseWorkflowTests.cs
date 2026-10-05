#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

/// <summary>
/// EF-shaped unit of work on SqlDatabase: Add / modify / remove then SaveChanges;
/// Dispose and DiscardChanges drop pending work. No proxies, no Include.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SqlDatabaseWorkflowTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SaveChangesRollsBackWhenSecondRowConflicts()
	{
		using var database = Open();
		var repository = database.GetRepository<AccountEntity>();
		var syncId = Guid.NewGuid();
		repository.Add(CreateAccount("First", "first@domain.com", syncId));
		repository.Add(CreateAccount("Second", "second@domain.com", syncId));

		var threw = false;
		try
		{
			database.SaveChanges();
		}
		catch (Exception)
		{
			threw = true;
		}

		IsTrue(threw);
		AreEqual(0, repository.Count());
	}

	[TestMethod]
	public void SaveChangesWritesTwoDistinctRows()
	{
		using var database = Open();
		var repository = database.GetRepository<AccountEntity>();
		repository.Add(CreateAccount("First", "first@domain.com", Guid.NewGuid()));
		repository.Add(CreateAccount("Second", "second@domain.com", Guid.NewGuid()));
		AreEqual(2, database.SaveChanges());
		AreEqual(2, repository.Count());
	}

	[TestMethod]
	public void DisposeWithoutSaveChangesDropsPendingWork()
	{
		var connectionString = SharedMemoryConnectionString();
		using var keepAlive = new SqliteConnection(connectionString);
		keepAlive.Open();

		using (var database = Open(connectionString))
		{
			database.GetRepository<AccountEntity>().Add(CreateAccount("John", "john@domain.com", Guid.NewGuid()));
		}

		using (var database = Open(connectionString))
		{
			AreEqual(0, database.GetRepository<AccountEntity>().Count());
		}
	}

	[TestMethod]
	public void DiscardChangesDropsPendingWork()
	{
		using var database = Open();
		var repository = database.GetRepository<AccountEntity>();
		repository.Add(CreateAccount("John", "john@domain.com", Guid.NewGuid()));
		database.DiscardChanges();
		AreEqual(0, database.SaveChanges());
		AreEqual(0, repository.Count());
	}

	[TestMethod]
	public void DisposeDropsUnsavedAddAfterASuccessfulSave()
	{
		var connectionString = SharedMemoryConnectionString();
		using var keepAlive = new SqliteConnection(connectionString);
		keepAlive.Open();

		using (var database = Open(connectionString))
		{
			var repository = database.GetRepository<AccountEntity>();
			repository.Add(CreateAccount("Saved", "saved@domain.com", Guid.NewGuid()));
			database.SaveChanges();
			repository.Add(CreateAccount("Pending", "pending@domain.com", Guid.NewGuid()));
		}

		using (var database = Open(connectionString))
		{
			AreEqual(1, database.GetRepository<AccountEntity>().Count());
			AreEqual("Saved", database.GetRepository<AccountEntity>().Where(x => x.Name == "Saved").Query().Single().Name);
		}
	}

	[TestMethod]
	public void SaveChangesAfterDisposeThrows()
	{
		var database = Open();
		database.Dispose();
		ExpectedException<InvalidOperationException>(() => database.SaveChanges());
	}

	[TestMethod]
	public void AddThenDeleteBeforeSaveChangesNetsOneRow()
	{
		using var database = Open();
		var repository = database.GetRepository<AccountEntity>();
		var first = CreateAccount("First", "first@domain.com", Guid.NewGuid());
		var second = CreateAccount("Second", "second@domain.com", Guid.NewGuid());
		repository.Add(first);
		repository.Add(second);
		repository.Delete(second);
		AreEqual(1, database.SaveChanges());
		AreEqual(1, repository.Count());
		AreEqual("First", repository.Where(x => x.Name == "First").Query().Single().Name);
	}

	[TestMethod]
	public void ModifyLoadedEntityThenSaveChangesPersists()
	{
		using var database = Open();
		var repository = database.GetRepository<AccountEntity>();
		var entity = CreateAccount("John", "john@domain.com", Guid.NewGuid());
		repository.Add(entity);
		database.SaveChanges();

		var loaded = repository.GetById(entity.Id);
		loaded.Name = "Jane";
		AreEqual(1, database.SaveChanges());
		AreEqual("Jane", repository.GetById(entity.Id).Name);
	}

	[TestMethod]
	public void DeleteAfterSaveChangesRemovesRow()
	{
		using var database = Open();
		var repository = database.GetRepository<AccountEntity>();
		var entity = CreateAccount("John", "john@domain.com", Guid.NewGuid());
		repository.Add(entity);
		database.SaveChanges();
		repository.Delete(entity);
		AreEqual(1, database.SaveChanges());
		AreEqual(0, repository.Count());
	}

	[TestMethod]
	public void UnsavedModifyIsNotVisibleOnAnotherDatabase()
	{
		var connectionString = SharedMemoryConnectionString();
		using var keepAlive = new SqliteConnection(connectionString);
		keepAlive.Open();
		using var first = Open(connectionString);
		using var second = Open(connectionString);
		var repository = first.GetRepository<AccountEntity>();
		var entity = CreateAccount("John", "john@domain.com", Guid.NewGuid());
		repository.Add(entity);
		first.SaveChanges();

		var loaded = repository.GetById(entity.Id);
		loaded.Name = "Jane";

		AreEqual("John", second.GetRepository<AccountEntity>().GetById(entity.Id).Name);
	}

	private AccountEntity CreateAccount(string name, string email, Guid syncId)
	{
		return new AccountEntity
		{
			CreatedOn = StartDateTime,
			EmailAddress = email,
			ModifiedOn = StartDateTime,
			Name = name,
			Roles = ",,",
			SyncId = syncId
		};
	}

	private static SqlDatabase Open(string connectionString = null)
	{
		connectionString ??= SharedMemoryConnectionString();
		var database = new SqlDatabase(connectionString, SqlProvider.Sqlite);
		database.GetRepository<AddressEntity>().EnsureTableCreated();
		database.GetRepository<AccountEntity>().EnsureTableCreated();
		return database;
	}

	private static string SharedMemoryConnectionString()
	{
		return $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
	}

	#endregion
}