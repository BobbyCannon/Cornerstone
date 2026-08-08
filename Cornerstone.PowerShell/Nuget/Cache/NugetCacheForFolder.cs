#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.PowerShell.Nuget.Cache;

public class NugetCacheForFolder : NugetCache
{
	#region Constructors

	public NugetCacheForFolder(string directory)
	{
		Directory = directory;
		new DirectoryInfo(Directory).SafeCreate();
	}

	#endregion

	#region Properties

	public string Directory { get; }

	#endregion

	#region Methods

	public override async Task<NugetPackage> GetOrAddAsync(string packageId, Func<string, Task<NugetPackage>> factoryAsync, CancellationToken ct = default)
	{
		if (TryGet(packageId, out var package)
			&& CacheStillValid(package))
		{
			return package;
		}

		package = await factoryAsync.Invoke(packageId).ConfigureAwait(false);
		return SetCache(packageId, package);
	}

	private NugetPackage SetCache(string packageId, NugetPackage package)
	{
		if (package == null)
		{
			return null;
		}

		var cacheFilePath = Path.Combine(Directory, $"{packageId}.json");
		package.UpdatedOn = DateTimeProvider.RealTime.UtcNow;
		var cacheJson = package.ToJson();
		File.WriteAllText(cacheFilePath, cacheJson);
		return package;
	}

	private bool TryGet(string packageId, out NugetPackage package)
	{
		var cacheFilePath = Path.Combine(Directory, $"{packageId}.json");
		var info = new FileInfo(cacheFilePath);
		
		if (!info.Exists)
		{
			package = null;
			return false;
		}

		var cacheJson = File.ReadAllText(cacheFilePath);
		var nugetPackage = cacheJson.FromJson<NugetPackage>();

		if (!CacheStillValid(nugetPackage))
		{
			package = null;
			return false;
		}

		package = SetCache(packageId, nugetPackage);
		return true;
	}

	#endregion
}