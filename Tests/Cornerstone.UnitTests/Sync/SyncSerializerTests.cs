#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Sample.Models;
using Cornerstone.Serialization;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncSerializerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SyncOperationAndResultRoundTripThroughJson()
	{
		var when = new DateTime(2026, 3, 1, 12, 30, 0, DateTimeKind.Utc);
		var sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
		var syncId = Guid.Parse("11111111-2222-3333-4444-555555555555");
		var issueId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
		var operation = new SyncOperation
		{
			Changes = new ServiceRequest<SyncObject>(new SyncObject
			{
				Data = [1, 2, 3, 4],
				ModifiedOn = when,
				Status = SyncObjectStatus.Updated,
				SyncId = syncId,
				TypeName = typeof(Address).ToAssemblyName()
			}),
			ClientHasNoChanges = true,
			EndSession = true,
			GetChangesSkip = 3,
			Issues = new ServiceRequest<SyncIssue>(new SyncIssue
			{
				Id = issueId,
				IssueType = SyncIssueType.UpdateException,
				Message = "nope",
				TypeName = "T"
			}),
			ResumeStatistics = new SyncStatistics
			{
				AppliedChanges = 4,
				Changes = 9
			},
			SessionId = sessionId,
			Settings = new SyncSettings
			{
				IncludeIssueDetails = true,
				ItemsPerSyncRequest = 25,
				LastSyncedOnClient = when,
				LastSyncedOnServer = when.AddMinutes(5),
				PermanentDeletions = true,
				SyncDirection = SyncDirection.PushUp,
				SyncType = "SyncAll",
				Values = new Dictionary<string, string>
				{
					["Device"] = "Phone"
				}
			}
		};
		operation.Settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");

		var operationJson = operation.ToJson();
		var restoredOperation = operationJson.FromJson<SyncOperation>();

		AreEqual(sessionId, restoredOperation.SessionId);
		IsTrue(restoredOperation.EndSession);
		IsTrue(restoredOperation.ClientHasNoChanges);
		AreEqual(3, restoredOperation.GetChangesSkip);
		AreEqual(4, restoredOperation.ResumeStatistics.AppliedChanges);
		AreEqual(9, restoredOperation.ResumeStatistics.Changes);
		AreEqual(25, restoredOperation.Settings.ItemsPerSyncRequest);
		IsTrue(restoredOperation.Settings.IncludeIssueDetails);
		IsTrue(restoredOperation.Settings.PermanentDeletions);
		AreEqual(SyncDirection.PushUp, restoredOperation.Settings.SyncDirection);
		AreEqual("SyncAll", restoredOperation.Settings.SyncType);
		AreEqual(when, restoredOperation.Settings.LastSyncedOnClient);
		AreEqual(when.AddMinutes(5), restoredOperation.Settings.LastSyncedOnServer);
		// DictionaryKeyPolicy writes dictionary keys in camel case.
		AreEqual("Phone", restoredOperation.Settings.Values["device"]);
		IsFalse(restoredOperation.Settings.HasFilters);
		AreEqual(1, restoredOperation.Changes.Collection.Count);
		AreEqual(syncId, restoredOperation.Changes.Collection[0].SyncId);
		AreEqual(SyncObjectStatus.Updated, restoredOperation.Changes.Collection[0].Status);
		AreEqual(when, restoredOperation.Changes.Collection[0].ModifiedOn);
		AreEqual(typeof(Address).ToAssemblyName(), restoredOperation.Changes.Collection[0].TypeName);
		CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, restoredOperation.Changes.Collection[0].Data);
		AreEqual(issueId, restoredOperation.Issues.Collection[0].Id);
		AreEqual(SyncIssueType.UpdateException, restoredOperation.Issues.Collection[0].IssueType);
		AreEqual("nope", restoredOperation.Issues.Collection[0].Message);

		var result = new SyncOperationResult
		{
			AppliedIssues = new ServiceResult<SyncIssue>(new SyncIssue
			{
				Id = issueId,
				IssueType = SyncIssueType.SyncEntityFiltered,
				Message = "filtered",
				TypeName = "Address"
			}),
			Changes = new ServiceResult<SyncObject>(operation.Changes.Collection.ToList())
			{
				Skipped = 2,
				TotalCount = 5
			},
			Corrections = new ServiceResult<SyncObject>(new SyncObject
			{
				Data = [9],
				ModifiedOn = when,
				Status = SyncObjectStatus.Added,
				SyncId = syncId,
				TypeName = "Fixed"
			})
			{
				TotalCount = 1
			},
			SessionEnded = true,
			SessionStart = new SyncSessionStart
			{
				Id = sessionId,
				StartedOn = when
			},
			Statistics = new SyncStatistics
			{
				IndividualProcessCount = 2
			}
		};

		var restoredResult = result.ToJson().FromJson<SyncOperationResult>();

		IsTrue(restoredResult.SessionEnded);
		AreEqual(sessionId, restoredResult.SessionStart.Id);
		AreEqual(when, restoredResult.SessionStart.StartedOn);
		AreEqual(2, restoredResult.Statistics.IndividualProcessCount);
		AreEqual(SyncIssueType.SyncEntityFiltered, restoredResult.AppliedIssues.Collection[0].IssueType);
		AreEqual("filtered", restoredResult.AppliedIssues.Collection[0].Message);
		AreEqual(2, restoredResult.Changes.Skipped);
		AreEqual(5, restoredResult.Changes.TotalCount);
		IsTrue(restoredResult.Changes.HasMore);
		AreEqual(syncId, restoredResult.Changes.Collection[0].SyncId);
		AreEqual(1, restoredResult.Corrections.TotalCount);
		AreEqual("Fixed", restoredResult.Corrections.Collection[0].TypeName);
		AreEqual(SyncObjectStatus.Added, restoredResult.Corrections.Collection[0].Status);
	}

	#endregion
}
