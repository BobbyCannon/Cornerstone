#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cornerstone.GrokMonitor.GrokUsage.Channels;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.Processors;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Processors;

/// <summary>
/// Loads local Grok CLI usage (unified log + session summaries) into <see cref="AppState.GrokUsage" />.
/// </summary>
[SourceReflection]
[DependencyInjected]
[ChannelHandlers]
public partial class GrokUsageProcessor : AppProcessor
{
	#region Fields

	/// <summary>
	/// Watches each home's logs/sessions and throttles RefreshHome when files change.
	/// </summary>
	private GrokUsageDiskMonitor _diskMonitor;

	/// <summary>
	/// Disk refresh requested while <see cref="GrokHomeUsageState.IsBusy" />; flushed after the active load.
	/// </summary>
	private readonly HashSet<Guid> _pendingDiskRefresh = new();

	/// <summary>
	/// Wall elapsed between replay ticks (view clock advances by elapsed × ReplaySpeed).
	/// </summary>
	private readonly Stopwatch _replayWallClock = new();

	/// <summary>
	/// Resolves ApplicationDataLocation for the usage archive.
	/// </summary>
	private readonly IRuntimeInformation _runtimeInformation;

	/// <summary>
	/// Last successful disk summary per home for clock-only reproject (slider / replay).
	/// </summary>
	private readonly Dictionary<Guid, GrokUsageSummary> _summaryByHomeId = new();

	/// <summary>
	/// Real (or test) wall clock; used when live and for LastRefreshedAt / period discovery.
	/// </summary>
	private readonly IDateTimeProvider _wallClock;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public GrokUsageProcessor(
		AppBus bus,
		AppState state,
		IRuntimeInformation runtimeInformation,
		IDateTimeProvider dateTimeProvider = null)
		: base(bus, state)
	{
		_runtimeInformation = runtimeInformation ?? new RuntimeInformation();
		_wallClock = dateTimeProvider ?? DateTimeProvider.RealTime;
	}

	#endregion

	#region Methods

	public override bool CanProcessLifecycle()
	{
		return AnyReplayPlaying() || base.CanProcessLifecycle();
	}

	public override void InitializeLifecycle()
	{
		_diskMonitor = new GrokUsageDiskMonitor(OnDiskHomeChanged);
		base.InitializeLifecycle();
	}

	public override void LoadLifecycle()
	{
		EnsureHomesCore(false);
		base.LoadLifecycle();
	}

	public override void ProcessLifecycle()
	{
		if (AnyReplayPlaying())
		{
			AdvanceReplay();
		}

		base.ProcessLifecycle();
	}

	public override void StartLifecycle()
	{
		SyncDiskMonitor();
		base.StartLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		StopAllReplay();

		if (_diskMonitor != null)
		{
			_diskMonitor.Dispose();
			_diskMonitor = null;
		}

		_pendingDiskRefresh.Clear();
		base.UninitializeLifecycle();
	}

	/// <summary>
	/// True when Grok logs report creditUsagePercent (allowance pool). Business-tier
	/// homes often have billing/tier without this field.
	/// </summary>
	internal static bool HasCreditUsagePercent(
		IReadOnlyList<BillingSnapshot> billingHistory,
		BillingSnapshot billingForView,
		BillingSnapshot latestBilling)
	{
		if ((billingForView != null) && billingForView.HasValue && billingForView.UsagePercent is not null)
		{
			return true;
		}

		if ((latestBilling != null) && latestBilling.HasValue && latestBilling.UsagePercent is not null)
		{
			return true;
		}

		if (billingHistory == null)
		{
			return false;
		}

		foreach (var snap in billingHistory)
		{
			if (snap.HasValue && snap.UsagePercent is not null)
			{
				return true;
			}
		}

		return false;
	}

	private static void ApplyAnalytics(
		GrokHomeUsageState home,
		GrokUsageSummary summary,
		IReadOnlyList<SessionUsage> filteredSessions,
		BillingSnapshot periodBilling,
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd,
		DateTimeOffset now)
	{
		var inferences = FlattenInferences(filteredSessions);

		var analytics = GrokUsageAnalytics.Compute(
			inferences,
			summary.BillingHistory,
			periodBilling ?? new BillingSnapshot(),
			now,
			periodStart,
			periodEnd);

		home.TokenBurnPerHourLast24h = analytics.TokenBurnPerHourLast24h;
		home.TokenBurnPerHourPeriod = analytics.TokenBurnPerHourPeriod;
		home.UsagePercentPerHour = analytics.UsagePercentPerHour;
		home.LinearPacePercent = analytics.LinearPacePercent;
		home.EstimatedUsageExhaustionAt = analytics.EstimatedUsageExhaustionAt;
		home.HasUsageEstimate = analytics.HasUsageEstimate;
		home.UsageRateSource = analytics.UsageRateSource ?? string.Empty;
		home.AnalyticsNote = analytics.AnalyticsNote ?? string.Empty;

		home.DailyTokenTotals.Clear();
		foreach (var day in analytics.DailyTokenTotals)
		{
			home.DailyTokenTotals.Add(day);
		}

		home.DailyUsageTotals.Clear();
		foreach (var day in analytics.DailyUsageTotals)
		{
			home.DailyUsageTotals.Add(day);
		}
	}

