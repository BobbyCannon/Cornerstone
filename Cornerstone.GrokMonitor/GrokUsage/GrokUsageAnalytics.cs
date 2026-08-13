#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.GrokMonitor.GrokUsage.Models;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// Pure helpers for daily token buckets, credit pace, and linear exhaustion ETA.
/// </summary>
public static class GrokUsageAnalytics
{
	#region Constants

	/// <summary>
	/// Period type when account billing has no currentPeriod; weeks are local Monday 00:00 half-open.
	/// </summary>
	public const string SyntheticWeeklyPeriodType = "SYNTHETIC_WEEKLY";

	/// <summary>
	/// Sessions at or above this total use full heat (red clamp).
	/// </summary>
	public const long TokenHeatHotThreshold = 10_000_000;

	/// <summary>
	/// Sessions under this total stay un-tinted in the Sessions grid.
	/// </summary>
	public const long TokenHeatSoftThreshold = 1_000_000;

	private const double EpsilonHours = 1e-6;
	private const double EpsilonRate = 1e-9;
	private const byte HeatAlphaHot = 107; // ~42%
	private const byte HeatAlphaSoft = 31; // ~12%
	private const byte HeatRedB = 45;
	private const byte HeatRedG = 45;
	private const byte HeatRedR = 220;
	private const byte HeatYellowB = 40;
	private const byte HeatYellowG = 210;
	private const byte HeatYellowR = 255;
	private const int MaxSyntheticPeriods = 26;
	private const double MinElapsedHoursForEstimate = 1.0;

	#endregion

	#region Methods

	/// <summary>
	/// Caption for the daily tokens chart (day span, peak, latest).
	/// </summary>
	public static string BuildDailyChartCaption(IReadOnlyList<DailyTokenTotal> days, int maxDays = 14)
	{
		if ((days == null) || (days.Count == 0))
		{
			return "No daily token data";
		}

		if (!TryGetChartWindow(
				days,
				maxDays,
				static d => d.TotalTokens,
				out var start,
				out var endExclusive,
				out var series))
		{
			return "No daily token data";
		}

		var peak = series.Max();
		var latest = series[series.Length - 1];
		var firstDay = days[start].Day;
		var lastDay = days[endExclusive - 1].Day;
		if (firstDay.Date == lastDay.Date)
		{
			return $"{firstDay:MMM d} · peak {FormatCompactTokens(peak)} · latest {FormatCompactTokens(latest)}";
		}

		return $"{firstDay:MMM d}–{lastDay:MMM d} · {series.Length}d · peak {FormatCompactTokens(peak)} · latest {FormatCompactTokens(latest)}";
	}

	/// <summary>
	/// Builds a line-series (oldest → newest) for the daily tokens chart.
	/// Keeps the full day window (including zero days) so a short active stretch
	/// still plots against the period axis; caps at <paramref name="maxDays" />
	/// (most recent). Always returns at least two samples (LineChart requires it).
	/// </summary>
	public static double[] BuildDailyChartSeries(
		IReadOnlyList<DailyTokenTotal> days,
		int maxDays = 14)
	{
		return BuildChartSeries(days, maxDays, static d => d.TotalTokens);
	}

	/// <summary>
	/// Caption for the cumulative tokens chart (day span, first-day total → running total).
	/// </summary>
	public static string BuildDailyTokenTotalChartCaption(IReadOnlyList<DailyTokenTotal> days, int maxDays = 14)
	{
		if ((days == null) || (days.Count == 0))
		{
			return "No token total data";
		}

		if (!TryGetChartWindow(
				days,
				maxDays,
				static d => d.TotalTokens,
				out var start,
				out var endExclusive,
				out var daily))
		{
			return "No token total data";
		}

		var firstDay = days[start].Day;
		var lastDay = days[endExclusive - 1].Day;
		var first = daily[0];
		var total = 0d;
		for (var i = 0; i < daily.Length; i++)
		{
			total += daily[i];
		}

		if (firstDay.Date == lastDay.Date)
		{
			return $"{firstDay:MMM d} · {FormatCompactTokens(total)} total";
		}

		return $"{firstDay:MMM d}–{lastDay:MMM d} · {daily.Length}d · {FormatCompactTokens(first)} → {FormatCompactTokens(total)}";
	}

	/// <summary>
	/// Builds a line-series of cumulative tokens (running sum of daily totals, oldest → newest).
	/// Same day window as <see cref="BuildDailyChartSeries" />; always at least two samples.
	/// </summary>
	public static double[] BuildDailyTokenTotalChartSeries(
		IReadOnlyList<DailyTokenTotal> days,
		int maxDays = 14)
	{
		var daily = BuildDailyChartSeries(days, maxDays);
		var cumulative = new double[daily.Length];
		var running = 0d;
		for (var i = 0; i < daily.Length; i++)
		{
			running += daily[i];
			cumulative[i] = running;
		}

		return cumulative;
	}

