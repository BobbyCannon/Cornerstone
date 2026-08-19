#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokUsageAnalyticsTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void BuildDailyTotalsFillsContinuousDays()
	{
		// Use local midnights so bucket assignment does not depend on machine timezone.
		var day0 = DateTime.Today.AddDays(-2);
		var day2 = DateTime.Today;
		var windowStart = new DateTimeOffset(day0);
		var windowEnd = new DateTimeOffset(day2.AddHours(12));
		var inferences = new List<InferenceUsage>
		{
			new()
			{
				Timestamp = new DateTimeOffset(day0.AddHours(15)),
				PromptTokens = 100,
				CompletionTokens = 20
			},
			new()
			{
				Timestamp = new DateTimeOffset(day2.AddHours(10)),
				PromptTokens = 50,
				CompletionTokens = 10
			}
		};

		var daily = GrokUsageAnalytics.BuildDailyTotals(inferences, windowStart, windowEnd);
		AreEqual(3, daily.Count);
		AreEqual(120, daily[0].TotalTokens);
		AreEqual(0, daily[1].TotalTokens);
		AreEqual(60, daily[2].TotalTokens);
		AreEqual(day0.Year, daily[0].Day.Year);
		AreEqual(day0.Month, daily[0].Day.Month);
		AreEqual(day0.Day, daily[0].Day.Day);
		AreEqual(day2.Year, daily[2].Day.Year);
		AreEqual(day2.Month, daily[2].Day.Month);
		AreEqual(day2.Day, daily[2].Day.Day);
	}

	[TestMethod]
	public void BuildDailyChartSeriesKeepsFullPeriodIncludingLeadingZeros()
	{
		var today = DateTime.Today;
		var days = new List<DailyTokenTotal>
		{
			new() { Day = today.AddDays(-6), TotalTokens = 0 },
			new() { Day = today.AddDays(-5), TotalTokens = 0 },
			new() { Day = today.AddDays(-4), TotalTokens = 0 },
			new() { Day = today.AddDays(-3), TotalTokens = 0 },
			new() { Day = today.AddDays(-2), TotalTokens = 65_000_000 },
			new() { Day = today.AddDays(-1), TotalTokens = 45_000_000 },
			new() { Day = today, TotalTokens = 400_000 }
		};

		var series = GrokUsageAnalytics.BuildDailyChartSeries(days);
		// Full period axis (leading zeros kept) so sparse activity still has context.
		AreEqual(7, series.Length);
		AreEqual(0, series[0]);
		AreEqual(0, series[3]);
		AreEqual(65_000_000, series[4]);
		AreEqual(45_000_000, series[5]);
		AreEqual(400_000, series[6]);

		var caption = GrokUsageAnalytics.BuildDailyChartCaption(days);
		IsTrue(caption.Contains("peak", StringComparison.OrdinalIgnoreCase));
		IsTrue(caption.Contains("65", StringComparison.Ordinal));
		IsTrue(caption.Contains("7d", StringComparison.Ordinal));
		IsFalse(caption.Contains("No daily", StringComparison.OrdinalIgnoreCase));

		// Cumulative tokens climb (leading zeros stay flat, then rise).
		var totals = GrokUsageAnalytics.BuildDailyTokenTotalChartSeries(days);
		AreEqual(7, totals.Length);
		AreEqual(0, totals[0]);
		AreEqual(0, totals[3]);
		AreEqual(65_000_000, totals[4]);
		AreEqual(110_000_000, totals[5]);
		AreEqual(110_400_000, totals[6]);
		var totalCaption = GrokUsageAnalytics.BuildDailyTokenTotalChartCaption(days);
		IsTrue(totalCaption.Contains("7d", StringComparison.Ordinal));
		IsTrue(totalCaption.Contains("→", StringComparison.Ordinal));
		IsFalse(totalCaption.Contains("No token", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public void BuildDailyChartSeriesEmptyIsTwoZeros()
	{
		var series = GrokUsageAnalytics.BuildDailyChartSeries([]);
		AreEqual(2, series.Length);
		AreEqual(0, series[0]);
		AreEqual(0, series[1]);
		AreEqual("No daily token data", GrokUsageAnalytics.BuildDailyChartCaption([]));

		var totals = GrokUsageAnalytics.BuildDailyTokenTotalChartSeries([]);
		AreEqual(2, totals.Length);
		AreEqual(0, totals[0]);
		AreEqual(0, totals[1]);
		AreEqual("No token total data", GrokUsageAnalytics.BuildDailyTokenTotalChartCaption([]));
	}

	[TestMethod]
	public void FormatCompactTokensUsesSuffixes()
	{
		AreEqual("500", GrokUsageAnalytics.FormatCompactTokens(500));
		AreEqual("1.50K", GrokUsageAnalytics.FormatCompactTokens(1_500));
		AreEqual("65.20M", GrokUsageAnalytics.FormatCompactTokens(65_200_000));
	}

	[TestMethod]
	public void GetTokenHeatIsNoneBelowOneMillion()
	{
		IsTrue(GrokUsageAnalytics.GetTokenHeat(0).IsNone);
		IsTrue(GrokUsageAnalytics.GetTokenHeat(999_999).IsNone);
		AreEqual(0, GrokUsageAnalytics.GetTokenHeat(500_000).A);
	}

	[TestMethod]
	public void GetTokenHeatSoftIsThemeZeroHotIsThemeNine()
	{
		var soft = GrokUsageAnalytics.GetTokenHeat(GrokUsageAnalytics.TokenHeatSoftThreshold);
		IsFalse(soft.IsNone);
		AreEqual(0, soft.ThemeIndex);

		var hot = GrokUsageAnalytics.GetTokenHeat(GrokUsageAnalytics.TokenHeatHotThreshold);
		IsFalse(hot.IsNone);
		AreEqual(9, hot.ThemeIndex);
		IsTrue(soft.A < hot.A);

		var over = GrokUsageAnalytics.GetTokenHeat(50_000_000);
		AreEqual(hot.A, over.A);
		AreEqual(hot.ThemeIndex, over.ThemeIndex);
	}

	[TestMethod]
	public void GetTokenHeatMidpointIsBetweenSoftAndHot()
	{
		var soft = GrokUsageAnalytics.GetTokenHeat(1_000_000);
		var mid = GrokUsageAnalytics.GetTokenHeat(5_500_000);
		var hot = GrokUsageAnalytics.GetTokenHeat(10_000_000);

		IsTrue(mid.A > soft.A && mid.A < hot.A);
		IsTrue(mid.ThemeIndex > soft.ThemeIndex && mid.ThemeIndex < hot.ThemeIndex);
	}

	[TestMethod]
	public void GetTokenHeatDisabledReturnsNone()
	{
		IsTrue(GrokUsageAnalytics.GetTokenHeat(5_000_000, false, 1_000_000, 10_000_000).IsNone);
	}

	[TestMethod]
	public void GetTokenHeatUsesCustomThresholds()
	{
		// Soft 100K, hot 200K — 100K is ThemeColor00, 200K is ThemeColor09.
		var soft = GrokUsageAnalytics.GetTokenHeat(100_000, true, 100_000, 200_000);
		var hot = GrokUsageAnalytics.GetTokenHeat(200_000, true, 100_000, 200_000);
		IsFalse(soft.IsNone);
		IsFalse(hot.IsNone);
		AreEqual(0, soft.ThemeIndex);
		AreEqual(9, hot.ThemeIndex);
		IsTrue(GrokUsageAnalytics.GetTokenHeat(99_999, true, 100_000, 200_000).IsNone);
	}

	[TestMethod]
	public void FormatPeriodTypeDisplayHumanizesWireValues()
	{
		AreEqual("Weekly", GrokUsageAnalytics.FormatPeriodTypeDisplay("USAGE_PERIOD_TYPE_WEEKLY"));
		AreEqual("Monthly", GrokUsageAnalytics.FormatPeriodTypeDisplay("USAGE_PERIOD_TYPE_MONTHLY"));
		AreEqual("Weekly", GrokUsageAnalytics.FormatPeriodTypeDisplay(GrokUsageAnalytics.SyntheticWeeklyPeriodType));
		AreEqual("Weekly", GrokUsageAnalytics.FormatPeriodTypeDisplay("weekly"));
		AreEqual(string.Empty, GrokUsageAnalytics.FormatPeriodTypeDisplay(null));
		AreEqual(string.Empty, GrokUsageAnalytics.FormatPeriodTypeDisplay(""));
		AreEqual(string.Empty, GrokUsageAnalytics.FormatPeriodTypeDisplay("   "));
	}

	[TestMethod]
	public void BuildDailyUsageTotalsUsesEndOfDayFromZeroOnFirstDay()
	{
		// First day with snaps: gain vs period baseline 0% (not last-minus-first).
		// Later days: end-of-day minus prior end.
		var day0 = DateTime.Today.AddDays(-2);
		var day1 = DateTime.Today.AddDays(-1);
		var day2 = DateTime.Today;
		var windowStart = new DateTimeOffset(day0);
		var windowEnd = new DateTimeOffset(day2.AddHours(18));
		var history = new List<BillingSnapshot>
		{
			new()
			{
				Timestamp = new DateTimeOffset(day0.AddHours(9)),
				UsagePercent = 67
			},
			new()
			{
				Timestamp = new DateTimeOffset(day0.AddHours(20)),
				UsagePercent = 94
			},
			new()
			{
				Timestamp = new DateTimeOffset(day1.AddHours(12)),
				UsagePercent = 96
			},
			new()
			{
				Timestamp = new DateTimeOffset(day2.AddHours(8)),
				UsagePercent = 97
			}
		};

		var daily = GrokUsageAnalytics.BuildDailyUsageTotals(history, windowStart, windowEnd);
		AreEqual(3, daily.Count);
		IsTrue(daily[0].HasSnapshot);
		AreEqual(94, daily[0].EndOfDayPercent);
		AreEqual(94, daily[0].DailyDelta); // EOD vs 0%
		AreEqual(96, daily[1].EndOfDayPercent);
		AreEqual(2, daily[1].DailyDelta); // 96 - 94
		AreEqual(97, daily[2].EndOfDayPercent);
		AreEqual(1, daily[2].DailyDelta); // 97 - 96

		var series = GrokUsageAnalytics.BuildDailyUsageChartSeries(daily);
		AreEqual(3, series.Length);
		AreEqual(94, series[0]);
		AreEqual(2, series[1]);
		AreEqual(1, series[2]);

		var caption = GrokUsageAnalytics.BuildDailyUsageChartCaption(daily);
		IsTrue(caption.Contains("peak +94", StringComparison.Ordinal));
		IsTrue(caption.Contains("now 97", StringComparison.Ordinal));

		var totalSeries = GrokUsageAnalytics.BuildDailyUsageTotalChartSeries(daily);
		AreEqual(3, totalSeries.Length);
		AreEqual(94, totalSeries[0]);
		AreEqual(96, totalSeries[1]);
		AreEqual(97, totalSeries[2]);
		var totalCaption = GrokUsageAnalytics.BuildDailyUsageTotalChartCaption(daily);
		IsTrue(totalCaption.Contains("94", StringComparison.Ordinal));
		IsTrue(totalCaption.Contains("97", StringComparison.Ordinal));
		IsTrue(totalCaption.Contains("%", StringComparison.Ordinal));
	}

	[TestMethod]
	public void BuildDailyUsageTotalsSingleSnapshotFirstDayUsesPercentFromZero()
	{
		var day = DateTime.Today;
		var history = new List<BillingSnapshot>
		{
			new()
			{
				Timestamp = new DateTimeOffset(day.AddHours(12)),
				UsagePercent = 40
			}
		};

		var daily = GrokUsageAnalytics.BuildDailyUsageTotals(
			history,
			new DateTimeOffset(day),
			new DateTimeOffset(day.AddHours(18)));
		AreEqual(1, daily.Count);
		AreEqual(40, daily[0].DailyDelta);
		AreEqual(40, daily[0].EndOfDayPercent);
	}

	[TestMethod]
	public void BuildDailyUsageTotalsEmptyHistory()
	{
		var day0 = DateTime.Today.AddDays(-1);
		var day1 = DateTime.Today;
		var daily = GrokUsageAnalytics.BuildDailyUsageTotals(
			[],
			new DateTimeOffset(day0),
			new DateTimeOffset(day1.AddHours(12)));
		AreEqual(2, daily.Count);
		AreEqual(0, daily[0].DailyDelta);
		IsFalse(daily[0].HasSnapshot);
		AreEqual("No daily usage data", GrokUsageAnalytics.BuildDailyUsageChartCaption([]));
	}

	[TestMethod]
	public void IsSubscriptionGrokModelAcceptsGrokIdsAndRejectsLocalModels()
	{
		IsTrue(GrokUsageAnalytics.IsSubscriptionGrokModel("grok-4.5"));
		IsTrue(GrokUsageAnalytics.IsSubscriptionGrokModel("Grok-4"));
		IsTrue(GrokUsageAnalytics.IsSubscriptionGrokModel("grok-3-mini"));
		IsTrue(GrokUsageAnalytics.IsSubscriptionGrokModel("grok-build"));
		IsFalse(GrokUsageAnalytics.IsSubscriptionGrokModel("qwen/qwen3.6-35b-a3b"));
		IsFalse(GrokUsageAnalytics.IsSubscriptionGrokModel("google/gemma-4-26b-a4b-qatb"));
		IsFalse(GrokUsageAnalytics.IsSubscriptionGrokModel("qwen36"));
		IsFalse(GrokUsageAnalytics.IsSubscriptionGrokModel("local"));
		IsFalse(GrokUsageAnalytics.IsSubscriptionGrokModel(string.Empty));
		IsFalse(GrokUsageAnalytics.IsSubscriptionGrokModel(null));
	}

	[TestMethod]
	public void FilterPeriodsWithTokenUsageDropsWeeksWithoutInferences()
	{
		var currentStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var currentEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var prevStart = currentStart.AddDays(-7);
		var options = new[]
		{
			new UsagePeriodOption { PeriodStart = currentStart, PeriodEnd = currentEnd, IsCurrent = true },
			new UsagePeriodOption { PeriodStart = prevStart, PeriodEnd = currentStart }
		};
		var inferences = new[]
		{
			new InferenceUsage
			{
				Timestamp = currentStart.AddDays(1),
				ModelId = "grok-4.5",
				PromptTokens = 10
			}
		};

		var kept = GrokUsageAnalytics.FilterPeriodsWithTokenUsage(options, inferences);
		AreEqual(1, kept.Count);
		AreEqual(currentStart, kept[0].PeriodStart);
	}

	[TestMethod]
	public void DiscoverBillingPeriodsIncludesCurrentAndSynthesizesPriorWeeks()
	{
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var now = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
		var earliest = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);
		var billing = new BillingSnapshot
		{
			Timestamp = now,
			UsagePercent = 40,
			PeriodStart = periodStart,
			PeriodEnd = periodEnd,
			PeriodType = "USAGE_PERIOD_TYPE_WEEKLY"
		};

		var periods = GrokUsageAnalytics.DiscoverBillingPeriods([billing], billing, earliest, now);
		IsTrue(periods.Count >= 2);
		IsTrue(periods.Any(x => x.IsCurrent && (x.PeriodStart == periodStart)));
		IsTrue(periods.Any(x => x.PeriodStart == periodStart.AddDays(-7)));
		IsTrue(periods[0].PeriodStart >= periods[1].PeriodStart);
	}

	[TestMethod]
	public void DiscoverBillingPeriodsUsesSyntheticLocalMondayWeeksWhenNoPeriodBounds()
	{
		// Billing present (Business-like) but no currentPeriod start/end.
		var now = new DateTimeOffset(2026, 8, 12, 15, 30, 0, TimeSpan.Zero); // Wednesday
		var earliest = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);
		var billing = new BillingSnapshot
		{
			Timestamp = now,
			UsagePercent = 22,
			SubscriptionTier = "Business"
		};

		var periods = GrokUsageAnalytics.DiscoverBillingPeriods([billing], billing, earliest, now);
		IsTrue(periods.Count >= 2);

		var current = periods.Single(x => x.IsCurrent);
		AreEqual(GrokUsageAnalytics.SyntheticWeeklyPeriodType, current.PeriodType);
		AreEqual(TimeSpan.FromDays(7), current.PeriodEnd - current.PeriodStart);
		IsTrue((now >= current.PeriodStart) && (now < current.PeriodEnd));
		AreEqual(DayOfWeek.Monday, current.PeriodStart.LocalDateTime.DayOfWeek);
		AreEqual(0, current.PeriodStart.LocalDateTime.TimeOfDay.Ticks);
		IsTrue(current.DisplayName.Contains("estimated", StringComparison.OrdinalIgnoreCase));
		IsTrue(current.DisplayName.Contains("current", StringComparison.OrdinalIgnoreCase));

		// Newest first; prior week is exactly 7 days earlier.
		IsTrue(periods[0].PeriodStart >= periods[1].PeriodStart);
		AreEqual(periods[0].PeriodStart, periods[1].PeriodEnd);

		// Real SuperGrok path must still win when period bounds exist (regression).
		var withPeriod = billing with
		{
			PeriodStart = new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero),
			PeriodEnd = new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.Zero),
			PeriodType = "USAGE_PERIOD_TYPE_WEEKLY"
		};
		var real = GrokUsageAnalytics.DiscoverBillingPeriods([withPeriod], withPeriod, earliest, now);
		IsTrue(real.Any(x => x.PeriodStart == withPeriod.PeriodStart.Value));
		IsFalse(real.Any(x => x.PeriodType == GrokUsageAnalytics.SyntheticWeeklyPeriodType));
	}

	[TestMethod]
	public void DiscoverBillingPeriodsAlignsSyntheticWeeksToPersonalPlanResetTime()
	{
		// Personal SuperGrok-style week: resets ~11:30pm local (use fixed offset for determinism).
		var offset = TimeSpan.FromHours(-5);
		var planStart = new DateTimeOffset(2026, 8, 4, 23, 30, 0, offset);
		var planEnd = new DateTimeOffset(2026, 8, 11, 23, 30, 0, offset);
		// Mid-week after that plan window started (still inside aligned current period).
		var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, offset);
		var earliest = new DateTimeOffset(2026, 7, 15, 0, 0, 0, offset);
		var businessBilling = new BillingSnapshot
		{
			Timestamp = now,
			UsagePercent = 5,
			SubscriptionTier = "Business"
		};

		var periods = GrokUsageAnalytics.DiscoverBillingPeriods(
			[businessBilling],
			businessBilling,
			earliest,
			now,
			planStart,
			planEnd);

		var current = periods.Single(x => x.IsCurrent);
		AreEqual(GrokUsageAnalytics.SyntheticWeeklyPeriodType, current.PeriodType);
		AreEqual(planEnd - planStart, current.PeriodEnd - current.PeriodStart);
		// Phase matches personal plan (23:30), not local Monday midnight.
		AreEqual(23, current.PeriodStart.Hour);
		AreEqual(30, current.PeriodStart.Minute);
		IsTrue((now >= current.PeriodStart) && (now < current.PeriodEnd));
		// now is in the week starting 2026-08-11 23:30
		AreEqual(new DateTimeOffset(2026, 8, 11, 23, 30, 0, offset), current.PeriodStart);
		AreEqual(new DateTimeOffset(2026, 8, 18, 23, 30, 0, offset), current.PeriodEnd);
		IsTrue(current.DisplayName.Contains("plan", StringComparison.OrdinalIgnoreCase));
		IsFalse(current.DisplayName.Contains("estimated", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public void GetAlignedPeriodStartHandlesNegativeAndBoundary()
	{
		var offset = TimeSpan.Zero;
		var template = new DateTimeOffset(2026, 8, 4, 23, 30, 0, offset);
		var duration = TimeSpan.FromDays(7);

		AreEqual(template, GrokUsageAnalytics.GetAlignedPeriodStart(template, duration, template));
		AreEqual(
			template,
			GrokUsageAnalytics.GetAlignedPeriodStart(template, duration, template.AddDays(3)));
		AreEqual(
			template.AddDays(7),
			GrokUsageAnalytics.GetAlignedPeriodStart(template, duration, template.AddDays(7)));
		AreEqual(
			template.AddDays(-7),
			GrokUsageAnalytics.GetAlignedPeriodStart(template, duration, template.AddDays(-1)));
	}

	[TestMethod]
	public void ComputeNotesEstimatedWeekWhenForcedPeriodWithoutBillingBounds()
	{
		var weekStart = GrokUsageAnalytics.GetLocalMondayWeekStart(
			new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
		var weekEnd = weekStart.AddDays(7);
		var now = weekStart.AddDays(2).AddHours(6);
		var billing = new BillingSnapshot
		{
			Timestamp = now,
			UsagePercent = 10
		};

		var analytics = GrokUsageAnalytics.Compute(
			[],
			[billing],
			billing,
			now,
			weekStart,
			weekEnd);

		IsTrue(analytics.AnalyticsNote.Contains("Estimated week", StringComparison.OrdinalIgnoreCase));
		IsTrue(analytics.LinearPacePercent > 0);
		AreEqual(
			GrokUsageAnalytics.GetDayBucketCount(weekStart, weekEnd),
			analytics.DailyTokenTotals.Count);
	}

	[TestMethod]
	public void GetDayBucketCountIncludesLastLocalDayForEveningAlignedWeeklyPeriod()
	{
		// Real billing periods often start/end late in the evening, still exactly 7×24h.
		// Charts and labels must include the end calendar day (usage until exclusive end).
		var periodStart = new DateTimeOffset(2026, 8, 10, 22, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 17, 22, 0, 0, TimeSpan.Zero);

		AreEqual(8, GrokUsageAnalytics.GetDayBucketCount(periodStart, periodEnd));
		AreEqual(
			GrokUsageAnalytics.ToLocalDay(periodEnd.AddTicks(-1)),
			GrokUsageAnalytics.GetInclusiveEndDay(periodStart, periodEnd));

		var label = GrokUsageAnalytics.FormatPeriodDisplayName(periodStart, periodEnd, true);
		IsTrue(label.Contains("Aug 10", StringComparison.Ordinal));
		IsTrue(label.Contains("Aug 17", StringComparison.Ordinal));
		IsTrue(label.Contains("current", StringComparison.Ordinal));

		var now = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
		var analytics = GrokUsageAnalytics.Compute(
			[],
			[],
			new BillingSnapshot
			{
				Timestamp = now,
				UsagePercent = 10,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			},
			now,
			periodStart,
			periodEnd);

		AreEqual(8, analytics.DailyTokenTotals.Count);
		AreEqual(GrokUsageAnalytics.ToLocalDay(periodStart), analytics.DailyTokenTotals[0].Day);
		AreEqual(
			GrokUsageAnalytics.GetInclusiveEndDay(periodStart, periodEnd),
			analytics.DailyTokenTotals[^1].Day);
	}

	[TestMethod]
	public void GetDayBucketCountForMidnightAlignedUtcWeekUsesInclusiveLocalSpan()
	{
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var expected = (GrokUsageAnalytics.GetInclusiveEndDay(periodStart, periodEnd)
			- GrokUsageAnalytics.ToLocalDay(periodStart)).Days + 1;
		AreEqual(expected, GrokUsageAnalytics.GetDayBucketCount(periodStart, periodEnd));
		IsTrue(expected >= 7);
	}

	[TestMethod]
	public void ComputeUsesPeriodAverageWhenSingleBillingPoint()
	{
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var now = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero); // 3.5 days in
		var billing = new BillingSnapshot
		{
			Timestamp = now,
			UsagePercent = 50,
			PeriodStart = periodStart,
			PeriodEnd = periodEnd
		};

		var analytics = GrokUsageAnalytics.Compute(
			[],
			[billing],
			billing,
			now);

		IsTrue(analytics.HasUsageEstimate);
		AreEqual("period average", analytics.UsageRateSource);
		IsTrue(analytics.UsagePercentPerHour > 0);
		// At 50% halfway through a week, ETA should land near period end.
		var hoursToEnd = (periodEnd - now).TotalHours;
		var hoursToEta = (analytics.EstimatedUsageExhaustionAt - now).TotalHours;
		IsTrue(Math.Abs(hoursToEta - hoursToEnd) < 12);
		IsTrue(analytics.LinearPacePercent > 40 && analytics.LinearPacePercent < 60);
	}

	[TestMethod]
	public void ComputeFillsDailyBucketsForFullBillingPeriod()
	{
		// Period Mon→next Mon; only mid-period snapshots — buckets still cover every local day.
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero); // Tuesday UTC
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var now = new DateTimeOffset(2026, 8, 9, 18, 0, 0, TimeSpan.Zero);
		var history = new List<BillingSnapshot>
		{
			new()
			{
				Timestamp = new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero),
				UsagePercent = 70,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			},
			new()
			{
				Timestamp = new DateTimeOffset(2026, 8, 8, 22, 0, 0, TimeSpan.Zero),
				UsagePercent = 97,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			},
			new()
			{
				Timestamp = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero),
				UsagePercent = 100,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			}
		};

		var analytics = GrokUsageAnalytics.Compute([], history, history[^1], now);

		// Exclusive period end: day count is inclusive local calendar span of [start, end).
		var expectedDays = GrokUsageAnalytics.GetDayBucketCount(periodStart, periodEnd);
		IsTrue(expectedDays >= 7);
		AreEqual(expectedDays, analytics.DailyUsageTotals.Count);
		AreEqual(expectedDays, analytics.DailyTokenTotals.Count);

		var series = GrokUsageAnalytics.BuildDailyUsageChartSeries(analytics.DailyUsageTotals);
		AreEqual(expectedDays, series.Length);
		// First day with snaps: EOD vs 0% (97), not last-minus-first (27).
		IsTrue(series.Max() >= 97);
		// Full axis includes quiet days (zeros), not collapsed to "2d".
		IsTrue(series.Length > 2);
		var caption = GrokUsageAnalytics.BuildDailyUsageChartCaption(analytics.DailyUsageTotals);
		IsTrue(caption.Contains($"{series.Length}d", StringComparison.Ordinal));
		IsFalse(caption.Contains("2d", StringComparison.Ordinal));
	}

	[TestMethod]
	public void ComputeDailyUsageIgnoresBillingAfterViewAsOf()
	{
		// Scrub mid-period: snaps after as-of must not appear as later-day gains.
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var asOf = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
		var history = new List<BillingSnapshot>
		{
			new()
			{
				Timestamp = new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero),
				UsagePercent = 20,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			},
			new()
			{
				Timestamp = new DateTimeOffset(2026, 8, 6, 18, 0, 0, TimeSpan.Zero),
				UsagePercent = 40,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			},
			new()
			{
				Timestamp = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero),
				UsagePercent = 90,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			}
		};

		var latestAtAsOf = history[1];
		var analytics = GrokUsageAnalytics.Compute(
			[],
			history,
			latestAtAsOf,
			asOf,
			periodStart,
			periodEnd);

		var expectedDays = GrokUsageAnalytics.GetDayBucketCount(periodStart, periodEnd);
		AreEqual(expectedDays, analytics.DailyUsageTotals.Count);

		// Post-as-of snap (90%) must not be the series max; peak is 40 from day-6 EOD.
		var series = GrokUsageAnalytics.BuildDailyUsageChartSeries(analytics.DailyUsageTotals);
		var totalSeries = GrokUsageAnalytics.BuildDailyUsageTotalChartSeries(analytics.DailyUsageTotals);
		IsTrue(series.Max() <= 40.001);
		IsTrue(totalSeries.Max() <= 40.001);
		// Full period axis retained (remaining days after as-of still present).
		IsTrue(series.Length >= 7);
	}

	[TestMethod]
	public void ComputeUsesBillingSlopeWhenTwoPoints()
	{
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		var t0 = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
		var t1 = new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);
		var now = t1;
		var history = new List<BillingSnapshot>
		{
			new()
			{
				Timestamp = t0,
				UsagePercent = 10,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			},
			new()
			{
				Timestamp = t1,
				UsagePercent = 34,
				PeriodStart = periodStart,
				PeriodEnd = periodEnd
			}
		};

		var analytics = GrokUsageAnalytics.Compute([], history, history[1], now);
		IsTrue(analytics.HasUsageEstimate);
		AreEqual("billing history", analytics.UsageRateSource);
		// 24% over 48h => 0.5 %/h
		IsTrue(Math.Abs(analytics.UsagePercentPerHour - 0.5) < 0.001);
	}

	[TestMethod]
	public void ComputeTokenBurnLast24hUsesWallClockDivisor()
	{
		var now = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
		var inferences = new List<InferenceUsage>
		{
			new()
			{
				Timestamp = now.AddHours(-2),
				PromptTokens = 1000,
				CompletionTokens = 200
			},
			new()
			{
				Timestamp = now.AddHours(-30),
				PromptTokens = 9999,
				CompletionTokens = 0
			}
		};

		var analytics = GrokUsageAnalytics.Compute(inferences, [], new BillingSnapshot(), now);
		// Only first inference in last 24h: 1200 / 24 = 50
		IsTrue(Math.Abs(analytics.TokenBurnPerHourLast24h - 50.0) < 0.001);
	}

	[TestMethod]
	public void ComputeInsufficientDataWhenCold()
	{
		var now = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
		var analytics = GrokUsageAnalytics.Compute([], [], new BillingSnapshot(), now);
		IsFalse(analytics.HasUsageEstimate);
		AreEqual("Insufficient data", analytics.AnalyticsNote);
	}

	[TestMethod]
	public void AllocateSessionUsagePercentSplitsByTokenShare()
	{
		AreEqual(30, GrokUsageAnalytics.AllocateSessionUsagePercent(300, 400, 40));
		AreEqual(10, GrokUsageAnalytics.AllocateSessionUsagePercent(100, 400, 40));
		AreEqual(0, GrokUsageAnalytics.AllocateSessionUsagePercent(0, 400, 40));
		AreEqual(0, GrokUsageAnalytics.AllocateSessionUsagePercent(100, 0, 40));
		AreEqual(0, GrokUsageAnalytics.AllocateSessionUsagePercent(100, 400, 0));
	}

	[TestMethod]
	public void FormatAllocatedUsagePercentIsEmptyWithoutCredit()
	{
		AreEqual("30.00%", GrokUsageAnalytics.FormatAllocatedUsagePercent(30, true));
		AreEqual("12.50%", GrokUsageAnalytics.FormatAllocatedUsagePercent(12.5, true));
		AreEqual(string.Empty, GrokUsageAnalytics.FormatAllocatedUsagePercent(12.5, false));
	}

	[TestMethod]
	public void TryCreditSlopeRejectsDecreasingPercent()
	{
		var history = new List<BillingSnapshot>
		{
			new() { Timestamp = DateTimeOffset.Parse("2026-08-05T00:00:00Z"), UsagePercent = 40 },
			new() { Timestamp = DateTimeOffset.Parse("2026-08-06T00:00:00Z"), UsagePercent = 10 }
		};

		IsFalse(GrokUsageAnalytics.TryCreditSlope(history, out _));
	}

	#endregion
}