	/// <summary>
	/// Projects a loaded summary onto home state using wall time for period discovery
	/// and the view clock for filters, billing as-of, and analytics.
	/// </summary>
	private void ApplySummary(GrokHomeUsageState home, GrokUsageSummary summary, DateTimeOffset diskRefreshedAt)
	{
		var viewNow = ViewUtcNowOffset(home);
		var wallNow = WallUtcNowOffset();
		var allInferences = FlattenInferences(summary.Sessions);
		var earliest = allInferences.Count > 0
			? allInferences.Min(x => x.Timestamp)
			: default;

		TryRememberPlanPeriodTemplate(summary.LatestBilling, summary.BillingHistory);

		DateTimeOffset? planStart = null;
		DateTimeOffset? planEnd = null;
		if (State.GrokUsage.HasPlanPeriodTemplate)
		{
			planStart = State.GrokUsage.PlanPeriodStart;
			planEnd = State.GrokUsage.PlanPeriodEnd;
		}
		else if (TryFindPlanPeriodTemplateFromHomes(home, out var fromStart, out var fromEnd))
		{
			planStart = fromStart;
			planEnd = fromEnd;
			State.GrokUsage.PlanPeriodStart = fromStart;
			State.GrokUsage.PlanPeriodEnd = fromEnd;
		}

		// Period list is real-world (wall); scrubbing must not rewrite available weeks.
		// Archive period folders are the dropdown when import has created any.
		IReadOnlyList<UsagePeriodOption> periodOptions;
		if ((summary.Periods != null) && (summary.Periods.Count > 0))
		{
			periodOptions = summary.Periods
				.Select(x =>
				{
					var isCurrent = (wallNow >= x.PeriodStart) && (wallNow < x.PeriodEnd);
					return x with
					{
						IsCurrent = isCurrent,
						DisplayName = string.Empty
					};
				})
				.OrderByDescending(x => x.PeriodStart)
				.ToList();
		}
		else
		{
			periodOptions = GrokUsageAnalytics.DiscoverBillingPeriods(
				summary.BillingHistory,
				summary.LatestBilling,
				earliest,
				wallNow,
				planStart,
				planEnd);
		}

		periodOptions = GrokUsageAnalytics.FilterPeriodsWithTokenUsage(periodOptions, allInferences);

		var selected = ResolveSelectedPeriod(home, periodOptions, summary.LatestBilling, wallNow);

		home.AvailablePeriods.Clear();
		foreach (var option in periodOptions)
		{
			home.AvailablePeriods.Add(new GrokUsagePeriodState
			{
				PeriodStart = option.PeriodStart,
				PeriodEnd = option.PeriodEnd,
				PeriodType = option.PeriodType ?? string.Empty,
				DisplayName = string.Empty,
				IsCurrent = option.IsCurrent
			});
		}

		home.SelectedPeriodStart = selected.PeriodStart;
		home.SelectedPeriodEnd = selected.PeriodEnd;

		// Inclusive view "now": half-open end is exclusive through viewNow (or full period if past).
		var activityEnd = viewNow < selected.PeriodEnd
			? ExclusiveEndThrough(viewNow)
			: selected.PeriodEnd;
		var filteredSessions = FilterSessionsToPeriod(summary.Sessions, selected.PeriodStart, activityEnd);
		home.GrandTotalPromptTokens = filteredSessions.Sum(s => s.TotalPromptTokens);
		home.GrandTotalCachedPromptTokens = filteredSessions.Sum(s => s.TotalCachedPromptTokens);
		home.GrandTotalCompletionTokens = filteredSessions.Sum(s => s.TotalCompletionTokens);
		home.GrandTotalReasoningTokens = filteredSessions.Sum(s => s.TotalReasoningTokens);
		home.GrandTotalTokens = filteredSessions.Sum(s => s.TotalTokens);

		var billingForView = ResolveBillingForPeriod(
			summary.BillingHistory,
			summary.LatestBilling,
			selected.PeriodStart,
			selected.PeriodEnd,
			viewNow);

		home.HasBilling = billingForView.HasValue || summary.LatestBilling.HasValue;
		home.HasCreditUsage = HasCreditUsagePercent(summary.BillingHistory, billingForView, summary.LatestBilling);
		if (billingForView.HasValue)
		{
			home.SubscriptionTier = billingForView.SubscriptionTier ?? summary.LatestBilling?.SubscriptionTier ?? string.Empty;
			home.UsagePercent = billingForView.UsagePercent ?? 0;
			home.PeriodType = selected.PeriodType ?? billingForView.PeriodType ?? string.Empty;
			home.PeriodStart = selected.PeriodStart;
			home.PeriodEnd = selected.PeriodEnd;
			home.OnDemandCap = billingForView.OnDemandCap ?? 0;
			home.OnDemandUsed = billingForView.OnDemandUsed ?? 0;
			home.PrepaidBalance = billingForView.PrepaidBalance ?? 0;
		}
		else if (summary.LatestBilling is { HasValue: true } latest)
		{
			// Keep tier/on-demand from latest account snapshot even if period had no snaps.
			home.SubscriptionTier = latest.SubscriptionTier ?? string.Empty;
			home.UsagePercent = 0;
			home.PeriodType = selected.PeriodType ?? string.Empty;
			home.PeriodStart = selected.PeriodStart;
			home.PeriodEnd = selected.PeriodEnd;
			home.OnDemandCap = latest.OnDemandCap ?? 0;
			home.OnDemandUsed = latest.OnDemandUsed ?? 0;
			home.PrepaidBalance = latest.PrepaidBalance ?? 0;
			home.HasBilling = true;
		}
		else
		{
			home.SubscriptionTier = string.Empty;
			home.UsagePercent = 0;
			home.PeriodType = selected.PeriodType ?? string.Empty;
			home.PeriodStart = selected.PeriodStart;
			home.PeriodEnd = selected.PeriodEnd;
			home.OnDemandCap = 0;
			home.OnDemandUsed = 0;
			home.PrepaidBalance = 0;
		}

		var rows = new List<GrokSessionUsageState>(filteredSessions.Count);
		foreach (var session in filteredSessions)
		{
			rows.Add(MapSession(home.Path, session));
		}

		foreach (var row in rows)
		{
			row.HasAllocatedUsage = home.HasCreditUsage;
			row.UsagePercent = home.HasCreditUsage
				? GrokUsageAnalytics.AllocateSessionUsagePercent(row.TotalTokens, home.GrandTotalTokens, home.UsagePercent)
				: 0;
		}

		home.Sessions.Clear();
		foreach (var row in rows)
		{
			home.Sessions.Add(row);
		}

		ApplyAnalytics(
			home,
			summary,
			filteredSessions,
			billingForView.HasValue ? billingForView : summary.LatestBilling ?? new BillingSnapshot(),
			selected.PeriodStart,
			selected.PeriodEnd,
			viewNow);

		// Disk read time stays real; scrub/reproject keeps prior stamp when diskRefreshedAt is default.
		if (diskRefreshedAt != default)
		{
			home.LastRefreshedAt = diskRefreshedAt;
		}

		home.ErrorText = string.Empty;
		ApplyViewClockRange(home);
	}