	/// <summary>
	/// Caption for the daily usage chart (day span, peak, latest, end-of-day %).
	/// </summary>
	public static string BuildDailyUsageChartCaption(IReadOnlyList<DailyUsageTotal> days, int maxDays = 14)
	{
		if ((days == null) || (days.Count == 0))
		{
			return "No daily usage data";
		}

		var anySnapshot = false;
		for (var i = 0; i < days.Count; i++)
		{
			if (days[i].HasSnapshot)
			{
				anySnapshot = true;
				break;
			}
		}

		if (!anySnapshot)
		{
			return "No daily usage data";
		}

		if (!TryGetChartWindow(
				days,
				maxDays,
				static d => d.DailyDelta,
				out var start,
				out var endExclusive,
				out var series))
		{
			return "No daily usage data";
		}

		var peak = series.Max();
		var latestDelta = series[series.Length - 1];
		var endPercent = days[endExclusive - 1].EndOfDayPercent;
		var firstDay = days[start].Day;
		var lastDay = days[endExclusive - 1].Day;
		if (firstDay.Date == lastDay.Date)
		{
			return $"{firstDay:MMM d} · peak +{peak:0.#} pts · latest +{latestDelta:0.#} · now {endPercent:0.#}%";
		}

		return $"{firstDay:MMM d}–{lastDay:MMM d} · {series.Length}d · peak +{peak:0.#} pts · latest +{latestDelta:0.#} · now {endPercent:0.#}%";
	}

	/// <summary>
	/// Builds a line-series of daily usage (percentage points of allowance) for the usage chart.
	/// Keeps the full day window (including days with no snapshot) for a full-period plot.
	/// </summary>
	public static double[] BuildDailyUsageChartSeries(
		IReadOnlyList<DailyUsageTotal> days,
		int maxDays = 14)
	{
		return BuildChartSeries(days, maxDays, static d => d.DailyDelta);
	}

	/// <summary>
	/// Caption for the cumulative usage % chart (day span, start %, now %).
	/// </summary>
	public static string BuildDailyUsageTotalChartCaption(IReadOnlyList<DailyUsageTotal> days, int maxDays = 14)
	{
		if ((days == null) || (days.Count == 0))
		{
			return "No usage total data";
		}

		var anySnapshot = false;
		for (var i = 0; i < days.Count; i++)
		{
			if (days[i].HasSnapshot)
			{
				anySnapshot = true;
				break;
			}
		}

		if (!anySnapshot)
		{
			return "No usage total data";
		}

		if (!TryGetChartWindow(
				days,
				maxDays,
				static d => d.EndOfDayPercent,
				out var start,
				out var endExclusive,
				out var series))
		{
			return "No usage total data";
		}

		var firstPercent = series[0];
		var nowPercent = series[series.Length - 1];
		var firstDay = days[start].Day;
		var lastDay = days[endExclusive - 1].Day;
		if (firstDay.Date == lastDay.Date)
		{
			return $"{firstDay:MMM d} · {nowPercent:0.#}% used";
		}

		return $"{firstDay:MMM d}–{lastDay:MMM d} · {series.Length}d · {firstPercent:0.#}% → {nowPercent:0.#}%";
	}

	/// <summary>
	/// Builds a line-series of cumulative usage % (end-of-day, 0–100 axis) for the period.
	/// </summary>
	public static double[] BuildDailyUsageTotalChartSeries(
		IReadOnlyList<DailyUsageTotal> days,
		int maxDays = 14)
	{
		return BuildChartSeries(days, maxDays, static d => d.EndOfDayPercent);
	}

	/// <summary>
	/// Builds synthetic weekly periods newest-first when billing has no period bounds.
	/// When <paramref name="planPeriodStart" /> and <paramref name="planPeriodEnd" /> are set
	/// (typically Personal SuperGrok), reuses that duration and reset time-of-day/day phase.
	/// Otherwise uses local Monday 00:00 weeks.
	/// </summary>
	public static IReadOnlyList<UsagePeriodOption> BuildSyntheticWeeklyPeriods(
		DateTimeOffset earliestActivity,
		DateTimeOffset now,
		DateTimeOffset? planPeriodStart = null,
		DateTimeOffset? planPeriodEnd = null)
	{
		TimeSpan duration;
		DateTimeOffset currentStart;
		var alignedToPlan = false;

		if (planPeriodStart is not null
			&& planPeriodEnd is not null
			&& (planPeriodEnd.Value > planPeriodStart.Value))
		{
			duration = planPeriodEnd.Value - planPeriodStart.Value;
			currentStart = GetAlignedPeriodStart(planPeriodStart.Value, duration, now);
			alignedToPlan = true;
		}
		else
		{
			duration = TimeSpan.FromDays(7);
			currentStart = GetLocalMondayWeekStart(now);
		}

		var floor = earliestActivity == default
			? currentStart.AddTicks(-duration.Ticks * 12)
			: earliestActivity;

		var options = new List<UsagePeriodOption>();
		var cursorStart = currentStart;
		for (var i = 0; i < MaxSyntheticPeriods; i++)
		{
			var cursorEnd = cursorStart + duration;
			var isCurrent = (now >= cursorStart) && (now < cursorEnd);
			options.Add(new UsagePeriodOption
			{
				PeriodStart = cursorStart,
				PeriodEnd = cursorEnd,
				PeriodType = SyntheticWeeklyPeriodType,
				IsCurrent = isCurrent,
				DisplayName = FormatPeriodDisplayName(
					cursorStart,
					cursorEnd,
					isCurrent,
					SyntheticWeeklyPeriodType,
					alignedToPlan)
			});

			// Walk back while the previous week still ends after the activity floor.
			var prevStart = cursorStart - duration;
			var prevEnd = cursorStart;
			if (prevEnd <= floor)
			{
				break;
			}

			cursorStart = prevStart;
		}

		return options;
	}

