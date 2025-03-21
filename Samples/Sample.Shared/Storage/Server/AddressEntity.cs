#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Data;
using Cornerstone.Sync;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Server;

public class AddressEntity : SyncEntity<long, Address>, IAddress
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public AddressEntity()
	{
		Accounts = new List<AccountEntity>();
		LinkedAddresses = new List<AddressEntity>();
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents the "primary" account for the address.
	/// </summary>
	public virtual AccountEntity Account { get; set; }

	/// <summary>
	/// The ID for the account.
	/// </summary>
	public int? AccountId { get; set; }

	/// <summary>
	/// The people associated with this address.
	/// </summary>
	public virtual ICollection<AccountEntity> Accounts { get; set; }

	/// <summary>
	/// The sync ID for the account.
	/// </summary>
	public Guid? AccountSyncId { get; set; }

	/// <inheritdoc />
	public string City { get; set; }

	/// <summary>
	/// Read only property
	/// </summary>
	public string FullAddress => $"{Line1}{Environment.NewLine}{City}, {State}  {Postal}";

	/// <summary>
	/// The ID for the address.
	/// </summary>
	public override long Id { get; set; }

	/// <inheritdoc />
	public string Line1 { get; set; }

	/// <inheritdoc />
	public string Line2 { get; set; }

	public virtual AddressEntity LinkedAddress { get; set; }

	public virtual ICollection<AddressEntity> LinkedAddresses { get; set; }

	public long? LinkedAddressId { get; set; }

	public Guid? LinkedAddressSyncId { get; set; }

	/// <inheritdoc />
	public string Postal { get; set; }

	/// <inheritdoc />
	public string State { get; set; }

	#endregion
}