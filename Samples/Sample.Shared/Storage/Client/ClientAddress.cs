#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Sync;

#endregion

namespace Sample.Shared.Storage.Client;

/// <summary>
/// Represents the client private address model.
/// </summary>
public class ClientAddress : SyncEntity<long>, IAddressSync, IClientEntity
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public ClientAddress()
	{
		Accounts = new List<ClientAccount>();
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The people associated with this address.
	/// </summary>
	public virtual ICollection<ClientAccount> Accounts { get; set; }

	/// <inheritdoc />
	public string City { get; set; }

	/// <inheritdoc />
	public override long Id { get; set; }

	/// <summary>
	/// Represents the last time this account was update on the client
	/// </summary>
	public DateTime LastClientUpdate { get; set; }

	/// <inheritdoc />
	public string Line1 { get; set; }

	/// <inheritdoc />
	public string Line2 { get; set; }

	/// <inheritdoc />
	public string Postal { get; set; }

	/// <inheritdoc />
	public string State { get; set; }

	#endregion
}