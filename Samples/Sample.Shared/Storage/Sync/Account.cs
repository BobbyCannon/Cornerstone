#region References

using System;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Sync;

/// <summary>
/// Represents the public account model.
/// </summary>
public class Account : SyncModel
{
	#region Properties

	/// <summary>
	/// The sync ID for the account.
	/// </summary>
	public Guid? AddressSyncId { get; set; }

	/// <summary>
	/// The email address for the account.
	/// </summary>
	public string EmailAddress { get; set; }

	/// <summary>
	/// The name of the account.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The list of roles for the account.
	/// </summary>
	public string[] Roles { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Combine the roles into a custom format for client side storage.
	/// This format is different from the server format just to show that
	/// client and server formats can be different.
	/// </summary>
	/// <param name="roles"> The roles for the account. </param>
	/// <returns> The roles in client storage format. </returns>
	public static string CombineRoles(params string[] roles)
	{
		return roles != null ? $";{string.Join(";", roles)};" : ";;";
	}

	/// <summary>
	/// Splits the roles from the custom format into an array.
	/// </summary>
	/// <param name="roles"> The roles for the account. </param>
	/// <returns> The array of roles. </returns>
	public static string[] SplitRoles(string roles)
	{
		return roles != null
			? roles.Split([";"], StringSplitOptions.RemoveEmptyEntries)
			: [];
	}

	#endregion
}

/// <summary>
/// Represents the account sync model.
/// </summary>
public interface IAccount
{
	#region Properties

	/// <summary>
	/// The sync ID for the account.
	/// </summary>
	Guid? AddressSyncId { get; set; }

	/// <summary>
	/// The email address for the account.
	/// </summary>
	string EmailAddress { get; set; }

	/// <summary>
	/// The name of the account.
	/// </summary>
	string Name { get; set; }

	/// <summary>
	/// The roles for the account.
	/// </summary>
	string Roles { get; set; }

	#endregion
}