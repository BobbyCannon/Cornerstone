#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Runtime;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

#endregion

namespace Cornerstone.PowerShell.Nuget;

public static class NugetService
{
	#region Methods

	public static NuGetFramework GetBestCompatibleFramework(IList<NuGetFramework> projectFrameworks, IList<NuGetFramework> sourceFrameworks)
	{
		var reducer = new FrameworkReducer();
		var preferred = projectFrameworks.Any()
			? projectFrameworks
			: sourceFrameworks;

		var preferredList = preferred.ToList();

		if (!preferredList.Any())
		{
			return null;
		}

		var representative = preferredList.First();
		var nearest = reducer.GetNearest(representative, sourceFrameworks);

		if ((nearest != null) && preferredList.Any(p => DefaultCompatibilityProvider.Instance.IsCompatible(p, nearest)))
		{
			return nearest;
		}

		return sourceFrameworks
			.OrderByDescending(fw => fw.Version)
			.FirstOrDefault(fw => preferredList.Any(p => DefaultCompatibilityProvider.Instance.IsCompatible(p, fw)));
	}

	public static async Task<NugetPackage> QueryForPackageAsync(string packageId, bool includePrerelease = false, CancellationToken token = default)
	{
		try
		{
			var providers = Repository.Provider.GetCoreV3().ToList();
			var source = new PackageSource("https://api.nuget.org/v3/index.json");
			var repo = Repository.CreateSource(providers, source);

			var metadataResource = await repo.GetResourceAsync<PackageMetadataResource>(token);

			var cacheContext = new SourceCacheContext
			{
				MaxAge = DateTimeProvider.RealTime.UtcNow.AddHours(6),
				DirectDownload = true,
				NoCache = true
			};

			var allVersionsMetadata = (await metadataResource.GetMetadataAsync(packageId, includePrerelease, false, cacheContext, NullLogger.Instance, token))?.ToList();
			if (allVersionsMetadata is not { Count: > 0 })
			{
				return null;
			}

			var result = new NugetPackage(packageId);

			foreach (var pkg in allVersionsMetadata.OrderByDescending(p => p.Identity.Version))
			{
				var versionInfo = new NugetPackageVersion
				{
					Frameworks = pkg.DependencySets
						.Where(ds => !ds.TargetFramework.IsUnsupported)
						.Select(ds => new NugetPackageFramework
						{
							Framework = ds.TargetFramework,
							Dependencies = ds.Packages
								.Select(dep => new NugetPackageDependency
								{
									Id = dep.Id,
									VersionRange = dep.VersionRange?.ToNormalizedString() ?? "[]",
									TargetFramework = ds.TargetFramework?.GetShortFolderName() ?? "(any)"
								})
								.ToList()
						})
						.ToList(),
					Version = pkg.Identity.Version.Version,
					VersionString = pkg.Identity.Version.OriginalVersion,
					Vulnerabilities = pkg.Vulnerabilities?.Select(ToVulnerability).ToList() ?? []
				};

				result.Versions.Add(versionInfo);
			}

			return result;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static NugetVulnerability ToVulnerability(PackageVulnerabilityMetadata item)
	{
		return new NugetVulnerability
		{
			Severity = item.Severity,
			AdvisoryUrl = item.AdvisoryUrl
		};
	}

	#endregion
}

public class NugetPackageDependency
{
	#region Properties

	public string Id { get; set; }
	public string TargetFramework { get; set; }
	public string VersionRange { get; set; }

	#endregion
}