	/// <summary>
	/// Builds analytics from inferences and billing snapshots.
	/// Day buckets use the local calendar (see <see cref="ToLocalDay" />).
	/// Period end is exclusive for token filters; day buckets use inclusive local calendar days
	/// of [start, end) so a late-evening start/end still includes the end calendar day (often 8 days).
	/// </summary>
	/// <param name="inferences"> Inference events (any order). </param>
	/// <param name="billingHistory"> All billing snapshots (any order). </param>
	/// <param name="latestBilling"> Snapshot for credit % (selected period's last snap or current). </param>
	/// <param name="now"> Current time (UTC preferred). </param>
	/// <param name="selectedPeriodStart"> When set with end, forces the analytics window to this period. </param>
	/// <param name="selectedPeriodEnd"> Exclusive period end when selected. </param>
	public static UsageAnalytics Compute(
		IReadOnlyList<InferenceUsage> inferences,
		IReadOnlyList<BillingSnapshot> billingHistory,
		BillingSnapshot latestBilling,
		DateTimeOffset now,
		DateTimeOffset? selectedPeriodStart = null,
		DateTimeOffset? selectedPeriodEnd = null)
	{
		inferences ??= [];
		billingHistory ??= [];
		latestBilling ??= new BillingSnapshot();

		ResolveWindow(
			latestBilling,
			now,
			inferences,
			selectedPeriodStart,
			selectedPeriodEnd,
			out var windowStart,
			out var windowEnd,
			out var periodStart,
			out var periodEnd,
			out var hasPeriod);

		// Day charts use the full billing period when known (zeros for days with no activity /
		// remaining days). Rate math still uses elapsed window (start → now, exclusive end).
		ResolveChartBucketRange(
			hasPeriod,
			periodStart,
			periodEnd,
			windowStart,
			windowEnd,
			out var bucketStart,
			out var bucketEnd);

		var daily = BuildDailyTotals(inferences, bucketStart, bucketEnd);

		// Day axis stays full period; snaps are clipped to windowEnd (view as-of / now)
		// so scrub/replay do not plot billing after the view clock.
		var historyForView = FilterBillingHistory(billingHistory, bucketStart, windowEnd);
		var dailyUsage = BuildDailyUsageTotals(historyForView, bucketStart, bucketEnd);

		// 24h burn: half-open [now-24h, now+1tick)
		var tokenBurn24h = ComputeTokenBurnPerHour(
			inferences,
			now.AddHours(-24),
			ExclusiveEndThrough(now),
			24.0);

		// windowEnd is exclusive; elapsed wall time uses the last included instant.
		var elapsedEndInstant = hasPeriod
			? now < periodEnd ? now : periodEnd
			: now;
		if (elapsedEndInstant < windowStart)
		{
			elapsedEndInstant = windowStart;
		}

		var elapsedPeriodHours = Math.Max((elapsedEndInstant - windowStart).TotalHours, EpsilonHours);
		var tokensInWindow = SumTokens(inferences, windowStart, windowEnd);
		var tokenBurnPeriod = tokensInWindow / elapsedPeriodHours;

		var creditPercent = latestBilling.HasValue ? latestBilling.UsagePercent ?? 0 : 0;
		var isCurrentPeriod = hasPeriod && (now >= periodStart) && (now < periodEnd);
		var linearPace = 0d;
		if (hasPeriod && (periodEnd > periodStart))
		{
			var periodHours = Math.Max((periodEnd - periodStart).TotalHours, EpsilonHours);
			var elapsedInPeriod = Math.Max((elapsedEndInstant - periodStart).TotalHours, 0);
			linearPace = (100.0 * Math.Min(elapsedInPeriod, periodHours)) / periodHours;
		}

		var periodBilling = FilterBillingHistory(billingHistory, windowStart, windowEnd);
		var creditRate = 0d;
		var rateSource = string.Empty;
		if (isCurrentPeriod && TryCreditSlope(periodBilling, out var slope) && (slope > EpsilonRate))
		{
			creditRate = slope;
			rateSource = "billing history";
		}
		else if (isCurrentPeriod && hasPeriod && (creditPercent > 0) && (elapsedPeriodHours >= MinElapsedHoursForEstimate))
		{
			creditRate = creditPercent / elapsedPeriodHours;
			rateSource = "period average";
		}

		var hasEstimate = false;
		var eta = default(DateTimeOffset);
		var note = string.Empty;

		if (isCurrentPeriod
			&& (creditRate > EpsilonRate)
			&& (creditPercent < 100)
			&& (elapsedPeriodHours >= MinElapsedHoursForEstimate))
		{
			var hoursToFull = (100.0 - creditPercent) / creditRate;
			if ((hoursToFull > 0) && !double.IsInfinity(hoursToFull) && !double.IsNaN(hoursToFull))
			{
				eta = now.AddHours(hoursToFull);
				hasEstimate = true;
			}
		}

		var isSyntheticPeriod = hasPeriod
			&& (latestBilling.PeriodStart is null || latestBilling.PeriodEnd is null)
			&& selectedPeriodStart is not null
			&& selectedPeriodEnd is not null;

		if (!isCurrentPeriod && hasPeriod)
		{
			note = isSyntheticPeriod ? "Historical estimated week" : "Historical period";
		}
		else if (isSyntheticPeriod)
		{
			note = "Estimated week (no billing period from account)";
		}
		else if (!hasEstimate && (creditPercent <= 0) && (tokensInWindow == 0))
		{
			note = "Insufficient data";
		}
		else if (!hasEstimate && latestBilling.HasValue && (creditPercent < 100))
		{
			note = "Insufficient data for estimate";
		}
		else if (creditPercent >= 100)
		{
			note = "Usage exhausted for this period";
		}

		return new UsageAnalytics
		{
			DailyTokenTotals = daily,
			DailyUsageTotals = dailyUsage,
			TokenBurnPerHourLast24h = tokenBurn24h,
			TokenBurnPerHourPeriod = tokenBurnPeriod,
			UsagePercentPerHour = creditRate,
			LinearPacePercent = linearPace,
			EstimatedUsageExhaustionAt = eta,
			HasUsageEstimate = hasEstimate,
			UsageRateSource = rateSource,
			AnalyticsNote = note
		};
	}

