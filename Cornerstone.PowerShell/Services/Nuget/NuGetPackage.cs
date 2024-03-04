#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget;

public class NugetPackage : Cloneable<NugetPackage>
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
	public virtual bool UpdateWith(NugetPackage update)
	{
		return UpdateWith(update, UpdateableOptions.Empty);
	}

	/// <summary>
	/// Update the NugetPackage with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(NugetPackage update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			PackageId = update.PackageId;
			UpdatedOn = update.UpdatedOn;
			Versions.Reconcile(update.Versions);
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(PackageId)), x => x.PackageId = update.PackageId);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(UpdatedOn)), x => x.UpdatedOn = update.UpdatedOn);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Versions)), x => x.Versions.Reconcile(update.Versions));
		}

		return base.UpdateWith(update, options);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			NugetPackage value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}