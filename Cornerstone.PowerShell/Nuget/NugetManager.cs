#region References

using Cornerstone.PowerShell.Nuget.Cache;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.PowerShell.Nuget;

[SourceReflection]
public class NugetManager
{
	#region Fields

	private readonly NugetCache _cache;

	#endregion

	#region Constructors

	public NugetManager(NugetCache cache)
	{
		_cache = cache;
	}

	#endregion

	#region Methods

	public Task<NugetPackage> QueryForPackageAsync(string packageId, bool includePrerelease = false, CancellationToken cancellationToken = default)
	{
		return _cache.GetOrAddAsync(packageId, p => NugetService.QueryForPackageAsync(p, includePrerelease));
	}

	#endregion
}