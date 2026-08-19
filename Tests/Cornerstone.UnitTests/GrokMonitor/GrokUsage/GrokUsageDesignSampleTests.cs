#region References

using System;
using System.Linq;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.Channels;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokUsageDesignSampleTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void CreateDesignSamplePopulatesDashboardFields()
	{
		var bus = new AppBus(new GrokUsageChannel());
		var state = new AppState(new AppSettings(), new GrokUsageState());
		var sample = GrokUsageTabViewModel.CreateDesignSample(bus, state, Dispatcher);

		AreEqual(1, state.GrokUsage.Homes.Count);
		AreEqual(sample.HomeId, state.GrokUsage.Homes[0].Id);
		AreEqual(GrokPaths.PrimaryHomeDisplayName, sample.DisplayName);
		IsTrue(sample.HasBilling);
		IsTrue(sample.HasAnalytics);
		IsTrue(sample.HasOnDemandCap);
		IsTrue(sample.GrandTotalTokens > 0);
		IsTrue(sample.Sessions.Count >= 4);
		IsTrue(sample.Sessions.Any(x => x.InferenceCount == 0));
		IsTrue(sample.UsagePercent > 0);
		IsFalse(string.IsNullOrEmpty(sample.StatusText));
		IsFalse(string.IsNullOrEmpty(sample.PeriodRemainingText));
		IsFalse(string.IsNullOrEmpty(sample.PaceLabel));
		IsFalse(string.IsNullOrEmpty(sample.UsageExhaustionText));
		IsTrue(sample.AvailablePeriods.Count >= 2);
		IsNotNull(sample.SelectedPeriod);
		IsFalse(string.IsNullOrEmpty(sample.TokenTotalsPeriodLabel));
		// Design sample fills 7 varied days for the current week.
		AreEqual(7, sample.DailyTokensChartData.Length);
		IsFalse(string.IsNullOrEmpty(sample.DailyTokensChartCaption));
		IsTrue(sample.DailyTokensChartCaption.Contains("peak", StringComparison.OrdinalIgnoreCase));
		// Values are not a flat line — peak differs from at least one other sample.
		var max = 0d;
		var minNonZero = double.MaxValue;
		for (var i = 0; i < sample.DailyTokensChartData.Length; i++)
		{
			var v = sample.DailyTokensChartData[i];
			if (v > max)
			{
				max = v;
			}

			if ((v > 0) && (v < minNonZero))
			{
				minNonZero = v;
			}
		}

		IsTrue(max > 0);
		IsTrue(minNonZero < max);

		// Cumulative tokens chart climbs; last sample is sum of all daily values.
		AreEqual(7, sample.DailyTokenTotalChartData.Length);
		IsFalse(string.IsNullOrEmpty(sample.DailyTokenTotalChartCaption));
		IsTrue(sample.DailyTokenTotalChartCaption.Contains("→", StringComparison.Ordinal));
		var tokenRunning = 0d;
		for (var i = 0; i < sample.DailyTokensChartData.Length; i++)
		{
			tokenRunning += sample.DailyTokensChartData[i];
		}

		AreEqual(tokenRunning, sample.DailyTokenTotalChartData[sample.DailyTokenTotalChartData.Length - 1]);
		IsTrue(sample.DailyTokenTotalChartData[sample.DailyTokenTotalChartData.Length - 1]
			> sample.DailyTokenTotalChartData[0]);

		// Usage-per-day chart: design sample has a heavy day (+27 pts) so peak ≠ latest.
		IsTrue(sample.DailyUsageChartData.Length >= 2);
		IsFalse(string.IsNullOrEmpty(sample.DailyUsageChartCaption));
		IsTrue(sample.DailyUsageChartCaption.Contains("peak +", StringComparison.OrdinalIgnoreCase));
		var creditMax = 0d;
		for (var i = 0; i < sample.DailyUsageChartData.Length; i++)
		{
			if (sample.DailyUsageChartData[i] > creditMax)
			{
				creditMax = sample.DailyUsageChartData[i];
			}
		}

		IsTrue(creditMax >= 20);

		// Cumulative credit % chart (0–100): design sample ends near UsagePercent.
		IsTrue(sample.DailyUsageTotalChartData.Length >= 2);
		IsFalse(string.IsNullOrEmpty(sample.DailyUsageTotalChartCaption));
		var totalLatest = sample.DailyUsageTotalChartData[sample.DailyUsageTotalChartData.Length - 1];
		IsTrue(totalLatest > 50);

		// Enumerating the series (as ItemsControl does) must see filled values, not the empty ring.
		var enumerated = sample.DailyTokensChartData.ToList();
		AreEqual(sample.DailyTokensChartData.Length, enumerated.Count);
		IsTrue(enumerated.Exists(x => x > 0));
		var tokenTotalEnumerated = sample.DailyTokenTotalChartData.ToList();
		IsTrue(tokenTotalEnumerated.Exists(x => x > 0));
		var creditEnumerated = sample.DailyUsageChartData.ToList();
		IsTrue(creditEnumerated.Exists(x => x > 0));
		var totalEnumerated = sample.DailyUsageTotalChartData.ToList();
		IsTrue(totalEnumerated.Exists(x => x > 0));
	}

	#endregion
}