	private void ApplyViewAsOf(GrokHomeUsageState home, DateTimeOffset asOf)
	{
		if (home == null)
		{
			return;
		}

		home.IsViewLive = false;
		home.ViewAsOf = asOf.ToUniversalTime();
		ReprojectHomeForViewClock(home.Id);
	}

	private void ApplyViewLive(GrokHomeUsageState home)
	{
		if (home == null)
		{
			return;
		}

		home.IsViewLive = true;
		home.ViewAsOf = default;
		ReprojectHomeForViewClock(home.Id);
	}

	private bool AnyReplayPlaying()
	{
		return State.GrokUsage.Homes.Any(x => (x != null) && x.IsReplayPlaying);
	}

	private static void ClearAnalytics(GrokHomeUsageState home)
	{
		home.TokenBurnPerHourLast24h = 0;
		home.TokenBurnPerHourPeriod = 0;
		home.UsagePercentPerHour = 0;
		home.LinearPacePercent = 0;
		home.EstimatedUsageExhaustionAt = default;
		home.HasUsageEstimate = false;
		home.HasCreditUsage = false;
		home.UsageRateSource = string.Empty;
		home.AnalyticsNote = string.Empty;
		home.DailyTokenTotals.Clear();
		home.DailyUsageTotals.Clear();
		home.AvailablePeriods.Clear();
		home.SelectedPeriodStart = default;
		home.SelectedPeriodEnd = default;
	}

