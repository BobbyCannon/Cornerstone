#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// Resolves Grok Build CLI home paths (logs, sessions).
/// </summary>
public static class GrokPaths
{
	#region Constants

	/// <summary>
	/// Environment variable that overrides the default personal ~/.grok location.
	/// </summary>
	public const string GrokHomeEnvironmentVariable = "GROK_HOME";

	/// <summary>
	/// Optional environment variable for an alternate Grok home.
	/// When unset, the work default path is ~/.grok-work (discovery still only seeds existing dirs).
	/// </summary>
	public const string GrokWorkHomeEnvironmentVariable = "GROK_WORK_HOME";

	/// <summary>
	/// Canonical primary home folder label (from ~/.grok after leading-dot strip).
	/// </summary>
	public const string PrimaryHomeDisplayName = "grok";

	#endregion

	#region Methods

	/// <summary>
	/// Discovers Grok home directories under the user profile (names starting with .grok)
	/// and unions paths from GROK_HOME / GROK_WORK_HOME when set and existing.
	/// Only directories that exist on disk are returned.
	/// </summary>
	/// <param name="userProfileRoot"> Optional profile root for tests; when set, only that tree is scanned (no env union). </param>
	public static IReadOnlyList<(string DisplayName, string Path)> DiscoverHomes(string userProfileRoot = null)
	{
		var byPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var isTestRoot = !string.IsNullOrWhiteSpace(userProfileRoot);

		var profile = isTestRoot
			? Path.GetFullPath(userProfileRoot)
			: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		if (!string.IsNullOrWhiteSpace(profile) && Directory.Exists(profile))
		{
			foreach (var dir in Directory.EnumerateDirectories(profile))
			{
				var name = Path.GetFileName(dir);
				if (string.IsNullOrEmpty(name) || !name.StartsWith(".grok", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				TryAddExistingHome(byPath, dir);
			}
		}

		// Env overrides / extras even when outside the profile (production only).
		if (!isTestRoot)
		{
			TryAddExistingHome(byPath, Environment.GetEnvironmentVariable(GrokHomeEnvironmentVariable));
			TryAddExistingHome(byPath, Environment.GetEnvironmentVariable(GrokWorkHomeEnvironmentVariable));
		}

		return byPath
			.Select(x => (DisplayName: GetDisplayNameFromPath(x.Key), Path: x.Key))
			.OrderBy(x => string.Equals(x.DisplayName, PrimaryHomeDisplayName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
			.ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>
	/// Finds the on-disk session directory for a session id under the given home, or empty when missing.
	/// Layout: sessions/{encodedCwd}/{sessionId}/.
	/// </summary>
	public static string FindSessionDirectory(string grokHome, string sessionId)
	{
		if (string.IsNullOrWhiteSpace(grokHome) || string.IsNullOrWhiteSpace(sessionId))
		{
			return string.Empty;
		}

		var root = GetSessionsRoot(grokHome);
		if (!Directory.Exists(root))
		{
			return string.Empty;
		}

		foreach (var cwdDir in Directory.EnumerateDirectories(root))
		{
			var candidate = Path.Combine(cwdDir, sessionId);
			if (Directory.Exists(candidate))
			{
				return Path.GetFullPath(candidate);
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Default primary home: GROK_HOME when set, otherwise ~/.grok.
	/// </summary>
	public static string GetDefaultPersonalHome()
	{
		return ResolveHome(null);
	}

	/// <summary>
	/// Alternate home path: GROK_WORK_HOME when set, otherwise ~/.grok-work.
	/// </summary>
	public static string GetDefaultWorkHome()
	{
		var fromEnvironment = Environment.GetEnvironmentVariable(GrokWorkHomeEnvironmentVariable);
		if (!string.IsNullOrWhiteSpace(fromEnvironment))
		{
			return Path.GetFullPath(fromEnvironment);
		}

		var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.GetFullPath(Path.Combine(userProfile, ".grok-work"));
	}

	/// <summary>
	/// Tab / UI label from a home path: last segment with leading dots stripped (.grok → grok).
	/// </summary>
	public static string GetDisplayNameFromPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var name = Path.GetFileName(trimmed);
		if (string.IsNullOrEmpty(name))
		{
			return string.Empty;
		}

		return name.TrimStart('.');
	}

	/// <summary>
	/// Returns the sessions root directory under the given Grok home.
	/// </summary>
	/// <param name="grokHome"> Absolute Grok home directory. </param>
	public static string GetSessionsRoot(string grokHome)
	{
		return Path.Combine(grokHome, "sessions");
	}

	/// <summary>
	/// Returns the path to the unified JSONL log under the given Grok home.
	/// </summary>
	/// <param name="grokHome"> Absolute Grok home directory. </param>
	public static string GetUnifiedLogPath(string grokHome)
	{
		return Path.Combine(grokHome, "logs", "unified.jsonl");
	}

	/// <summary>
	/// True when this home is the primary ~/.grok-style home (display name grok).
	/// </summary>
	public static bool IsPrimaryHomeDisplayName(string displayName)
	{
		return string.Equals(displayName, PrimaryHomeDisplayName, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Resolves a single Grok home directory.
	/// Priority: explicit path, then GROK_HOME, then ~/.grok.
	/// </summary>
	/// <param name="grokHome"> Optional explicit home path; null or empty uses env/default. </param>
	public static string ResolveHome(string grokHome = null)
	{
		if (!string.IsNullOrWhiteSpace(grokHome))
		{
			return Path.GetFullPath(grokHome);
		}

		var fromEnvironment = Environment.GetEnvironmentVariable(GrokHomeEnvironmentVariable);
		if (!string.IsNullOrWhiteSpace(fromEnvironment))
		{
			return Path.GetFullPath(fromEnvironment);
		}

		var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.GetFullPath(Path.Combine(userProfile, ".grok"));
	}

	private static void TryAddExistingHome(Dictionary<string, string> byPath, string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		string full;
		try
		{
			full = Path.GetFullPath(path);
		}
		catch
		{
			return;
		}

		if (!Directory.Exists(full))
		{
			return;
		}

		byPath.TryAdd(full, full);
	}

	#endregion
}