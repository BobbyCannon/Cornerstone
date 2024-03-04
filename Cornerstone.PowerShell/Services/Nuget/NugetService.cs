#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.Parsers.VisualStudio;
using Cornerstone.Parsers.VisualStudio.Project;
using Cornerstone.Parsers.VisualStudio.Solution;
using Cornerstone.Parsers.Xml;
using Cornerstone.Testing;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using PackageReference = Cornerstone.Parsers.VisualStudio.Project.PackageReference;

#endregion

namespace Cornerstone.PowerShell.Services.Nuget;

/// <summary>
/// make local repository
/// - cleanup
/// use local repo first then go to online version
/// load sln or csproj
/// locate nuget packages per framework
/// </summary>
/// <remarks>
/// https://gist.github.com/cpyfferoen/74092a74b165e85aed5ca1d51973b9d2
/// </remarks>
public static class NugetService
{
	#region Methods

	public static NugetPackage QueryForPackage(string packageId)
	{
		var logger = new NullLogger();
		var providers = new List<Lazy<INuGetResourceProvider>>();
		providers.AddRange(Repository.Provider.GetCoreV3());

		var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
		var sourceRepository = new SourceRepository(packageSource, providers);
		var packageMetadataResource = sourceRepository.GetResourceAsync<PackageMetadataResource>().Result;
		var context = new SourceCacheContext();
		context.GeneratedTempFolder = @"C:\Workspaces\Nuget\QueryCache";
		context.NoCache = false;
		context.RefreshMemoryCache = true;
		var searchMetadata = packageMetadataResource
			.GetMetadataAsync(packageId, false, false, context, logger, CancellationToken.None)
			.AwaitResults();

		var nugetPackage = new NugetPackage(packageId);

		foreach (var m in searchMetadata)
		{
			var packageVersion = new NugetPackageVersion
			{
				Version = m.Identity.Version.Version,
				VersionString = m.Identity.Version.OriginalVersion,
				Frameworks = m.DependencySets
					.Select(ToFramework)
					.ToList()
			};
			nugetPackage.Versions.Add(packageVersion);
		}

		return nugetPackage;
	}

