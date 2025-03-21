#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Server;

/// <summary>
/// Represents the account entity.
/// </summary>
public class AccountEntity : SyncEntity<int, Account>
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public AccountEntity()
	{
		Groups = new List<GroupMemberEntity>();
		Pets = new List<PetEntity>();
		ResetHasChanges();
	}

	#endregion

	#region Properties

	public virtual AddressEntity Address { get; set; }

	public long? AddressId { get; set; }

	public Guid? AddressSyncId { get; set; }

	public string EmailAddress { get; set; }

	public string ExternalId { get; set; }

	public virtual ICollection<GroupMemberEntity> Groups { get; set; }

	public override int Id { get; set; }

	public DateTime LastLoginDate { get; set; }

	public string Name { get; set; }

	public string Nickname { get; set; }

	public string PasswordHash { get; set; }

	public virtual ICollection<PetEntity> Pets { get; set; }

	public string Roles { get; set; }

	#endregion

	#region Methods

	public bool InRole(params AccountRole[] roles)
	{
		return roles.Any(x => InRole(x.ToString()));
	}

	public bool InRole(string roleName)
	{
		return Roles.SplitIntoArray().Any(x => x.Equals(roleName, StringComparison.OrdinalIgnoreCase));
	}

	#endregion
}