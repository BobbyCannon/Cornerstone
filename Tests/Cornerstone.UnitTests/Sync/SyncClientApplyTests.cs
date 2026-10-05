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
public class SyncClientApplyTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void ApplyCorrectionsWritesIncomingObjects()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var target = NewClient("Target", provider);
			var sessionId = Guid.NewGuid();
			target.BeginSync(sessionId, NewSettings());
			var before = ReadAll<AddressEntity>(database.Addresses).Count;

			var createdOn = UtcNow;
			var incoming = new Address
			{
				City = "City",
				CreatedOn = createdOn,
				Line1 = "Correction",
				ModifiedOn = createdOn,
				Postal = "29640",
				State = "SC",
				SyncId = Guid.NewGuid()
			};
			var result = target.ApplyCorrections(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(incoming)));
			AreEqual(0, result.Collection.Count);
			DetachTrackedEntities(database);
			AreEqual(before + 1, ReadAll<AddressEntity>(database.Addresses).Count);
		});
	}

	[TestMethod]
	public void ConverterCannotConvertOutgoingSkipsPrimaryKeyLookup()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home");
			provider.KeyCache?.Clear();
			var client = new AccountOnlyConverterClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			client.BeginSync(Guid.NewGuid(), NewSettings());
			IsFalse(client.Converter.CanConvertOutgoing(typeof(AddressEntity).ToAssemblyName()));
			AreEqual(0L, client.GetEntityPrimaryKey<AddressEntity, long>(address.SyncId));
		});
	}

	[TestMethod]
	public void ConverterCopyThenFalseDoesNotPersistUpdate()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Home");
			var target = new CopyThenRejectUpdateSyncClient("Target", provider, this, new SyncStatistics(), new Profiler("Target"));
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			IncrementTime(seconds: 1);

			var modifiedOn = UtcNow;
			var payload = new Address
			{
				City = "City",
				CreatedOn = modifiedOn.AddMinutes(-1),
				Line1 = "Hacked",
				ModifiedOn = modifiedOn,
				Postal = "29640",
				State = "SC",
				SyncId = stored.SyncId
			};
			var result = target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload)));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.UpdateException, result.Collection[0].IssueType);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Home", remaining.Line1);
		});
	}

	[TestMethod]
	public void ConverterSyncIssueExceptionIsRecorded()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());
			var incoming = NewIncomingAddress(changes.Collection[0]);

			var target = new RelationshipIssueSyncClient("Target", provider, this, new SyncStatistics(), new Profiler("Target"));
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			var result = target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(incoming));
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.RelationshipConstraint, result.Collection[0].IssueType);
		});
	}

	[TestMethod]
	public void ConverterSyncUpdateExceptionIsRecorded()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());
			var incoming = NewIncomingAddress(changes.Collection[0]);

			var target = new ThrowingUpdateSyncClient("Target", provider, this, new SyncStatistics(), new Profiler("Target"));
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			var result = target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(incoming));
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.UpdateException, result.Collection[0].IssueType);
			IsTrue(result.Collection[0].Message.Contains("rejected by converter"));
		});
	}

	[TestMethod]
	public void ConverterUpdateFalseProducesUpdateException()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());
			var incoming = NewIncomingAddress(changes.Collection[0]);

			var target = new RejectUpdateSyncClient("Target", provider, this, new SyncStatistics(), new Profiler("Target"));
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			var result = target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(incoming));
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.UpdateException, result.Collection[0].IssueType);
			IsTrue(result.Collection[0].Message.Contains("The converter failed to update the entity."));
		});
	}

	[TestMethod]
	public void EmptySyncObjectIsSkipped()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());

			var target = NewClient("Target", provider);
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			var mixed = new ServiceRequest<SyncObject>(SyncObjectExtensions.Empty, changes.Collection[0]);
			var result = target.ApplyChanges(targetSession, mixed);
			AreEqual(0, result.Collection.Count);
		});
	}

	[TestMethod]
	public void GetEntityPrimaryKeyEmptyAndCacheAndMissingConverter()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home");
			var client = NewClient("Client", provider);
			AreEqual(0L, client.GetEntityPrimaryKey<AddressEntity, long>(Guid.Empty));

			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			var id = client.GetEntityPrimaryKey<AddressEntity, long>(address.SyncId);
			AreEqual(address.Id, id);
			AreEqual(address.Id, client.GetEntityPrimaryKey<AddressEntity, long>(address.SyncId));
		});
	}

	[TestMethod]
	public void MissingConverterOnApplyChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);

			var target = new NoConverterSyncClient("Target", provider, this, new SyncStatistics(), new Profiler("Target"));
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			var result = target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(changes.Collection));
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.UpdateException, result.Collection[0].IssueType);
			IsTrue(result.Collection[0].Message.Contains("A sync converter is required."));
		});
	}

	[TestMethod]
	public void RequiredFieldFailureIncrementsIndividualProcessCount()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());
			var good = NewIncomingAddress(changes.Collection[0]);
			var badModel = (Address) good.ToSyncModel();
			badModel.SyncId = Guid.NewGuid();
			badModel.Line1 = null;
			var bad = SyncObject.ToSyncObject(badModel);

			var target = NewClient("Target", provider);
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(good, bad));
			IsTrue(target.Statistics.IndividualProcessCount >= 1);
		});
	}

	[TestMethod]
	public void SuccessfulApplyIncrementsAppliedChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var source = NewClient("Source", provider);
			var sessionId = Guid.NewGuid();
			source.BeginSync(sessionId, NewSettings());
			var changes = source.GetChanges(sessionId, NewRequest());
			var incoming = NewIncomingAddress(changes.Collection[0]);

			var target = NewClient("Target", provider);
			var targetSession = Guid.NewGuid();
			target.BeginSync(targetSession, NewSettings());
			var result = target.ApplyChanges(targetSession, new ServiceRequest<SyncObject>(incoming));
			AreEqual(0, result.Collection.Count);
			AreEqual(1, target.Statistics.AppliedChanges);
		});
	}

	[TestMethod]
	public void ValidateSyncClientFalseThrowsOnBegin()
	{
		WithEachProvider((provider, _) =>
		{
			var server = new RejectingServerSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			ExpectedException<CornerstoneException>(() => server.BeginSync(Guid.NewGuid(), NewSettings()));
		});
	}

	private SampleSyncClient NewClient(string name, ISyncableDatabaseProvider provider)
	{
		return new SampleSyncClient(name, provider, this, new SyncStatistics(), new Profiler(name));
	}

	private static SyncObject NewIncomingAddress(SyncObject existing)
	{
		var model = (Address) existing.ToSyncModel();
		model.SyncId = Guid.NewGuid();
		model.Line1 = "Incoming";
		model.ModifiedOn = model.CreatedOn.AddMinutes(1);
		return SyncObject.ToSyncObject(model);
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

	private sealed class AccountOnlyConverterClient : SampleSyncClient
	{
		#region Constructors

		public AccountOnlyConverterClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Account, AccountEntity>()
			);
		}

		#endregion
	}

	private sealed class CopyThenRejectUpdateSyncClient : SampleSyncClient
	{
		#region Constructors

		public CopyThenRejectUpdateSyncClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>(update: (_, _, _, processUpdate, _) =>
				{
					processUpdate();
					return false;
				})
			);
		}

		#endregion
	}

	private sealed class NoConverterSyncClient : SampleSyncClient
	{
		#region Constructors

		public NoConverterSyncClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return null;
		}

		#endregion
	}

	private sealed class RejectUpdateSyncClient : SampleSyncClient
	{
		#region Constructors

		public RejectUpdateSyncClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>(update: (_, _, _, _, _) => false)
			);
		}

		#endregion
	}

	private sealed class RejectingServerSyncClient : SampleServerSyncClient
	{
		#region Constructors

		public RejectingServerSyncClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override bool ValidateSyncClient()
		{
			return false;
		}

		#endregion
	}

	private sealed class RelationshipIssueSyncClient : SampleSyncClient
	{
		#region Constructors

		public RelationshipIssueSyncClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>(
					update: (_, _, _, _, _) => throw new SyncIssueException(SyncIssueType.RelationshipConstraint, "missing related")
				)
			);
		}

		#endregion
	}

	private sealed class ThrowingUpdateSyncClient : SampleSyncClient
	{
		#region Constructors

		public ThrowingUpdateSyncClient(string name, ISyncableDatabaseProvider provider, IDateTimeProvider time, SyncStatistics statistics, Profiler profiler)
			: base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>(update: (_, _, _, _, _) => throw new SyncUpdateException("rejected by converter"))
			);
		}

		#endregion
	}

	#endregion
}