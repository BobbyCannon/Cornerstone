#region References

using Cornerstone.PowerShell.Services.Nuget.Cache;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget;

public class NugetManager
{
	#region Fields

	private readonly INugetCache _cache;

	#endregion

	#region Constructors

	public NugetManager(INugetCache cache)
	{
		_cache = cache;
	}

	#endregion

	#region Methods

	public NugetPackage QueryForPackage(string packageId)
	{
		return _cache.GetOrAdd(packageId, NugetService.QueryForPackage);
	}

	#endregion
}