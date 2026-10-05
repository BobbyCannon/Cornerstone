#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Sample.Models;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Storage;

/// <summary>
/// Sample syncable database. Typed accessors are the non-generic
/// <see cref="ISyncableRepository" /> so both SQL and EF can implement this.
/// </summary>
public interface ISampleDatabase : ISyncableDatabase
{
	#region Properties

	ISyncableRepository Accounts { get; }

	ISyncableRepository Addresses { get; }

	ISyncableRepository Bookmarks { get; }

	ISyncableRepository Customers { get; }

	ISyncableRepository Settings { get; }

	#endregion

	#region Methods

	public static DatabaseSettings GetDefaultDatabaseSettings()
	{
		return new DatabaseSettings { SyncOrder = GetSyncOrder() };
	}

	public static Type[] GetEntityTypes()
	{
		return [typeof(CustomerEntity), typeof(AddressEntity), typeof(AccountEntity), typeof(BookmarkEntity), typeof(SettingEntity)];
	}

	public static (string entity, string syncObject)[] GetSyncOrder()
	{
		return
		[
			(typeof(CustomerEntity).ToAssemblyName(), typeof(Customer).ToAssemblyName()),
			(typeof(AddressEntity).ToAssemblyName(), typeof(Address).ToAssemblyName()),
			(typeof(AccountEntity).ToAssemblyName(), typeof(Account).ToAssemblyName()),
			(typeof(BookmarkEntity).ToAssemblyName(), typeof(Bookmark).ToAssemblyName()),
			(typeof(SettingEntity).ToAssemblyName(), typeof(Setting).ToAssemblyName())
		];
	}

	#endregion
}