	/// <summary>
	/// Builds distinct billing periods from snapshots, newest first.
	/// Optionally walks backward by period length so weeks with activity but no billing poll still appear.
	/// When this home has no period bounds, uses <paramref name="planPeriodStart" /> /
	/// <paramref name="planPeriodEnd" /> (e.g. Personal SuperGrok reset) as the phase template;
	/// otherwise falls back to local Monday 00:00 weeks.
	/// </summary>
	public static IReadOnlyList<UsagePeriodOption> DiscoverBillingPeriods(
		IReadOnlyList<BillingSnapshot> billingHistory,
		BillingSnapshot latestBilling,
		DateTimeOffset earliestActivity,
		DateTimeOffset now,
		DateTimeOffset? planPeriodStart = null,
		DateTimeOffset? planPeriodEnd = null)
	{
		billingHistory ??= [];
		latestBilling ??= new BillingSnapshot();

		var byStart = new Dictionary<DateTimeOffset, UsagePeriodOption>();

		void TryAdd(DateTimeOffset start, DateTimeOffset end, string periodType)
		{
			if (end <= start)
			{
				return;
			}

			if (!byStart.ContainsKey(start))
			{
				byStart[start] = new UsagePeriodOption
				{
					PeriodStart = start,
					PeriodEnd = end,
					PeriodType = periodType ?? string.Empty
				};
			}
		}

		foreach (var snap in billingHistory)
		{
			if (snap.PeriodStart is null || snap.PeriodEnd is null)
			{
				continue;
			}

			TryAdd(snap.PeriodStart.Value, snap.PeriodEnd.Value, snap.PeriodType);
		}

		if (latestBilling.HasValue
			&& latestBilling.PeriodStart is not null
			&& latestBilling.PeriodEnd is not null)
		{
			TryAdd(latestBilling.PeriodStart.Value, latestBilling.PeriodEnd.Value, latestBilling.PeriodType);
		}

		// Synthesize prior periods of the same length when we have a current weekly (or any) period.
		if (latestBilling.HasValue
			&& latestBilling.PeriodStart is not null
			&& latestBilling.PeriodEnd is not null
			&& (latestBilling.PeriodEnd.Value > latestBilling.PeriodStart.Value))
		{
			var duration = latestBilling.PeriodEnd.Value - latestBilling.PeriodStart.Value;
			var cursorStart = latestBilling.PeriodStart.Value;
			var floor = earliestActivity == default
				? latestBilling.PeriodStart.Value.AddDays(-duration.TotalDays * 12)
				: earliestActivity;

			// Walk back at most 26 periods (~half year weekly).
			for (var i = 0; i < 26; i++)
			{
				var prevStart = cursorStart - duration;
				var prevEnd = cursorStart;
				if (prevEnd <= floor)
				{
					break;
				}

				TryAdd(prevStart, prevEnd, latestBilling.PeriodType);
				cursorStart = prevStart;
			}
		}

		if (byStart.Count == 0)
		{
			// Account has no period bounds (common for Grok Business): align to Personal plan
			// reset when known, else local Monday 00:00 weeks.
			return BuildSyntheticWeeklyPeriods(earliestActivity, now, planPeriodStart, planPeriodEnd);
		}

		var currentStart = latestBilling.HasValue && latestBilling.PeriodStart is not null
			? latestBilling.PeriodStart.Value
			: default;

		var list = byStart.Values
			.OrderByDescending(x => x.PeriodStart)
			.Select(x =>
			{
				var isCurrent = (currentStart != default) && (x.PeriodStart == currentStart);

				// Also treat as current when now is inside the period.
				if (!isCurrent && (now >= x.PeriodStart) && (now < x.PeriodEnd))
				{
					isCurrent = true;
				}

				return new UsagePeriodOption
				{
					PeriodStart = x.PeriodStart,
					PeriodEnd = x.PeriodEnd,
					PeriodType = x.PeriodType,
					IsCurrent = isCurrent,
					DisplayName = FormatPeriodDisplayName(x.PeriodStart, x.PeriodEnd, isCurrent, x.PeriodType)
				};
			})
			.ToList();

		return list;
	}

