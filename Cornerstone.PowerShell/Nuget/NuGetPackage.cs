#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.PowerShell.Nuget;

public partial class NugetPackage : CornerstoneObject<NugetPackage>
{
	#region Constructors

	/// <summary>
	/// For serialization, do not use.
	/// </summary>
	public NugetPackage() : this(string.Empty)
	{
	}

	public NugetPackage(string packageId)
	{
		PackageId = packageId;
		Versions = new PresentationList<NugetPackageVersion>(null, new OrderBy<NugetPackageVersion>(x => x.Version, true));
	}

	#endregion

	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string PackageId { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime UpdatedOn { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial PresentationList<NugetPackageVersion> Versions { get; set; }

	#endregion
}