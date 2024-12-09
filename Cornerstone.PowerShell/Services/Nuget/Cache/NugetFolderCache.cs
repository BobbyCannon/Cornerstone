#region References

using System;
using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Text;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget.Cache;

public class NugetFolderCache : NugetMemoryCache
{
	#region Constructors

	public NugetFolderCache(string directory)
	{
		Directory = directory;
		CacheRefreshPeriod = TimeSpan.FromDays(1);
	}

	#endregion

	#region Properties

	public TimeSpan CacheRefreshPeriod { get; set; }

	public string Directory { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override NugetPackage GetOrAdd(string packageId, Func<string, NugetPackage> factory)
	{
		// First try to get from the cache.
		if (TryGet(packageId, out var package) && CacheStillValid(package))
		{
			return package;
		}

		new DirectoryInfo(Directory).SafeCreate();
		var cacheFilePath = Path.Combine(Directory, packageId + ".json");

		if (File.Exists(cacheFilePath))
		{
			var cacheJson = File.ReadAllText(cacheFilePath);
			package = cacheJson.FromJson<NugetPackage>();
			if (CacheStillValid(package))
			{
				FixUpReferences(package);
				SetCache(packageId, package);
				return package;
			}
		}

		package = base.AddOrUpdate(packageId, NugetService.QueryForPackage);
		var settings = new SerializationSettings { TextFormat = TextFormat.Indented };
		var packageJson = package.ToJson(settings);
		File.WriteAllText(cacheFilePath, packageJson);
		return package;
	}

	private bool CacheStillValid(NugetPackage package)
	{
		if (package == null)
		{
			return false;
		}

		var cachePassedTime = DateTimeProvider.RealTime.UtcNow - package.UpdatedOn;
		return cachePassedTime < CacheRefreshPeriod;
	}

	private void FixUpReferences(NugetPackage package)
	{
		foreach (var version in package.Versions)
		{
			//version.Frameworks = version.Frameworks
			//	.Select(x => TargetFrameworkService.ByType(x.FrameworkType) ?? x)
			//	.ToArray();
		}
	}

	#endregion
}