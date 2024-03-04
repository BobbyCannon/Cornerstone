#region References

using System;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget.Cache;

public interface INugetCache
{
	#region Methods

	NugetPackage AddOrUpdate(string packageId, Func<string, NugetPackage> factory);

	NugetPackage GetOrAdd(string packageId, Func<string, NugetPackage> factory);

	bool TryGet(string packageId, out NugetPackage package);

	#endregion
}