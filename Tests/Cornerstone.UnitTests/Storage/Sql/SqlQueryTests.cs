#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
[DoNotParallelize]
public class SqlQueryTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DeleteEntity()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);
			var account = CreateAccount("John", "john@domain.com");
			repository.Add(account);
			AreEqual(0, repository.Count());
			database.SaveChanges();
			AreEqual(1, account.Id);
			AreEqual(1, repository.Count());
		});
	}

	[TestMethod]
	public void Query()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			var account = new AccountEntity
			{
				CreatedOn = StartDateTime.AddDays(-1),
				EmailAddress = "john@domain.com",
				Name = "John",
				ModifiedOn = StartDateTime.AddDays(1),
				Roles = ",,",
				SyncId = new Guid("3B0AB396-BE53-4C48-9EFD-6C22D1098524")
			};
			var account2 = new AccountEntity
			{
				CreatedOn = StartDateTime.AddDays(-1),
				EmailAddress = "jane@domain.com",
				Name = "Jane",
				ModifiedOn = StartDateTime.AddDays(1),
				Roles = ",,",
				SyncId = new Guid("02FDCC17-61A5-4C9E-83EA-3DB562E08E65")
			};
			repository.Add(account);
			repository.Add(account2);
			database.SaveChanges();

			var accounts = repository
				.Where(x => x.Name == "John")
				.Query()
				.ToArray();

			AreEqual(1, accounts.Length);
			AreEqual(StartDateTime.AddDays(-1), accounts[0].CreatedOn);
			AreEqual("John", accounts[0].Name);
			AreEqual(1, accounts[0].Id);
			AreEqual(false, accounts[0].IsDeleted);
			AreEqual(StartDateTime.AddDays(1), accounts[0].ModifiedOn);
			AreEqual(new Guid("3B0AB396-BE53-4C48-9EFD-6C22D1098524"), accounts[0].SyncId);
		});
	}

	[TestMethod]
	public void QueryCombinedWhereUsesDistinctParameterIndexes()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);
			repository.Add(CreateAccount("John", "john@domain.com"));
			repository.Add(CreateAccount("Jane", "jane@domain.com"));
			database.SaveChanges();

			var since = StartDateTime.AddDays(-2);
			var until = StartDateTime.AddDays(2);
			var accounts = new SqlQuery<AccountEntity>(database)
				.Where(x =>
					((x.CreatedOn >= since) && (x.CreatedOn < until))
					|| ((x.ModifiedOn >= since) && (x.ModifiedOn < until)))
				.Where(x => x.Name == "John")
				.Query()
				.ToArray();

			AreEqual(1, accounts.Length);
			AreEqual("John", accounts[0].Name);
		});
	}

	[TestMethod]
	public void QueryMultipleRecords()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			for (var i = 0; i < 10; i++)
			{
				repository.Add(CreateAccount($"User{i}", $"user{i}@domain.com"));
			}

			database.SaveChanges();
			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query.Query().ToArray();

			AreEqual(10, accounts.Length);

			for (var i = 0; i < 10; i++)
			{
				AreEqual($"User{i}", accounts[i].Name);
				AreEqual(i + 1, accounts[i].Id);
			}
		});
	}

	[TestMethod]
	public void QueryReadsForeignKeyAndGuid()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var addresses = database.GetRepository<AddressEntity>();
			var accounts = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, accounts);

			var syncId = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
			addresses.Add(new AddressEntity
			{
				City = "Atlanta",
				CreatedOn = StartDateTime,
				Line1 = "1 Sample St",
				ModifiedOn = StartDateTime,
				Postal = "30301",
				State = "GA",
				SyncId = syncId
			});
			database.SaveChanges();

			var storedAddress = addresses.Where(x => x.Line1 == "1 Sample St").Query().Single();
			IsTrue(storedAddress.Id > 0);
			AreEqual(syncId, storedAddress.SyncId);
			AreEqual(DateTimeKind.Utc, storedAddress.CreatedOn.Kind);
			AreEqual(StartDateTime, storedAddress.CreatedOn);

			accounts.Add(new AccountEntity
			{
				AddressId = storedAddress.Id,
				AddressSyncId = storedAddress.SyncId,
				CreatedOn = StartDateTime,
				EmailAddress = "john@domain.com",
				Name = "John",
				ModifiedOn = StartDateTime,
				Roles = ",,",
				SyncId = Guid.NewGuid()
			});
			database.SaveChanges();

			var storedAccount = accounts.Where(x => x.Name == "John").Query().Single();
			AreEqual(storedAddress.Id, storedAccount.AddressId);
			AreEqual(storedAddress.SyncId, storedAccount.AddressSyncId);
			AreEqual(null, accounts.Where(x => x.Name == "Missing").Query().FirstOrDefault());
		});
	}

	[TestMethod]
	public void QuerySeesUncommittedWork()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			try
			{
				database.ExecuteInTransaction(() =>
				{
					repository.Add(CreateAccount("John", "john@domain.com"));
					database.SaveChanges();
					AreEqual(1, repository.Where(x => x.Name == "John").Query().Count());
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
	public void QueryWithEnumProperty()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice@domain.com", status: AccountStatus.Enabled));
			repository.Add(CreateAccount("Bob", "bob@domain.com", status: AccountStatus.Unknown));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.Where(x => x.Status == AccountStatus.Enabled)
				.Query()
				.ToArray();

			AreEqual(1, accounts.Length);
			AreEqual("Alice", accounts[0].Name);
			AreEqual(AccountStatus.Enabled, accounts[0].Status);
		});
	}

	[TestMethod]
	public void QueryWithInList()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);
			repository.Add(CreateAccount("Alice", "alice@domain.com"));
			repository.Add(CreateAccount("Bob", "bob@domain.com"));
			repository.Add(CreateAccount("Charlie", "charlie@domain.com"));
			database.SaveChanges();

			var ids = new[] { 1, 3 };
			var accounts = repository
				.Where(x => ids.Contains(x.Id))
				.OrderBy(x => x.Id)
				.Query()
				.ToArray();

			AreEqual(2, accounts.Length);
			AreEqual("Alice", accounts[0].Name);
			AreEqual("Charlie", accounts[1].Name);
		});
	}

	[TestMethod]
	public void QueryWithMultipleWherePredicates()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice@domain.com", true));
			repository.Add(CreateAccount("Bob", "bob@domain.com", false));
			repository.Add(CreateAccount("Alice", "alice2@domain.com", false));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.Where(x => x.Name == "Alice")
				.Where(x => x.IsDeleted == false)
				.Query()
				.ToArray();

			AreEqual(1, accounts.Length);
			AreEqual("alice2@domain.com", accounts[0].EmailAddress);
		});
	}

	[TestMethod]
	public void QueryWithNoResults()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice@domain.com"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.Where(x => x.Name == "NonExistent")
				.Query()
				.ToArray();

			AreEqual(0, accounts.Length);
		});
	}

	[TestMethod]
	public void QueryWithNullableProperties()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			var account = new AccountEntity
			{
				CreatedOn = StartDateTime,
				EmailAddress = "test@domain.com",
				Name = "Test",
				Roles = "",
				Picture = null,
				TimeZoneId = null,
				SyncId = Guid.NewGuid()
			};
			repository.Add(account);
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query.Query().ToArray();

			AreEqual(1, accounts.Length);
			IsNull(accounts[0].Picture);
			IsNull(accounts[0].TimeZoneId);
		});
	}

	[TestMethod]
	public void QueryWithOrderByAscending()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Charlie", "charlie@domain.com"));
			repository.Add(CreateAccount("Alice", "alice@domain.com"));
			repository.Add(CreateAccount("Bob", "bob@domain.com"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.OrderBy(x => x.Name)
				.Query()
				.ToArray();

			AreEqual(3, accounts.Length);
			AreEqual("Alice", accounts[0].Name);
			AreEqual("Bob", accounts[1].Name);
			AreEqual("Charlie", accounts[2].Name);
		});
	}

	[TestMethod]
	public void QueryWithOrderByDescending()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice@domain.com"));
			repository.Add(CreateAccount("Charlie", "charlie@domain.com"));
			repository.Add(CreateAccount("Bob", "bob@domain.com"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.OrderByDescending(x => x.Name)
				.Query()
				.ToArray();

			AreEqual(3, accounts.Length);
			AreEqual("Charlie", accounts[0].Name);
			AreEqual("Bob", accounts[1].Name);
			AreEqual("Alice", accounts[2].Name);
		});
	}

	[TestMethod]
	public void QueryWithThenBy()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice2@domain.com"));
			repository.Add(CreateAccount("Alice", "alice1@domain.com"));
			repository.Add(CreateAccount("Bob", "bob@domain.com"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.OrderBy(x => x.Name)
				.ThenBy(x => x.EmailAddress)
				.Query()
				.ToArray();

			AreEqual(3, accounts.Length);
			AreEqual("alice1@domain.com", accounts[0].EmailAddress);
			AreEqual("alice2@domain.com", accounts[1].EmailAddress);
			AreEqual("bob@domain.com", accounts[2].EmailAddress);
		});
	}

	[TestMethod]
	public void QueryWithThenByDescending()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice1@domain.com"));
			repository.Add(CreateAccount("Alice", "alice2@domain.com"));
			repository.Add(CreateAccount("Bob", "bob@domain.com"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.OrderBy(x => x.Name)
				.ThenByDescending(x => x.EmailAddress)
				.Query()
				.ToArray();

			AreEqual(3, accounts.Length);
			AreEqual("alice2@domain.com", accounts[0].EmailAddress);
			AreEqual("alice1@domain.com", accounts[1].EmailAddress);
			AreEqual("bob@domain.com", accounts[2].EmailAddress);
		});
	}

	[TestMethod]
	public void QueryWithWhereAndOrderBy()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Charlie", "charlie@domain.com", roles: "Admin"));
			repository.Add(CreateAccount("Alice", "alice@domain.com", roles: "Admin"));
			repository.Add(CreateAccount("Bob", "bob@domain.com", roles: "User"));
			repository.Add(CreateAccount("Dave", "dave@domain.com", roles: "Admin"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.Where(x => x.Roles == "Admin")
				.OrderBy(x => x.Name)
				.Query()
				.ToArray();

			AreEqual(3, accounts.Length);
			AreEqual("Alice", accounts[0].Name);
			AreEqual("Charlie", accounts[1].Name);
			AreEqual("Dave", accounts[2].Name);
		});
	}

	[TestMethod]
	public void QueryWithWhereFilter()
	{
		ForEach((connectionString, provider) =>
		{
			using var database = new SqlDatabase(connectionString, provider);
			var repository = database.GetRepository<AccountEntity>();
			EnsureAccountGraph(database, repository);

			repository.Add(CreateAccount("Alice", "alice@domain.com"));
			repository.Add(CreateAccount("Bob", "bob@domain.com"));
			repository.Add(CreateAccount("Charlie", "charlie@domain.com"));
			database.SaveChanges();

			var query = new SqlQuery<AccountEntity>(database);
			var accounts = query
				.Where(x => x.Name == "Bob")
				.Query()
				.ToArray();

			AreEqual(1, accounts.Length);
			AreEqual("Bob", accounts[0].Name);
			AreEqual("bob@domain.com", accounts[0].EmailAddress);
			IsTrue(query.Any());
			IsFalse(repository.Where(x => x.Name == "Missing").Any());
		});
	}

	[TestMethod]
	public void ThenByDescendingWithoutOrderByThrows()
	{
		ForEach((connectionString, provider) =>
		{
			var query = new SqlQuery<AccountEntity>(connectionString, provider);

			ExpectedException<InvalidOperationException>(
				() => query.ThenByDescending(x => x.Name),
				"ThenBy can only be used after OrderBy"
			);
		});
	}

	[TestMethod]
	public void ThenByWithoutOrderByThrows()
	{
		ForEach((connectionString, provider) =>
		{
			var query = new SqlQuery<AccountEntity>(connectionString, provider);

			ExpectedException<InvalidOperationException>(
				() => query.ThenBy(x => x.Name),
				"ThenBy can only be used after OrderBy"
			);
		});
	}

	[TestMethod]
	public void ToSqlQueryWithNoPredicatesOrOrdering()
	{
		ForEach((connectionString, provider) =>
		{
			var query = new SqlQuery<AccountEntity>(connectionString, provider);
			var (sql, parameters) = SqlQuery<AccountEntity>.ToSqlQuery(query);
			sql.Dump();

			var b = SqlGenerator.GetIdentifierBrackets(provider);
			IsTrue(sql.StartsWith("SELECT "));
			IsTrue(sql.Contains($"FROM {b.Open}Accounts{b.Close}"));
			IsFalse(sql.Contains("WHERE"));
			IsFalse(sql.Contains("ORDER BY"));
			AreEqual(0, parameters.Length);
		});
	}

	[TestMethod]
	public void ToSqlQueryWithOrderBy()
	{
		ForEach((connectionString, provider) =>
		{
			var query = new SqlQuery<AccountEntity>(connectionString, provider);
			query.OrderBy(x => x.Name);

			var (sql, parameters) = SqlQuery<AccountEntity>.ToSqlQuery(query);

			var b = SqlGenerator.GetIdentifierBrackets(provider);
			IsTrue(sql.Contains("ORDER BY"));
			IsTrue(sql.Contains($"{b.Open}Name{b.Close}"));
			IsFalse(sql.Contains("DESC"));
			AreEqual(0, parameters.Length);
		});
	}

	[TestMethod]
	public void ToSqlQueryWithOrderByDescending()
	{
		ForEach((connectionString, provider) =>
		{
			var query = new SqlQuery<AccountEntity>(connectionString, provider);
			query.OrderByDescending(x => x.Name);
			var (sql, _) = SqlQuery<AccountEntity>.ToSqlQuery(query);
			IsTrue(sql.Contains("ORDER BY"));
			IsTrue(sql.Contains("DESC"));
		});
	}

	[TestMethod]
	public void ToSqlQueryWithWhereClause()
	{
		ForEach((connectionString, provider) =>
		{
			var query = new SqlQuery<AccountEntity>(connectionString, provider);
			query.Where(x => x.Name == "Test");

			var (sql, parameters) = SqlQuery<AccountEntity>.ToSqlQuery(query);
			var b = SqlGenerator.GetIdentifierBrackets(provider);

			IsTrue(sql.Contains("WHERE"));
			IsTrue(sql.Contains($"{b.Open}Name{b.Close}"));
			IsTrue(sql.Contains($"FROM {b.Open}Accounts{b.Close}"));
			AreEqual(1, parameters.Length);
			AreEqual("Test", parameters[0]);
		});
	}

	private AccountEntity CreateAccount(string name, string email, bool isDeleted = false, string roles = "",
		AccountStatus status = AccountStatus.Unknown)
	{
		return new AccountEntity
		{
			CreatedOn = StartDateTime,
			EmailAddress = email,
			Name = name,
			IsDeleted = isDeleted,
			ModifiedOn = StartDateTime,
			Roles = roles,
			Status = status,
			SyncId = Guid.NewGuid()
		};
	}

	private static void EnsureAccountGraph(SqlDatabase database, SqlRepository<AccountEntity> repository)
	{
		database.GetRepository<AddressEntity>().EnsureTableCreated();
		repository.EnsureTableCreated();
	}

	private void ForEach(Action<string, SqlProvider> action)
	{
		var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
		action(connectionString, SqlProvider.Sqlite);
	}

	#endregion
}