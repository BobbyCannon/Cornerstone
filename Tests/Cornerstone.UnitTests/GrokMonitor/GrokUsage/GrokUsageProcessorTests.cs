#region References

using System;
using System.IO;
using System.Linq;
using System.Text;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.Channels;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.GrokMonitor.GrokUsage.Processors;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokUsageProcessorTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void EnsureHomesSeedsDiscoveredHomesIdempotently()
	{
		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		bus.GrokUsage.EnsureHomes();

		var firstCount = state.GrokUsage.Homes.Count;
		// Count matches whatever exists on this machine (often 1–2; may be 0 in CI).
		AreEqual(GrokPaths.DiscoverHomes().Count, firstCount);
		foreach (var home in state.GrokUsage.Homes)
		{
			IsFalse(string.IsNullOrWhiteSpace(home.DisplayName));
			IsFalse(string.IsNullOrWhiteSpace(home.Path));
			IsTrue(home.HomeExists);
		}

		if (firstCount > 0)
		{
			AreNotEqual(Guid.Empty, state.GrokUsage.SelectedHomeId);
		}

		// Idempotent
		bus.GrokUsage.EnsureHomes();
		AreEqual(firstCount, state.GrokUsage.Homes.Count);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshHomeWhileBusyQueuesPendingDiskRefresh()
	{
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":10,"cached_prompt_tokens":0,"completion_tokens":5,"reasoning_tokens":0}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":12.5,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\proj" }, "generated_title": "First", "current_model_id": "grok-4.5", "num_messages": 1 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		state.GrokUsage.SelectedHomeId = home.Id;

		bus.GrokUsage.RefreshHome(home.Id);
		AreEqual(15, home.GrandTotalTokens);

		// Simulate an in-flight load. A disk refresh while busy must queue and apply after the next load.
		home.IsBusy = true;
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":10,"cached_prompt_tokens":0,"completion_tokens":5,"reasoning_tokens":0}}
			{"ts":"2026-08-09T12:01:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":0,"completion_tokens":50,"reasoning_tokens":0}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":12.5,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		bus.GrokUsage.RefreshHome(home.Id);
		AreEqual(15, home.GrandTotalTokens);
		IsTrue(home.IsBusy);

		// Next successful RefreshHomeCore finishes and flushes pending for a second disk read.
		home.IsBusy = false;
		bus.GrokUsage.RefreshHome(home.Id);
		AreEqual(165, home.GrandTotalTokens);
		IsFalse(home.IsBusy);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshAllRediscoverHomesAfterClear()
	{
		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		bus.GrokUsage.EnsureHomes();
		var expected = GrokPaths.DiscoverHomes().Count;
		AreEqual(expected, state.GrokUsage.Homes.Count);

		// Simulate stale state: homes wiped. Refresh must re-scan disk, not only refresh existing ids.
		state.GrokUsage.Homes.Clear();
		state.GrokUsage.SelectedHomeId = Guid.Empty;

		bus.GrokUsage.RefreshAll();

		AreEqual(expected, state.GrokUsage.Homes.Count);
		foreach (var home in state.GrokUsage.Homes)
		{
			IsTrue(home.HomeExists);
		}

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshHomeLoadsSessionsAndBilling()
	{
		// View clock follows IDateTimeProvider; unit host defaults to year 2000.
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":10,"completion_tokens":20,"reasoning_tokens":5}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":12.5,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\proj" }, "generated_title": "Test session", "current_model_id": "grok-4.5", "num_messages": 3 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		state.GrokUsage.SelectedHomeId = home.Id;

		bus.GrokUsage.RefreshHome(home.Id);

		IsFalse(home.IsBusy);
		IsTrue(home.HasBilling);
		IsTrue(home.HasCreditUsage);
		AreEqual("SuperGrok Plus", home.SubscriptionTier);
		AreEqual(12.5, home.UsagePercent);
		AreEqual(100, home.GrandTotalPromptTokens);
		AreEqual(20, home.GrandTotalCompletionTokens);
		AreEqual(1, home.Sessions.Count);
		AreEqual("Test session", home.Sessions[0].Title);
		AreEqual("grok-4.5", home.Sessions[0].CurrentModelId);
		AreEqual(1, home.Sessions[0].InferenceCount);
		IsTrue(home.Sessions[0].HasAllocatedUsage);
		AreEqual(12.5, home.Sessions[0].UsagePercent);
		IsFalse(string.IsNullOrEmpty(home.Sessions[0].SessionDirectory));
		IsFalse(string.IsNullOrEmpty(home.Sessions[0].SummaryPath));
		AreNotEqual(default, home.LastRefreshedAt);
		IsTrue(home.DailyTokenTotals.Count > 0);
		IsTrue(home.TokenBurnPerHourPeriod > 0 || home.TokenBurnPerHourLast24h > 0 || home.HasBilling);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshHomeBusinessTierWithoutCreditPercentHidesCreditUsage()
	{
		// Wednesday so synthetic Mon–Sun week includes Tuesday activity.
		SetTime(new DateTime(2026, 8, 12, 15, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		// Business billing often reports tier only — no creditUsagePercent / currentPeriod.
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-11T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":200,"cached_prompt_tokens":0,"completion_tokens":40,"reasoning_tokens":0}}
			{"ts":"2026-08-11T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{},"subscriptionTier":"Business"}}
			"""
		);
		fixture.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\proj" }, "generated_title": "Work session", "current_model_id": "grok-4.5", "num_messages": 2 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Work",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		state.GrokUsage.SelectedHomeId = home.Id;

		bus.GrokUsage.RefreshHome(home.Id);

		IsTrue(home.HasBilling);
		IsFalse(home.HasCreditUsage);
		AreEqual("Business", home.SubscriptionTier);
		AreEqual(0, home.UsagePercent);
		AreEqual(200, home.GrandTotalPromptTokens);
		AreEqual(1, home.Sessions.Count);
		IsFalse(home.Sessions[0].HasAllocatedUsage);
		AreEqual(0, home.Sessions[0].UsagePercent);
		IsTrue(home.DailyTokenTotals.Count > 0);
		// No credit percent series to chart.
		IsTrue(home.DailyUsageTotals.All(d => !d.HasSnapshot));

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshHomeAllocatesSessionUsagePercentByTokenShare()
	{
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":270,"cached_prompt_tokens":0,"completion_tokens":30,"reasoning_tokens":0}}
			{"ts":"2026-08-09T12:01:00.000Z","sid":"s2","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":90,"cached_prompt_tokens":0,"completion_tokens":10,"reasoning_tokens":0}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":40,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\proj" }, "generated_title": "Heavy", "current_model_id": "grok-4.5", "num_messages": 4 }
								""");
		fixture.WriteSession("s2", """
								{ "info": { "id": "s2", "cwd": "C:\\proj" }, "generated_title": "Light", "current_model_id": "grok-4.5", "num_messages": 2 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		bus.GrokUsage.RefreshHome(home.Id);

		AreEqual(40, home.UsagePercent);
		AreEqual(400, home.GrandTotalTokens);
		AreEqual(2, home.Sessions.Count);
		var heavy = home.Sessions.First(x => x.SessionId == "s1");
		var light = home.Sessions.First(x => x.SessionId == "s2");
		AreEqual(30, heavy.UsagePercent);
		AreEqual(10, light.UsagePercent);
		AreEqual(home.UsagePercent, heavy.UsagePercent + light.UsagePercent);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void HasCreditUsagePercentDetectsNullablePercentOnly()
	{
		var withPercent = new BillingSnapshot
		{
			Timestamp = DateTimeOffset.UtcNow,
			UsagePercent = 0,
			SubscriptionTier = "SuperGrok"
		};
		var businessOnly = new BillingSnapshot
		{
			Timestamp = DateTimeOffset.UtcNow,
			SubscriptionTier = "Business"
		};
		var empty = new BillingSnapshot();

		IsTrue(GrokUsageProcessor.HasCreditUsagePercent([withPercent], withPercent, withPercent));
		IsFalse(GrokUsageProcessor.HasCreditUsagePercent([businessOnly], businessOnly, businessOnly));
		IsFalse(GrokUsageProcessor.HasCreditUsagePercent([], empty, empty));
		IsTrue(GrokUsageProcessor.HasCreditUsagePercent([withPercent], businessOnly, businessOnly));
	}

	[TestMethod]
	public void SessionWithSummaryOnlyIsOmittedFromPeriodScopedView()
	{
		using var fixture = new GrokHomeFixture();
		// No unified log inference lines — summary-only session has no period activity.
		fixture.WriteSession("empty-tokens", """
										{ "info": { "id": "empty-tokens", "cwd": "C:\\proj" }, "generated_title": "No inferences", "current_model_id": "grok-4.5", "num_messages": 2 }
										""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		bus.GrokUsage.RefreshHome(home.Id);

		AreEqual(0, home.Sessions.Count);
		AreEqual(0, home.GrandTotalTokens);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshHomeScopesTotalsToCurrentBillingPeriod()
	{
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		// Prior week + current week inferences; totals must use current period only.
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-07-28T12:00:00.000Z","sid":"old","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":1000000,"cached_prompt_tokens":0,"completion_tokens":1000,"reasoning_tokens":0}}
			{"ts":"2026-08-05T12:00:00.000Z","sid":"cur","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":10,"completion_tokens":20,"reasoning_tokens":5}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":12.5,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("old", """
								{ "info": { "id": "old", "cwd": "C:\\proj" }, "generated_title": "Old week", "current_model_id": "grok-4.5", "num_messages": 1 }
								""");
		fixture.WriteSession("cur", """
								{ "info": { "id": "cur", "cwd": "C:\\proj" }, "generated_title": "Current week", "current_model_id": "grok-4.5", "num_messages": 2 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		bus.GrokUsage.RefreshHome(home.Id);

		AreEqual(100, home.GrandTotalPromptTokens);
		AreEqual(20, home.GrandTotalCompletionTokens);
		AreEqual(120, home.GrandTotalTokens);
		AreEqual(1, home.Sessions.Count);
		AreEqual("Current week", home.Sessions[0].Title);
		IsTrue(home.AvailablePeriods.Count >= 1);
		IsTrue(home.AvailablePeriods.Any(x => x.IsCurrent));
		var periodStart = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		var periodEnd = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
		AreEqual(
			GrokUsageAnalytics.GetDayBucketCount(periodStart, periodEnd),
			home.DailyTokenTotals.Count);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshHomeExcludesLocalModelInferencesFromTotals()
	{
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		// Same session: local qwen turn + grok turn; only grok tokens should count.
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-05T12:00:00.000Z","sid":"mix","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":999000,"cached_prompt_tokens":0,"completion_tokens":1000,"reasoning_tokens":0}}
			{"ts":"2026-08-05T13:00:00.000Z","sid":"mix","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":10,"completion_tokens":20,"reasoning_tokens":5}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":12.5,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("mix", """
								{ "info": { "id": "mix", "cwd": "C:\\proj" }, "generated_title": "Mixed models", "current_model_id": "qwen/qwen3.6-35b-a3b", "num_messages": 2 }
								""",
			"""
			{"ts":"2026-08-05T11:59:00.000Z","type":"turn_started","model_id":"qwen/qwen3.6-35b-a3b"}
			{"ts":"2026-08-05T12:59:00.000Z","type":"turn_started","model_id":"grok-4.5"}
			""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		bus.GrokUsage.RefreshHome(home.Id);

		AreEqual(100, home.GrandTotalPromptTokens);
		AreEqual(20, home.GrandTotalCompletionTokens);
		AreEqual(120, home.GrandTotalTokens);
		AreEqual(1, home.Sessions.Count);
		AreEqual("grok-4.5", home.Sessions[0].CurrentModelId);
		AreEqual(1, home.Sessions[0].InferenceCount);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void SelectPeriodFiltersToPreviousWeek()
	{
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-07-29T12:00:00.000Z","sid":"old","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":500,"cached_prompt_tokens":0,"completion_tokens":50,"reasoning_tokens":0}}
			{"ts":"2026-08-05T12:00:00.000Z","sid":"cur","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":0,"completion_tokens":20,"reasoning_tokens":0}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":12.5,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("old", """
								{ "info": { "id": "old", "cwd": "C:\\proj" }, "generated_title": "Old week", "current_model_id": "grok-4.5", "num_messages": 1 }
								""");
		fixture.WriteSession("cur", """
								{ "info": { "id": "cur", "cwd": "C:\\proj" }, "generated_title": "Current week", "current_model_id": "grok-4.5", "num_messages": 1 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		bus.GrokUsage.RefreshHome(home.Id);

		AreEqual(100, home.GrandTotalPromptTokens);

		var prevStart = new DateTimeOffset(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);
		var prevEnd = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
		bus.GrokUsage.SelectPeriod(home.Id, prevStart, prevEnd);

		AreEqual(500, home.GrandTotalPromptTokens);
		AreEqual(50, home.GrandTotalCompletionTokens);
		AreEqual(550, home.GrandTotalTokens);
		AreEqual(1, home.Sessions.Count);
		AreEqual("Old week", home.Sessions[0].Title);
		AreEqual(prevStart, home.SelectedPeriodStart);
		AreEqual(prevEnd, home.SelectedPeriodEnd);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void RefreshMissingHomeSetsError()
	{
		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var missingPath = Path.Combine(Path.GetTempPath(), "GrokUsageMissing_" + Guid.NewGuid().ToString("N"));
		var home = new GrokHomeUsageState
		{
			DisplayName = "Missing",
			Path = missingPath
		};
		state.GrokUsage.Homes.Add(home);

		bus.GrokUsage.RefreshHome(home.Id);

		IsFalse(home.HomeExists);
		IsFalse(string.IsNullOrEmpty(home.ErrorText));
		AreEqual(0, home.Sessions.Count);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void SelectHomeUpdatesSelectedHomeId()
	{
		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var a = new GrokHomeUsageState { DisplayName = "A", Path = @"C:\a" };
		var b = new GrokHomeUsageState { DisplayName = "B", Path = @"C:\b" };
		state.GrokUsage.Homes.Add(a);
		state.GrokUsage.Homes.Add(b);
		state.GrokUsage.SelectedHomeId = a.Id;

		bus.GrokUsage.SelectHome(b.Id);
		AreEqual(b.Id, state.GrokUsage.SelectedHomeId);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void SetViewAsOfReprojectsNonSelectedHomeWhenCached()
	{
		// Regression: scrub used to refresh only SelectedHomeId, so a second tab's slider
		// looked dead until Refresh (tab switch never called SelectHome either).
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixtureA = new GrokHomeFixture();
		using var fixtureB = new GrokHomeFixture();
		const string log = """
			{"ts":"2026-08-05T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":0,"completion_tokens":20,"reasoning_tokens":0,"model":"grok-4.5"}}
			{"ts":"2026-08-08T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":400,"cached_prompt_tokens":0,"completion_tokens":80,"reasoning_tokens":0,"model":"grok-4.5"}}
			{"ts":"2026-08-09T12:00:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":40,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			""";
		fixtureA.WriteUnifiedLog(log);
		fixtureB.WriteUnifiedLog(log);
		fixtureA.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\a" }, "generated_title": "A", "current_model_id": "grok-4.5", "num_messages": 2 }
								""");
		fixtureB.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\b" }, "generated_title": "B", "current_model_id": "grok-4.5", "num_messages": 2 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var homeA = new GrokHomeUsageState { DisplayName = "A", Path = fixtureA.Root, HomeExists = true };
		var homeB = new GrokHomeUsageState { DisplayName = "B", Path = fixtureB.Root, HomeExists = true };
		state.GrokUsage.Homes.Add(homeA);
		state.GrokUsage.Homes.Add(homeB);
		// Selected stays on A while B is the "second tab" user is scrubbing.
		state.GrokUsage.SelectedHomeId = homeA.Id;

		bus.GrokUsage.RefreshHome(homeA.Id);
		bus.GrokUsage.RefreshHome(homeB.Id);
		var liveB = homeB.GrandTotalTokens;
		IsTrue(liveB >= 500);

		bus.GrokUsage.SetViewAsOf(new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero));
		// Both cached homes reproject — not only the selected one.
		AreEqual(120, homeA.GrandTotalTokens);
		AreEqual(120, homeB.GrandTotalTokens);
		IsTrue(homeB.GrandTotalTokens < liveB);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void SetViewAsOfReprojectsLowerTotalsThanLiveEnd()
	{
		// Live end of the open period (after both inferences).
		SetTime(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

		using var fixture = new GrokHomeFixture();
		// Two inferences in the same weekly period; scrub before the second.
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-05T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":0,"completion_tokens":20,"reasoning_tokens":0,"model":"grok-4.5"}}
			{"ts":"2026-08-08T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":400,"cached_prompt_tokens":0,"completion_tokens":80,"reasoning_tokens":0,"model":"grok-4.5"}}
			{"ts":"2026-08-09T12:00:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":40,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			{"ts":"2026-08-05T12:05:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":10,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("s1", """
								{ "info": { "id": "s1", "cwd": "C:\\proj" }, "generated_title": "Scrub session", "current_model_id": "grok-4.5", "num_messages": 4 }
								""");

		var (bus, state, processor) = CreateProcessor();
		processor.InitializeLifecycle();

		var home = new GrokHomeUsageState
		{
			DisplayName = "Fixture",
			Path = fixture.Root,
			HomeExists = true
		};
		state.GrokUsage.Homes.Add(home);
		state.GrokUsage.SelectedHomeId = home.Id;

		bus.GrokUsage.RefreshHome(home.Id);
		var liveTokens = home.GrandTotalTokens;
		IsTrue(liveTokens >= 500);

		// Mid-period, after first inference only.
		bus.GrokUsage.SetViewAsOf(new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero));
		IsFalse(state.GrokUsage.IsViewLive);
		IsTrue(home.GrandTotalTokens < liveTokens);
		AreEqual(120, home.GrandTotalTokens);
		AreEqual(10, home.UsagePercent);

		bus.GrokUsage.SetViewLive();
		IsTrue(state.GrokUsage.IsViewLive);
		AreEqual(liveTokens, home.GrandTotalTokens);

		processor.UninitializeLifecycle();
	}

	[TestMethod]
	public void DefaultPathsAndDisplayNamesResolveFromFolderNames()
	{
		var personal = GrokPaths.GetDefaultPersonalHome();
		var work = GrokPaths.GetDefaultWorkHome();
		IsFalse(string.IsNullOrWhiteSpace(personal));
		IsFalse(string.IsNullOrWhiteSpace(work));
		IsTrue(personal.EndsWith(".grok") || personal.Contains(".grok"));
		IsTrue(work.EndsWith(".grok-work") || work.Contains("grok-work") || work.Contains(".grok-work"));

		AreEqual("grok", GrokPaths.GetDisplayNameFromPath(personal));
		AreEqual("grok-work", GrokPaths.GetDisplayNameFromPath(work));
		AreEqual("grok-client", GrokPaths.GetDisplayNameFromPath(@"C:\Users\Ada\.grok-client"));
		IsTrue(GrokPaths.IsPrimaryHomeDisplayName("grok"));
		IsFalse(GrokPaths.IsPrimaryHomeDisplayName("grok-work"));
	}

	[TestMethod]
	public void DiscoverHomesFindsGrokFoldersUnderProfileRoot()
	{
		var root = Path.Combine(Path.GetTempPath(), "GrokDiscover_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		try
		{
			Directory.CreateDirectory(Path.Combine(root, ".grok-work"));
			Directory.CreateDirectory(Path.Combine(root, ".grok"));
			Directory.CreateDirectory(Path.Combine(root, ".other"));
			Directory.CreateDirectory(Path.Combine(root, ".grok-client"));

			var homes = GrokPaths.DiscoverHomes(root);
			AreEqual(3, homes.Count);
			// Primary "grok" first, then alphabetical.
			AreEqual("grok", homes[0].DisplayName);
			AreEqual("grok-client", homes[1].DisplayName);
			AreEqual("grok-work", homes[2].DisplayName);
			IsTrue(Directory.Exists(homes[0].Path));
		}
		finally
		{
			try
			{
				Directory.Delete(root, true);
			}
			catch
			{
				// Best-effort cleanup.
			}
		}
	}

	private (AppBus Bus, AppState State, GrokUsageProcessor Processor) CreateProcessor()
	{
		// Wire Keystone manually so processor tests do not pull Avalonia / host-only DI graph.
		var channel = new GrokUsageChannel();
		var bus = new AppBus(channel);
		var usage = new GrokUsageState();
		var state = new AppState(new AppSettings(), usage);
		var processor = new GrokUsageProcessor(bus, state, this);
		return (bus, state, processor);
	}

	#endregion

	#region Classes

	private sealed class GrokHomeFixture : IDisposable
	{
		#region Constructors

		public GrokHomeFixture()
		{
			Root = Path.Combine(Path.GetTempPath(), "GrokUsageProc_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Root);
			Directory.CreateDirectory(Path.Combine(Root, "logs"));
			Directory.CreateDirectory(Path.Combine(Root, "sessions"));
		}

		#endregion

		#region Properties

		public string Root { get; }

		#endregion

		#region Methods

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(Root))
				{
					Directory.Delete(Root, true);
				}
			}
			catch
			{
				// best-effort cleanup
			}
		}

		public void WriteSession(string sessionId, string summaryJson, string eventsJsonl = null, string cwd = @"C:\proj")
		{
			var encodedCwd = Uri.EscapeDataString(cwd);
			var sessionDir = Path.Combine(Root, "sessions", encodedCwd, sessionId);
			Directory.CreateDirectory(sessionDir);
			File.WriteAllText(Path.Combine(sessionDir, "summary.json"), summaryJson.Trim(), Encoding.UTF8);
			if (eventsJsonl is not null)
			{
				File.WriteAllText(Path.Combine(sessionDir, "events.jsonl"), eventsJsonl.Trim() + Environment.NewLine, Encoding.UTF8);
			}
		}

		public void WriteUnifiedLog(string content)
		{
			var path = Path.Combine(Root, "logs", "unified.jsonl");
			File.WriteAllText(path, content.Trim() + Environment.NewLine, Encoding.UTF8);
		}

		#endregion
	}

	#endregion
}
