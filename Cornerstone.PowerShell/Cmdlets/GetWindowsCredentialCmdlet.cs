#region References

using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.PowerShell.Security;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup("Security")]
[Cmdlet("Get", "WindowsCredential")]
[CmdletDescription("Gets a credential from the Windows Credential Manager.")]
[CmdletExample(Code = "Get-WindowsCredential -Name \"Online Bank\"\r\n# ex.\r\n# UserName                     Password\r\n# --------                     --------\r\n# John Doe System.Security.SecureString\r\n", Remarks = "Gets the windows credential by name.")]
public class GetWindowsCredentialCmdlet : PSCmdlet
{
	#region Constructors

	public GetWindowsCredentialCmdlet()
	{
		Type = WindowsCredentialType.Generic;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the credential to read.")]
	public string Name { get; set; }

	[Parameter(Mandatory = false, Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The type of the credential to read.")]
	public WindowsCredentialType Type { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		var credential = WindowsCredentialManager.ReadCredential(Name, Type);
		if (credential == null)
		{
			throw new ItemNotFoundException(Babel.Tower[BabelKeys.NotFound]);
		}
		WriteObject(new PSCredential(credential.UserName, credential.SecurePassword));
	}

	#endregion
}