#region References

using System;
using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Server-side sample sync client. Sanitizes incoming settings like production servers.
/// Tenant scope comes from AuthenticatedAccount, never from client-supplied SyncSettings.
/// </summary>
public class SampleServerSyncClient : ServerSyncClient
{
	#region Fields

	private static readonly SyncClientConverter ConverterInstance;

	#endregion

	#region Constructors

	public SampleServerSyncClient(
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

	static SampleServerSyncClient()
	{
		ConverterInstance = new SyncClientConverter(
			new SyncObjectConverter<SampleServerSyncClient, Customer, CustomerEntity>(update: UpdateCustomer),
			new SyncObjectConverter<SampleServerSyncClient, Address, AddressEntity>(update: UpdateAddress),
			new SyncObjectConverter<SampleServerSyncClient, Account, AccountEntity>(update: UpdateAccount),
			new SyncObjectConverter<SampleServerSyncClient, Bookmark, BookmarkEntity>(update: UpdateBookmark),
			new SyncObjectConverter<SampleServerSyncClient, Setting, SettingEntity>(update: UpdateSetting)
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The account the server authenticated for this session. Incoming settings cannot change this.
	/// </summary>
	public AccountEntity AuthenticatedAccount { get; set; }

	#endregion

	#region Methods

	protected override SyncClientConverter GetConverter()
	{
		return ConverterInstance;
	}

	protected override void SetSyncSettings()
	{
		if (AuthenticatedAccount?.CustomerSyncId is { } customerSyncId && (customerSyncId != Guid.Empty))
		{
			SampleSyncFilters.ApplyForCustomer(SyncSettings, customerSyncId);
			return;
		}

		SampleSyncFilters.ApplyAll(SyncSettings);
	}

	private static void StampAuthenticatedCustomer(SampleServerSyncClient client, Action<Guid?> setCustomerSyncId, Action<int?> setCustomerId)
	{
		if (client.AuthenticatedAccount == null)
		{
			return;
		}

		setCustomerSyncId(client.AuthenticatedAccount.CustomerSyncId);
		if (client.AuthenticatedAccount.CustomerId is > 0)
		{
			setCustomerId(client.AuthenticatedAccount.CustomerId);
			return;
		}

		SampleSyncRelationships.SetCustomerId(client, client.AuthenticatedAccount.CustomerSyncId, setCustomerId);
	}

	private static bool UpdateAccount(
		SampleServerSyncClient client,
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
				// Throw before copying payload fields onto the stored row.
				SampleSyncRelationships.SetAccountAddressId(
					client,
					source,
					destination,
					client.AuthenticatedAccount?.CustomerSyncId
				);
				processUpdate();
				if (string.IsNullOrEmpty(destination.Roles))
				{
					destination.Roles = ",,";
				}
				StampAuthenticatedCustomer(client, id => destination.CustomerSyncId = id, id => destination.CustomerId = id);
				if (client.AuthenticatedAccount == null)
				{
					SampleSyncRelationships.SetCustomerId(client, source.CustomerSyncId, id => destination.CustomerId = id);
				}
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
		SampleServerSyncClient client,
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
				StampAuthenticatedCustomer(client, id => destination.CustomerSyncId = id, id => destination.CustomerId = id);
				if (client.AuthenticatedAccount == null)
				{
					SampleSyncRelationships.SetCustomerId(client, source.CustomerSyncId, id => destination.CustomerId = id);
				}
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
		SampleServerSyncClient client,
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
				SampleSyncRelationships.SetBookmarkParentId(
					client,
					source,
					destination,
					client.AuthenticatedAccount?.CustomerSyncId
				);
				processUpdate();
				StampAuthenticatedCustomer(client, id => destination.CustomerSyncId = id, id => destination.CustomerId = id);
				if (client.AuthenticatedAccount == null)
				{
					SampleSyncRelationships.SetCustomerId(client, source.CustomerSyncId, id => destination.CustomerId = id);
				}
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
		SampleServerSyncClient client,
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
		SampleServerSyncClient client,
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
