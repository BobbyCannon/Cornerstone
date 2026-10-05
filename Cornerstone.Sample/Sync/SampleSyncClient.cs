#region References

using System;
using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

public class SampleSyncClient : SyncClientForDatabase
{
	#region Constants

	public const string SyncAll = "SyncAll";

	#endregion

	#region Fields

	private static readonly SyncClientConverter _converter;

	#endregion

	#region Constructors

	public SampleSyncClient(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		SyncStatistics syncStatistics,
		Profiler syncClientProfiler,
		Logger logger = null
	)
		: base(name, databaseProvider, dateTimeProvider, syncStatistics, syncClientProfiler, logger)
	{
	}

	static SampleSyncClient()
	{
		_converter = new SyncClientConverter(
			new SyncObjectConverter<SampleSyncClient, Customer, CustomerEntity>(update: UpdateCustomer),
			new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>(update: UpdateAddress),
			new SyncObjectConverter<SampleSyncClient, Account, AccountEntity>(update: UpdateAccount),
			new SyncObjectConverter<SampleSyncClient, Bookmark, BookmarkEntity>(update: UpdateBookmark),
			new SyncObjectConverter<SampleSyncClient, Setting, SettingEntity>(update: UpdateSetting)
		);
	}

	#endregion

	#region Methods

	protected override SyncClientConverter GetConverter()
	{
		return _converter;
	}

	protected override void SetSyncSettings()
	{
		SampleSyncFilters.ApplyAll(SyncSettings);
	}

	private static bool UpdateAccount(
		SampleSyncClient client,
		AccountEntity source,
		AccountEntity destination,
		Action processUpdate,
		SyncObjectStatus status
	)
	{
		switch (status)
		{
			case SyncObjectStatus.Added:
			case SyncObjectStatus.Updated:
			{
				SampleSyncRelationships.SetAccountAddressId(client, source, destination);
				processUpdate();
				if (string.IsNullOrEmpty(destination.Roles))
				{
					destination.Roles = ",,";
				}
				SampleSyncRelationships.SetCustomerId(client, source.CustomerSyncId, id => destination.CustomerId = id);
				return true;
			}
			case SyncObjectStatus.Deleted:
			{
				processUpdate();
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	private static bool UpdateAddress(
		SampleSyncClient client,
		AddressEntity source,
		AddressEntity destination,
		Action processUpdate,
		SyncObjectStatus status
	)
	{
		switch (status)
		{
			case SyncObjectStatus.Added:
			case SyncObjectStatus.Updated:
			{
				processUpdate();
				SampleSyncRelationships.SetCustomerId(client, source.CustomerSyncId, id => destination.CustomerId = id);
				return true;
			}
			case SyncObjectStatus.Deleted:
			{
				processUpdate();
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	private static bool UpdateBookmark(
		SampleSyncClient client,
		BookmarkEntity source,
		BookmarkEntity destination,
		Action processUpdate,
		SyncObjectStatus status
	)
	{
		switch (status)
		{
			case SyncObjectStatus.Added:
			case SyncObjectStatus.Updated:
			{
				SampleSyncRelationships.SetBookmarkParentId(client, source, destination);
				processUpdate();
				SampleSyncRelationships.SetCustomerId(client, source.CustomerSyncId, id => destination.CustomerId = id);
				return true;
			}
			case SyncObjectStatus.Deleted:
			{
				processUpdate();
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	private static bool UpdateCustomer(
		SampleSyncClient client,
		CustomerEntity source,
		CustomerEntity destination,
		Action processUpdate,
		SyncObjectStatus status
	)
	{
		switch (status)
		{
			case SyncObjectStatus.Added:
			case SyncObjectStatus.Updated:
			case SyncObjectStatus.Deleted:
			{
				processUpdate();
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	private static bool UpdateSetting(
		SampleSyncClient client,
		SettingEntity source,
		SettingEntity destination,
		Action processUpdate,
		SyncObjectStatus status
	)
	{
		switch (status)
		{
			case SyncObjectStatus.Added:
			case SyncObjectStatus.Updated:
			case SyncObjectStatus.Deleted:
			{
				processUpdate();
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	#endregion
}