	/// <summary>
	/// Re-scans the profile for ~/.grok* folders (and env homes) and adds any new ones.
	/// Safe to call on every refresh.
	/// </summary>
	private void EnsureHomesCore(bool syncMonitor = true)
	{
		var usage = State.GrokUsage;
		usage.LastError = string.Empty;

		foreach (var (displayName, path) in GrokPaths.DiscoverHomes())
		{
			var normalized = GrokUsageState.NormalizePath(path);
			if (string.IsNullOrEmpty(normalized))
			{
				continue;
			}

			var existing = usage.FindByPath(normalized);
			if (existing != null)
			{
				if (string.IsNullOrEmpty(existing.DisplayName))
				{
					existing.DisplayName = displayName;
				}

				existing.HomeExists = Directory.Exists(normalized);
				continue;
			}

			usage.Homes.Add(new GrokHomeUsageState
			{
				DisplayName = displayName,
				Path = normalized,
				HomeExists = true
			});
		}

		// Homes that left the disk (or env) stay listed so the user can see they are missing.
		foreach (var home in usage.Homes)
		{
			if ((home == null) || string.IsNullOrEmpty(home.Path))
			{
				continue;
			}

			home.HomeExists = Directory.Exists(home.Path);
		}

		if ((usage.SelectedHomeId == Guid.Empty) && (usage.Homes.Count > 0))
		{
			usage.SelectedHomeId = usage.Homes[0].Id;
		}

		if (syncMonitor)
		{
			SyncDiskMonitor();
		}
	}

	private static DateTimeOffset ExclusiveEndThrough(DateTimeOffset inclusiveInstant)
	{
		return inclusiveInstant < DateTimeOffset.MaxValue.AddTicks(-1)
			? inclusiveInstant.AddTicks(1)
			: inclusiveInstant;
	}

	private static IReadOnlyList<SessionUsage> FilterSessionsToPeriod(
		IReadOnlyList<SessionUsage> sessions,
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd)
	{
		if ((sessions == null) || (sessions.Count == 0))
		{
			return [];
		}

		var result = new List<SessionUsage>();
		foreach (var session in sessions)
		{
			if ((session.Inferences == null) || (session.Inferences.Count == 0))
			{
				continue;
			}

			// Period-scoped subscription usage only: drop local/custom model inferences
			// (credits and SuperGrok allowance apply to grok-* models, not localhost endpoints).
			var inRange = session.Inferences
				.Where(x => GrokUsageAnalytics.IsInHalfOpenRange(x.Timestamp, periodStart, periodEnd)
					&& GrokUsageAnalytics.IsSubscriptionGrokModel(x.ModelId))
				.ToList();
			if (inRange.Count == 0)
			{
				continue;
			}

			result.Add(session with { Inferences = inRange });
		}

		return result;
	}

	private static List<InferenceUsage> FlattenInferences(IReadOnlyList<SessionUsage> sessions)
	{
		var inferences = new List<InferenceUsage>();
		if (sessions == null)
		{
			return inferences;
		}

		foreach (var session in sessions)
		{
			if (session.Inferences == null)
			{
				continue;
			}

			inferences.AddRange(session.Inferences);
		}

		return inferences;
	}

	private static bool IsRealPlanPeriod(DateTimeOffset periodStart, DateTimeOffset periodEnd, string periodType)
	{
		if ((periodStart == default) || (periodEnd == default) || (periodEnd <= periodStart))
		{
			return false;
		}

		return !string.Equals(periodType, GrokUsageAnalytics.SyntheticWeeklyPeriodType, StringComparison.Ordinal);
	}

	private bool IsViewClockAtLiveEnd(GrokHomeUsageState home)
	{
		if ((home == null) || !TryGetViewClockRange(home, out var start, out var max))
		{
			return true;
		}

		if (home.IsViewLive || (home.ViewAsOf == default))
		{
			return true;
		}

		var asOf = home.ViewAsOf;
		var span = max.UtcTicks - start.UtcTicks;
		if (span <= 0)
		{
			return true;
		}

		var progress = (double) (asOf.UtcTicks - start.UtcTicks) / span;
		return progress >= 0.999;
	}

	private static GrokSessionUsageState MapSession(string grokHome, SessionUsage session)
	{
		var sessionId = session.Info.SessionId ?? string.Empty;
		var sessionDirectory = GrokPaths.FindSessionDirectory(grokHome, sessionId);
		var summaryPath = string.Empty;
		var eventsPath = string.Empty;
		if (!string.IsNullOrEmpty(sessionDirectory))
		{
			var summaryCandidate = Path.Combine(sessionDirectory, "summary.json");
			if (File.Exists(summaryCandidate))
			{
				summaryPath = summaryCandidate;
			}

			var eventsCandidate = Path.Combine(sessionDirectory, "events.jsonl");
			if (File.Exists(eventsCandidate))
			{
				eventsPath = eventsCandidate;
			}
		}

		// Prefer last subscription-model inference; summary current_model may be a local endpoint.
		var displayModelId = session.Inferences
			.Where(x => GrokUsageAnalytics.IsSubscriptionGrokModel(x.ModelId))
			.Select(x => x.ModelId)
			.LastOrDefault();
		if (string.IsNullOrEmpty(displayModelId)
			&& GrokUsageAnalytics.IsSubscriptionGrokModel(session.Info.CurrentModelId))
		{
			displayModelId = session.Info.CurrentModelId;
		}

		return new GrokSessionUsageState
		{
			SessionId = sessionId,
			Title = session.Info.Title ?? string.Empty,
			WorkingDirectory = session.Info.WorkingDirectory ?? string.Empty,
			CurrentModelId = displayModelId ?? string.Empty,
			MessageCount = session.Info.MessageCount,
			InferenceCount = session.Inferences.Count,
			PromptTokens = session.TotalPromptTokens,
			CachedPromptTokens = session.TotalCachedPromptTokens,
			CompletionTokens = session.TotalCompletionTokens,
			ReasoningTokens = session.TotalReasoningTokens,
			TotalTokens = session.TotalTokens,
			FirstInferenceAt = session.FirstInference,
			LastInferenceAt = session.LastInference ?? default,
			SessionDirectory = sessionDirectory,
			SummaryPath = summaryPath,
			EventsPath = eventsPath
		};
	}

