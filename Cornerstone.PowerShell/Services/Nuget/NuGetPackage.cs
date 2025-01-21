#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget;

public class NugetPackage : Notifiable<NugetPackage>
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
		Versions = new SpeedyList<NugetPackageVersion>(null, new OrderBy<NugetPackageVersion>(x => x.Version, true));
	}

	#endregion

	#region Properties

	public string PackageId { get; set; }

	public DateTime UpdatedOn { get; set; }

	public IList<NugetPackageVersion> Versions { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the NugetPackage with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public override bool UpdateWith(NugetPackage update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			PackageId = update.PackageId;
			UpdatedOn = update.UpdatedOn;
			Versions.Reconcile(update.Versions);
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(PackageId)), x => x.PackageId = update.PackageId);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(UpdatedOn)), x => x.UpdatedOn = update.UpdatedOn);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Versions)), x => x.Versions.Reconcile(update.Versions));
		}

		return true;
	}

	#endregion
}