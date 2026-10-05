#region References

using System;
using System.Linq;
using Cornerstone.Diagnostics;
using Cornerstone.Profiling;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncProfilerTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void ApplyChangesRecordsExpectedScopes()
	{
		WithEachProvider((provider, database) =>
		{
			var profiler = new Profiler("Client", this);
			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), profiler);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());

			var createdOn = UtcNow;
			var incoming = new Address
			{
				City = "City",
				CreatedOn = createdOn,
				Line1 = "Profiled",
				ModifiedOn = createdOn,
				Postal = "29640",
				State = "SC",
				SyncId = Guid.NewGuid()
			};

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(incoming)));
			AreEqual(0, result.Collection.Count);

			AreEqual(1, ScopeCount(profiler, "ApplyChanges"));
			AreEqual(1, ScopeCount(profiler, "ProcessSyncObjects"));
			IsTrue(ScopeCount(profiler, "ProcessSyncObject") >= 1);
			IsTrue(ScopeCount(profiler, "ConvertIncoming") >= 1);
			IsTrue(ScopeCount(profiler, "ProcessSyncObjectsSaveDatabase") >= 1);
			IsTrue(
				(ScopeCount(profiler, "SaveChangesSavePending") >= 1)
				|| (ScopeCount(profiler, "SaveChangesEfCore") >= 1)
			);
		});
	}

	[TestMethod]
	public void GetChangesRecordsQueryAndConvertScopes()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var profiler = new Profiler("Client", this);
			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), profiler);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			var changes = client.GetChanges(sessionId, NewRequest());
			IsTrue(changes.Collection.Count >= 1);
			IsTrue(ScopeCount(profiler, "GetChanges") >= 1);
			IsTrue(ScopeCount(profiler, "GetChangeCount") >= 1);
			IsTrue(ScopeCount(profiler, "GetChangesQuery") >= 1);
			IsTrue(ScopeCount(profiler, "ConvertOutgoing") >= 1);
		});
	}

	[TestMethod]
	public void ProfilerScopeModelStoresTotalAndPercent()
	{
		var ticks = TimeSpan.FromMilliseconds(90).Ticks;
		var row = new ProfilerScopeModel("ProcessSyncObjectsSaveDatabase", count: 2, totalTicks: ticks, percent: 90);
		AreEqual("ProcessSyncObjectsSaveDatabase", row.Name);
		AreEqual(2, row.Count);
		AreEqual(ticks, row.TotalTicks);
		AreEqual(90, row.Percent);
		AreEqual(TimeSpan.FromTicks(ticks), row.Elapsed);
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

	private static long ScopeCount(Profiler profiler, string name)
	{
		var stats = profiler.FirstOrDefault(x => x.Name == name);
		return stats?.Count ?? 0;
	}

	#endregion
}