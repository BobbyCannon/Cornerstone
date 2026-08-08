#region References

using System.Collections.Generic;
using System.Security;
using Cornerstone.Extensions;
using Cornerstone.Web;

#endregion

namespace Cornerstone.PowerShell.Security;

/// <summary>
/// Represents a credential for windows.
/// </summary>
public partial class WindowsCredential : Credential
{
	#region Constructors

	/// <summary>
	/// Create an instance of a windows credential.
	/// </summary>
	public WindowsCredential()
	{
		// For serialization, do not remove
	}

	/// <summary>
	/// Create an instance of a windows credential.
	/// </summary>
	public WindowsCredential(string applicationName, string userName, string password, string comment = null)
		: this(WindowsCredentialType.Generic, applicationName, userName, password, comment)
	{
	}

	/// <summary>
	/// Create an instance of a windows credential.
	/// </summary>
	public WindowsCredential(WindowsCredentialType credentialType, string applicationName, string userName, string password, string comment = null)
	{
		ApplicationName = applicationName;
		Attributes = new Dictionary<string, string>();
		CredentialType = credentialType;
		Comment = comment;
		UserName = userName;
		Password = password;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The name of the application.
	/// </summary>
	public string ApplicationName { get; }

	/// <summary>
	/// Attributes for the credential.
	/// </summary>
	public Dictionary<string, string> Attributes { get; }

	/// <summary>
	/// A comment for the credential.
	/// </summary>
	public string Comment { get; }

	/// <summary>
	/// The type of the credential.
	/// </summary>
	public WindowsCredentialType CredentialType { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return $"CredentialType: {CredentialType}, ApplicationName: {ApplicationName}, UserName: {UserName}";
	}

	#endregion
}