#region References

using System;

#endregion

namespace Cornerstone.Storage;

/// <inheritdoc cref="CreatedEntity{TKey}" />
public abstract class ClientCreatedEntity<T> : CreatedEntity<T>, IClientEntity
{
	#region Properties

	/// <inheritdoc />
	public DateTime LastClientUpdate { get; set; }

	#endregion
}