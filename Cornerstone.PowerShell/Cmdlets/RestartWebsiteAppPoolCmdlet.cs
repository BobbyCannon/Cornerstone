#region References

using System.Linq;
using System.Management.Automation;
using Cornerstone.PowerShell.Documentation;
using Microsoft.Web.Administration;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Environment)]
[Cmdlet(VerbsLifecycle.Restart, "WebsiteAppPool")]
[CmdletDescription("Restart (or start) a website application pool.")]
[CmdletExample(Code = "", Remarks = "")]
public class RestartWebsiteAppPoolCmdlet : PSCmdlet
{
	#region Properties

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the IIS application pool.")]
	public string Name { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		using var server = new ServerManager();
		var sitePool = server.ApplicationPools.FirstOrDefault(pool => pool.Name == Name);
		if (sitePool == null)
		{
			return;
		}

		// Recycle throws 0x800710D8 when the pool is stopped.
		if (sitePool.State == ObjectState.Stopped)
		{
			sitePool.Start();
			return;
		}

		sitePool.Recycle();
	}

	#endregion
}