#region References

using System;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <inheritdoc cref="SyncEntity{T}" />
public abstract class ClientEntity<T> : SyncEntity<T>, IClientEntity
{
	#region Properties

	/// <inheritdoc />
	public DateTime LastClientUpdate { get; set; }

	#endregion
}

/// <summary>
/// Represents a client entity.
/// </summary>
public interface IClientEntity
{
	#region Properties

	/// <summary>
	/// The last time the client entity was updated.
	/// </summary>
	public DateTime LastClientUpdate { get; set; }

	#endregion
}