#region References

using System;
using Cornerstone.Location;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncObjectTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToSyncObject()
	{
		var scenarios = new (SyncModel Value, int Size)[]
		{
			(
				new Account
				{
					CreatedOn = StartDateTime,
					EmailAddress = "john@domain.com",
					IsDeleted = false,
					LastLoginDate = StartDateTime,
					ModifiedOn = StartDateTime,
					Name = "John Doe",
					Picture = null,
					Roles = ",,",
					Status = AccountStatus.Enabled,
					SyncId = new Guid("B9825F51-AEC7-4C18-A83F-6B4558B0EA20"),
					TimeZoneId = string.Empty
				},
				84
			),
			(
				new SyncDevice
				{
					Altitude = 1,
					AltitudeReference = AltitudeReferenceType.Ellipsoid,
					ApplicationName = "application name",
					ApplicationVersion = new Version(1, 2, 3, 4),
					DeviceId = "device id",
					DeviceName = "device name",
					DevicePlatform = DevicePlatform.Windows,
					DevicePlatformVersion = new Version(4, 3, 2, 1),
					DeviceType = DeviceType.Desktop,
					Latitude = 1.2345,
					Longitude = 5.4321,
					LocationSource = "wifi",
					LocationUpdatedOn = StartDateTime,
					CreatedOn = StartDateTime,
					IsDeleted = false,
					ModifiedOn = StartDateTime,
					SyncId = Guid.Parse("C5319ED5-458D-43FF-9809-3534028EE4AE")
				},
				155
			)
		};

		foreach (var scenario in scenarios)
		{
			var syncObject = SyncObject.ToSyncObject(scenario.Value);
			AreEqual(scenario.Size, syncObject.Data.Length);
			AreEqual(SyncObjectStatus.Added, syncObject.Status);

			var syncModel = syncObject.ToSyncModel();
			AreEqual(scenario.Value, syncModel);
		}
	}

	#endregion
}