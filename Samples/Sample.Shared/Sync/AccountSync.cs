#region References

using System;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Sync;

/// <inheritdoc cref="IAccountSync" />
public class AccountSync : SyncModel, IAccountSync
{
	#region Properties

	/// <inheritdoc />
	public Guid? AddressSyncId { get; set; }

	/// <inheritdoc />
	public string EmailAddress { get; set; }

	/// <inheritdoc />
	public string Name { get; set; }

	/// <inheritdoc />
	public string Roles { get; set; }

	#endregion

	#region Methods

	public int CompareTo(AccountSync other)
	{
		if (ReferenceEquals(this, other))
		{
			return 0;
		}
		if (ReferenceEquals(null, other))
		{
			return 1;
		}
		var addressSyncIdComparison = AddressSyncId?.CompareTo(other.AddressSyncId) ?? 0;
		if (addressSyncIdComparison != 0)
		{
			return addressSyncIdComparison;
		}
		var emailAddressComparison = string.Compare(EmailAddress, other.EmailAddress, StringComparison.Ordinal);
		if (emailAddressComparison != 0)
		{
			return emailAddressComparison;
		}
		return string.Compare(Name, other.Name, StringComparison.Ordinal);
	}

	public int CompareTo(object obj)
	{
		return CompareTo((AccountSync) obj);
	}

	#endregion
}

/// <summary>
/// Represents the account sync model.
/// </summary>
public interface IAccountSync
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