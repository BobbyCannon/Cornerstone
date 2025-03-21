#region References

using System;

#endregion

namespace Cornerstone.Storage;

/// <inheritdoc cref="ModifiableEntity{TKey}" />
public abstract class ClientModifiableEntity<T> : ModifiableEntity<T>, IClientEntity
{
	#region Properties

	/// <inheritdoc />
	public DateTime LastClientUpdate { get; set; }

	#endregion
}