	/// <summary>
	/// Allocates home credit-usage percent to one session by period token share.
	/// Returns 0 when there is no pool, no session tokens, or no home percent.
	/// </summary>
	public static double AllocateSessionUsagePercent(long sessionTokens, long grandTotalTokens, double homeUsagePercent)
	{
		if ((grandTotalTokens <= 0) || (sessionTokens <= 0) || (homeUsagePercent <= 0))
		{
			return 0;
		}

		return homeUsagePercent * ((double) sessionTokens / grandTotalTokens);
	}

	/// <summary>
	/// Compact token count for chart captions (e.g. 65.2M, 374K, 1.2K).
	/// </summary>
	public static string FormatCompactTokens(double tokens)
	{
		var value = Math.Abs(tokens);
		if (value >= 1_000_000_000)
		{
			return $"{tokens / 1_000_000_000:0.##}B";
		}

		if (value >= 1_000_000)
		{
			return $"{tokens / 1_000_000:0.##}M";
		}

		if (value >= 1_000)
		{
			return $"{tokens / 1_000:0.##}K";
		}

		return $"{tokens:0}";
	}

	/// <summary>
	/// Session-row usage percent (allocated share of the home allowance).
	/// Empty when this home does not report credit usage.
	/// </summary>
	public static string FormatAllocatedUsagePercent(double percent, bool hasAllocatedUsage)
	{
		return hasAllocatedUsage ? $"{percent:0.##}%" : string.Empty;
	}

	/// <summary>
	/// Label for a period option (local inclusive calendar span; end is exclusive).
	/// </summary>
	public static string FormatPeriodDisplayName(
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd,
		bool isCurrent,
		string periodType = null,
		bool alignedToPlan = false)
	{
		var first = ToLocalDay(periodStart);
		var last = GetInclusiveEndDay(periodStart, periodEnd);
		string range;
		if (first == last)
		{
			range = first.ToString("MMM d");
		}
		else if (first.Year == last.Year)
		{
			range = $"{first:MMM d} – {last:MMM d}";
		}
		else
		{
			range = $"{first:MMM d yyyy} – {last:MMM d yyyy}";
		}

		var isSynthetic = string.Equals(periodType, SyntheticWeeklyPeriodType, StringComparison.Ordinal);
		if (isSynthetic)
		{
			// "plan" = phase copied from another home's real SuperGrok period (e.g. 11:30pm reset).
			var estimate = alignedToPlan ? "plan" : "estimated";
			return isCurrent ? $"{range} · {estimate} · current" : $"{range} · {estimate}";
		}

		return isCurrent ? $"{range} · current" : range;
	}

	/// <summary>
	/// Human label for a billing period type (wire values like USAGE_PERIOD_TYPE_WEEKLY).
	/// Presentation only; does not change stored period type strings.
	/// </summary>
	public static string FormatPeriodTypeDisplay(string periodType)
	{
		if (string.IsNullOrWhiteSpace(periodType))
		{
			return string.Empty;
		}

		var raw = periodType.Trim();
		if (string.Equals(raw, SyntheticWeeklyPeriodType, StringComparison.OrdinalIgnoreCase))
		{
			return "Weekly";
		}

		// USAGE_PERIOD_TYPE_WEEKLY → WEEKLY; also accept already-short forms (weekly).
		const string prefix = "USAGE_PERIOD_TYPE_";
		var core = raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			? raw.Substring(prefix.Length)
			: raw;

		if (core.Length == 0)
		{
			return string.Empty;
		}

		// Title-case single token: WEEKLY → Weekly, monthly → Monthly.
		return char.ToUpperInvariant(core[0]) + core.Substring(1).ToLowerInvariant();
	}

	/// <summary>
	/// Inclusive start of the half-open period that contains <paramref name="now" />, using the
	/// same phase as <paramref name="templateStart" /> and length <paramref name="duration" />.
	/// Matches SuperGrok-style resets (e.g. weekly at ~11:30pm local).
	/// </summary>
	public static DateTimeOffset GetAlignedPeriodStart(
		DateTimeOffset templateStart,
		TimeSpan duration,
		DateTimeOffset now)
	{
		if (duration <= TimeSpan.Zero)
		{
			return templateStart;
		}

		var elapsedTicks = (now - templateStart).Ticks;
		var durationTicks = duration.Ticks;

		// Floor division so now in [start, start+duration) maps to start (works for negative too).
		var n = elapsedTicks >= 0
			? elapsedTicks / durationTicks
			: -(((-elapsedTicks + durationTicks) - 1) / durationTicks);
		return templateStart.AddTicks(n * durationTicks);
	}