	/// <summary>
	/// File-system callback (thread pool). Publishes the same RefreshHome intent as the toolbar.
	/// </summary>
	private void OnDiskHomeChanged(Guid homeId)
	{
		if (homeId == Guid.Empty)
		{
			return;
		}

		Bus.GrokUsage.RefreshHome(homeId);
	}

	private void OnEnsureHomes(GrokUsageChannel.EnsureHomesMessage _)
	{
		EnsureHomesCore();
	}

	private void OnRefreshAll(GrokUsageChannel.RefreshAllMessage _)
	{
		// Pick up newly created ~/.grok* folders before loading usage.
		EnsureHomesCore();

		// Snapshot ids so collection mutations during refresh cannot skip homes.
		var ids = State.GrokUsage.Homes.Select(x => x.Id).ToList();
		foreach (var id in ids)
		{
			RefreshHomeCore(id);
		}
	}

	private void OnRefreshHome(GrokUsageChannel.RefreshHomeMessage message)
	{
		// Toolbar "Refresh" is per-tab; still re-discover so new homes appear without a second action.
		var knownBefore = new HashSet<Guid>(State.GrokUsage.Homes.Select(x => x.Id));
		EnsureHomesCore();

		RefreshHomeCore(message.HomeId);

		foreach (var home in State.GrokUsage.Homes)
		{
			if ((home == null) || knownBefore.Contains(home.Id))
			{
				continue;
			}

			RefreshHomeCore(home.Id);
		}
	}

	private void AdvanceReplay()
	{
		if (!AnyReplayPlaying())
		{
			return;
		}

		var wallSeconds = _replayWallClock.Elapsed.TotalSeconds;
		_replayWallClock.Restart();
		if (wallSeconds <= 0)
		{
			return;
		}

		var playing = State.GrokUsage.Homes.Where(x => (x != null) && x.IsReplayPlaying).ToList();
		foreach (var home in playing)
		{
			AdvanceReplay(home, wallSeconds);
		}

		if (!AnyReplayPlaying())
		{
			_replayWallClock.Reset();
		}
	}

	private void AdvanceReplay(GrokHomeUsageState home, double wallSeconds)
	{
		if ((home == null) || !TryGetViewClockRange(home, out var start, out var max))
		{
			StopReplay(home);
			return;
		}

		var next = ResolveCurrentViewAsOf(home, start, max)
			+ TimeSpan.FromSeconds(wallSeconds * GrokUsageState.ReplaySpeed);
		if (next >= max)
		{
			StopReplay(home);
			ApplyViewLive(home);
			return;
		}

		if (next < start)
		{
			next = start;
		}

		ApplyViewAsOf(home, next);
	}

	private void OnSelectHome(GrokUsageChannel.SelectHomeMessage message)
	{
		if (message.HomeId == Guid.Empty)
		{
			return;
		}

		if (State.GrokUsage.FindById(message.HomeId) == null)
		{
			return;
		}

		State.GrokUsage.SelectedHomeId = message.HomeId;
	}

	private void OnSelectPeriod(GrokUsageChannel.SelectPeriodMessage message)
	{
		var home = State.GrokUsage.FindById(message.HomeId);
		if (home == null)
		{
			return;
		}

		if (message.PeriodEnd <= message.PeriodStart)
		{
			return;
		}

		home.SelectedPeriodStart = message.PeriodStart;
		home.SelectedPeriodEnd = message.PeriodEnd;

		StopReplay(home);

		// New period → pin this home to live end of that view, then reproject (cache if present).
		home.IsViewLive = true;
		home.ViewAsOf = default;
		RefreshHomeCore(message.HomeId, false);
	}

	private void OnSetSince(GrokUsageChannel.SetSinceMessage message)
	{
		State.GrokUsage.SinceUtc = message.SinceUtc;
	}

	private void OnSetViewAsOf(GrokUsageChannel.SetViewAsOfMessage message)
	{
		var home = State.GrokUsage.FindById(message.HomeId);
		var asOf = message.ViewAsOf;
		if ((home == null) || (asOf == default))
		{
			return;
		}

		StopReplay(home);
		home.IsViewLive = false;
		home.ViewAsOf = asOf.ToUniversalTime();
		ReprojectHomeForViewClock(home.Id);
	}

