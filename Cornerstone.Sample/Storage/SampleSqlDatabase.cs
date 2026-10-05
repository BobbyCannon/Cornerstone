#region References

using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Storage;

public partial class SampleSqlDatabase : SqlSyncableDatabase, ISampleDatabase
{
	#region Constructors

	public SampleSqlDatabase(
		string connectionString,
		SqlProvider provider,
		DatabaseSettings settings,
		DatabaseKeyCache keyCache,
		IDateTimeProvider dateTimeProvider
	)
		: base(connectionString, provider, settings, keyCache, dateTimeProvider, ISampleDatabase.GetEntityTypes())
	{
		SyncOrder = ISampleDatabase.GetSyncOrder();
	}

	#endregion

	#region Properties

	public ISyncableRepository Accounts => GetSyncableRepository(typeof(AccountEntity));

	public ISyncableRepository Addresses => GetSyncableRepository(typeof(AddressEntity));

	public ISyncableRepository Bookmarks => GetSyncableRepository(typeof(BookmarkEntity));

	public ISyncableRepository Customers => GetSyncableRepository(typeof(CustomerEntity));

	public ISyncableRepository Settings => GetSyncableRepository(typeof(SettingEntity));

	#endregion
}