#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers.VisualStudio;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget;

public class NugetPackageVersion : Notifiable<NugetPackageVersion>
{
	#region Properties

	public IList<TargetFrameworkMoniker> Frameworks { get; set; }

	public Version Version { get; set; }

	public string VersionString { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the NugetPackageVersion with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public override bool UpdateWith(NugetPackageVersion update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Frameworks.Reconcile(update.Frameworks);
			Version = update.Version;
			VersionString = update.VersionString;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Frameworks)), x => x.Frameworks.Reconcile(update.Frameworks));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Version)), x => x.Version = update.Version);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(VersionString)), x => x.VersionString = update.VersionString);
		}

		return true;
	}

	#endregion
}