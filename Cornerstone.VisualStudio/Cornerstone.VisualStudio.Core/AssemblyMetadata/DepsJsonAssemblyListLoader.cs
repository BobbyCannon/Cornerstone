#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public static class DepsJsonAssemblyListLoader
{
	#region Methods

	public static IEnumerable<string> ParseFile(string path)
	{
		var dir = Path.GetDirectoryName(path);
		var nugetDirs = GetNugetPackagesDirs();
		var deps = JsonDocument.Parse(File.ReadAllText(path));
		if (deps is null || dir is null)
		{
			yield break;
		}

		var target = deps.RootElement.GetProperty("runtimeTarget").GetProperty("name").GetString();
		if (target is null)
		{
			yield break;
		}

		foreach (var l in TransformDeps(deps.RootElement.GetProperty("targets").GetProperty(target)))
		{
			var localPath = Path.Combine(dir, l.DllName);
			if (File.Exists(localPath))
			{
				yield return localPath;
				continue;
			}
			foreach (var nugetPath in nugetDirs)
			{
				foreach (var lower in new[] { false, true })
				{
					var packagePath = Path.Combine(nugetPath,
						lower ? l.PackageName.ToLowerInvariant() : l.PackageName,
						l.LibraryPath
					);

					if (File.Exists(packagePath))
					{
						yield return packagePath;
						break;
					}
				}
			}
		}
	}

	private static IEnumerable<string> GetNugetPackagesDirs()
	{
		var home = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME";

		if (home is not null)
		{
			yield return Path.Combine(home, ".nuget/packages");
		}

		var redirectedPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

		if (redirectedPath is not null)
		{
			yield return redirectedPath;
		}
	}

	private static IEnumerable<Library> TransformDeps(JsonElement lstr)
	{
		foreach (var prop in lstr.EnumerateObject())
		{
			var package = prop.Name;
			if (prop.Value.TryGetProperty("runtime", out var runtime))
			{
				foreach (var dllprop in runtime.EnumerateObject())
				{
					var libraryPath = dllprop.Name;
					var dllName = libraryPath.Split('/').Last();
					yield return new Library(package, libraryPath, dllName);
				}
			}
		}
	}

	#endregion

	#region Records

	private record Library(string PackageName, string LibraryPath, string DllName);

	#endregion
}