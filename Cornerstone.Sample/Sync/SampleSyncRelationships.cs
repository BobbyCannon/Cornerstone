#region References

using System;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Resolves local FKs from SyncIds and rejects cross-customer relationships.
/// </summary>
public static class SampleSyncRelationships
{
	#region Methods

	public static void SetAccountAddressId(SyncClientForDatabase client, AccountEntity source, AccountEntity destination)
	{
		SetAccountAddressId(client, source, destination, null);
	}

	public static void SetAccountAddressId(
		SyncClientForDatabase client,
		AccountEntity source,
		AccountEntity destination,
		Guid? sessionCustomerSyncId
	)
	{
		if (source.AddressSyncId is not { } addressSyncId || (addressSyncId == Guid.Empty))
		{
			destination.AddressId = null;
			return;
		}

		var addressId = client.GetEntityPrimaryKey<AddressEntity, long>(addressSyncId);
		if (addressId == 0)
		{
			destination.AddressId = null;
			return;
		}

		EnsureSameCustomer(
			client,
			addressSyncId,
			OwnerCustomerSyncId(sessionCustomerSyncId, destination.CustomerSyncId, source.CustomerSyncId),
			"Address does not belong to the account customer."
		);
		destination.AddressId = addressId;
	}

	public static void SetBookmarkParentId(SyncClientForDatabase client, BookmarkEntity source, BookmarkEntity destination)
	{
		SetBookmarkParentId(client, source, destination, null);
	}

	public static void SetBookmarkParentId(
		SyncClientForDatabase client,
		BookmarkEntity source,
		BookmarkEntity destination,
		Guid? sessionCustomerSyncId
	)
	{
		if (source.ParentSyncId is not { } parentSyncId || (parentSyncId == Guid.Empty))
		{
			destination.ParentId = null;
			return;
		}

		var parentId = client.GetEntityPrimaryKey<BookmarkEntity, long>(parentSyncId);
		if (parentId == 0)
		{
			destination.ParentId = null;
			return;
		}

		EnsureSameCustomer(
			client,
			parentSyncId,
			OwnerCustomerSyncId(sessionCustomerSyncId, destination.CustomerSyncId, source.CustomerSyncId),
			"Bookmark parent does not belong to the same customer.",
			isBookmarkParent: true
		);
		destination.ParentId = parentId;
	}

	public static void SetCustomerId(SyncClientForDatabase client, Guid? customerSyncId, Action<int?> setCustomerId)
	{
		if (customerSyncId is not { } syncId || (syncId == Guid.Empty))
		{
			setCustomerId(null);
			return;
		}

		var customerId = client.GetEntityPrimaryKey<CustomerEntity, int>(syncId);
		setCustomerId(customerId == 0 ? null : customerId);
	}

	private static Guid? OwnerCustomerSyncId(Guid? sessionCustomerSyncId, Guid? storedCustomerSyncId, Guid? incomingCustomerSyncId)
	{
		if (sessionCustomerSyncId is { } session && (session != Guid.Empty))
		{
			return session;
		}

		if (storedCustomerSyncId is { } stored && (stored != Guid.Empty))
		{
			return stored;
		}

		if (incomingCustomerSyncId is { } incoming && (incoming != Guid.Empty))
		{
			return incoming;
		}

		return null;
	}

	private static void EnsureSameCustomer(
		SyncClientForDatabase client,
		Guid relatedSyncId,
		Guid? ownerCustomerSyncId,
		string message,
		bool isBookmarkParent = false
	)
	{
		Guid? relatedCustomerSyncId;
		if (isBookmarkParent)
		{
			var parent = client.FindBySyncId<BookmarkEntity>(relatedSyncId);
			relatedCustomerSyncId = parent?.CustomerSyncId;
		}
		else
		{
			var address = client.FindBySyncId<AddressEntity>(relatedSyncId);
			relatedCustomerSyncId = address?.CustomerSyncId;
		}

		if (relatedCustomerSyncId is not { } related || (related == Guid.Empty))
		{
			return;
		}

		if (ownerCustomerSyncId is not { } owner || (owner == Guid.Empty) || (owner != related))
		{
			throw new SyncUpdateException(message);
		}
	}

	#endregion
}