	private void OnSetViewLive(GrokUsageChannel.SetViewLiveMessage message)
	{
		var home = State.GrokUsage.FindById(message.HomeId);
		if (home == null)
		{
			return;
		}

		StopReplay(home);
		ApplyViewLive(home);
	}

	private void OnStartReplay(GrokUsageChannel.StartReplayMessage message)
	{
		var home = State.GrokUsage.FindById(message.HomeId);
		if ((home == null) || home.IsReplayPlaying)
		{
			return;
		}

		if (!TryGetViewClockRange(home, out var start, out _))
		{
			return;
		}

		if (home.IsViewLive || IsViewClockAtLiveEnd(home))
		{
			ApplyViewAsOf(home, start);
		}

		var alreadyPlaying = AnyReplayPlaying();
		home.IsReplayPlaying = true;
		if (!alreadyPlaying)
		{
			_replayWallClock.Restart();
		}
	}

	private void OnStopReplay(GrokUsageChannel.StopReplayMessage message)
	{
		StopReplay(State.GrokUsage.FindById(message.HomeId));
	}

	private void RefreshHomeCore(Guid homeId, bool forceDisk = true)
	{
		var home = State.GrokUsage.FindById(homeId);
		if (home == null)
		{
			State.GrokUsage.LastError = "Unknown Grok home.";
			return;
		}

		if (home.IsBusy)
		{
			// Do not drop disk-driven updates that arrive mid-load; flush after IsBusy clears.
			if (forceDisk)
			{
				_pendingDiskRefresh.Add(homeId);
			}
			else if (_summaryByHomeId.TryGetValue(homeId, out var busyCached) && (busyCached != null))
			{
				// Clock scrub must still reproject a home that is mid disk load.
				ApplySummary(home, busyCached, default);
			}

			return;
		}

		// Clock-only reproject: reuse cached summary (slider / live pin).
		if (!forceDisk && _summaryByHomeId.TryGetValue(homeId, out var cached) && (cached != null))
		{
			ApplySummary(home, cached, default);
			return;
		}

		home.IsBusy = true;
		home.ProgressText = "Reading usage…";
		home.ErrorText = string.Empty;

		try
		{
			var path = home.Path ?? string.Empty;
			home.HomeExists = !string.IsNullOrEmpty(path) && Directory.Exists(path);

			if (!home.HomeExists)
			{
				home.Sessions.Clear();
				home.GrandTotalPromptTokens = 0;
				home.GrandTotalCachedPromptTokens = 0;
				home.GrandTotalCompletionTokens = 0;
				home.GrandTotalReasoningTokens = 0;
				home.GrandTotalTokens = 0;
				home.HasBilling = false;
				ClearAnalytics(home);
				_summaryByHomeId.Remove(homeId);
				home.ErrorText = "Home folder not found.";
				home.LastRefreshedAt = WallUtcNowOffset();
				return;
			}

			// Load full log so period discovery can list past weeks; totals are filtered after.
			DateTimeOffset? since = State.GrokUsage.SinceUtc == default
				? null
				: State.GrokUsage.SinceUtc;

			var reader = new GrokUsageReader(path, null, _runtimeInformation);
			reader.ImportFromGrokHome();
			var summary = reader.GetSummary(since);
			_summaryByHomeId[homeId] = summary;
			ApplySummary(home, summary, WallUtcNowOffset());
		}
		catch (Exception ex)
		{
			home.ErrorText = ex.Message;
		}
		finally
		{
			home.IsBusy = false;
			home.ProgressText = string.Empty;

			// Watchers may attach only after logs/sessions appear; re-sync after each disk load.
			if (forceDisk)
			{
				SyncDiskMonitor();
			}

			if (_pendingDiskRefresh.Remove(homeId))
			{
				RefreshHomeCore(homeId, true);
			}
		}
	}

	private void ReprojectHomeForViewClock(Guid homeId)
	{
		if (!_summaryByHomeId.ContainsKey(homeId))
		{
			return;
		}

		RefreshHomeCore(homeId, false);
	}

