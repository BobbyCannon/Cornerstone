#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Security.Vault;

public interface ISecureVaultCredential : INotifyPropertyChanged
{
	#region Properties

	public DateTime CreatedOn { get; set; }

	public DateTime ExpiresOn { get; set; }

	public string Group { get; set; }

	/// <summary>
	/// Gets or sets the ID of the view.
	/// </summary>
	public Guid Id { get; set; }

	public bool IsFavorite { get; set; }

	public DateTime ModifiedOn { get; set; }

	public string Name { get; set; }

	public string Notes { get; set; }

	public string PasswordChange { get; set; }

	public DateTime PasswordChangedOn { get; set; }

	public string Uri { get; set; }

	public string UserName { get; set; }

	#endregion
}