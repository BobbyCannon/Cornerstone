#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Server;

/// <summary>
/// Represents the account entity.
/// </summary>
public class AccountEntity : SyncEntity<int>
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

	/// <summary>
	/// Combine the roles into a custom format for server side storage.
	/// This format is different from the client format just to show that
	/// client and server formats can be different.
	/// </summary>
	/// <param name="roles"> The roles for the account. </param>
	/// <returns> The roles in the server storage format. </returns>
	public static string CombineRoles(IEnumerable<string> roles)
	{
		return roles != null ? $",{string.Join(",", roles)}," : ",,";
	}

	public string GetCookieValue()
	{
		return $"{Id};{Name}";
	}

	public IEnumerable<string> GetRoles()
	{
		return string.IsNullOrEmpty(Roles) ? Array.Empty<string>() : Roles.Split([","], StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
	}

	public bool InRole(params AccountRole[] roles)
	{
		return roles.Any(x => InRole(x.ToString()));
	}

	public bool InRole(string roleName)
	{
		return GetRoles().Any(x => x.Equals(roleName, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Splits the roles from the custom format into an array.
	/// </summary>
	/// <param name="roles"> The roles for the account. </param>
	/// <returns> The array of roles. </returns>
	public static IEnumerable<string> SplitRoles(string roles)
	{
		return roles?.Split([","], StringSplitOptions.RemoveEmptyEntries)
			?? Array.Empty<string>();
	}

	#endregion
}