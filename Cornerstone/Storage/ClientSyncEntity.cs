#region References

using System;
using Cornerstone.Data;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <inheritdoc cref="SyncEntity{T}" />
[Updateable]
public abstract partial class ClientSyncEntity<TKey>
	: SyncEntity<TKey>, IClientEntity
{
	#region Properties

	[UpdateableAction(UpdateableAction.EverythingExceptSync)]
	public DateTime LastClientUpdate { get; set; }

	#endregion
}