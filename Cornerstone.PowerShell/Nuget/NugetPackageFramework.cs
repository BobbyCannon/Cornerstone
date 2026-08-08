#region References

using System.Collections.Generic;
using Cornerstone.Data;
using NuGet.Frameworks;

#endregion

namespace Cornerstone.PowerShell.Nuget;

public partial class NugetPackageFramework : CornerstoneObject<NugetPackageFramework>
{
	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial IList<NugetPackageDependency> Dependencies { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial NuGetFramework Framework { get; set; }

	#endregion
}