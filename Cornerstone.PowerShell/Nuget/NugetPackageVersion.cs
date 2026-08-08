#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;

#endregion

namespace Cornerstone.PowerShell.Nuget;

public partial class NugetPackageVersion : CornerstoneObject<NugetPackageVersion>
{
	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial IList<NugetPackageFramework> Frameworks { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Version Version { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string VersionString { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial IList<NugetVulnerability> Vulnerabilities { get; set; }

	#endregion
}