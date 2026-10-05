#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncClientProtocolTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void AppliedIssuesKeepSessionOpen()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			client.BeginSync(sessionId, settings);
			client.SyncSettings.AddFilter<AddressEntity>(incomingFilter: x => false);

			var outgoing = NewClient("Other", provider);
			outgoing.BeginSync(sessionId, NewSettings());
			var changes = outgoing.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);

			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				Changes = new ServiceRequest<SyncObject>(changes.Collection)
			});

			AreEqual(1, result.AppliedIssues.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.AppliedIssues.Collection[0].IssueType);
			IsFalse(result.SessionEnded);
		});
	}

	[TestMethod]
	public void CustomGetCorrectionsReturnedOnIssuesPath()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var client = new CorrectionSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			var changes = client.GetChanges(sessionId, NewRequest());
			client.Correction = changes.Collection[0];
			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = NewSettings(),
				Issues = new ServiceRequest<SyncIssue>(new SyncIssue { Id = changes.Collection[0].SyncId, TypeName = changes.Collection[0].TypeName })
			});
			AreEqual(1, result.Corrections.Collection.Count);
			AreEqual(changes.Collection[0].SyncId, result.Corrections.Collection[0].SyncId);
			AreEqual(0, result.Changes.Collection.Count);
		});
	}

	[TestMethod]
	public void EndSessionEndsWhileMoreChangesRemain()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "One");
			AddAddress(database, "Two");
			AddAddress(database, "Three");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.ItemsPerSyncRequest = 1;
			client.BeginSync(sessionId, settings);

			var paged = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				GetChangesSkip = 0
			});
			IsFalse(paged.SessionEnded);
			IsTrue(paged.Changes.HasMore);

			var ended = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				GetChangesSkip = 0,
				EndSession = true
			});
			IsTrue(ended.SessionEnded);
		});
	}

	[TestMethod]
	public void EndSyncRejectsWrongSessionThenAllowsBeginAfterEnd()
	{
		WithEachProvider((provider, _) =>
		{
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			ExpectedException<InvalidOperationException>(() => client.EndSync(Guid.NewGuid()), "The sync session ID is invalid.");
			client.EndSync(sessionId);
			client.BeginSync(Guid.NewGuid(), NewSettings());
		});
	}

	[TestMethod]
	public void GetChangesSkipCrossesRepositories()
	{
		WithEachProvider((provider, database) =>
		{
			AddCustomer(database, "Cust");
			AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			var first = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = DateTime.MaxValue.Subtract(TimeSpan.FromDays(1)),
				Take = 1
			});
			AreEqual(1, first.Collection.Count);
			var second = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = DateTime.MaxValue.Subtract(TimeSpan.FromDays(1)),
				Skip = first.Collection.Count,
				Take = 1
			});
			AreEqual(1, second.Collection.Count);
			AreNotEqual(first.Collection[0].SyncId, second.Collection[0].SyncId);
		});
	}

	[TestMethod]
	public void GetChangesTakeZeroUsesItemsPerSyncRequest()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "One");
			AddAddress(database, "Two");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.ItemsPerSyncRequest = 1;
			client.BeginSync(sessionId, settings);
			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = DateTime.MaxValue.Subtract(TimeSpan.FromDays(1)),
				Take = 0
			});
			AreEqual(1, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void GetChangesUntilIsSessionStartNotNow()
	{
		WithEachProvider((provider, database) =>
		{
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			client.BeginSync(sessionId, settings);
			IncrementTime(minutes: 5);
			AddAddress(database, "Late");

			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings
			});

			AreEqual(0, result.Changes?.Collection?.Count ?? 0);
			IsFalse(result.SessionEnded);
		});
	}

	[TestMethod]
	public void ClientHasNoChangesEndsPullOnLastPage()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "One");
			AddAddress(database, "Two");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.SyncDirection = SyncDirection.PullDownThenPushUp;
			settings.ItemsPerSyncRequest = 1;
			client.BeginSync(sessionId, settings);

			var first = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				GetChangesSkip = 0,
				ClientHasNoChanges = true
			});
			IsFalse(first.SessionEnded);
			IsTrue(first.Changes.HasMore);

			var last = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				GetChangesSkip = 1,
				ClientHasNoChanges = true
			});
			IsTrue(last.SessionEnded);
			IsFalse(last.Changes.HasMore);
			AreEqual(2, last.Statistics.Changes);
		});
	}

	[TestMethod]
	public void EndSessionDoesNotGetChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "One");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.SyncDirection = SyncDirection.PullDownThenPushUp;
			client.BeginSync(sessionId, settings);

			var ended = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				EndSession = true
			});

			IsTrue(ended.SessionEnded);
			IsTrue((ended.Changes == null) || (ended.Changes.Collection.Count == 0));
			AreEqual(0, ended.Statistics.Changes);
		});
	}

	[TestMethod]
	public void PullDownOnlyEndsOnLastPage()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "One");
			AddAddress(database, "Two");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.SyncDirection = SyncDirection.PullDown;
			settings.ItemsPerSyncRequest = 1;
			client.BeginSync(sessionId, settings);

			var first = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				GetChangesSkip = 0
			});
			IsFalse(first.SessionEnded);
			IsTrue(first.Changes.HasMore);
			AreEqual(1, first.Statistics.Changes);

			var last = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				GetChangesSkip = 1
			});
			IsTrue(last.SessionEnded);
			IsFalse(last.Changes.HasMore);
			AreEqual(2, last.Statistics.Changes);
		});
	}

	[TestMethod]
	public void IdleRoundTripEndsSession()
	{
		WithEachProvider((provider, _) =>
		{
			var client = NewClient("Client", provider);
			var result = client.Sync(new SyncOperation
			{
				SessionId = Guid.NewGuid(),
				Settings = NewSettings(),
				EndSession = true
			});
			IsTrue(result.SessionEnded);
			IsNotNull(result.SessionStart);
		});
	}

	[TestMethod]
	public void IssuesPathAppliesCorrectionsNotChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);

			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = NewSettings(),
				Changes = new ServiceRequest<SyncObject>(changes.Collection),
				Issues = new ServiceRequest<SyncIssue>(new SyncIssue { Id = changes.Collection[0].SyncId, TypeName = changes.Collection[0].TypeName })
			});

			AreEqual(0, result.AppliedIssues.Collection.Count);
			AreEqual(0, result.Corrections.Collection.Count);
			AreEqual(0, result.Changes.Collection.Count);
		});
	}

	[TestMethod]
	public void IssuesPathReturnsEmptyChangesAndEmptyCorrections()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());

			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = NewSettings(),
				Issues = new ServiceRequest<SyncIssue>(new SyncIssue
				{
					Id = Guid.NewGuid(),
					IssueType = SyncIssueType.RelationshipConstraint,
					TypeName = typeof(Address).ToAssemblyName()
				})
			});

			AreEqual(0, result.Corrections.Collection.Count);
			AreEqual(0, result.Changes.Collection.Count);
		});
	}

	[TestMethod]
	public void PushUpOnlyDoesNotReturnServerChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.SyncDirection = SyncDirection.PushUp;
			client.BeginSync(sessionId, settings);

			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				EndSession = true
			});

			IsTrue((result.Changes == null) || (result.Changes.Collection.Count == 0));
			IsTrue(result.SessionEnded);
		});
	}

	[TestMethod]
	public void ResumeStatisticsRestoreOnFirstCall()
	{
		WithEachProvider((provider, _) =>
		{
			var statistics = new SyncStatistics { Changes = 7, AppliedChanges = 3 };
			var client = new SampleSyncClient("Client", provider, this, statistics, new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = NewSettings(),
				ResumeStatistics = new SyncStatistics { Changes = 11, AppliedChanges = 5 },
				EndSession = true
			});

			AreEqual(11, result.Statistics.Changes);
			AreEqual(5, result.Statistics.AppliedChanges);
			IsTrue(result.SessionEnded);
		});
	}

	[TestMethod]
	public void ServerBeginSyncRaisesZeroItemsPerRequestToOne()
	{
		WithEachProvider((provider, _) =>
		{
			var server = new SampleServerSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			var settings = NewSettings();
			settings.ItemsPerSyncRequest = 0;
			server.BeginSync(Guid.NewGuid(), settings);
			AreEqual(1, settings.ItemsPerSyncRequest);
		});
	}

	[TestMethod]
	public void SyncNullOperationThrows()
	{
		WithEachProvider((provider, _) =>
		{
			var client = NewClient("Client", provider);
			ExpectedException<ArgumentNullException>(() => client.Sync(null));
		});
	}

	[TestMethod]
	public void SyncWithNullSettingsUsesBoundSessionSettings()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.SyncDirection = SyncDirection.PushUp;
			client.BeginSync(sessionId, settings);
			var result = client.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = null,
				EndSession = true
			});
			IsTrue(result.SessionEnded);
			IsTrue((result.Changes == null) || (result.Changes.Collection.Count == 0));
		});
	}

	private SampleSyncClient NewClient(string name, ISyncableDatabaseProvider provider)
	{
		return new SampleSyncClient(name, provider, this, new SyncStatistics(), new Profiler(name));
	}

	private static SyncRequest NewRequest()
	{
		return new SyncRequest
		{
			Since = DateTime.MinValue,
			Until = DateTime.MaxValue.Subtract(TimeSpan.FromDays(1)),
			Take = 100
		};
	}

	private static SyncSettings NewSettings()
	{
		var settings = new SyncSettings { IncludeIssueDetails = true };
		settings.AddFilter<AddressEntity>();
		settings.AddFilter<AccountEntity>();
		settings.AddFilter<CustomerEntity>();
		settings.AddFilter<BookmarkEntity>();
		settings.AddFilter<SettingEntity>();
		return settings;
	}

	#endregion

	#region Classes

	private sealed class CorrectionSyncClient : SampleSyncClient
	{
		#region Constructors

		public CorrectionSyncClient(
			string name,
			ISyncableDatabaseProvider provider,
			IDateTimeProvider time,
			SyncStatistics statistics,
			Profiler profiler
		) : base(name, provider, time, statistics, profiler)
		{
			Correction = null;
		}

		#endregion

		#region Properties

		public SyncObject Correction { get; set; }

		#endregion

		#region Methods

		protected internal override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
		{
			ValidateSession(sessionId);
			return new ServiceResult<SyncObject> { Collection = Correction == null ? [] : [Correction] };
		}

		#endregion
	}

	#endregion
}