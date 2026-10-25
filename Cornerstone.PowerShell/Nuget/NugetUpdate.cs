#region References

using System.Collections.Generic;
using NuGet.Frameworks;
using NuGet.Versioning;

#endregion

namespace Cornerstone.PowerShell.Nuget;

/// <summary>
/// Decides whether a declared package version can move, and what text to write.
/// Restore selects the lowest version a range allows. An update raises that lower bound to the highest allowed version and keeps the upper bound.
/// </summary>
public sealed class NugetUpdate
{
	#region Constants

	public const string StatusEmptyRange = "EmptyRange";
	public const string StatusFailed = "Failed";
	public const string StatusFloating = "Floating";
	public const string StatusNoMatch = "NoMatch";
	public const string StatusNotAVersion = "NotAVersion";
	public const string StatusNotFound = "NotFound";
	public const string StatusPinned = "Pinned";
	public const string StatusUpToDate = "UpToDate";
	public const string StatusUpdateAvailable = "UpdateAvailable";

	#endregion

	#region Constructors

	private NugetUpdate(string status, string latest, string replacement)
	{
		Status = status;
		Latest = latest;
		Replacement = replacement;
	}

	#endregion

	#region Properties

	public string Latest { get; }

	public string Replacement { get; }

	public string Status { get; }

	#endregion

	#region Methods

	public static NugetUpdate QueryFailed()
	{
		return new NugetUpdate(StatusFailed, "Failed", null);
	}

	public static NugetUpdate Select(string versionSpec, NugetPackage catalog, IList<NuGetFramework> projectFrameworks, bool includePrerelease)
	{
		if (string.IsNullOrWhiteSpace(versionSpec) || versionSpec.Contains('$'))
		{
			return new NugetUpdate(StatusNotAVersion, "Not a version", null);
		}

		if (!VersionRange.TryParse(versionSpec.Trim(), out var range) || (range == null))
		{
			if (IsEmptySingleVersion(versionSpec))
			{
				return new NugetUpdate(StatusEmptyRange, "Empty range", null);
			}

			return new NugetUpdate(StatusNotAVersion, "Not a version", null);
		}

		if (IsEmpty(range))
		{
			return new NugetUpdate(StatusEmptyRange, "Empty range", null);
		}

		if (IsPinned(range))
		{
			return new NugetUpdate(StatusPinned, "Pinned", null);
		}

		if ((catalog == null) || (catalog.Versions == null) || (catalog.Versions.Count == 0))
		{
			return new NugetUpdate(StatusNotFound, "Not found", null);
		}

		NuGetVersion lowest = null;
		NuGetVersion highest = null;
		string highestText = null;

		foreach (var candidate in catalog.Versions)
		{
			if ((candidate == null) || string.IsNullOrWhiteSpace(candidate.VersionString))
			{
				continue;
			}

			if (!NuGetVersion.TryParse(candidate.VersionString, out var parsed))
			{
				continue;
			}

			if (!Allows(range, parsed, includePrerelease) || !SupportsProject(candidate, projectFrameworks))
			{
				continue;
			}

			if ((lowest == null) || (parsed.CompareTo(lowest) < 0))
			{
				lowest = parsed;
			}

			if ((highest == null) || (parsed.CompareTo(highest) > 0))
			{
				highest = parsed;
				highestText = candidate.VersionString;
			}
		}

		if (highest == null)
		{
			return new NugetUpdate(StatusNoMatch, "No match", null);
		}

		if (range.IsFloating)
		{
			return new NugetUpdate(StatusFloating, highestText, null);
		}

		if (highest.CompareTo(lowest) > 0)
		{
			var replacement = Rewrite(versionSpec, highestText);
			if (replacement != null)
			{
				return new NugetUpdate(StatusUpdateAvailable, highestText, replacement);
			}
		}

		return new NugetUpdate(StatusUpToDate, highestText, null);
	}

