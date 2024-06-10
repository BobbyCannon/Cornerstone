#region References

using System.Management.Automation;
using Cornerstone.Extensions;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup("Environment")]
[Cmdlet(VerbsCommon.Get, "RuntimeInformation")]
[CmdletDescription("The runtime information of the system.")]
[CmdletExample(Code = """
					Get-RuntimeInformation | Format-List

					ApplicationBitness            : X64
					ApplicationDataLocation       : C:\Users\Account\AppData\Local\Console
					ApplicationFileName           : Cornerstone.Console.exe
					ApplicationFilePath           : C:\Workspaces\Console\Console\bin\x64\Debug\net8.0-windows\Console.exe
					ApplicationIsDevelopmentBuild : True
					ApplicationIsElevated         : True
					ApplicationLocation           : C:\Workspaces\Console\Console\bin\x64\Debug\net8.0-windows
					ApplicationName               : Console
					ApplicationVersion            : 1.2.3.4
					DeviceId                      : ABC123
					DeviceManufacturer            : Microsoft
					DeviceModel                   : Surface
					DeviceName                    : MyComputer
					DevicePlatform                : Windows
					DevicePlatformBitness         : X64
					DevicePlatformVersion         : 10.0.22621.0
					DeviceType                    : Desktop
					""", Remarks = "")]
public class GetRuntimeInformationCmdlet : PSCmdlet
{
	#region Methods

	protected override void ProcessRecord()
	{
		WriteObject(new RuntimeInformation().Copy());
	}

	#endregion
}