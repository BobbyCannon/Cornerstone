#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Sync;

#endregion

namespace Sample.Shared.Storage.Client;

/// <summary>
/// Represents the client private account model. That
/// does not inherit from a "SyncApi" model due to differences
/// of property types.
/// </summary>
public class ClientAccount : SyncEntity<int>, IAccountSync, IClientEntity
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

	#region Methods

	/// <inheritdoc />
	public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		var response = base.GetDefaultIncludedProperties(action);

		switch (action)
		{
			case UpdateableAction.SyncIncomingAdd:
			case UpdateableAction.SyncIncomingUpdate:
			case UpdateableAction.SyncOutgoing:
			{
				var syncProperties = typeof(AccountSync)
					.GetCachedProperties()
					.Select(x => x.Name)
					.ToList();
				response.Add(syncProperties);
				break;
			}
		}

		return response;
	}

	#endregion
}