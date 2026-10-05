#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncIssueTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConvertChangesTypeName()
	{
		var issue = new SyncIssue
		{
			Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
			IssueType = SyncIssueType.UpdateException,
			Message = "failed",
			TypeName = "Old.Type"
		};
		var converted = issue.Convert("New.Type");
		AreEqual(issue.Id, converted.Id);
		AreEqual(issue.IssueType, converted.IssueType);
		AreEqual(issue.Message, converted.Message);
		AreEqual("New.Type", converted.TypeName);
		AreEqual("Old.Type", issue.TypeName);
	}

	[TestMethod]
	public void ExceptionConstructors()
	{
		var inner = new InvalidOperationException("inner");
		var sync = new SyncException("outer", inner);
		AreEqual("outer", sync.Message);
		AreEqual(inner, sync.InnerException);

		var child = new SyncIssue { Message = "child" };
		var issue = new SyncIssueException(SyncIssueType.ValidationException, "bad", inner, child);
		AreEqual(SyncIssueType.ValidationException, issue.IssueType);
		AreEqual("bad", issue.Message);
		AreEqual(1, issue.Issues.Count());

		var update = new SyncUpdateException("u", inner);
		AreEqual("u", update.Message);
		AreEqual(inner, update.InnerException);
	}

	[TestMethod]
	public void StatisticsResetAndToString()
	{
		var statistics = new SyncStatistics
		{
			Changes = 1,
			Corrections = 2,
			AppliedChanges = 3,
			AppliedCorrections = 4,
			IndividualProcessCount = 5
		};
		IsFalse(statistics.IsReset);
		AreEqual("C1,C+2,A3,A+4,I5", statistics.ToString());
		statistics.Reset();
		IsTrue(statistics.IsReset);
		var copy = new SyncStatistics();
		copy.UpdateWith(new SyncStatistics { Changes = 9 }, IncludeExcludeSettings.Empty);
		AreEqual(9, copy.Changes);
	}

	[TestMethod]
	public void SyncDirectionCombinesPullAndPush()
	{
		IsTrue(SyncDirection.PullDownThenPushUp.HasFlag(SyncDirection.PullDown));
		IsTrue(SyncDirection.PullDownThenPushUp.HasFlag(SyncDirection.PushUp));
		IsFalse(SyncDirection.PullDown.HasFlag(SyncDirection.PushUp));
	}

	[TestMethod]
	public void SyncTimerToString()
	{
		var timer = new SyncTimer
		{
			SuccessfulSyncs = 2,
			CancelledSyncs = 3,
			FailedSyncs = 4
		};
		AreEqual("S2,C3,F4", timer.ToString());
	}

	[TestMethod]
	public void SyncTimesConstructor()
	{
		var client = UtcNow;
		var server = UtcNow.AddMinutes(1);
		var times = new SyncTimes("Full", client, server);
		AreEqual("Full", times.SyncType);
		AreEqual(client, times.LastSyncedOnClient);
		AreEqual(server, times.LastSyncedOnServer);
	}

	[TestMethod]
	public void ToStringAndDetailedString()
	{
		var issue = new SyncIssue
		{
			IssueType = SyncIssueType.RelationshipConstraint,
			TypeName = "T",
			Message = "m"
		};
		AreEqual("RelationshipConstraint : T - m", issue.ToString());
		AreEqual(string.Empty, new List<SyncIssue>().ToDetailedString());
		AreEqual("T: RelationshipConstraint m", new List<SyncIssue> { issue }.ToDetailedString());
	}

	#endregion
}