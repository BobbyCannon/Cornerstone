#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncObjectTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConvertModelsToEntities()
	{
		var createdOn = new DateTime(2019, 01, 01, 02, 03, 04, DateTimeKind.Utc);
		var modifiedOn = new DateTime(2019, 02, 03, 04, 05, 06, DateTimeKind.Utc);

		var address = new ClientAddress
		{
			City = "City",
			Id = 99,
			Line1 = "Line1",
			Line2 = "Line2",
			Postal = "Postal",
			State = "State",
			SyncId = Guid.Parse("efc2c530-37b6-4fa5-ab71-bd38a3b4d277"),
			CreatedOn = createdOn,
			ModifiedOn = modifiedOn
		};

		var person = new ClientAccount
		{
			Address = address,
			AddressId = address.Id,
			AddressSyncId = address.SyncId,
			Id = 100,
			Name = "John",
			SyncId = Guid.Parse("7d880bb3-183f-4f75-86d8-9834ffe8fad2"),
			CreatedOn = createdOn,
			ModifiedOn = modifiedOn
		};

		var addressSyncObject = address.ToSyncObject();
		var personSyncObject = person.ToSyncObject();
		var changes = new[] { addressSyncObject, personSyncObject };
		var converter = new SyncClientIncomingConverter(
			new SyncObjectIncomingConverter<ClientAddress, AddressEntity>(),
			new SyncObjectIncomingConverter<ClientAccount, AccountEntity>()
		);

		var actual = converter.Convert(changes).ToList();
		var expectedAddress = "{\"City\":\"City\",\"CreatedOn\":\"2019-01-01T02:03:04Z\",\"Line1\":\"Line1\",\"Line2\":\"Line2\",\"ModifiedOn\":\"2019-02-03T04:05:06Z\",\"Postal\":\"Postal\",\"State\":\"State\",\"SyncId\":\"efc2c530-37b6-4fa5-ab71-bd38a3b4d277\"}";
		var expectedAccount = "{\"AddressSyncId\":\"efc2c530-37b6-4fa5-ab71-bd38a3b4d277\",\"CreatedOn\":\"2019-01-01T02:03:04Z\",\"ModifiedOn\":\"2019-02-03T04:05:06Z\",\"Name\":\"John\",\"SyncId\":\"7d880bb3-183f-4f75-86d8-9834ffe8fad2\"}";

		AreEqual(address.SyncId, actual[0].SyncId);
		AreEqual(expectedAddress, actual[0].Data);
		AreEqual(SyncObjectStatus.Modified, actual[0].Status);

		actual[1].Data.Escape().Dump();

		AreEqual(person.SyncId, actual[1].SyncId);
		AreEqual(expectedAccount, actual[1].Data);
		AreEqual(SyncObjectStatus.Modified, actual[1].Status);
	}

	[TestMethod]
	public void ToSyncObject()
	{
		var createdOn = new DateTime(2019, 01, 01, 02, 03, 04, DateTimeKind.Utc);
		var modifiedOn = new DateTime(2019, 02, 03, 04, 05, 06, DateTimeKind.Utc);

		var address = new ClientAddress
		{
			City = "City",
			Id = 99,
			Line1 = "Line1",
			Line2 = "Line2",
			Postal = "Postal",
			State = "State",
			SyncId = Guid.Parse("efc2c530-37b6-4fa5-ab71-bd38a3b4d277"),
			CreatedOn = createdOn,
			ModifiedOn = modifiedOn
		};

		var account = new ClientAccount
		{
			Address = address,
			AddressId = address.Id,
			AddressSyncId = address.SyncId,
			Id = 100,
			Name = "John",
			SyncId = Guid.Parse("7d880bb3-183f-4f75-86d8-9834ffe8fad2"),
			CreatedOn = createdOn,
			ModifiedOn = modifiedOn
		};

		var actualAddress = address.ToSyncObject();
		var actualAccount = account.ToSyncObject();

		var expectedAddress = "{\"City\":\"City\",\"CreatedOn\":\"2019-01-01T02:03:04Z\",\"Line1\":\"Line1\",\"Line2\":\"Line2\",\"ModifiedOn\":\"2019-02-03T04:05:06Z\",\"Postal\":\"Postal\",\"State\":\"State\",\"SyncId\":\"efc2c530-37b6-4fa5-ab71-bd38a3b4d277\"}";
		var expectedAccount = "{\"AddressSyncId\":\"efc2c530-37b6-4fa5-ab71-bd38a3b4d277\",\"CreatedOn\":\"2019-01-01T02:03:04Z\",\"ModifiedOn\":\"2019-02-03T04:05:06Z\",\"Name\":\"John\",\"SyncId\":\"7d880bb3-183f-4f75-86d8-9834ffe8fad2\"}";

		AreEqual(expectedAccount, actualAccount.Data);
		AreEqual(expectedAddress, actualAddress.Data);
	}

	#endregion
}