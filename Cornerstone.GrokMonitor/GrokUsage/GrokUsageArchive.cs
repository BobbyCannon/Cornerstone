#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Cornerstone.GrokMonitor.GrokUsage.Models;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// Period-folder store for inferences, billing snaps, and session stubs.
/// System of record for the usage dropdown after import from a CLI home.
/// </summary>
public sealed class GrokUsageArchive
{
	#region Constants

	private const string BillingFileName = "billing.jsonl";
	private const string InferencesFileName = "inferences.jsonl";
	private const string PeriodFileName = "period.json";
	private const string PeriodsFolderName = "periods";
	private const string SessionsFileName = "sessions.jsonl";
	private const string UnassignedFolderName = "unassigned";

	#endregion

	#region Fields

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private bool _legacyImportRunning;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an archive rooted at the given directory (created on first write).
	/// </summary>
	/// <param name="directory"> Absolute archive directory. Empty disables persist. </param>
	/// <param name="grokHome"> CLI home path written into home.json on persist. </param>
	public GrokUsageArchive(string directory, string grokHome = null)
	{
		Directory = string.IsNullOrWhiteSpace(directory) ? string.Empty : Path.GetFullPath(directory);
		GrokHome = string.IsNullOrWhiteSpace(grokHome) ? string.Empty : Path.GetFullPath(grokHome);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Archive directory; empty when persist is disabled.
	/// </summary>
	public string Directory { get; }

	/// <summary>
	/// CLI home this archive belongs to; empty when unknown.
	/// </summary>
	public string GrokHome { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Loads stored billing snaps from every period folder (and leftover flat files).
	/// </summary>
	public IReadOnlyList<BillingSnapshot> LoadBilling()
	{
		return LoadBilling(string.Empty);
	}

	/// <summary>
	/// Loads billing snaps. When periodDirectory is set, only that folder is read.
	/// </summary>
	public IReadOnlyList<BillingSnapshot> LoadBilling(string periodDirectory)
	{
		var results = new List<BillingSnapshot>();
		foreach (var folder in ResolveLoadFolders(periodDirectory))
		{
			foreach (var item in ReadJsonl<BillingSnapshot>(Path.Combine(folder, BillingFileName)))
			{
				if ((item != null) && item.HasValue)
				{
					results.Add(item);
				}
			}
		}

		return results;
	}

	/// <summary>
	/// Loads stored inferences from every period folder (and leftover flat files).
	/// </summary>
	public IReadOnlyList<InferenceUsage> LoadInferences()
	{
		return LoadInferences(string.Empty);
	}

	/// <summary>
	/// Loads inferences. When periodDirectory is set, only that folder is read.
	/// </summary>
	public IReadOnlyList<InferenceUsage> LoadInferences(string periodDirectory)
	{
		var results = new List<InferenceUsage>();
		foreach (var folder in ResolveLoadFolders(periodDirectory))
		{
			foreach (var item in ReadJsonl<InferenceUsage>(Path.Combine(folder, InferencesFileName)))
			{
				if (item != null)
				{
					results.Add(item);
				}
			}
		}

		return results;
	}

	/// <summary>
	/// Periods stored as period.json, newest first. now marks IsCurrent.
	/// </summary>
	public IReadOnlyList<UsagePeriodOption> LoadPeriods(DateTimeOffset now)
	{
		var results = new List<UsagePeriodOption>();
		if (string.IsNullOrEmpty(Directory))
		{
			return results;
		}

		var root = Path.Combine(Directory, PeriodsFolderName);
		if (!System.IO.Directory.Exists(root))
		{
			return results;
		}

		foreach (var folder in System.IO.Directory.EnumerateDirectories(root))
		{
			if (string.Equals(Path.GetFileName(folder), UnassignedFolderName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!TryReadPeriodFile(folder, out var start, out var end, out var type))
			{
				continue;
			}

			var isCurrent = (now >= start) && (now < end);
			results.Add(new UsagePeriodOption
			{
				PeriodStart = start,
				PeriodEnd = end,
				PeriodType = type,
				IsCurrent = isCurrent,
				DisplayName = GrokUsageAnalytics.FormatPeriodDisplayName(start, end, isCurrent, type)
			});
		}

		return results
			.OrderByDescending(x => x.PeriodStart)
			.ToList();
	}

	/// <summary>
	/// Loads session stubs from every period folder.
	/// </summary>
	public IReadOnlyList<SessionInfo> LoadSessionInfos()
	{
		return LoadSessionInfos(string.Empty);
	}

	/// <summary>
	/// Loads session stubs. When periodDirectory is set, only that folder is read.
	/// </summary>
	public IReadOnlyList<SessionInfo> LoadSessionInfos(string periodDirectory)
	{
		var results = new List<SessionInfo>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var folder in ResolveLoadFolders(periodDirectory))
		{
			foreach (var item in ReadJsonl<SessionInfo>(Path.Combine(folder, SessionsFileName)))
			{
				if ((item == null) || string.IsNullOrEmpty(item.SessionId))
				{
					continue;
				}

				if (seen.Add(item.SessionId))
				{
					results.Add(item);
				}
			}
		}

		return results;
	}

	/// <summary>
	/// Appends billing snaps into the matching period folder.
	/// </summary>
	public void MergeBilling(IReadOnlyList<BillingSnapshot> snapshots)
	{
		if (string.IsNullOrEmpty(Directory) || (snapshots == null) || (snapshots.Count == 0))
		{
			return;
		}

		EnsureHomeFile();
		ImportLegacyFlatFiles();
		MergeBillingCore(snapshots);
	}

	/// <summary>
	/// Appends inferences into the matching period folder.
	/// </summary>
	public void MergeInferences(IReadOnlyList<InferenceUsage> inferences)
	{
		if (string.IsNullOrEmpty(Directory) || (inferences == null) || (inferences.Count == 0))
		{
			return;
		}

		EnsureHomeFile();
		ImportLegacyFlatFiles();
		MergeInferencesCore(inferences);
	}

	/// <summary>
	/// Writes session stubs into each period that contains at least one of the session's inferences.
	/// Existing stubs for the same session id are updated when CLI metadata changes
	/// (title, message count, model, timestamps).
	/// </summary>
	public void MergeSessions(IReadOnlyList<SessionInfo> sessions, IReadOnlyList<InferenceUsage> inferences)
	{
		if (string.IsNullOrEmpty(Directory) || (sessions == null) || (sessions.Count == 0))
		{
			return;
		}

		EnsureHomeFile();
		var template = ResolveTemplate(LoadBilling(), DateTimeOffset.UtcNow);
		inferences ??= [];
		var bySession = inferences
			.Where(x => !string.IsNullOrEmpty(x.SessionId))
			.GroupBy(x => x.SessionId, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

		foreach (var info in sessions)
		{
			if ((info == null) || string.IsNullOrEmpty(info.SessionId))
			{
				continue;
			}

			var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (bySession.TryGetValue(info.SessionId, out var list))
			{
				foreach (var inference in list)
				{
					folders.Add(EnsurePeriodFolder(AssignTimestampPeriod(inference.Timestamp, template)));
				}
			}
			else
			{
				var ts = info.UpdatedAt ?? info.CreatedAt ?? default;
				if (ts != default)
				{
					folders.Add(EnsurePeriodFolder(AssignTimestampPeriod(ts, template)));
				}
			}

			foreach (var folder in folders)
			{
				UpsertSession(folder, info);
			}
		}
	}

	internal static string BillingKey(BillingSnapshot snap)
	{
		return snap.Timestamp.ToString("o");
	}

	private void MergeBillingCore(IReadOnlyList<BillingSnapshot> snapshots)
	{
		var template = ResolveTemplate(snapshots, DateTimeOffset.UtcNow);
		foreach (var group in snapshots.Where(x => (x != null) && x.HasValue).GroupBy(x => PeriodKey(AssignBillingPeriod(x, template))))
		{
			var bounds = AssignBillingPeriod(group.First(), template);
			var folder = EnsurePeriodFolder(bounds);
			var known = new HashSet<string>(StringComparer.Ordinal);
			foreach (var existing in LoadBilling(folder))
			{
				known.Add(BillingKey(existing));
			}

			var additions = new List<BillingSnapshot>();
			foreach (var snap in group)
			{
				if (known.Add(BillingKey(snap)))
				{
					additions.Add(snap);
				}
			}

			Append(Path.Combine(folder, BillingFileName), additions);
		}
	}

	private void MergeInferencesCore(IReadOnlyList<InferenceUsage> inferences)
	{
		var template = ResolveTemplate(LoadBilling(), DateTimeOffset.UtcNow);
		foreach (var group in inferences.Where(x => x != null).GroupBy(x => PeriodKey(AssignTimestampPeriod(x.Timestamp, template))))
		{
			var bounds = AssignTimestampPeriod(group.First().Timestamp, template);
			var folder = EnsurePeriodFolder(bounds);
			var known = new HashSet<string>(StringComparer.Ordinal);
			foreach (var existing in LoadInferences(folder))
			{
				known.Add(InferenceKey(existing));
			}

			var additions = new List<InferenceUsage>();
			foreach (var item in group)
			{
				if (known.Add(InferenceKey(item)))
				{
					additions.Add(item);
				}
			}

			Append(Path.Combine(folder, InferencesFileName), additions);
		}
	}

	internal static string InferenceKey(InferenceUsage item)
	{
		return string.Join(
			"|",
			item.Timestamp.ToString("o"),
			item.SessionId ?? string.Empty,
			item.LoopIndex.GetValueOrDefault(-1).ToString(),
			item.PromptTokens.ToString(),
			item.CompletionTokens.ToString(),
			item.ReasoningTokens.ToString(),
			item.CachedPromptTokens.ToString());
	}

	private static void Append<T>(string path, IReadOnlyList<T> items)
	{
		if ((items == null) || (items.Count == 0))
		{
			return;
		}

		var folder = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(folder))
		{
			System.IO.Directory.CreateDirectory(folder);
		}

		try
		{
			using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var item in items)
			{
				writer.WriteLine(JsonSerializer.Serialize(item, SerializerOptions));
			}
		}
		catch (IOException)
		{
			// next refresh retries
		}
		catch (UnauthorizedAccessException)
		{
			// next refresh retries
		}
	}

	private static void UpsertSession(string folder, SessionInfo info)
	{
		var path = Path.Combine(folder, SessionsFileName);
		var existing = new List<SessionInfo>();
		foreach (var item in ReadJsonl<SessionInfo>(path))
		{
			if ((item == null) || string.IsNullOrEmpty(item.SessionId))
			{
				continue;
			}

			existing.Add(item);
		}

		var index = existing.FindIndex(x => string.Equals(x.SessionId, info.SessionId, StringComparison.Ordinal));
		if (index < 0)
		{
			Append(path, [info]);
			return;
		}

		if (existing[index] == info)
		{
			return;
		}

		existing[index] = info;
		WriteAll(path, existing);
	}

	private static void WriteAll<T>(string path, IReadOnlyList<T> items)
	{
		if (items == null)
		{
			return;
		}

		var folder = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(folder))
		{
			System.IO.Directory.CreateDirectory(folder);
		}

		try
		{
			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var item in items)
			{
				writer.WriteLine(JsonSerializer.Serialize(item, SerializerOptions));
			}
		}
		catch (IOException)
		{
			// next refresh retries
		}
		catch (UnauthorizedAccessException)
		{
			// next refresh retries
		}
	}

	private PeriodBounds AssignBillingPeriod(BillingSnapshot snap, PeriodBounds template)
	{
		if ((snap.PeriodStart is not null) && (snap.PeriodEnd is not null) && (snap.PeriodEnd.Value > snap.PeriodStart.Value))
		{
			return new PeriodBounds(snap.PeriodStart.Value, snap.PeriodEnd.Value, snap.PeriodType ?? string.Empty);
		}

		return AssignTimestampPeriod(snap.Timestamp, template);
	}

	private PeriodBounds AssignTimestampPeriod(DateTimeOffset timestamp, PeriodBounds template)
	{
		if ((template.Start == default) || (template.End <= template.Start))
		{
			return new PeriodBounds(default, default, string.Empty);
		}

		var duration = template.End - template.Start;
		var start = template.Start;
		if (timestamp >= start)
		{
			while (timestamp >= start + duration)
			{
				start += duration;
			}
		}
		else
		{
			while (timestamp < start)
			{
				start -= duration;
			}
		}

		return new PeriodBounds(start, start + duration, template.Type);
	}

	private void EnsureHomeFile()
	{
		if (!string.IsNullOrEmpty(GrokHome))
		{
			GrokPaths.WriteUsageArchiveHomeFile(Directory, GrokHome);
		}
	}

	private string EnsurePeriodFolder(PeriodBounds bounds)
	{
		if (string.IsNullOrEmpty(Directory))
		{
			return string.Empty;
		}

		string folder;
		if ((bounds.Start == default) || (bounds.End <= bounds.Start))
		{
			folder = Path.Combine(Directory, PeriodsFolderName, UnassignedFolderName);
		}
		else
		{
			folder = Path.Combine(Directory, PeriodsFolderName, FormatPeriodFolderName(bounds.Start, bounds.End));
		}

		System.IO.Directory.CreateDirectory(folder);
		if ((bounds.Start != default) && (bounds.End > bounds.Start))
		{
			WritePeriodFile(folder, bounds);
		}

		return folder;
	}

	private static string FormatPeriodFolderName(DateTimeOffset start, DateTimeOffset end)
	{
		return start.UtcDateTime.ToString("yyyy-MM-ddTHHmmssfffZ", CultureInfo.InvariantCulture)
			+ "_"
			+ end.UtcDateTime.ToString("yyyy-MM-ddTHHmmssfffZ", CultureInfo.InvariantCulture);
	}

	private void ImportLegacyFlatFiles()
	{
		if (string.IsNullOrEmpty(Directory) || _legacyImportRunning)
		{
			return;
		}

		_legacyImportRunning = true;
		try
		{
			ImportLegacyFlatFilesCore();
		}
		finally
		{
			_legacyImportRunning = false;
		}
	}

	private void ImportLegacyFlatFilesCore()
	{
		if (string.IsNullOrEmpty(Directory))
		{
			return;
		}

		var legacyInferences = Path.Combine(Directory, InferencesFileName);
		var legacyBilling = Path.Combine(Directory, BillingFileName);
		if (!File.Exists(legacyInferences) && !File.Exists(legacyBilling))
		{
			return;
		}

		var billing = new List<BillingSnapshot>();
		foreach (var item in ReadJsonl<BillingSnapshot>(legacyBilling))
		{
			if ((item != null) && item.HasValue)
			{
				billing.Add(item);
			}
		}

		var inferences = new List<InferenceUsage>();
		foreach (var item in ReadJsonl<InferenceUsage>(legacyInferences))
		{
			if (item != null)
			{
				inferences.Add(item);
			}
		}

		try
		{
			if (File.Exists(legacyBilling))
			{
				File.Move(legacyBilling, legacyBilling + ".imported", true);
			}

			if (File.Exists(legacyInferences))
			{
				File.Move(legacyInferences, legacyInferences + ".imported", true);
			}
		}
		catch (IOException)
		{
			return;
		}

		if (billing.Count > 0)
		{
			MergeBillingCore(billing);
		}

		if (inferences.Count > 0)
		{
			MergeInferencesCore(inferences);
		}
	}

	private static string PeriodKey(PeriodBounds bounds)
	{
		return bounds.Start.ToString("o") + "|" + bounds.End.ToString("o");
	}

	private static IEnumerable<T> ReadJsonl<T>(string path)
	{
		foreach (var line in ReadLines(path))
		{
			T item;
			try
			{
				item = JsonSerializer.Deserialize<T>(line, SerializerOptions);
			}
			catch (JsonException)
			{
				continue;
			}

			if (item != null)
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<string> ReadLines(string path)
	{
		if (!File.Exists(path))
		{
			yield break;
		}

		FileStream stream;
		try
		{
			stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		}
		catch (IOException)
		{
			yield break;
		}
		catch (UnauthorizedAccessException)
		{
			yield break;
		}

		using (stream)
		using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
		{
			while (true)
			{
				string line;
				try
				{
					line = reader.ReadLine();
				}
				catch (IOException)
				{
					yield break;
				}

				if (line is null)
				{
					yield break;
				}

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				yield return line;
			}
		}
	}

	private IEnumerable<string> ResolveLoadFolders(string periodDirectory)
	{
		if (string.IsNullOrEmpty(Directory))
		{
			yield break;
		}

		if (!string.IsNullOrWhiteSpace(periodDirectory) && System.IO.Directory.Exists(periodDirectory))
		{
			yield return periodDirectory;
			yield break;
		}

		if (File.Exists(Path.Combine(Directory, InferencesFileName)) || File.Exists(Path.Combine(Directory, BillingFileName)))
		{
			yield return Directory;
		}

		var root = Path.Combine(Directory, PeriodsFolderName);
		if (!System.IO.Directory.Exists(root))
		{
			yield break;
		}

		foreach (var folder in System.IO.Directory.EnumerateDirectories(root))
		{
			yield return folder;
		}
	}

	private PeriodBounds ResolveTemplate(IReadOnlyList<BillingSnapshot> snapshots, DateTimeOffset now)
	{
		BillingSnapshot best = null;
		if (snapshots != null)
		{
			foreach (var snap in snapshots)
			{
				if ((snap == null) || !snap.HasValue || (snap.PeriodStart is null) || (snap.PeriodEnd is null))
				{
					continue;
				}

				if ((best == null) || (snap.Timestamp > best.Timestamp))
				{
					best = snap;
				}
			}
		}

		if ((best != null) && (best.PeriodEnd.Value > best.PeriodStart.Value))
		{
			return new PeriodBounds(best.PeriodStart.Value, best.PeriodEnd.Value, best.PeriodType ?? string.Empty);
		}

		foreach (var option in LoadPeriods(now))
		{
			if ((now >= option.PeriodStart) && (now < option.PeriodEnd))
			{
				return new PeriodBounds(option.PeriodStart, option.PeriodEnd, option.PeriodType);
			}
		}

		var synthetic = GrokUsageAnalytics.BuildSyntheticWeeklyPeriods(default, now);
		if (synthetic.Count > 0)
		{
			var current = synthetic.FirstOrDefault(x => x.IsCurrent) ?? synthetic[0];
			return new PeriodBounds(current.PeriodStart, current.PeriodEnd, current.PeriodType);
		}

		return new PeriodBounds(default, default, string.Empty);
	}

	private static bool TryReadPeriodFile(string folder, out DateTimeOffset start, out DateTimeOffset end, out string type)
	{
		start = default;
		end = default;
		type = string.Empty;
		var path = Path.Combine(folder, PeriodFileName);
		if (!File.Exists(path))
		{
			return false;
		}

		try
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var document = JsonDocument.Parse(stream);
			var root = document.RootElement;
			if (!root.TryGetProperty("periodStart", out var startProp)
				|| !root.TryGetProperty("periodEnd", out var endProp))
			{
				return false;
			}

			if ((startProp.ValueKind != JsonValueKind.String)
				|| (endProp.ValueKind != JsonValueKind.String)
				|| !DateTimeOffset.TryParse(startProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out start)
				|| !DateTimeOffset.TryParse(endProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out end))
			{
				return false;
			}

			if (root.TryGetProperty("periodType", out var typeProp) && (typeProp.ValueKind == JsonValueKind.String))
			{
				type = typeProp.GetString() ?? string.Empty;
			}

			return end > start;
		}
		catch (JsonException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	private static void WritePeriodFile(string folder, PeriodBounds bounds)
	{
		var path = Path.Combine(folder, PeriodFileName);
		if (File.Exists(path))
		{
			return;
		}

		try
		{
			var json = JsonSerializer.Serialize(
				new Dictionary<string, string>
				{
					["periodStart"] = bounds.Start.ToString("o"),
					["periodEnd"] = bounds.End.ToString("o"),
					["periodType"] = bounds.Type ?? string.Empty
				},
				SerializerOptions);
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

	#endregion

	#region Structures

	private readonly struct PeriodBounds
	{
		public PeriodBounds(DateTimeOffset start, DateTimeOffset end, string type)
		{
			Start = start;
			End = end;
			Type = type ?? string.Empty;
		}

		public DateTimeOffset End { get; }

		public DateTimeOffset Start { get; }

		public string Type { get; }
	}

	#endregion
}
