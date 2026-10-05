#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Default and tenant repository filters for the sample sync clients.
/// Tenant ownership is a scopeFilter from the server authenticated account, not from client settings.
/// </summary>
public static class SampleSyncFilters
{
	#region Methods

	public static void ApplyAll(SyncSettings settings)
	{
		if (!settings.HasFilter(typeof(CustomerEntity)))
		{
			settings.AddFilter<CustomerEntity>();
		}

		if (!settings.HasFilter(typeof(AddressEntity)))
		{
			settings.AddFilter<AddressEntity>();
		}

		if (!settings.HasFilter(typeof(AccountEntity)))
		{
			settings.AddFilter<AccountEntity>();
		}

		if (!settings.HasFilter(typeof(BookmarkEntity)))
		{
			settings.AddFilter<BookmarkEntity>(
				orderBy: [
					new OrderBy<BookmarkEntity>(x => x.ParentSyncId),
					new OrderBy<BookmarkEntity>(x => x.Order)
				]
			);
		}

		if (!settings.HasFilter(typeof(SettingEntity)))
		{
			settings.AddFilter<SettingEntity>();
		}
	}

	public static void ApplyForCustomer(SyncSettings settings, Guid customerSyncId)
	{
		Guid? tenant = customerSyncId;

		settings.AddFilter<CustomerEntity>(
			scopeFilter: x => x.SyncId == customerSyncId
		);
		settings.AddFilter<AddressEntity>(
			scopeFilter: x => x.CustomerSyncId == tenant
		);
		settings.AddFilter<AccountEntity>(
			scopeFilter: x => x.CustomerSyncId == tenant
		);
		settings.AddFilter<BookmarkEntity>(
			scopeFilter: x => x.CustomerSyncId == tenant,
			orderBy: [
				new OrderBy<BookmarkEntity>(x => x.ParentSyncId),
				new OrderBy<BookmarkEntity>(x => x.Order)
			]
		);
		settings.AddFilter<SettingEntity>();
	}

	#endregion
}
