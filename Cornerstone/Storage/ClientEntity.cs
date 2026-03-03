#region References

using System;

#endregion

namespace Cornerstone.Storage;

/// <inheritdoc cref="Entity{T}" />
public abstract class ClientEntity<T> : Entity<T>, IClientEntity
{
	#region Properties

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