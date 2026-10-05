#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncClientConverterTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void ConvertIncomingAndOutgoingRoundTrip()
	{
		WithEachProvider((provider, _) =>
		{
			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			client.BeginSync(Guid.NewGuid(), new SyncSettings { IncludeIssueDetails = true });
			var entity = new AddressEntity
			{
				CreatedOn = UtcNow,
				Line1 = "Round",
				ModifiedOn = UtcNow,
				State = "SC",
				City = "City",
				Postal = "29640",
				SyncId = Guid.NewGuid()
			};
			var outgoing = client.Converter.ConvertOutgoing(client, entity);
			IsNotNull(outgoing);
			AreEqual(entity.SyncId, outgoing.SyncId);
			var incoming = client.Converter.ConvertIncoming(client, outgoing);
			IsNotNull(incoming);
			AreEqual(entity.SyncId, incoming.SyncId);
			IsNull(client.Converter.ConvertIncoming(client, new SyncObject { TypeName = "Unknown.Type", SyncId = Guid.NewGuid() }));
		});
	}

	[TestMethod]
	public void ConvertOutgoingReturnsNullWhenTypeIsNotRegistered()
	{
		var converter = new SyncClientConverter(
			new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>()
		);
		IsNull(converter.ConvertOutgoing(null, new AccountEntity { SyncId = Guid.NewGuid() }));
		IsFalse(converter.Update(null, new AccountEntity(), new AccountEntity(), SyncObjectStatus.Updated));
	}

	[TestMethod]
	public void DefaultConverterUpdateCopiesWithoutCustomAction()
	{
		WithEachProvider((provider, _) =>
		{
			var client = new DefaultAddressConverterClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			client.BeginSync(Guid.NewGuid(), new SyncSettings { IncludeIssueDetails = true });
			var source = new AddressEntity
			{
				CreatedOn = UtcNow,
				Line1 = "Copied",
				ModifiedOn = UtcNow,
				State = "SC",
				SyncId = Guid.NewGuid()
			};
			var destination = new AddressEntity();
			IsTrue(client.Converter.Update(client, source, destination, SyncObjectStatus.Added));
			AreEqual("Copied", destination.Line1);
			AreEqual(source.SyncId, destination.SyncId);
		});
	}

	[TestMethod]
	public void GetDatabaseReturnsSampleDatabase()
	{
		WithEachProvider((provider, _) =>
		{
			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			using var database = client.GetDatabase<ISampleDatabase>();
			IsNotNull(database);
			using var untyped = client.GetDatabase();
			IsTrue(untyped is ISampleDatabase);
		});
	}

	[TestMethod]
	public void GetEntityPrimaryKeyReadsBySyncIdWhenLookupFilterIsPresent()
	{
		WithEachProvider((provider, database) =>
		{
			var account = AddAccount(database, "Lookup");
			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			client.BeginSync(Guid.NewGuid(), new SyncSettings { IncludeIssueDetails = true });
			client.SyncSettings.AddFilter<AccountEntity>(lookupFilter: e => x => x.EmailAddress == e.EmailAddress);
			provider.KeyCache?.Clear();
			AreEqual(account.Id, client.GetEntityPrimaryKey<AccountEntity, int>(account.SyncId));
		});
	}

	[TestMethod]
	public void GetSyncableDatabaseOverloadPassesKeyCache()
	{
		WithEachProvider((provider, _) =>
		{
			var cache = new DatabaseKeyCache();
			using var database = provider.GetSyncableDatabase(new DatabaseSettings(), cache);
			AreEqual(cache, database.KeyCache);
		});
	}

	[TestMethod]
	public void MapsIncomingModelToEntityTypeName()
	{
		var converter = new SyncClientConverter(
			new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>()
		);
		AreEqual(typeof(AddressEntity).ToAssemblyName(), converter.GetEntityTypeNameForIncoming(typeof(Address).ToAssemblyName()));
		IsNull(converter.GetEntityTypeNameForIncoming(typeof(Account).ToAssemblyName()));
		IsTrue(converter.CanConvertOutgoing(typeof(AddressEntity).ToAssemblyName()));
		IsFalse(converter.CanConvertOutgoing(typeof(AccountEntity).ToAssemblyName()));
	}

	[TestMethod]
	public void SyncableDatabaseProvider2UsesFactory()
	{
		WithEachProvider((provider, _) =>
		{
			var cache = new DatabaseKeyCache();
			var wrapper = new SyncableDatabaseProvider2<ISyncableDatabase>(
				(settings, keyCache) => provider.GetSyncableDatabase(settings, keyCache),
				cache,
				new DatabaseSettings(),
				this
			);
			using var database = wrapper.GetSyncableDatabase();
			AreEqual(cache, database.KeyCache);
		});
	}

	#endregion

	#region Classes

	private sealed class DefaultAddressConverterClient : SampleSyncClient
	{
		#region Constructors

		public DefaultAddressConverterClient(
			string name,
			ISyncableDatabaseProvider provider,
			IDateTimeProvider time,
			SyncStatistics statistics,
			Profiler profiler
		) : base(name, provider, time, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>()
			);
		}

		#endregion
	}

	#endregion
}