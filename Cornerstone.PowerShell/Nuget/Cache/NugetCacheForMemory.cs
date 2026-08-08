#region References

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.PowerShell.Nuget.Cache;

public class NugetCacheForMemory : NugetCache
{
	#region Fields

	private readonly ConcurrentDictionary<string, Task<NugetPackage>> _cache;

	#endregion

	#region Constructors

	public NugetCacheForMemory()
	{
		_cache = new ConcurrentDictionary<string, Task<NugetPackage>>();

		EnableMemoryCache = true;
	}

	#endregion

	#region Properties

	public bool EnableMemoryCache { get; set; }

	#endregion

	#region Methods

	public override async Task<NugetPackage> GetOrAddAsync(string packageId, Func<string, Task<NugetPackage>> factoryAsync, CancellationToken ct = default)
	{
		if (!EnableMemoryCache)
		{
			var p = await factoryAsync(packageId).ConfigureAwait(false);
			p.UpdatedOn = DateTimeProvider.RealTime.UtcNow;
			return p.ShallowClone();
		}

		var responseTask = _cache.GetOrAdd(packageId,
			id =>
			{
				var task = factoryAsync(id);
				_ = task.ContinueWith(t =>
				{
					if (t is { IsCompletedSuccessfully: true, Result: not null })
					{
						t.Result.UpdatedOn = DateTimeProvider.RealTime.UtcNow;
					}
				});
				return task;
			}
		);

		var response = await responseTask.ConfigureAwait(false);
		return response?.ShallowClone();
	}

	#endregion
}