	/// <summary>
	/// Billing snap for the period as of <paramref name="asOf" />: last history snap with
	/// Timestamp in [periodStart, min(periodEnd, asOf+epsilon)). For an open period at asOf
	/// with no earlier snap, falls back to account latest (live behavior).
	/// </summary>
	private static BillingSnapshot ResolveBillingForPeriod(
		IReadOnlyList<BillingSnapshot> history,
		BillingSnapshot latest,
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd,
		DateTimeOffset asOf)
	{
		history ??= [];
		latest ??= new BillingSnapshot();

		var windowEnd = asOf < periodEnd
			? ExclusiveEndThrough(asOf)
			: periodEnd;
		var periodContainsAsOf = (asOf >= periodStart) && (asOf < periodEnd);

		// Live open period with no scrub: keep previous behavior (prefer account latest).
		if (periodContainsAsOf && latest.HasValue && ((latest.Timestamp <= asOf) || (latest.Timestamp == default)))
		{
			// When scrubbing mid-period, only use latest if it is not after asOf (always true here)
			// and there is no older in-window history snap that better represents asOf.
			BillingSnapshot bestInWindow = null;
			foreach (var snap in history)
			{
				if (!snap.HasValue)
				{
					continue;
				}

				if (!GrokUsageAnalytics.IsInHalfOpenRange(snap.Timestamp, periodStart, windowEnd))
				{
					continue;
				}

				if ((bestInWindow == null) || (snap.Timestamp > bestInWindow.Timestamp))
				{
					bestInWindow = snap;
				}
			}

			if (bestInWindow != null)
			{
				// Prefer history as-of when it is strictly before account latest (scrub mid-period).
				if (latest.Timestamp > asOf)
				{
					return bestInWindow;
				}

				if (bestInWindow.Timestamp >= latest.Timestamp)
				{
					return bestInWindow;
				}

				// latest is newer but still <= asOf — live end.
				return latest;
			}

			return latest;
		}

		BillingSnapshot best = null;
		foreach (var snap in history)
		{
			if (!snap.HasValue)
			{
				continue;
			}

			if (!GrokUsageAnalytics.IsInHalfOpenRange(snap.Timestamp, periodStart, windowEnd))
			{
				continue;
			}

			if ((best == null) || (snap.Timestamp > best.Timestamp))
			{
				best = snap;
			}
		}

		if (best != null)
		{
			return best;
		}

		// Prefer a snap that declared this period even if timestamp is outside (clock skew).
		foreach (var snap in history.OrderByDescending(x => x.Timestamp))
		{
			if (!snap.HasValue || snap.PeriodStart is null || snap.PeriodEnd is null)
			{
				continue;
			}

			if ((snap.PeriodStart.Value != periodStart) || (snap.PeriodEnd.Value != periodEnd))
			{
				continue;
			}

			if (snap.Timestamp > asOf)
			{
				continue;
			}

			return snap;
		}

		// Historical closed period: last known latest for this period bounds when history filter missed.
		if (latest.HasValue
			&& latest.PeriodStart is not null
			&& latest.PeriodEnd is not null
			&& (latest.PeriodStart.Value == periodStart)
			&& (latest.PeriodEnd.Value == periodEnd)
			&& ((latest.Timestamp <= asOf) || !periodContainsAsOf))
		{
			return latest;
		}

		return new BillingSnapshot();
	}

	private static DateTimeOffset ResolveCurrentViewAsOf(
		GrokHomeUsageState home,
		DateTimeOffset start,
		DateTimeOffset max)
	{
		if ((home == null) || home.IsViewLive || (home.ViewAsOf == default))
		{
			return max;
		}

		var asOf = home.ViewAsOf;
		if (asOf < start)
		{
			return start;
		}

		if (asOf > max)
		{
			return max;
		}

		return asOf;
	}

	private static UsagePeriodOption ResolveSelectedPeriod(
		GrokHomeUsageState home,
		IReadOnlyList<UsagePeriodOption> options,
		BillingSnapshot latestBilling,
		DateTimeOffset now)
	{
		if ((options == null) || (options.Count == 0))
		{
			return new UsagePeriodOption
			{
				PeriodStart = now.AddDays(-7),
				PeriodEnd = ExclusiveEndThrough(now),
				IsCurrent = true,
				DisplayName = "Last 7 days · current"
			};
		}

		if ((home.SelectedPeriodStart != default) && (home.SelectedPeriodEnd != default))
		{
			foreach (var option in options)
			{
				if ((option.PeriodStart == home.SelectedPeriodStart)
					&& (option.PeriodEnd == home.SelectedPeriodEnd))
				{
					return option;
				}
			}
		}

		foreach (var option in options)
		{
			if (option.IsCurrent)
			{
				return option;
			}
		}

		if (latestBilling is { HasValue: true, PeriodStart: not null, PeriodEnd: not null })
		{
			foreach (var option in options)
			{
				if ((option.PeriodStart == latestBilling.PeriodStart.Value)
					&& (option.PeriodEnd == latestBilling.PeriodEnd.Value))
				{
					return option;
				}
			}
		}

		return options[0];
	}

	private void StopAllReplay()
	{
		foreach (var home in State.GrokUsage.Homes)
		{
			if (home != null)
			{
				home.IsReplayPlaying = false;
			}
		}

		_replayWallClock.Reset();
	}

	private void StopReplay(GrokHomeUsageState home)
	{
		if (home != null)
		{
			home.IsReplayPlaying = false;
		}

		if (!AnyReplayPlaying())
		{
			_replayWallClock.Reset();
		}
	}

