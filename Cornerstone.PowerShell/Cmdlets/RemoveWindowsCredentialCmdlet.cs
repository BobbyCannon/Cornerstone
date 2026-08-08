#region References

using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.PowerShell.Security;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup("Security")]
[Cmdlet("Remove", "WindowsCredential")]
[CmdletDescription("Remove a credential from the Windows Credential Manager.")]
[CmdletExample(Code = "Remove-WindowsCredential -Name \"Online Bank\"\r\n# ex.\r\n# UserName                     Password\r\n# --------                     --------\r\n# John Doe System.Security.SecureString\r\n", Remarks = "Gets the windows credential by name.")]
public class RemoveWindowsCredentialCmdlet : PSCmdlet
{
	#region Constructors

	public RemoveWindowsCredentialCmdlet()
	{
		Type = WindowsCredentialType.Generic;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the credential to delete.")]
	public string Name { get; set; }

	[Parameter(Mandatory = false, Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The type of the credential to delete.")]
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

		WindowsCredentialManager.Delete(credential);
	}

	#endregion
}