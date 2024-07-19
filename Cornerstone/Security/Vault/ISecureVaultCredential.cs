#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.Vault;

public interface ISecureVaultCredentialView : ISecureVaultCredential, IViewModel<Guid>, ITrackPropertyChanges
{
}

public interface ISecureVaultCredential
{
	#region Properties

	public DateTime CreatedOn { get; set; }

	public DateTime ExpiresOn { get; set; }

	public string Group { get; set; }

	/// <summary>
	/// Gets or sets the ID of the view.
	/// </summary>
	public Guid Id { get; set; }

	public DateTime ModifiedOn { get; set; }

	public string Name { get; set; }

	public string Notes { get; set; }

	public DateTime PasswordChangedOn { get; set; }

	public string Uri { get; set; }

	public string UserName { get; set; }

	#endregion
}