	private static bool Allows(VersionRange range, NuGetVersion version, bool includePrerelease)
	{
		if (version.IsPrerelease && !includePrerelease && !DeclaresPrerelease(range))
		{
			return false;
		}

		// VersionRange.Satisfies ignores the floating pattern and accepts later versions.
		if (range.IsFloating)
		{
			return (range.Float != null) && range.Float.Satisfies(version);
		}

		if (range.Satisfies(version))
		{
			return true;
		}

		return includePrerelease && version.IsPrerelease && WithinBounds(range, version);
	}

	private static bool DeclaresPrerelease(VersionRange range)
	{
		if ((range.MinVersion != null) && range.MinVersion.IsPrerelease)
		{
			return true;
		}

		return (range.MaxVersion != null) && range.MaxVersion.IsPrerelease;
	}

	private static bool IsEmptySingleVersion(string versionSpec)
	{
		var spec = versionSpec.Trim();
		if ((spec.Length < 3) || spec.Contains(','))
		{
			return false;
		}

		var open = spec[0];
		var close = spec[^1];
		if (((open != '[') && (open != '(')) || ((close != ']') && (close != ')')))
		{
			return false;
		}

		if ((open == '[') && (close == ']'))
		{
			return false;
		}

		return NuGetVersion.TryParse(spec[1..^1].Trim(), out _);
	}

	private static bool IsEmpty(VersionRange range)
	{
		if (range.IsFloating || !range.HasLowerBound || !range.HasUpperBound)
		{
			return false;
		}

		var compare = range.MinVersion.CompareTo(range.MaxVersion);
		if (compare > 0)
		{
			return true;
		}

		if (compare < 0)
		{
			return false;
		}

		return !range.IsMinInclusive || !range.IsMaxInclusive;
	}

	private static bool IsPinned(VersionRange range)
	{
		if (range.IsFloating || !range.HasLowerBound || !range.HasUpperBound)
		{
			return false;
		}

		if (!range.IsMinInclusive || !range.IsMaxInclusive)
		{
			return false;
		}

		return range.MinVersion.Equals(range.MaxVersion);
	}

	private static string Rewrite(string versionSpec, string bestVersion)
	{
		var spec = versionSpec.Trim();
		if (spec.Length == 0)
		{
			return null;
		}

		var open = spec[0];
		if ((open != '[') && (open != '('))
		{
			return bestVersion;
		}

		var comma = spec.IndexOf(',');
		if (comma < 0)
		{
			return null;
		}

		return "[" + bestVersion + spec[comma..];
	}

	private static bool SupportsProject(NugetPackageVersion version, IList<NuGetFramework> projectFrameworks)
	{
		if ((projectFrameworks == null) || (projectFrameworks.Count == 0))
		{
			return true;
		}

		var groups = version.Frameworks;
		if ((groups == null) || (groups.Count == 0))
		{
			return true;
		}

		var matched = false;
		foreach (var group in groups)
		{
			var framework = group?.Framework;
			if ((framework == null) || framework.IsAny)
			{
				return true;
			}

			if (framework.IsUnsupported)
			{
				continue;
			}

			foreach (var projectFramework in projectFrameworks)
			{
				if (projectFramework == null)
				{
					continue;
				}

				if (DefaultCompatibilityProvider.Instance.IsCompatible(projectFramework, framework))
				{
					matched = true;
					break;
				}
			}

			if (matched)
			{
				return true;
			}
		}

		return false;
	}

	private static bool WithinBounds(VersionRange range, NuGetVersion version)
	{
		if (range.HasLowerBound)
		{
			var compare = version.CompareTo(range.MinVersion);
			if ((compare < 0) || ((compare == 0) && !range.IsMinInclusive))
			{
				return false;
			}
		}

		if (range.HasUpperBound)
		{
			var compare = version.CompareTo(range.MaxVersion);
			if ((compare > 0) || ((compare == 0) && !range.IsMaxInclusive))
			{
				return false;
			}
		}

		if (range.IsFloating && (range.Float != null) && !range.Float.Satisfies(version))
		{
			return false;
		}

		return true;
	}

	#endregion
}
