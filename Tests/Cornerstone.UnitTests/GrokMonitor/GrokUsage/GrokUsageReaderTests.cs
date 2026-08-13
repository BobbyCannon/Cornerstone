#region References

using System;
using System.IO;
using System.Linq;
using System.Text;
using Cornerstone.GrokMonitor.GrokUsage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokUsageReaderTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void FallsBackToCurrentModelId()
	{
		using var fixture = new GrokHomeFixture();
		var sid = "session-fallback-model";
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"session-fallback-model","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":10,"cached_prompt_tokens":0,"completion_tokens":5,"reasoning_tokens":0}}
			"""
		);
		fixture.WriteSession(sid, """
								{
								  "info": { "id": "session-fallback-model", "cwd": "C:\\proj" },
								  "generated_title": "Fallback",
								  "current_model_id": "grok-4.5",
								  "created_at": "2026-08-09T11:00:00.000Z",
								  "updated_at": "2026-08-09T12:00:00.000Z",
								  "num_messages": 2
								}
								""", null);

		var session = new GrokUsageReader(fixture.Root).GetSession(sid);
		IsNotNull(session);
		AreEqual("grok-4.5", session.Inferences.Single().ModelId);
	}

	[TestMethod]
	public void FiltersInferencesBySince()
	{
		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T10:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":1,"completion_tokens":1,"reasoning_tokens":0,"cached_prompt_tokens":0}}
			{"ts":"2026-08-09T12:00:00.000Z","sid":"s1","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":2,"completion_tokens":2,"reasoning_tokens":0,"cached_prompt_tokens":0}}
			"""
		);

		var since = DateTimeOffset.Parse("2026-08-09T11:00:00Z");
		var inferences = new GrokUsageReader(fixture.Root).GetAllInferences(since);
		AreEqual(1, inferences.Count);
		AreEqual(2, inferences[0].PromptTokens);
	}

	[TestMethod]
	public void GetSummaryAggregatesTotals()
	{
		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"a","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":10,"completion_tokens":20,"reasoning_tokens":5}}
			{"ts":"2026-08-09T12:01:00.000Z","sid":"b","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":50,"cached_prompt_tokens":5,"completion_tokens":10,"reasoning_tokens":2}}
			{"ts":"2026-08-09T12:02:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":10.0,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);
		fixture.WriteSession("a", """
								{ "info": { "id": "a", "cwd": "C:\\a" }, "generated_title": "A", "current_model_id": "grok-4.5", "num_messages": 1 }
								""");
		fixture.WriteSession("b", """
								{ "info": { "id": "b", "cwd": "C:\\b" }, "generated_title": "B", "current_model_id": "grok-4.5", "num_messages": 1 }
								""");

		var summary = new GrokUsageReader(fixture.Root).GetSummary();
		AreEqual(2, summary.Sessions.Count);
		AreEqual(150, summary.GrandTotalPromptTokens);
		AreEqual(15, summary.GrandTotalCachedPromptTokens);
		AreEqual(30, summary.GrandTotalCompletionTokens);
		AreEqual(7, summary.GrandTotalReasoningTokens);
		AreEqual(180, summary.GrandTotalTokens);
		IsNotNull(summary.LatestBilling);
		AreEqual(10.0, summary.LatestBilling.UsagePercent);
		AreEqual(1, summary.BillingHistory.Count);
		AreEqual(10.0, summary.BillingHistory[0].UsagePercent);
	}

	[TestMethod]
	public void GetAllBillingSnapshotsReturnsChronologicalHistory()
	{
		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-05T12:00:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":5.0,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}}}}
			{"ts":"2026-08-07T12:00:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":20.0,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T00:00:00Z","end":"2026-08-11T00:00:00Z"}}}}
			"""
		);

		var history = new GrokUsageReader(fixture.Root).GetAllBillingSnapshots();
		AreEqual(2, history.Count);
		AreEqual(5.0, history[0].UsagePercent);
		AreEqual(20.0, history[1].UsagePercent);
		IsTrue(history[0].Timestamp < history[1].Timestamp);
	}

	[TestMethod]
	public void JoinsSessionSummaryAndInferences()
	{
		using var fixture = new GrokHomeFixture();
		var sid = "019fe000-0000-0000-0000-000000000001";
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:00.000Z","sid":"019fe000-0000-0000-0000-000000000001","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":100,"cached_prompt_tokens":50,"completion_tokens":25,"reasoning_tokens":3}}
			{"ts":"2026-08-09T12:01:00.000Z","sid":"019fe000-0000-0000-0000-000000000001","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":200,"cached_prompt_tokens":100,"completion_tokens":40,"reasoning_tokens":7}}
			"""
		);
		fixture.WriteSession(sid, """
								{
								  "info": { "id": "019fe000-0000-0000-0000-000000000001", "cwd": "C:\\Workspaces\\MyApp" },
								  "session_summary": "Old title",
								  "generated_title": "Plan usage reader",
								  "created_at": "2026-08-09T11:00:00.000Z",
								  "updated_at": "2026-08-09T12:01:00.000Z",
								  "num_messages": 42,
								  "current_model_id": "grok-4.5"
								}
								""");

		var session = new GrokUsageReader(fixture.Root).GetSession(sid);
		IsNotNull(session);
		AreEqual("Plan usage reader", session.Info.Title);
		AreEqual(@"C:\Workspaces\MyApp", session.Info.WorkingDirectory);
		AreEqual(42, session.Info.MessageCount);
		AreEqual(2, session.Inferences.Count);
		AreEqual(300, session.TotalPromptTokens);
		AreEqual(150, session.TotalCachedPromptTokens);
		AreEqual(65, session.TotalCompletionTokens);
		AreEqual(10, session.TotalReasoningTokens);
		AreEqual(365, session.TotalTokens);
	}

	[TestMethod]
	public void ParsesBillingSnapshot()
	{
		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T10:00:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":50.0,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T03:37:46.859294+00:00","end":"2026-08-11T03:37:46.859294+00:00"},"onDemandCap":{"val":0},"onDemandUsed":{"val":1},"prepaidBalance":{"val":2},"isUnifiedBillingUser":true},"subscriptionTier":"SuperGrok"}}
			{"ts":"2026-08-09T12:00:00.000Z","msg":"billing: fetched credits config","ctx":{"config":{"creditUsagePercent":95.0,"currentPeriod":{"type":"USAGE_PERIOD_TYPE_WEEKLY","start":"2026-08-04T03:37:46.859294+00:00","end":"2026-08-11T03:37:46.859294+00:00"},"onDemandCap":{"val":0},"onDemandUsed":{"val":0},"prepaidBalance":{"val":0},"isUnifiedBillingUser":true},"subscriptionTier":"SuperGrok Plus"}}
			"""
		);

		var billing = new GrokUsageReader(fixture.Root).GetLatestBillingSnapshot();
		IsNotNull(billing);
		AreEqual(95.0, billing.UsagePercent);
		AreEqual("SuperGrok Plus", billing.SubscriptionTier);
		AreEqual("USAGE_PERIOD_TYPE_WEEKLY", billing.PeriodType);
		AreEqual(true, billing.IsUnifiedBillingUser);
		AreEqual(DateTimeOffset.Parse("2026-08-09T12:00:00.000Z"), billing.Timestamp);
	}

	[TestMethod]
	public void ParsesInferenceDoneLine()
	{
		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T21:22:41.579Z","src":"shell","sid":"019fe866-a0bd-7050-a8a2-424befdaa7bb","msg":"shell.turn.inference_done","ctx":{"loop_index":1,"model_elapsed_ms":4490,"prompt_tokens":16762,"cached_prompt_tokens":5504,"completion_tokens":272,"reasoning_tokens":106,"tokens_per_sec":104.1}}
			"""
		);

		var inferences = new GrokUsageReader(fixture.Root).GetAllInferences();
		AreEqual(1, inferences.Count);
		var item = inferences[0];
		AreEqual("019fe866-a0bd-7050-a8a2-424befdaa7bb", item.SessionId);
		AreEqual(16762, item.PromptTokens);
		AreEqual(5504, item.CachedPromptTokens);
		AreEqual(272, item.CompletionTokens);
		AreEqual(106, item.ReasoningTokens);
		AreEqual(1, item.LoopIndex);
		AreEqual(4490, item.ModelElapsedMs);
		AreEqual(104.1, item.TokensPerSecond);
		AreEqual(DateTimeOffset.Parse("2026-08-09T21:22:41.579Z"), item.Timestamp);
	}

	[TestMethod]
	public void ResolvesGrokHomeFromArgument()
	{
		using var fixture = new GrokHomeFixture();
		var reader = new GrokUsageReader(fixture.Root);
		AreEqual(Path.GetFullPath(fixture.Root), reader.GrokHome);
	}

	[TestMethod]
	public void ResolvesModelIdFromTurnStarted()
	{
		using var fixture = new GrokHomeFixture();
		var sid = "session-model-map";
		fixture.WriteUnifiedLog(
			"""
			{"ts":"2026-08-09T12:00:10.000Z","sid":"session-model-map","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":1,"completion_tokens":1,"reasoning_tokens":0,"cached_prompt_tokens":0}}
			{"ts":"2026-08-09T12:01:10.000Z","sid":"session-model-map","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":2,"completion_tokens":2,"reasoning_tokens":0,"cached_prompt_tokens":0}}
			"""
		);
		fixture.WriteSession(sid, """
								{
								  "info": { "id": "session-model-map", "cwd": "C:\\proj" },
								  "generated_title": "Model map",
								  "current_model_id": "fallback-model",
								  "num_messages": 4
								}
								""",
			"""
			{"ts":"2026-08-09T12:00:00.000Z","type":"turn_started","model_id":"grok-4.5"}
			{"ts":"2026-08-09T12:01:00.000Z","type":"turn_started","model_id":"local"}
			""");

		var session = new GrokUsageReader(fixture.Root).GetSession(sid);
		IsNotNull(session);
		AreEqual(2, session.Inferences.Count);
		AreEqual("grok-4.5", session.Inferences[0].ModelId);
		AreEqual("local", session.Inferences[1].ModelId);
	}

	[TestMethod]
	public void ReturnsEmptyWhenLogMissing()
	{
		using var fixture = new GrokHomeFixture(false);
		var reader = new GrokUsageReader(fixture.Root);
		AreEqual(0, reader.GetAllInferences().Count);
		AreEqual(0, reader.GetSessions().Count);
		IsFalse(reader.GetLatestBillingSnapshot().HasValue);
		AreEqual(0, reader.GetSummary().Sessions.Count);
		IsFalse(reader.GetSummary().LatestBilling.HasValue);
	}

	[TestMethod]
	public void SkipsMalformedJsonLines()
	{
		using var fixture = new GrokHomeFixture();
		fixture.WriteUnifiedLog(
			"""
			not-json
			{"ts":"2026-08-09T12:00:00.000Z","sid":"ok","msg":"shell.turn.inference_done","ctx":{"prompt_tokens":9,"completion_tokens":1,"reasoning_tokens":0,"cached_prompt_tokens":0}}
			{broken
			{"ts":"2026-08-09T12:00:01.000Z","sid":"ok","msg":"other.message","ctx":{}}
			"""
		);

		var inferences = new GrokUsageReader(fixture.Root).GetAllInferences();
		AreEqual(1, inferences.Count);
		AreEqual(9, inferences[0].PromptTokens);
	}

	#endregion

	#region Classes

	private sealed class GrokHomeFixture : IDisposable
	{
		#region Constructors

		public GrokHomeFixture(bool createLogDirectory = true)
		{
			Root = Path.Combine(Path.GetTempPath(), "GrokUsageTests_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Root);
			if (createLogDirectory)
			{
				Directory.CreateDirectory(Path.Combine(Root, "logs"));
			}

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
				// best-effort cleanup for temp fixtures
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
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, content.Trim() + Environment.NewLine, Encoding.UTF8);
		}

		#endregion
	}

	#endregion
}