#region References

using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

#endregion

namespace Cornerstone.Parsers.VisualStudio;

public static class TargetFrameworkService
{
	#region Fields

	private static readonly FrameworkReducer _reducer;

	#endregion

	#region Constructors

	static TargetFrameworkService()
	{
		_reducer = new();
	}

	#endregion

	#region Methods

	public static NuGetFramework GetBestCompatiblePackageFramework(
		IReadOnlyList<NuGetFramework> projectFrameworks,
		IReadOnlyList<NuGetFramework> packageSupportedFrameworks)
	{
		if ((projectFrameworks == null) || (projectFrameworks.Count == 0))
		{
			return null;
		}

		if ((packageSupportedFrameworks == null) || (packageSupportedFrameworks.Count == 0))
		{
			return null;
		}

		var provider = DefaultCompatibilityProvider.Instance;

		// Determine the target framework from the project
		var targetFramework = projectFrameworks.FirstOrDefault() ?? NuGetFramework.AnyFramework;

		// Use FrameworkReducer to get the "nearest" / most specific matches first
		var reduced = _reducer.GetNearest(targetFramework, packageSupportedFrameworks);

		if ((reduced != null) && provider.IsCompatible(targetFramework, reduced))
		{
			return reduced;
		}

		// Fallback: sort by version to ensure we pick the newest compatible framework
		var sortedByNewest = packageSupportedFrameworks
			.OrderByDescending(fw => fw.Version)
			.ThenByDescending(fw => fw.DotNetFrameworkName.Length)
			.ToList();

		foreach (var pkgFw in sortedByNewest)
		{
			if (provider.IsCompatible(targetFramework, pkgFw))
			{
				return pkgFw;
			}
		}

		return null;
	}

	public static bool Supports(NuGetFramework current, NuGetFramework suggestion)
	{
		if ((current == null) || (suggestion == null))
		{
			return false;
		}

		var provider = DefaultCompatibilityProvider.Instance;
		return provider.IsCompatible(current, suggestion);
	}

	#endregion
}