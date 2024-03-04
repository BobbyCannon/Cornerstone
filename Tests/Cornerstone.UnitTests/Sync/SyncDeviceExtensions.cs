#region References

using System;
using Cornerstone.Location;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncDeviceExtensions : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetDetails()
	{
		//GenerateSampleCode<SyncDevice>();

		var device = new SyncDevice
		{
			Altitude = 0.382,
			AltitudeReference = AltitudeReferenceType.Unspecified,
			ApplicationName = "Hello World",
			ApplicationVersion = new Version(1, 2, 3, 4),
			CreatedOn = new DateTime(4662, 11, 11, 8, 9, 5, 301, 223, DateTimeKind.Unspecified),
			DeviceId = "Ln00c51LzRsI",
			DeviceName = "My Phone",
			DevicePlatform = DevicePlatform.Android,
			DevicePlatformVersion = new Version(13, 0, 1),
			DeviceType = DeviceType.Phone,
			IsDeleted = true,
			Latitude = 0.491,
			LocationUpdatedOn = new DateTime(4599, 3, 25, 2, 2, 7, 530, 860, DateTimeKind.Unspecified),
			Longitude = 0.923,
			ModifiedOn = new DateTime(7589, 7, 18, 9, 58, 33, 25, 81, DateTimeKind.Unspecified),
			SyncId = Guid.Parse("377d53fa-dd3a-4d2f-a2ab-93b6b3a0ed4a")
		};

		AreEqual("My Phone, Hello World, 1.2.3.4", device.GetDetails());
	}

	#endregion
}