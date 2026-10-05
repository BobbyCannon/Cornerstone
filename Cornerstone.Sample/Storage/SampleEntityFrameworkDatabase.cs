#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Cornerstone.Sample.Models;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.Sample.Storage;

public class SampleEntityFrameworkDatabase : EntityFrameworkSyncableDatabase, ISampleDatabase
{
	#region Constructors

	public SampleEntityFrameworkDatabase(DbContextOptions options, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(options, settings, keyCache)
	{
	}

	#endregion

	#region Properties

	public DbSet<AccountEntity> AccountEntities { get; set; }

	public ISyncableRepository Accounts => GetSyncableRepository(typeof(AccountEntity));

	public DbSet<AddressEntity> AddressEntities { get; set; }

	public ISyncableRepository Addresses => GetSyncableRepository(typeof(AddressEntity));

	public DbSet<BookmarkEntity> BookmarkEntities { get; set; }

	public ISyncableRepository Bookmarks => GetSyncableRepository(typeof(BookmarkEntity));

	public DbSet<CustomerEntity> CustomerEntities { get; set; }

	public ISyncableRepository Customers => GetSyncableRepository(typeof(CustomerEntity));

	public DbSet<SettingEntity> SettingEntities { get; set; }

	public ISyncableRepository Settings => GetSyncableRepository(typeof(SettingEntity));

	public override string LastDatabaseMigrationId => string.Empty;

	public override (string entity, string syncObject)[] SyncOrder => ISampleDatabase.GetSyncOrder();

	#endregion

	#region Methods

	public override IEnumerable<ISyncableRepository> GetSyncableRepositories()
	{
		foreach (var type in ISampleDatabase.GetEntityTypes())
		{
			GetSyncableRepository(type);
		}

		return base.GetSyncableRepositories();
	}

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "EF EnsureCreated is the non-AOT sample backend; AOT uses SqlDatabase.")]
	public override bool IsDatabaseMigrated()
	{
		Database.EnsureCreated();
		return true;
	}

	#endregion
}