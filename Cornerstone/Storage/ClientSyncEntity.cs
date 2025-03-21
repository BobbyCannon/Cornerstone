#region References

using System;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <inheritdoc cref="SyncEntity{T}" />
public abstract class ClientSyncEntity<TKey>
	: SyncEntity<TKey>, IClientEntity
{
	#region Properties

	/// <inheritdoc />
	public DateTime LastClientUpdate { get; set; }

	#endregion
}

/// <inheritdoc cref="SyncEntity{T}" />
public abstract class ClientSyncEntity<TKey, TModel>
	: SyncEntity<TKey, TModel>, IClientEntity
	where TModel : SyncModel
{
	#region Properties

	/// <inheritdoc />
	public DateTime LastClientUpdate { get; set; }

	#endregion
}