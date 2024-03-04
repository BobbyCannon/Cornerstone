#region References

using System;
using System.Collections.Concurrent;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget.Cache;

public class NugetMemoryCache : INugetCache
{
	#region Fields

	private readonly ConcurrentDictionary<string, NugetPackage> _cache;

	#endregion

	#region Constructors

	public NugetMemoryCache()
	{
		_cache = new ConcurrentDictionary<string, NugetPackage>();

		EnableMemoryCache = true;
	}

	#endregion

	#region Properties

	public bool EnableMemoryCache { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual NugetPackage AddOrUpdate(string packageId, Func<string, NugetPackage> factory)
	{
		if (!EnableMemoryCache)
		{
			return factory.Invoke(packageId);
		}

		var response = _cache.AddOrUpdate(packageId,
			x =>
			{
				var p = factory.Invoke(x);
				p.UpdatedOn = TimeService.CurrentTime.UtcNow;
				return p;
			},
			(x, y) =>
			{
				var p = factory.Invoke(x);
				p.UpdatedOn = TimeService.CurrentTime.UtcNow;
				return p;
			}
		);

		return response.ShallowClone();
	}

	/// <inheritdoc />
	public virtual NugetPackage GetOrAdd(string packageId, Func<string, NugetPackage> factory)
	{
		if (!EnableMemoryCache)
		{
			return factory.Invoke(packageId);
		}

		var response = _cache.GetOrAdd(packageId, x =>
		{
			var p = factory.Invoke(x);
			p.UpdatedOn = TimeService.CurrentTime.UtcNow;
			return p;
		});

		return response.ShallowClone();
	}

	/// <inheritdoc />
	public bool TryGet(string packageId, out NugetPackage package)
	{
		return _cache.TryGetValue(packageId, out package);
	}

	protected void SetCache(string packageId, NugetPackage package)
	{
		_cache.AddOrUpdate(packageId, _ => package, (_, _) => package);
	}

	#endregion
}