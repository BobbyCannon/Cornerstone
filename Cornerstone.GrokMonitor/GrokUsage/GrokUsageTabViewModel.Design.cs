#region References

using System;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

public partial class GrokUsageTabViewModel
{
	#region Methods

	/// <summary>
	/// Design-time / preview sample: one home with billing, analytics, and sessions.
	/// Writes sample State only for the Avalonia previewer and design tests.
	/// </summary>
	public static GrokUsageTabViewModel CreateDesignSample(AppBus bus, AppState state, IDispatcher dispatcher)
	{
		var now = DateTimeOffset.UtcNow;
		var periodStart = now.Date.AddDays(-((int) now.DayOfWeek + 6) % 7);
		var periodEnd = periodStart.AddDays(7);
		var periodStartOffset = new DateTimeOffset(periodStart, TimeSpan.Zero);
		var periodEndOffset = new DateTimeOffset(periodEnd, TimeSpan.Zero);

		var homeId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
		var home = CreatePrimaryHomeSample(homeId, now, periodStartOffset, periodEndOffset);

		state.GrokUsage.Homes.Clear();
		state.GrokUsage.Homes.Add(home);
		state.GrokUsage.SelectedHomeId = home.Id;
		state.GrokUsage.LastError = string.Empty;

		var sample = new GrokUsageTabViewModel(
			bus,
			home,
			state.GrokUsage,
			state.Settings,
			dispatcher,
			DateTimeProvider.RealTime);
		sample._isDesignSample = true;
		sample.InitializeLifecycle();
		return sample;
	}

