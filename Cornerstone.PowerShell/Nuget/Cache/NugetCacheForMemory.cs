#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

	public override async Task<NugetPackage> GetOrAddAsync(string packageId, Func<string, CancellationToken, Task<NugetPackage>> factoryAsync, CancellationToken ct = default)
	{
		if (!EnableMemoryCache)
		{
			return CloneResult(await factoryAsync(packageId, ct).ConfigureAwait(false));
		}

		var task = _cache.GetOrAdd(packageId, id => Create(id, factoryAsync, ct));

		try
		{
			var response = await task.ConfigureAwait(false);
			if (response == null)
			{
				_cache.TryRemove(new KeyValuePair<string, Task<NugetPackage>>(packageId, task));
				return null;
			}

			return response.ShallowClone();
		}
		catch
		{
			_cache.TryRemove(new KeyValuePair<string, Task<NugetPackage>>(packageId, task));
			throw;
		}
	}

	private static async Task<NugetPackage> Create(string packageId, Func<string, CancellationToken, Task<NugetPackage>> factoryAsync, CancellationToken ct)
	{
		var package = await factoryAsync(packageId, ct).ConfigureAwait(false);
		if (package != null)
		{
			package.UpdatedOn = DateTimeProvider.RealTime.UtcNow;
		}

		return package;
	}

	private static NugetPackage CloneResult(NugetPackage package)
	{
		if (package == null)
		{
			return null;
		}

		package.UpdatedOn = DateTimeProvider.RealTime.UtcNow;
		return package.ShallowClone();
	}

	#endregion
}