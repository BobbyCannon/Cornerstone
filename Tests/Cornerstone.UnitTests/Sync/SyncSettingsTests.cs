#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddFilterInstanceAndResetFilters()
	{
		var settings = new SyncSettings();
		settings.AddFilter(new SyncRepositoryFilter<AddressEntity>(x => !x.IsDeleted));
		IsTrue(settings.ShouldSyncRepository(typeof(AddressEntity)));
		IsTrue(settings.HasFilter(typeof(AddressEntity)));
		settings.ResetFilters();
		IsFalse(settings.HasFilters);
		IsFalse(settings.ShouldSyncRepository(typeof(AddressEntity)));
		IsTrue(settings.ShouldExcludeRepository(typeof(AddressEntity)));
		IsFalse(settings.HasFilter(typeof(AddressEntity)));
	}

	[TestMethod]
	public void AddFilterReplacesExisting()
	{
		var settings = new SyncSettings();
		settings.AddFilter<AddressEntity>(outgoingFilter: x => x.Line1 == "A");
		settings.AddFilter<AddressEntity>(outgoingFilter: x => x.Line1 == "B");
		IsTrue(settings.ShouldSyncRepository(typeof(AddressEntity)));
		IsTrue(settings.ShouldSyncRepository(typeof(AddressEntity).ToAssemblyName()));
		IsFalse(settings.ShouldSyncRepository(typeof(AccountEntity)));
		IsTrue(settings.ShouldExcludeRepository(typeof(AccountEntity)));
		IsFalse(settings.ShouldExcludeRepository(typeof(AddressEntity)));
	}

	[TestMethod]
	public void EmptySettingsExcludeEveryRepository()
	{
		var settings = new SyncSettings();
		IsFalse(settings.HasFilters);
		IsFalse(settings.ShouldSyncRepository(typeof(AddressEntity)));
		IsFalse(settings.ShouldSyncRepository(typeof(AddressEntity).ToAssemblyName()));
		IsTrue(settings.ShouldExcludeRepository(typeof(AddressEntity)));
		IsTrue(settings.ShouldExcludeRepository((string) null));
		IsFalse(settings.HasFilter(typeof(AddressEntity)));
	}

	[TestMethod]
	public void IncomingFilterWrongTypeIsNotFiltered()
	{
		var filter = new SyncRepositoryFilter<AddressEntity>(incomingFilter: x => x.Line1 == "Keep");
		IsFalse(filter.ShouldFilterIncomingEntity(new AccountEntity()));
		IsFalse(filter.ShouldFilterIncomingEntity(new AddressEntity { Line1 = "Keep" }));
		IsTrue(filter.ShouldFilterIncomingEntity(new AddressEntity { Line1 = "Drop" }));
	}

	[TestMethod]
	public void ScopeFilterIsApplyKeepTestWithoutIncoming()
	{
		var filter = new SyncRepositoryFilter<AddressEntity>(scopeFilter: x => x.State == "SC");
		IsTrue(filter.HasScopeFilter);
		IsTrue(filter.HasApplyKeepTest);
		IsFalse(filter.HasIncomingFilter);
		IsFalse(filter.ShouldFilterIncomingEntity(new AddressEntity { State = "SC" }));
		IsTrue(filter.ShouldFilterIncomingEntity(new AddressEntity { State = "GA" }));
		IsFalse(filter.ShouldFilterIncomingEntity(new AccountEntity { Name = "John" }));
	}

	[TestMethod]
	public void ScopeFilterAndIncomingBothMustPass()
	{
		var filter = new SyncRepositoryFilter<AddressEntity>(
			incomingFilter: x => x.Line1 == "Keep",
			scopeFilter: x => x.State == "SC"
		);
		IsFalse(filter.ShouldFilterIncomingEntity(new AddressEntity { Line1 = "Keep", State = "SC" }));
		IsTrue(filter.ShouldFilterIncomingEntity(new AddressEntity { Line1 = "Keep", State = "GA" }));
		IsTrue(filter.ShouldFilterIncomingEntity(new AddressEntity { Line1 = "Drop", State = "SC" }));
	}

	[TestMethod]
	public void ResetDefaults()
	{
		var settings = new SyncSettings
		{
			ItemsPerSyncRequest = 1,
			PermanentDeletions = true,
			LastSyncedOnClient = UtcNow,
			LastSyncedOnServer = UtcNow,
			SyncDirection = SyncDirection.PushUp
		};
		settings.Values["k"] = "v";
		settings.AddFilter<AddressEntity>();
		settings.Reset();

		AreEqual(10000, settings.ItemsPerSyncRequest);
		AreEqual(SyncDirection.PullDownThenPushUp, settings.SyncDirection);
		AreEqual(DateTime.MinValue, settings.LastSyncedOnClient);
		AreEqual(DateTime.MinValue, settings.LastSyncedOnServer);
		AreEqual(0, settings.Values.Count);
		IsFalse(settings.HasFilters);
		IsFalse(settings.ShouldSyncRepository(typeof(AddressEntity)));
		IsFalse(settings.HasFilter(typeof(AddressEntity)));
	}

	[TestMethod]
	public void SyncClientSettingsUpdateWith()
	{
		var source = new SyncClientSettings { EnablePrimaryKeyCache = true, IsServerClient = true };
		var destination = new SyncClientSettings();
		IsTrue(destination.UpdateWith(source, IncludeExcludeSettings.Empty));
		IsTrue(destination.EnablePrimaryKeyCache);
		IsTrue(destination.IsServerClient);
		IsFalse(destination.UpdateWith(null, IncludeExcludeSettings.Empty));
	}

	[TestMethod]
	public void SyncOperationDefaults()
	{
		var operation = new SyncOperation();
		IsNotNull(operation.Changes);
		IsNotNull(operation.Issues);
		IsNotNull(operation.Settings);
		AreEqual(Guid.Empty, operation.SessionId);
		IsFalse(operation.EndSession);
		IsFalse(operation.ClientHasNoChanges);
		AreEqual(0, operation.GetChangesSkip);
		IsNull(operation.ResumeStatistics);
	}

	[TestMethod]
	public void SyncRequestReset()
	{
		var request = new SyncRequest
		{
			Since = UtcNow,
			Until = UtcNow.AddHours(1),
			Skip = 9,
			Take = 2
		};
		request.Reset();
		AreEqual(DateTime.MinValue, request.Since);
		AreEqual(0, request.Skip);
		AreEqual(1000, request.Take);
	}

	[TestMethod]
	public void UpdateWithDoesNotCopyFilters()
	{
		var source = new SyncSettings { ItemsPerSyncRequest = 12, SyncType = "Full" };
		source.AddFilter<AddressEntity>();
		source.Values["k"] = "v";

		var destination = new SyncSettings();
		destination.UpdateWith(source, IncludeExcludeSettings.Empty);

		AreEqual(12, destination.ItemsPerSyncRequest);
		AreEqual("Full", destination.SyncType);
		AreEqual("v", destination.Values["k"]);
		IsFalse(destination.HasFilters);
		IsFalse(destination.ShouldSyncRepository(typeof(AddressEntity)));
		IsFalse(destination.HasFilter(typeof(AddressEntity)));
	}

	#endregion
}