	private static GrokHomeUsageState CreatePrimaryHomeSample(
		Guid id,
		DateTimeOffset now,
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd)
	{
		var home = new GrokHomeUsageState(id)
		{
			DisplayName = GrokPaths.PrimaryHomeDisplayName,
			Path = @"C:\Users\Ada\.grok",
			HomeExists = true,
			IsBusy = false,
			ProgressText = string.Empty,
			ErrorText = string.Empty,
			HasBilling = true,
			HasCreditUsage = true,
			SubscriptionTier = "SuperGrok",
			UsagePercent = 62.4,
			PeriodType = "weekly",
			PeriodStart = periodStart,
			PeriodEnd = periodEnd,
			OnDemandCap = 50,
			OnDemandUsed = 12.75,
			PrepaidBalance = 0,
			GrandTotalPromptTokens = 1_284_500,
			GrandTotalCachedPromptTokens = 412_200,
			GrandTotalCompletionTokens = 318_750,
			GrandTotalReasoningTokens = 96_400,
			GrandTotalTokens = 1_603_250,
			LastRefreshedAt = now.AddMinutes(-4),
			TokenBurnPerHourLast24h = 48_200,
			TokenBurnPerHourPeriod = 31_500,
			UsagePercentPerHour = 0.42,
			LinearPacePercent = 55.0,
			HasUsageEstimate = true,
			UsageRateSource = "billing history",
			EstimatedUsageExhaustionAt = now.AddHours(90),
			AnalyticsNote = string.Empty,
			SelectedPeriodStart = periodStart,
			SelectedPeriodEnd = periodEnd,
			ViewClockStart = periodStart,
			ViewClockMax = now < periodEnd ? now : periodEnd
		};

		var prevStart = periodStart.AddDays(-7);
		var prevEnd = periodStart;
		home.AvailablePeriods.Load(
		[
			new GrokUsagePeriodState
			{
				PeriodStart = periodStart,
				PeriodEnd = periodEnd,
				PeriodType = "weekly",
				IsCurrent = true
			},
			new GrokUsagePeriodState
			{
				PeriodStart = prevStart,
				PeriodEnd = prevEnd,
				PeriodType = "weekly",
				IsCurrent = false
			}
		]);

		var today = DateTime.Today;
		var daily = new DailyTokenTotal[7];
		long[] dayTokens =
		[
			51_300, 67_900, 142_000, 155_600, 88_200, 72_400, 61_800
		];
		for (var i = 0; i < daily.Length; i++)
		{
			daily[i] = new DailyTokenTotal
			{
				Day = today.AddDays(i - (daily.Length - 1)),
				TotalTokens = dayTokens[i]
			};
		}

		home.DailyTokenTotals.Load(daily);

		double[] usageEnds = [42, 48, 75, 82, 88, 91, 62.4];
		double[] usageDeltas = [7, 6, 27, 7, 6, 3, 2];
		var dailyUsage = new DailyUsageTotal[7];
		for (var i = 0; i < dailyUsage.Length; i++)
		{
			dailyUsage[i] = new DailyUsageTotal
			{
				Day = today.AddDays(i - (dailyUsage.Length - 1)),
				EndOfDayPercent = usageEnds[i],
				DailyDelta = usageDeltas[i],
				HasSnapshot = true
			};
		}

		home.DailyUsageTotals.Load(dailyUsage);

		home.Sessions.Load(
		[
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-001",
				Title = "Wire Grok usage dashboard",
				WorkingDirectory = @"C:\Workspaces\MyApp",
				CurrentModelId = "grok-4",
				MessageCount = 48,
				InferenceCount = 36,
				PromptTokens = 520_100,
				CachedPromptTokens = 180_400,
				CompletionTokens = 112_300,
				ReasoningTokens = 41_200,
				TotalTokens = 632_400,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(632_400, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-3).AddHours(-2),
				LastInferenceAt = now.AddMinutes(-12),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-001",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-001\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-001\events.jsonl"
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-002",
				Title = "Refactor TreeDataGrid selection",
				WorkingDirectory = @"C:\Workspaces\MyApp\Client",
				CurrentModelId = "grok-4",
				MessageCount = 22,
				InferenceCount = 18,
				PromptTokens = 310_200,
				CachedPromptTokens = 95_100,
				CompletionTokens = 78_400,
				ReasoningTokens = 22_800,
				TotalTokens = 388_600,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(388_600, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-5),
				LastInferenceAt = now.AddHours(-6),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-002",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-002\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-002\events.jsonl"
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-003",
				Title = "Design review notes",
				WorkingDirectory = @"C:\Workspaces\Docs",
				CurrentModelId = "grok-3-mini",
				MessageCount = 9,
				InferenceCount = 7,
				PromptTokens = 84_500,
				CachedPromptTokens = 12_200,
				CompletionTokens = 31_050,
				ReasoningTokens = 0,
				TotalTokens = 115_550,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(115_550, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-8),
				LastInferenceAt = now.AddDays(-1).AddHours(-3),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-003",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-003\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-003\events.jsonl"
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-004",
				Title = "Untitled session",
				WorkingDirectory = @"C:\Workspaces\Scratch",
				CurrentModelId = string.Empty,
				MessageCount = 2,
				InferenceCount = 0,
				PromptTokens = 0,
				CachedPromptTokens = 0,
				CompletionTokens = 0,
				ReasoningTokens = 0,
				TotalTokens = 0,
				HasAllocatedUsage = true,
				UsagePercent = 0,
				FirstInferenceAt = default,
				LastInferenceAt = default,
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-004",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-004\summary.json",
				EventsPath = string.Empty
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-005",
				Title = "PowerShell host diagnostics",
				WorkingDirectory = @"C:\Workspaces\MyApp",
				CurrentModelId = "grok-4",
				MessageCount = 15,
				InferenceCount = 11,
				PromptTokens = 198_700,
				CachedPromptTokens = 64_300,
				CompletionTokens = 45_200,
				ReasoningTokens = 18_400,
				TotalTokens = 243_900,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(243_900, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-2),
				LastInferenceAt = now.AddHours(-1),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-005",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-005\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-005\events.jsonl"
			}
		]);

		return home;
	}

	#endregion
}