	private void SyncDiskMonitor()
	{
		if (_diskMonitor == null)
		{
			return;
		}

		var homes = State.GrokUsage.Homes
			.Where(x => (x != null) && (x.Id != Guid.Empty) && !string.IsNullOrWhiteSpace(x.Path))
			.Select(x => (x.Id, x.Path));
		_diskMonitor.SyncHomes(homes);
	}

	private bool TryFindPlanPeriodTemplateFromHomes(
		GrokHomeUsageState excluding,
		out DateTimeOffset periodStart,
		out DateTimeOffset periodEnd)
	{
		periodStart = default;
		periodEnd = default;

		// Prefer primary "grok" home when present, then any other home with a real (non-synthetic) period.
		GrokHomeUsageState preferred = null;
		foreach (var other in State.GrokUsage.Homes)
		{
			if ((other == null) || ((excluding != null) && (other.Id == excluding.Id)))
			{
				continue;
			}

			if (!IsRealPlanPeriod(other.PeriodStart, other.PeriodEnd, other.PeriodType))
			{
				continue;
			}

			if (GrokPaths.IsPrimaryHomeDisplayName(other.DisplayName))
			{
				periodStart = other.PeriodStart;
				periodEnd = other.PeriodEnd;
				return true;
			}

			preferred ??= other;
		}

		if (preferred != null)
		{
			periodStart = preferred.PeriodStart;
			periodEnd = preferred.PeriodEnd;
			return true;
		}

		return false;
	}

	private static bool TryGetRealPeriodBounds(
		BillingSnapshot snap,
		out DateTimeOffset periodStart,
		out DateTimeOffset periodEnd)
	{
		periodStart = default;
		periodEnd = default;
		if ((snap == null)
			|| !snap.HasValue
			|| snap.PeriodStart is null
			|| snap.PeriodEnd is null
			|| (snap.PeriodEnd.Value <= snap.PeriodStart.Value))
		{
			return false;
		}

		if (string.Equals(snap.PeriodType, GrokUsageAnalytics.SyntheticWeeklyPeriodType, StringComparison.Ordinal))
		{
			return false;
		}

		periodStart = snap.PeriodStart.Value;
		periodEnd = snap.PeriodEnd.Value;
		return true;
	}

	private void ApplyViewClockRange(GrokHomeUsageState home)
	{
		if ((home == null) || !TryGetViewClockRange(home, out var start, out var max))
		{
			if (home != null)
			{
				home.ViewClockStart = default;
				home.ViewClockMax = default;
			}

			return;
		}

		home.ViewClockStart = start;
		home.ViewClockMax = max;
	}

	private bool TryGetViewClockRange(
		GrokHomeUsageState home,
		out DateTimeOffset start,
		out DateTimeOffset max)
	{
		start = default;
		max = default;

		if ((home == null)
			|| (home.PeriodStart == default)
			|| (home.PeriodEnd == default)
			|| (home.PeriodEnd <= home.PeriodStart))
		{
			return false;
		}

		start = home.PeriodStart;
		var wallNow = WallUtcNowOffset();
		max = home.PeriodEnd <= wallNow
			? home.PeriodEnd
			: wallNow < home.PeriodStart
				? home.PeriodStart
				: wallNow;
		if (max < start)
		{
			max = start;
		}

		return max > start;
	}

	/// <summary>
	/// When this home's billing has a real SuperGrok-style period, remember it app-wide so
	/// Business/Work synthetic weeks share the same reset phase (e.g. ~11:30pm weekly).
	/// </summary>
	private void TryRememberPlanPeriodTemplate(
		BillingSnapshot latestBilling,
		IReadOnlyList<BillingSnapshot> billingHistory)
	{
		if (TryGetRealPeriodBounds(latestBilling, out var start, out var end))
		{
			State.GrokUsage.PlanPeriodStart = start;
			State.GrokUsage.PlanPeriodEnd = end;
			return;
		}

		if (billingHistory == null)
		{
			return;
		}

		foreach (var snap in billingHistory.OrderByDescending(x => x.Timestamp))
		{
			if (TryGetRealPeriodBounds(snap, out start, out end))
			{
				State.GrokUsage.PlanPeriodStart = start;
				State.GrokUsage.PlanPeriodEnd = end;
				return;
			}
		}
	}

	private DateTimeOffset ViewUtcNowOffset(GrokHomeUsageState home)
	{
		if ((home == null) || home.IsViewLive || (home.ViewAsOf == default))
		{
			return WallUtcNowOffset();
		}

		return home.ViewAsOf.ToUniversalTime();
	}

	private DateTimeOffset WallUtcNowOffset()
	{
		return new DateTimeOffset(_wallClock.UtcNow, TimeSpan.Zero);
	}

	#endregion
}