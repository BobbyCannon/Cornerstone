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
/// Reads local Grok Build CLI usage data (unified log + session summaries).
/// </summary>
public sealed class GrokUsageReader
{
	#region Constants

	private const string BillingMessage = "billing: fetched credits config";
	private const string EventsFileName = "events.jsonl";
	private const string InferenceMessage = "shell.turn.inference_done";
	private const string SummaryFileName = "summary.json";

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a reader for the given Grok home (or the default resolved home).
	/// </summary>
	/// <param name="grokHome"> Optional Grok home path. When null, uses GROK_HOME or ~/.grok. </param>
	public GrokUsageReader(string grokHome = null)
	{
		GrokHome = GrokPaths.ResolveHome(grokHome);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Resolved Grok home directory used by this reader.
	/// </summary>
	public string GrokHome { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Returns every billing snapshot from the unified log in chronological order.
	/// </summary>
	public IReadOnlyList<BillingSnapshot> GetAllBillingSnapshots()
	{
		var results = new List<BillingSnapshot>();
		foreach (var line in ReadLogLines())
		{
			if (TryParseBilling(line, out var snapshot))
			{
				results.Add(snapshot);
			}
		}

		return results
			.OrderBy(x => x.Timestamp)
			.ToList();
	}

	/// <summary>
	/// Streams all inference_done events from the unified log.
	/// </summary>
	/// <param name="since"> When set, only inferences at or after this time are returned. </param>
	public IReadOnlyList<InferenceUsage> GetAllInferences(DateTimeOffset? since = null)
	{
		var results = new List<InferenceUsage>();
		foreach (var line in ReadLogLines())
		{
			if (!TryParseInference(line, out var inference))
			{
				continue;
			}

			if (since is not null && (inference.Timestamp < since.Value))
			{
				continue;
			}

			results.Add(inference);
		}

		return results;
	}

	/// <summary>
	/// Returns the most recent billing snapshot from the unified log.
	/// When none is found, returns a default instance with <see cref="BillingSnapshot.HasValue" /> false.
	/// </summary>
	public BillingSnapshot GetLatestBillingSnapshot()
	{
		var history = GetAllBillingSnapshots();
		return history.Count == 0 ? new BillingSnapshot() : history[history.Count - 1];
	}

	/// <summary>
	/// Returns usage for a single session by id, or null if neither summary nor log data exists.
	/// </summary>
	/// <param name="sessionId"> Session identifier. </param>
	public SessionUsage GetSession(string sessionId)
	{
		if (string.IsNullOrWhiteSpace(sessionId))
		{
			return null;
		}

		var sessions = GetSessions(sessionId, null);
		return sessions.FirstOrDefault();
	}

	/// <summary>
	/// Discovers sessions and joins them with inference events.
	/// </summary>
	/// <param name="since">
	/// When set, only sessions with at least one inference at or after this time are returned
	/// (orphan log sessions included). Sessions with no inferences are omitted when since is set.
	/// </param>
	public IReadOnlyList<SessionUsage> GetSessions(DateTimeOffset? since = null)
	{
		return GetSessions(null, since);
	}

	/// <summary>
	/// Builds a summary of sessions and billing history from the unified log.
	/// </summary>
	/// <param name="since"> Optional lower bound for inference timestamps / session inclusion. </param>
	public GrokUsageSummary GetSummary(DateTimeOffset? since = null)
	{
		var billingHistory = GetAllBillingSnapshots();
		return new GrokUsageSummary
		{
			Sessions = GetSessions(since),
			BillingHistory = billingHistory,
			LatestBilling = billingHistory.Count == 0
				? new BillingSnapshot()
				: billingHistory[billingHistory.Count - 1]
		};
	}

	private IReadOnlyList<InferenceUsage> AttributeModels(SessionInfo info, IReadOnlyList<InferenceUsage> inferences)
	{
		if (inferences.Count == 0)
		{
			return inferences;
		}

		var timeline = LoadModelTimeline(info.SessionId);
		if (timeline.Count == 0)
		{
			if (string.IsNullOrEmpty(info.CurrentModelId))
			{
				return inferences;
			}

			return inferences
				.Select(x => x with { ModelId = info.CurrentModelId })
				.ToList();
		}

		var result = new List<InferenceUsage>(inferences.Count);
		var index = 0;
		string currentModel = null;

		foreach (var inference in inferences)
		{
			while ((index < timeline.Count) && (timeline[index].Timestamp <= inference.Timestamp))
			{
				currentModel = timeline[index].ModelId;
				index++;
			}

			// If inference is before first turn_started, walk may leave currentModel null.
			var modelId = currentModel ?? info.CurrentModelId;
			result.Add(inference with { ModelId = modelId });
		}

		return result;
	}

	private List<SessionInfo> DiscoverSessionInfos()
	{
		var root = GrokPaths.GetSessionsRoot(GrokHome);
		var results = new List<SessionInfo>();
		if (!Directory.Exists(root))
		{
			return results;
		}

		foreach (var cwdDir in Directory.EnumerateDirectories(root))
		{
			// Skip non-session containers (e.g. files named oddly); only look one level for session folders.
			string cwdFallback = null;
			try
			{
				cwdFallback = Uri.UnescapeDataString(Path.GetFileName(cwdDir));
			}
			catch (UriFormatException)
			{
				cwdFallback = Path.GetFileName(cwdDir);
			}

			foreach (var sessionDir in Directory.EnumerateDirectories(cwdDir))
			{
				var summaryPath = Path.Combine(sessionDir, SummaryFileName);
				if (!File.Exists(summaryPath))
				{
					continue;
				}

				var info = TryReadSessionInfo(summaryPath, cwdFallback);
				if (info is not null)
				{
					results.Add(info);
				}
			}
		}

		return results;
	}

	private string FindSessionDirectory(string sessionId)
	{
		var root = GrokPaths.GetSessionsRoot(GrokHome);
		if (!Directory.Exists(root))
		{
			return null;
		}

		foreach (var cwdDir in Directory.EnumerateDirectories(root))
		{
			var candidate = Path.Combine(cwdDir, sessionId);
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	private static double? GetNestedVal(JsonElement parent, string propertyName)
	{
		if (!parent.TryGetProperty(propertyName, out var node))
		{
			return null;
		}

		if ((node.ValueKind == JsonValueKind.Object) && node.TryGetProperty("val", out var val))
		{
			return TryGetDouble(val);
		}

		return TryGetDouble(node);
	}

	private IReadOnlyList<SessionUsage> GetSessions(string sessionIdFilter, DateTimeOffset? since)
	{
		var allInferences = GetAllInferences(null);
		var bySession = allInferences
			.GroupBy(x => x.SessionId, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Timestamp).ToList(), StringComparer.Ordinal);

		var discovered = DiscoverSessionInfos();
		var results = new List<SessionUsage>();
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (var info in discovered)
		{
			if (sessionIdFilter is not null && !string.Equals(info.SessionId, sessionIdFilter, StringComparison.Ordinal))
			{
				continue;
			}

			seen.Add(info.SessionId);
			bySession.TryGetValue(info.SessionId, out var sessionInferences);
			sessionInferences ??= [];

			IReadOnlyList<InferenceUsage> filtered = sessionInferences;
			if (since is not null)
			{
				filtered = sessionInferences.Where(x => x.Timestamp >= since.Value).ToList();
				if (filtered.Count == 0)
				{
					// Include empty sessions only when no since filter (or explicit GetSession by id).
					if (sessionIdFilter is null)
					{
						continue;
					}
				}
			}

			var attributed = AttributeModels(info, filtered);
			results.Add(new SessionUsage
			{
				Info = info,
				Inferences = attributed
			});
		}

		// Orphan sids: present in log but no summary directory.
		foreach (var pair in bySession)
		{
			if (string.IsNullOrEmpty(pair.Key) || seen.Contains(pair.Key))
			{
				continue;
			}

			if (sessionIdFilter is not null && !string.Equals(pair.Key, sessionIdFilter, StringComparison.Ordinal))
			{
				continue;
			}

			IReadOnlyList<InferenceUsage> filtered = pair.Value;
			if (since is not null)
			{
				filtered = pair.Value.Where(x => x.Timestamp >= since.Value).ToList();
				if (filtered.Count == 0)
				{
					continue;
				}
			}

			var info = new SessionInfo { SessionId = pair.Key };
			results.Add(new SessionUsage
			{
				Info = info,
				Inferences = AttributeModels(info, filtered)
			});
		}

		// When filtering by id and nothing found, return empty list (GetSession maps to null).
		return results
			.OrderByDescending(s => s.LastInference ?? s.Info.UpdatedAt ?? s.Info.CreatedAt ?? DateTimeOffset.MinValue)
			.ToList();
	}

	private static string GetString(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var prop) || (prop.ValueKind != JsonValueKind.String))
		{
			return null;
		}

		return prop.GetString();
	}

	private List<(DateTimeOffset Timestamp, string ModelId)> LoadModelTimeline(string sessionId)
	{
		var timeline = new List<(DateTimeOffset, string)>();
		var sessionDir = FindSessionDirectory(sessionId);
		if (sessionDir is null)
		{
			return timeline;
		}

		var eventsPath = Path.Combine(sessionDir, EventsFileName);
		if (!File.Exists(eventsPath))
		{
			return timeline;
		}

		foreach (var line in ReadLinesShared(eventsPath))
		{
			try
			{
				using var document = JsonDocument.Parse(line);
				var root = document.RootElement;
				if (!root.TryGetProperty("type", out var typeProp) || (typeProp.GetString() != "turn_started"))
				{
					continue;
				}

				var modelId = GetString(root, "model_id");
				if (string.IsNullOrEmpty(modelId))
				{
					continue;
				}

				if (!root.TryGetProperty("ts", out var tsProp) || !TryGetDateTimeOffset(tsProp, out var ts))
				{
					continue;
				}

				timeline.Add((ts, modelId));
			}
			catch (JsonException)
			{
				// skip bad event lines
			}
		}

		timeline.Sort((a, b) => a.Item1.CompareTo(b.Item1));
		return timeline;
	}

	private static IEnumerable<string> ReadLinesShared(string path)
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

	private IEnumerable<string> ReadLogLines()
	{
		var path = GrokPaths.GetUnifiedLogPath(GrokHome);
		return ReadLinesShared(path);
	}

	private static bool TryGetDateTimeOffset(JsonElement element, out DateTimeOffset value)
	{
		value = default;
		if (element.ValueKind != JsonValueKind.String)
		{
			return false;
		}

		var text = element.GetString();
		return !string.IsNullOrWhiteSpace(text)
			&& DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
	}

	private static double? TryGetDouble(JsonElement element)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Number when element.TryGetDouble(out var d) => d,
			JsonValueKind.String when double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
			_ => null
		};
	}

	private static int? TryGetInt32(JsonElement element)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Number when element.TryGetInt32(out var i) => i,
			JsonValueKind.Number when element.TryGetInt64(out var l) && l is >= int.MinValue and <= int.MaxValue => (int) l,
			JsonValueKind.String when int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) => i,
			_ => null
		};
	}

	private static long TryGetInt64(JsonElement element, long defaultValue = 0)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Number when element.TryGetInt64(out var l) => l,
			JsonValueKind.Number when element.TryGetDouble(out var d) => (long) d,
			JsonValueKind.String when long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) => l,
			_ => defaultValue
		};
	}

	private static long TryGetInt64Property(JsonElement parent, string name, long defaultValue = 0)
	{
		return parent.TryGetProperty(name, out var prop) ? TryGetInt64(prop, defaultValue) : defaultValue;
	}

	private static bool TryParseBilling(string line, out BillingSnapshot snapshot)
	{
		snapshot = null!;
		try
		{
			using var document = JsonDocument.Parse(line);
			var root = document.RootElement;
			if (!root.TryGetProperty("msg", out var msgProp) || (msgProp.GetString() != BillingMessage))
			{
				return false;
			}

			if (!root.TryGetProperty("ts", out var tsProp) || !TryGetDateTimeOffset(tsProp, out var timestamp))
			{
				return false;
			}

			string subscriptionTier = null;
			double? creditUsagePercent = null;
			string periodType = null;
			DateTimeOffset? periodStart = null;
			DateTimeOffset? periodEnd = null;
			double? onDemandCap = null;
			double? onDemandUsed = null;
			double? prepaidBalance = null;
			bool? isUnifiedBillingUser = null;

			if (root.TryGetProperty("ctx", out var ctx) && (ctx.ValueKind == JsonValueKind.Object))
			{
				subscriptionTier = GetString(ctx, "subscriptionTier");
				if (ctx.TryGetProperty("config", out var config) && (config.ValueKind == JsonValueKind.Object))
				{
					if (config.TryGetProperty("creditUsagePercent", out var cup))
					{
						creditUsagePercent = TryGetDouble(cup);
					}

					if (config.TryGetProperty("currentPeriod", out var period) && (period.ValueKind == JsonValueKind.Object))
					{
						periodType = GetString(period, "type");
						if (period.TryGetProperty("start", out var startProp) && TryGetDateTimeOffset(startProp, out var start))
						{
							periodStart = start;
						}

						if (period.TryGetProperty("end", out var endProp) && TryGetDateTimeOffset(endProp, out var end))
						{
							periodEnd = end;
						}
					}

					// Fall back to billingPeriod* when currentPeriod is missing.
					if (periodStart is null && config.TryGetProperty("billingPeriodStart", out var bps) && TryGetDateTimeOffset(bps, out var bpsValue))
					{
						periodStart = bpsValue;
					}

					if (periodEnd is null && config.TryGetProperty("billingPeriodEnd", out var bpe) && TryGetDateTimeOffset(bpe, out var bpeValue))
					{
						periodEnd = bpeValue;
					}

					onDemandCap = GetNestedVal(config, "onDemandCap");
					onDemandUsed = GetNestedVal(config, "onDemandUsed");
					prepaidBalance = GetNestedVal(config, "prepaidBalance");
					if (config.TryGetProperty("isUnifiedBillingUser", out var unified) && unified.ValueKind is JsonValueKind.True or JsonValueKind.False)
					{
						isUnifiedBillingUser = unified.GetBoolean();
					}
				}
			}

			snapshot = new BillingSnapshot
			{
				Timestamp = timestamp,
				UsagePercent = creditUsagePercent,
				SubscriptionTier = subscriptionTier,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd,
				PeriodType = periodType,
				OnDemandCap = onDemandCap,
				OnDemandUsed = onDemandUsed,
				PrepaidBalance = prepaidBalance,
				IsUnifiedBillingUser = isUnifiedBillingUser
			};
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private static bool TryParseInference(string line, out InferenceUsage inference)
	{
		inference = null!;
		try
		{
			using var document = JsonDocument.Parse(line);
			var root = document.RootElement;
			if (!root.TryGetProperty("msg", out var msgProp) || (msgProp.GetString() != InferenceMessage))
			{
				return false;
			}

			if (!root.TryGetProperty("ts", out var tsProp) || !TryGetDateTimeOffset(tsProp, out var timestamp))
			{
				return false;
			}

			var sessionId = GetString(root, "sid") ?? "";
			long promptTokens = 0;
			long cachedPromptTokens = 0;
			long completionTokens = 0;
			long reasoningTokens = 0;
			int? loopIndex = null;
			long? modelElapsedMs = null;
			double? tokensPerSecond = null;

			if (root.TryGetProperty("ctx", out var ctx) && (ctx.ValueKind == JsonValueKind.Object))
			{
				promptTokens = TryGetInt64Property(ctx, "prompt_tokens");
				cachedPromptTokens = TryGetInt64Property(ctx, "cached_prompt_tokens");
				completionTokens = TryGetInt64Property(ctx, "completion_tokens");
				reasoningTokens = TryGetInt64Property(ctx, "reasoning_tokens");
				if (ctx.TryGetProperty("loop_index", out var loopProp))
				{
					loopIndex = TryGetInt32(loopProp);
				}

				if (ctx.TryGetProperty("model_elapsed_ms", out var elapsedProp))
				{
					var elapsed = TryGetInt64(elapsedProp, long.MinValue);
					if (elapsed != long.MinValue)
					{
						modelElapsedMs = elapsed;
					}
				}

				if (ctx.TryGetProperty("tokens_per_sec", out var tpsProp))
				{
					tokensPerSecond = TryGetDouble(tpsProp);
				}
			}

			inference = new InferenceUsage
			{
				Timestamp = timestamp,
				SessionId = sessionId,
				PromptTokens = promptTokens,
				CachedPromptTokens = cachedPromptTokens,
				CompletionTokens = completionTokens,
				ReasoningTokens = reasoningTokens,
				LoopIndex = loopIndex,
				ModelElapsedMs = modelElapsedMs,
				TokensPerSecond = tokensPerSecond
			};
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private static SessionInfo TryReadSessionInfo(string summaryPath, string cwdFallback)
	{
		try
		{
			using var stream = new FileStream(summaryPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var document = JsonDocument.Parse(stream);
			var root = document.RootElement;

			var sessionId = "";
			string workingDirectory = null;
			if (root.TryGetProperty("info", out var info) && (info.ValueKind == JsonValueKind.Object))
			{
				sessionId = GetString(info, "id") ?? "";
				workingDirectory = GetString(info, "cwd");
			}

			if (string.IsNullOrEmpty(sessionId))
			{
				sessionId = Path.GetFileName(Path.GetDirectoryName(summaryPath)!) ?? "";
			}

			if (string.IsNullOrEmpty(sessionId))
			{
				return null;
			}

			workingDirectory ??= cwdFallback;

			var title = GetString(root, "generated_title") ?? GetString(root, "session_summary");
			var currentModelId = GetString(root, "current_model_id");

			DateTimeOffset? createdAt = null;
			DateTimeOffset? updatedAt = null;
			if (root.TryGetProperty("created_at", out var createdProp) && TryGetDateTimeOffset(createdProp, out var created))
			{
				createdAt = created;
			}

			if (root.TryGetProperty("updated_at", out var updatedProp) && TryGetDateTimeOffset(updatedProp, out var updated))
			{
				updatedAt = updated;
			}

			var messageCount = 0;
			if (root.TryGetProperty("num_messages", out var msgCountProp))
			{
				messageCount = (int) Math.Clamp(TryGetInt64(msgCountProp), 0, int.MaxValue);
			}

			return new SessionInfo
			{
				SessionId = sessionId,
				WorkingDirectory = workingDirectory,
				Title = title,
				CurrentModelId = currentModelId,
				CreatedAt = createdAt,
				UpdatedAt = updatedAt,
				MessageCount = messageCount
			};
		}
		catch (JsonException)
		{
			return null;
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	#endregion
}