	/// <summary>
	/// Number of local day buckets for [start, endExclusive): inclusive local dates from
	/// start through the last instant still in range. Prefer this over raw duration days so
	/// non-midnight weekly periods still chart the final calendar day.
	/// </summary>
	public static int GetDayBucketCount(DateTimeOffset rangeStart, DateTimeOffset rangeEndExclusive)
	{
		if (rangeEndExclusive <= rangeStart)
		{
			return 1;
		}

		var startDay = ToLocalDay(rangeStart);
		var endDay = ToLocalDay(rangeEndExclusive.AddTicks(-1));
		if (endDay < startDay)
		{
			return 1;
		}

		return (endDay - startDay).Days + 1;
	}

	/// <summary>
	/// Last local calendar day included in [start, endExclusive).
	/// For a weekly period that starts late on day D and ends late on day D+7, this is D+7
	/// (usage is still valid until exclusive end), not D+6.
	/// </summary>
	public static DateTime GetInclusiveEndDay(DateTimeOffset rangeStart, DateTimeOffset rangeEndExclusive)
	{
		if (rangeEndExclusive <= rangeStart)
		{
			return ToLocalDay(rangeStart);
		}

		var endDay = ToLocalDay(rangeEndExclusive.AddTicks(-1));
		var startDay = ToLocalDay(rangeStart);
		return endDay < startDay ? startDay : endDay;
	}

	/// <summary>
	/// Local Monday 00:00 (machine timezone) as the inclusive start of the week containing
	/// <paramref name="instant" />. Used for synthetic periods when no plan template is available.
	/// </summary>
	public static DateTimeOffset GetLocalMondayWeekStart(DateTimeOffset instant)
	{
		var local = instant.ToLocalTime();
		var localDate = local.Date;
		var daysFromMonday = (((int) localDate.DayOfWeek - (int) DayOfWeek.Monday) + 7) % 7;
		var mondayLocal = localDate.AddDays(-daysFromMonday);
		var offset = TimeZoneInfo.Local.GetUtcOffset(mondayLocal);
		return new DateTimeOffset(mondayLocal, offset);
	}

	/// <summary>
	/// Session-row heat using default soft/hot thresholds (1M → 10M).
	/// </summary>
	public static TokenHeatColor GetTokenHeat(long totalTokens)
	{
		return GetTokenHeat(totalTokens, true, TokenHeatSoftThreshold, TokenHeatHotThreshold);
	}

	/// <summary>
	/// Session-row heat color from total tokens and thresholds.
	/// None when disabled or under soft; yellow at soft; red at hot and above.
	/// Pure ARGB channels for UI converters and tests (no Avalonia types).
	/// </summary>
	public static TokenHeatColor GetTokenHeat(
		long totalTokens,
		bool enabled,
		long softThreshold,
		long hotThreshold)
	{
		if (!enabled)
		{
			return TokenHeatColor.None;
		}

		if (softThreshold < 0)
		{
			softThreshold = 0;
		}

		if (hotThreshold <= softThreshold)
		{
			hotThreshold = softThreshold + 1;
		}

		if (totalTokens < softThreshold)
		{
			return TokenHeatColor.None;
		}

		var span = (double) (hotThreshold - softThreshold);
		var t = Math.Clamp((totalTokens - softThreshold) / span, 0d, 1d);
		return new TokenHeatColor(
			LerpByte(HeatAlphaSoft, HeatAlphaHot, t),
			LerpByte(HeatYellowR, HeatRedR, t),
			LerpByte(HeatYellowG, HeatRedG, t),
			LerpByte(HeatYellowB, HeatRedB, t));
	}

	/// <summary>
	/// True when timestamp is in [from, to) (exclusive end).
	/// </summary>
	public static bool IsInHalfOpenRange(DateTimeOffset timestamp, DateTimeOffset from, DateTimeOffset to)
	{
		return (timestamp >= from) && (timestamp < to);
	}

