#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Services;

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

	/// <summary>
	/// Sidecar in each archive folder: full CLI home path + display name.
	/// </summary>
	public const string UsageArchiveHomeFileName = "home.json";

	/// <summary>
	/// Folder under ApplicationDataLocation that holds one archive directory per Grok home.
	/// </summary>
	public const string UsageArchiveRootName = "usage-archive";

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
	/// Monitor-owned archive for one Grok home (survives CLI log rotation).
	/// Under ApplicationDataLocation / usage-archive / {.grok | .grok-work | name-hash8}.
	/// </summary>
	/// <param name="grokHome"> Absolute Grok home directory. </param>
	public static string GetUsageArchiveDirectory(string grokHome)
	{
		return GetUsageArchiveDirectory(grokHome, new RuntimeInformation());
	}

	/// <summary>
	/// Monitor-owned archive for one Grok home (survives CLI log rotation).
	/// Folder name is the CLI home last segment (.grok, .grok-work). When that folder
	/// already belongs to a different path, a dash plus an 8-character path hash is appended.
	/// </summary>
	/// <param name="grokHome"> Absolute Grok home directory. </param>
	/// <param name="runtimeInformation"> Runtime used to resolve ApplicationDataLocation. </param>
	public static string GetUsageArchiveDirectory(string grokHome, IRuntimeInformation runtimeInformation)
	{
		var root = GetUsageArchiveRoot(runtimeInformation);
		var fullHome = string.IsNullOrWhiteSpace(grokHome) ? "default" : Path.GetFullPath(grokHome);
		var name = Path.GetFileName(fullHome.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (string.IsNullOrEmpty(name))
		{
			name = "default";
		}

		var preferred = Path.GetFullPath(Path.Combine(root, name));
		var stored = TryReadUsageArchiveHomePath(preferred);
		if (!Directory.Exists(preferred)
			|| string.IsNullOrWhiteSpace(stored)
			|| PathsEqual(stored, fullHome))
		{
			return preferred;
		}

		return Path.GetFullPath(Path.Combine(root, name + "-" + HomePathHash8(fullHome)));
	}

	/// <summary>
	/// Root folder that contains per-home archive directories.
	/// </summary>
	public static string GetUsageArchiveRoot(IRuntimeInformation runtimeInformation)
	{
		var local = string.Empty;
		if (runtimeInformation != null)
		{
			local = runtimeInformation.ApplicationDataLocation ?? string.Empty;
		}

		if (string.IsNullOrWhiteSpace(local))
		{
			local = new RuntimeInformation().ApplicationDataLocation;
		}

		if (string.IsNullOrWhiteSpace(local))
		{
			local = Path.GetTempPath();
		}

		return Path.GetFullPath(Path.Combine(local, UsageArchiveRootName));
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

	/// <summary>
	/// Reads the CLI home path from an archive home.json; empty when missing or invalid.
	/// </summary>
	public static string TryReadUsageArchiveHomePath(string archiveDirectory)
	{
		if (string.IsNullOrWhiteSpace(archiveDirectory))
		{
			return string.Empty;
		}

		var path = Path.Combine(archiveDirectory, UsageArchiveHomeFileName);
		if (!File.Exists(path))
		{
			return string.Empty;
		}

		try
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var document = JsonDocument.Parse(stream);
			if (document.RootElement.TryGetProperty("path", out var prop) && (prop.ValueKind == JsonValueKind.String))
			{
				return prop.GetString() ?? string.Empty;
			}
		}
		catch (JsonException)
		{
			return string.Empty;
		}
		catch (IOException)
		{
			return string.Empty;
		}

		return string.Empty;
	}

	/// <summary>
	/// Writes home.json so the archive folder can be matched back to a CLI home.
	/// </summary>
	public static void WriteUsageArchiveHomeFile(string archiveDirectory, string grokHome)
	{
		if (string.IsNullOrWhiteSpace(archiveDirectory) || string.IsNullOrWhiteSpace(grokHome))
		{
			return;
		}

		Directory.CreateDirectory(archiveDirectory);
		var fullHome = Path.GetFullPath(grokHome);
		var displayName = GetDisplayNameFromPath(fullHome);
		var json = JsonSerializer.Serialize(
			new Dictionary<string, string>
			{
				["path"] = fullHome,
				["displayName"] = displayName
			});
		var path = Path.Combine(archiveDirectory, UsageArchiveHomeFileName);
		try
		{
			File.WriteAllText(path, json);
		}
		catch (IOException)
		{
			// next persist retries
		}
		catch (UnauthorizedAccessException)
		{
			// next persist retries
		}
	}

	private static string HomePathHash8(string fullHome)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fullHome.ToUpperInvariant()));
		return System.Convert.ToHexString(hash).ToLowerInvariant()[..8];
	}

	private static bool PathsEqual(string left, string right)
	{
		return string.Equals(
			Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
			Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
			StringComparison.OrdinalIgnoreCase);
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