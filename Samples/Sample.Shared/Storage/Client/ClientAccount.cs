#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Client;

/// <summary>
/// Represents the client private account model. That
/// does not inherit from a "SyncApi" model due to differences
/// of property types.
/// </summary>
public class ClientAccount : SyncEntity<int, Account>, IAccount, IClientEntity
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public ClientAccount()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The required address for this person.
	/// </summary>
	public virtual ClientAddress Address { get; set; }

	/// <summary>
	/// The ID for the address.
	/// </summary>
	public long? AddressId { get; set; }

	/// <summary>
	/// The sync ID for the address.
	/// </summary>
	public Guid? AddressSyncId { get; set; }

	/// <summary>
	/// The email address for the account.
	/// </summary>
	public string EmailAddress { get; set; }

	/// <inheritdoc />
	public override int Id { get; set; }

	/// <summary>
	/// Represents the last time this account was update on the client
	/// </summary>
	public DateTime LastClientUpdate { get; set; }

	/// <summary>
	/// The name of the account.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The roles in storage format.
	/// </summary>
	public string Roles { get; set; }

	#endregion
}