	/// <summary>
	/// True when the model id is an xAI / subscription Grok model (not a custom local endpoint).
	/// Official CLI models use ids such as grok-4.5; local custom models log ids like
	/// qwen/qwen3.6-35b-a3b or config aliases that do not start with "grok".
	/// </summary>
	public static bool IsSubscriptionGrokModel(string modelId)
	{
		if (string.IsNullOrWhiteSpace(modelId))
		{
			return false;
		}

		return modelId.StartsWith("grok", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Local calendar day for bucketing (wall-clock day for the user).
	/// </summary>
	public static DateTime ToLocalDay(DateTimeOffset timestamp)
	{
		return timestamp.ToLocalTime().Date;
	}

	internal static IReadOnlyList<DailyTokenTotal> BuildDailyTotals(
		IReadOnlyList<InferenceUsage> inferences,
		DateTimeOffset windowStart,
		DateTimeOffset windowEnd)
	{
		// windowEnd is exclusive for period bounds (e.g. Mon 00:00 → next Mon 00:00 → 7 local days).
		var startDay = ToLocalDay(windowStart);
		var endDay = GetInclusiveEndDay(windowStart, windowEnd);

		var sums = new Dictionary<DateTime, long>();
		foreach (var inference in inferences)
		{
			if (!IsInHalfOpenRange(inference.Timestamp, windowStart, windowEnd))
			{
				continue;
			}

			var day = ToLocalDay(inference.Timestamp);
			var tokens = inference.PromptTokens + inference.CompletionTokens;
			if (sums.TryGetValue(day, out var existing))
			{
				sums[day] = existing + tokens;
			}
			else
			{
				sums[day] = tokens;
			}
		}

		var list = new List<DailyTokenTotal>();
		for (var day = startDay; day <= endDay; day = day.AddDays(1))
		{
			sums.TryGetValue(day, out var total);
			list.Add(new DailyTokenTotal { Day = day, TotalTokens = total });
		}

		return list;
	}

	/// <summary>
	/// Builds continuous local-day usage totals for the window from billing snapshots.
	/// End-of-day % is the last snapshot that day (or carried forward). Daily delta is
	/// end-of-day minus prior end; on the first day with snaps there is no prior end, so
	/// delta is end-of-day vs 0% (period baseline), whether one snap or many.
	/// </summary>
	internal static IReadOnlyList<DailyUsageTotal> BuildDailyUsageTotals(
		IReadOnlyList<BillingSnapshot> billingHistory,
		DateTimeOffset windowStart,
		DateTimeOffset windowEnd)
	{
		// windowEnd is exclusive for period bounds.
		var startDay = ToLocalDay(windowStart);
		var endDay = GetInclusiveEndDay(windowStart, windowEnd);

		var byDay = new Dictionary<DateTime, List<BillingSnapshot>>();
		if (billingHistory != null)
		{
			foreach (var snap in billingHistory)
			{
				if (!snap.HasValue || snap.UsagePercent is null)
				{
					continue;
				}

				if (!IsInHalfOpenRange(snap.Timestamp, windowStart, windowEnd))
				{
					continue;
				}

				var day = ToLocalDay(snap.Timestamp);
				if (!byDay.TryGetValue(day, out var list))
				{
					list = [];
					byDay[day] = list;
				}

				list.Add(snap);
			}
		}

		foreach (var pair in byDay)
		{
			pair.Value.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
		}

		var result = new List<DailyUsageTotal>();
		var hasPriorEnd = false;
		var priorEnd = 0d;

		for (var day = startDay; day <= endDay; day = day.AddDays(1))
		{
			if (byDay.TryGetValue(day, out var snaps) && (snaps.Count > 0))
			{
				var last = snaps[snaps.Count - 1].UsagePercent ?? 0;

				// First day with data: used vs period baseline 0%. Later days: vs prior EOD.
				var delta = hasPriorEnd ? last - priorEnd : last;

				result.Add(new DailyUsageTotal
				{
					Day = day,
					EndOfDayPercent = last,
					DailyDelta = delta,
					HasSnapshot = true
				});
				priorEnd = last;
				hasPriorEnd = true;
			}
			else
			{
				result.Add(new DailyUsageTotal
				{
					Day = day,
					EndOfDayPercent = hasPriorEnd ? priorEnd : 0,
					DailyDelta = 0,
					HasSnapshot = false
				});
			}
		}

		return result;
	}

	internal static bool TryCreditSlope(IReadOnlyList<BillingSnapshot> orderedOrAny, out double percentPerHour)
	{
		percentPerHour = 0;
		if ((orderedOrAny == null) || (orderedOrAny.Count < 2))
		{
			return false;
		}

		var ordered = orderedOrAny
			.Where(x => x.HasValue && x.UsagePercent is not null)
			.OrderBy(x => x.Timestamp)
			.ToList();

		if (ordered.Count < 2)
		{
			return false;
		}

		var first = ordered[0];
		var last = ordered[ordered.Count - 1];
		var hours = (last.Timestamp - first.Timestamp).TotalHours;
		if (hours < EpsilonHours)
		{
			return false;
		}

		var delta = (last.UsagePercent ?? 0) - (first.UsagePercent ?? 0);
		if (delta <= 0)
		{
			// Period reset or noise — caller falls back to average pace.
			return false;
		}

		percentPerHour = delta / hours;
		return true;
	}

	private static double[] BuildChartSeries<T>(
		IReadOnlyList<T> days,
		int maxDays,
		Func<T, double> valueSelector)
	{
		if (maxDays < 2)
		{
			maxDays = 2;
		}

		if ((days == null) || (days.Count == 0))
		{
			return [0, 0];
		}

		if (!TryGetChartWindow(days, maxDays, valueSelector, out _, out _, out var values))
		{
			return [0, 0];
		}

		return values;
	}

	private static double ComputeTokenBurnPerHour(
		IReadOnlyList<InferenceUsage> inferences,
		DateTimeOffset from,
		DateTimeOffset to,
		double divisorHours)
	{
		var hours = Math.Max(divisorHours, EpsilonHours);
		return SumTokens(inferences, from, to) / hours;
	}

	/// <summary>
	/// Exclusive end bound that still includes <paramref name="inclusiveInstant" /> in a half-open range.
	/// </summary>
	private static DateTimeOffset ExclusiveEndThrough(DateTimeOffset inclusiveInstant)
	{
		return inclusiveInstant < DateTimeOffset.MaxValue.AddTicks(-1)
			? inclusiveInstant.AddTicks(1)
			: inclusiveInstant;
	}

	private static IReadOnlyList<BillingSnapshot> FilterBillingHistory(
		IReadOnlyList<BillingSnapshot> billingHistory,
		DateTimeOffset windowStart,
		DateTimeOffset windowEndExclusive)
	{
		return billingHistory
			.Where(x => x.HasValue && IsInHalfOpenRange(x.Timestamp, windowStart, windowEndExclusive))
			.OrderBy(x => x.Timestamp)
			.ToList();
	}

	private static byte LerpByte(byte from, byte to, double t)
	{
		return (byte) Math.Clamp(Math.Round(from + ((to - from) * t)), 0, 255);
	}

	/// <summary>
	/// Day-bucket range for charts: full billing period when known (including remaining
	/// days after now), otherwise the elapsed analytics window.
	/// </summary>
	private static void ResolveChartBucketRange(
		bool hasPeriod,
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd,
		DateTimeOffset windowStart,
		DateTimeOffset windowEnd,
		out DateTimeOffset bucketStart,
		out DateTimeOffset bucketEnd)
	{
		if (hasPeriod && (periodEnd > periodStart))
		{
			bucketStart = periodStart;
			bucketEnd = periodEnd;
			return;
		}

		bucketStart = windowStart;
		bucketEnd = windowEnd;
	}

	private static void ResolveWindow(
		BillingSnapshot latestBilling,
		DateTimeOffset now,
		IReadOnlyList<InferenceUsage> inferences,
		DateTimeOffset? selectedPeriodStart,
		DateTimeOffset? selectedPeriodEnd,
		out DateTimeOffset windowStart,
		out DateTimeOffset windowEnd,
		out DateTimeOffset periodStart,
		out DateTimeOffset periodEnd,
		out bool hasPeriod)
	{
		periodStart = default;
		periodEnd = default;
		hasPeriod = false;

		if (selectedPeriodStart is not null
			&& selectedPeriodEnd is not null
			&& (selectedPeriodEnd.Value > selectedPeriodStart.Value))
		{
			periodStart = selectedPeriodStart.Value;
			periodEnd = selectedPeriodEnd.Value;
			hasPeriod = true;
			windowStart = periodStart;
			windowEnd = now < periodEnd ? ExclusiveEndThrough(now) : periodEnd;
			if (windowEnd < windowStart)
			{
				windowEnd = windowStart;
			}

			return;
		}

		if (latestBilling.HasValue
			&& latestBilling.PeriodStart is not null
			&& latestBilling.PeriodEnd is not null
			&& (latestBilling.PeriodEnd.Value > latestBilling.PeriodStart.Value))
		{
			periodStart = latestBilling.PeriodStart.Value;
			periodEnd = latestBilling.PeriodEnd.Value;
			hasPeriod = true;
			windowStart = periodStart;
			windowEnd = now < periodEnd ? ExclusiveEndThrough(now) : periodEnd;
			if (windowEnd < windowStart)
			{
				windowEnd = windowStart;
			}

			return;
		}

		// Fallback: last 7×24h ending now (exclusive end just after now so current events count).
		windowEnd = ExclusiveEndThrough(now);
		windowStart = now.AddDays(-7);
		if (inferences.Count > 0)
		{
			var minTs = inferences.Min(x => x.Timestamp);
			var maxTs = inferences.Max(x => x.Timestamp);
			var maxExclusive = ExclusiveEndThrough(maxTs);
			if (maxExclusive > windowEnd)
			{
				windowEnd = maxExclusive;
			}

			var sevenAgo = windowEnd.AddDays(-7);
			windowStart = minTs > sevenAgo ? sevenAgo : minTs;
			if ((windowEnd - windowStart).TotalDays > 14)
			{
				windowStart = windowEnd.AddDays(-14);
			}
		}
	}

	/// <summary>
	/// Sums prompt+completion in [from, toExclusive).
	/// </summary>
	private static long SumTokens(IReadOnlyList<InferenceUsage> inferences, DateTimeOffset from, DateTimeOffset toExclusive)
	{
		long sum = 0;
		foreach (var inference in inferences)
		{
			if (!IsInHalfOpenRange(inference.Timestamp, from, toExclusive))
			{
				continue;
			}

			sum += inference.PromptTokens + inference.CompletionTokens;
		}

		return sum;
	}

	/// <summary>
	/// Takes the most recent <paramref name="maxDays" /> buckets without dropping leading
	/// zeros, so the series spans the full period axis.
	/// </summary>
	private static bool TryGetChartWindow<T>(
		IReadOnlyList<T> days,
		int maxDays,
		Func<T, double> valueSelector,
		out int start,
		out int endExclusive,
		out double[] values)
	{
		values = null;
		endExclusive = days.Count;
		start = Math.Max(0, endExclusive - maxDays);

		var count = endExclusive - start;
		if (count < 2)
		{
			if (endExclusive <= 0)
			{
				return false;
			}

			start = Math.Max(0, endExclusive - 1);
			var latest = valueSelector(days[endExclusive - 1]);
			values = [0, latest];
			return true;
		}

		values = new double[count];
		for (var i = 0; i < count; i++)
		{
			values[i] = valueSelector(days[start + i]);
		}

		return true;
	}

	#endregion
}