	public static void RollbackClassicReferences(string nugetPrefix, DotNetSolution sourceSolution, DotNetSolution targetSolution)
	{
		foreach (var project in targetSolution.Projects)
		{
			if (project.SupportsPackageReferences)
			{
				// Supports the new format so bounce
				continue;
			}

			foreach (var itemGroup in project.ItemGroups)
			{
				if (!itemGroup.References.Any())
				{
					continue;
				}

				var references = itemGroup
					.References
					.Where(x => x.Include.StartsWith(nugetPrefix, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (references.Any())
				{
					foreach (var reference in references)
					{
						var sourceProject = sourceSolution.Projects.FirstOrDefault(x => x.ProjectName == reference.IncludeName);
						if (sourceProject == null)
						{
							continue;
						}

						//  <HintPath>..\packages\Speedy.12.0.1\lib\netstandard2.0\Speedy.dll</HintPath>
						// 	<HintPath>C:\Workspaces\GitHub\Speedy\Speedy\bin\debug\netstandard2.0\Speedy.dll</HintPath>

						var bestFramework = GetCompatibleFramework(sourceProject, itemGroup, project);
						if (bestFramework != null)
						{
							var hintPath = Path.Combine("..\\packages",
								$"{sourceProject.ProjectName}.{sourceProject.VersionString}",
								"lib",
								$"{bestFramework.Moniker}{bestFramework.OperatingSystemVersion}",
								$"{sourceProject.ProjectName}.dll");

							reference.Update(reference.IncludeVersion, hintPath);
						}
					}
				}
			}

			project.Save();
		}
	}

	public static void RollbackLocalReferences(string nugetPrefix, string sourcePath, string targetPath)
	{
		var sourceSolution = DotNetSolution.LoadFile(sourcePath);
		var targetSolution = DotNetSolution.LoadFile(targetPath);

		RollbackClassicReferences(nugetPrefix, sourceSolution, targetSolution);
		RollbackLocalReferences(nugetPrefix, sourceSolution, targetSolution);
	}

	public static void RollbackLocalReferences(string nugetPrefix, DotNetSolution sourceSolution, DotNetSolution targetSolution)
	{
		foreach (var project in targetSolution.Projects)
		{
			if (!project.SupportsPackageReferences)
			{
				// Supports the new format so bounce
				continue;
			}

			foreach (var itemGroup in project.ItemGroups)
			{
				if (!itemGroup.References.Any())
				{
					continue;
				}

				var references = itemGroup
					.References
					.Where(x => x.Include.StartsWith(nugetPrefix, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (references.Any())
				{
					foreach (var reference in references)
					{
						// Locate the reference project
						var sourceProject = sourceSolution.Projects
							.FirstOrDefault(x => x.ProjectName == reference.IncludeName);

						if (sourceProject == null)
						{
							continue;
						}

						var bestFramework = GetCompatibleFramework(sourceProject, itemGroup, project);
						if (bestFramework != null)
						{
							var packageReference = new PackageReference();
							packageReference.SetOrAddAttributeValue(nameof(packageReference.Include), sourceProject.ProjectName);
							packageReference.SetOrAddAttributeValue(nameof(packageReference.Version), sourceProject.VersionString);

							var index = itemGroup.Elements.IndexOf(reference);
							itemGroup.Elements.RemoveAt(index);
							itemGroup.Elements.Insert(index, packageReference);
						}
					}
				}
			}

			project.Save();
		}
	}

	public static void SetClassicReferences(string nugetPrefix, bool debug, DotNetSolution sourceSolution, DotNetSolution targetSolution)
	{
		//<Reference Include="Speedy, Version=12.0.1.0, Culture=neutral, PublicKeyToken=8db7b042d9663bf8, processorArchitecture=MSIL">
		//	<HintPath>..\packages\Speedy.12.0.1\lib\netstandard2.0\Speedy.dll</HintPath>
		//</Reference>
		foreach (var project in targetSolution.Projects)
		{
			foreach (var itemGroup in project.ItemGroups)
			{
				if (!itemGroup.References.Any())
				{
					continue;
				}

				var references = itemGroup
					.References
					.Where(x => x.Include.StartsWith(nugetPrefix, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (references.Any())
				{
					foreach (var reference in references)
					{
						var sourceProject = sourceSolution.Projects.FirstOrDefault(x => x.ProjectName == reference.IncludeName);
						if (sourceProject == null)
						{
							continue;
						}

						var bestFramework = GetCompatibleFramework(sourceProject, itemGroup, project);
						if (bestFramework != null)
						{
							var hintPath = CalculateAssemblyPath(sourceProject, bestFramework, debug);
							if (hintPath != null)
							{
								var versionString = sourceProject.Version?.ToString(4)
									?? sourceProject.VersionString;

								reference.Update(versionString, hintPath);
							}
						}
					}
				}
			}

			project.Save();
		}
	}

	public static void SetLocalReferences(string nugetPrefix, bool debug, string sourcePath, string targetPath)
	{
		var sourceSolution = DotNetSolution.LoadFile(sourcePath);
		var targetSolution = DotNetSolution.LoadFile(targetPath);

		SetClassicReferences(nugetPrefix, debug, sourceSolution, targetSolution);
		SetLocalReferences(nugetPrefix, debug, sourceSolution, targetSolution);
	}

	public static void SetLocalReferences(string nugetPrefix, bool debug, DotNetSolution sourceSolution, DotNetSolution targetSolution)
	{
		foreach (var project in targetSolution.Projects)
		{
			foreach (var itemGroup in project.ItemGroups)
			{
				if (!itemGroup.PackageReferences.Any())
				{
					continue;
				}

				var packageReferences = itemGroup
					.PackageReferences
					.Where(x => x.Include.StartsWith(nugetPrefix, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (packageReferences.Any())
				{
					foreach (var packageReference in packageReferences)
					{
						// Locate the reference project
						var sourceProject = sourceSolution.Projects
							.FirstOrDefault(x => x.ProjectName == packageReference.Include);

						if (sourceProject == null)
						{
							continue;
						}

						var bestFramework = GetCompatibleFramework(sourceProject, itemGroup, project);
						if (bestFramework != null)
						{
							var path = CalculateAssemblyPath(sourceProject, bestFramework, debug);
							if (path != null)
							{
								var reference = new ClassicReference();
								var hintPath = new XmlElement("HintPath") { ElementValue = path };
								reference.Attributes.Add(new XmlAttribute("Include", sourceProject.ProjectName));
								reference.Elements.Add(hintPath);

								var index = itemGroup.Elements.IndexOf(packageReference);
								itemGroup.Elements.RemoveAt(index);
								itemGroup.Elements.Insert(index, reference);
							}
						}
					}
				}
			}

			project.Save();
		}
	}

	public static void UpgradePackages(string solutionPath, NugetManager service)
	{
		var solution = DotNetSolution.LoadFile(solutionPath);
		UpgradePackages(solution, service);
	}

	public static void UpgradePackages(DotNetSolution solution, NugetManager service)
	{
		foreach (var project in solution.Projects)
		{
			foreach (var itemGroup in project.ItemGroups)
			{
				var itemGroupReferences = itemGroup.PackageReferences.ToList();

				if (!itemGroupReferences.Any())
				{
					continue;
				}

				foreach (var packageReference in itemGroupReferences)
				{
					var nugetPackage = service.QueryForPackage(packageReference.Include);
					if (nugetPackage == null)
					{
						continue;
					}

					// see if there is a new version, greater than version and supported by new version
					var newerVersions = nugetPackage.Versions
						.Where(x => x.Version > packageReference.Version)
						.ToList();

					if (newerVersions.Count <= 0)
					{
						// Could not find any newer versions
						continue;
					}

					// Line up the desire project framework
					var itemGroupFrameworks = itemGroup.TargetFrameworks.ToList();
					var projectFrameworks = project.TargetFrameworks.ToList();
					var latestSupportedVersion = newerVersions
						.FirstOrDefault(x => SupportsPackage(itemGroupFrameworks, x.Frameworks)
							|| SupportsPackage(projectFrameworks, x.Frameworks)
						);

					if (latestSupportedVersion == null)
					{
						// Could not find a newer version for this framework
						continue;
					}

					if (packageReference.Compare(latestSupportedVersion.Version) >= 0)
					{
						// Current package is good (latest or better)
						continue;
					}

					if (packageReference.UpdateVersion(latestSupportedVersion.VersionString))
					{
						// We set the version so bounce.
					}
				}
			}
		}
	}

	private static string CalculateAssemblyPath(DotNetSolutionProject sourceProject, TargetFramework bestFramework, bool debug)
	{
		// Calculate local file location path
		var directory = Path.GetDirectoryName(sourceProject.AbsolutePath);
		var configuration = debug ? "Debug" : "Release";
		var paths = new[]
		{
			Path.Combine(directory, "bin", configuration, $"{bestFramework.Moniker}{bestFramework.OperatingSystemVersion}", $"{sourceProject.ProjectName}.dll"),
			Path.Combine(directory, "bin", configuration, bestFramework.Moniker, $"{sourceProject.ProjectName}.dll"),
			Path.Combine(directory, "bin", configuration, $"{sourceProject.ProjectName}.dll")
		};

		var path = paths.FirstOrDefault(File.Exists);
		return path;
	}

	private static TargetFramework GetCompatibleFramework(DotNetSolutionProject sourceProject, ItemGroup itemGroup, DotNetSolutionProject project)
	{
		// Line up the desire project framework
		var sourceProjectFrameworks = sourceProject.TargetFrameworks.ToList();
		var itemGroupFrameworks = itemGroup.TargetFrameworks.ToList();
		if (itemGroupFrameworks.Any())
		{
			var bestFrameworkForItemGroup = TargetFrameworkService.GetCompatibleFramework(itemGroupFrameworks, sourceProjectFrameworks);
			return bestFrameworkForItemGroup;
		}

		var projectFrameworks = project.TargetFrameworks.ToList();
		var bestFrameworkForProject = TargetFrameworkService.GetCompatibleFramework(projectFrameworks, sourceProjectFrameworks);
		return bestFrameworkForProject;
	}

	private static bool SupportsPackage(IEnumerable<ITargetFrameworkMoniker> currentFrameworks, IEnumerable<ITargetFrameworkMoniker> packageFrameworks)
	{
		var packageList = packageFrameworks.ToList();

		foreach (var current in currentFrameworks)
		{
			foreach (var package in packageList)
			{
				if (TargetFrameworkService.Supports(package, current))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static TargetFrameworkMoniker ToFramework(PackageDependencyGroup arg)
	{
		var moniker = arg.TargetFramework.ToString();
		var framework = TargetFrameworkService.GetOrAddFramework(moniker);

		if (framework == null)
		{
			var response = moniker switch
			{
				"DNX,Version=v4.5.1" => framework,
				"DNX,Version=v4.5.2" => framework,
				"DNXCore,Version=v5.0" => framework,
				".NETCoreApp,Version=v0.0" => framework,
				".NETCoreApp,Version=v1.0" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp10),
				".NETCoreApp,Version=v1.1" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp11),
				".NETCoreApp,Version=v2.0" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp20),
				".NETCoreApp,Version=v2.1" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp21),
				".NETCoreApp,Version=v2.2" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp22),
				".NETCoreApp,Version=v3.0" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp30),
				".NETCoreApp,Version=v3.1" => TargetFrameworkService.GetByType(TargetFrameworkType.NetCoreApp31),
				".NETCore,Version=v5.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net5),
				".NETPlatform,Version=v5.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net5),
				".NETPlatform,Version=v6.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net6),
				".NETPlatform,Version=v7.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net7),
				".NETPlatform,Version=v8.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net8),
				".NETFramework,Version=v2.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net20),
				".NETFramework,Version=v3.5" => TargetFrameworkService.GetByType(TargetFrameworkType.Net35),
				".NETFramework,Version=v4.0" => TargetFrameworkService.GetByType(TargetFrameworkType.Net40),
				".NETFramework,Version=v4.5" => TargetFrameworkService.GetByType(TargetFrameworkType.Net45),
				".NETFramework,Version=v4.5.1" => TargetFrameworkService.GetByType(TargetFrameworkType.Net451),
				".NETFramework,Version=v4.5.2" => TargetFrameworkService.GetByType(TargetFrameworkType.Net452),
				".NETFramework,Version=v4.6" => TargetFrameworkService.GetByType(TargetFrameworkType.Net46),
				".NETFramework,Version=v4.6.1" => TargetFrameworkService.GetByType(TargetFrameworkType.Net461),
				".NETFramework,Version=v4.6.2" => TargetFrameworkService.GetByType(TargetFrameworkType.Net462),
				".NETFramework,Version=v4.6.3" => TargetFrameworkService.GetByType(TargetFrameworkType.Net463),
				".NETFramework,Version=v4.7" => TargetFrameworkService.GetByType(TargetFrameworkType.Net47),
				".NETFramework,Version=v4.7.1" => TargetFrameworkService.GetByType(TargetFrameworkType.Net471),
				".NETFramework,Version=v4.7.2" => TargetFrameworkService.GetByType(TargetFrameworkType.Net472),
				".NETFramework,Version=v4.8" => TargetFrameworkService.GetByType(TargetFrameworkType.Net48),
				".NETPlatform,Version=v5.4" => framework,
				".NETStandard,Version=v1.0" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard10),
				".NETStandard,Version=v1.1" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard11),
				".NETStandard,Version=v1.2" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard12),
				".NETStandard,Version=v1.3" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard13),
				".NETStandard,Version=v1.4" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard14),
				".NETStandard,Version=v1.5" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard15),
				".NETStandard,Version=v1.6" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard16),
				".NETStandard,Version=v2.0" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard20),
				".NETStandard,Version=v2.1" => TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard21),
				"Xamarin.Mac,Version=v2.0" => TargetFrameworkService.GetByType(TargetFrameworkType.XamarinMac),
				"Xamarin.TVOS,Version=v1.0" => TargetFrameworkService.GetByType(TargetFrameworkType.XamarinTvOs),
				"Xamarin.WatchOS,Version=v1.0" => TargetFrameworkService.GetByType(TargetFrameworkType.XamarinWatchOs),
				"Any,Version=v0.0" => framework,
				"MonoTouch,Version=v0.0" => framework,
				"MonoTouch,Version=v1.0" => framework,
				".NETPortable,Version=v0.0,Profile=Profile78" => framework,
				".NETPortable,Version=v0.0,Profile=Profile84" => framework,
				".NETPortable,Version=v0.0,Profile=Profile111" => framework,
				".NETPortable,Version=v0.0,Profile=Profile136" => framework,
				".NETPortable,Version=v0.0,Profile=Profile158" => framework,
				".NETPortable,Version=v0.0,Profile=Profile259" => framework,
				".NETPortable,Version=v0.0,Profile=Profile328" => framework,
				".NETPortable,Version=v0.0,Profile=net45+sl5+netcore45+wp8+wp81" => framework,
				".NETPortable,Version=v0.0,Profile=wp8+netcore45+net45+wp81+wpa81" => framework,
				".NETPortable,Version=v0.0,Profile=win8+wpa81" => framework,
				".NETPortable,Version=v0.0,Profile=win8+wp8+wpa81" => framework,
				".NETPortable,Version=v4.0,Profile=Profile328" => framework,
				".NETPortable,Version=v4.5,Profile=Profile78" => framework,
				".NETPortable,Version=v4.5,Profile=Profile111" => framework,
				".NETPortable,Version=v4.5,Profile=Profile259" => framework,
				"Windows,Version=v8.0" => framework,
				"Windows,Version=v8.1" => framework,
				"WindowsPhone,Version=v8.0" => framework,
				"WindowsPhoneApp,Version=v8.1" => framework,
				"Silverlight,Version=v4.0" => framework,
				"Silverlight,Version=v5.0" => framework,
				"Tizen,Version=v4.0" => framework,
				_ => null
			};

			#if DEBUG
			if (response == null)
			{
				Debugger.Break();
				moniker.Dump();
			}
			#endif

			if (response != null)
			{
				return response;
			}
		}

		return framework;
	}

	#endregion
}