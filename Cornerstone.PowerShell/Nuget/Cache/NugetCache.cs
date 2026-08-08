#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.PowerShell.Nuget.Cache;

[SourceReflection]
public abstract class NugetCache
{
	#region Constructors

	protected NugetCache()
	{
		CacheRefreshPeriod = TimeSpan.FromDays(1);
	}

	#endregion

	#region Properties

	public TimeSpan CacheRefreshPeriod { get; set; }

	#endregion

	#region Methods

	public abstract Task<NugetPackage> GetOrAddAsync(string packageId, Func<string, Task<NugetPackage>> factoryAsync, CancellationToken ct = default);

	protected bool CacheStillValid(NugetPackage package)
	{
		if (package == null)
		{
			return false;
		}

		var passedTime = DateTimeProvider.RealTime.UtcNow - package.UpdatedOn;
		return passedTime < CacheRefreshPeriod;
	}

	#endregion
}