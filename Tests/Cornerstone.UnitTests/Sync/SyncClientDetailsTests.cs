#region References

using System;
using System.Collections.Generic;
using System.Net.Http;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncClientDetailsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void LoadFallsBackOnBadValues()
	{
		var loaded = new SyncClientDetails();
		loaded.Load(new Dictionary<string, string>
		{
			{ SyncClientDetailsExtensions.ApplicationVersionValueKey, "not-a-version" },
			{ SyncClientDetailsExtensions.DevicePlatformValueKey, "nope" }
		});
		AreEqual(new Version(0, 0, 0, 0), loaded.ApplicationVersion);
		AreEqual(DevicePlatform.Unknown, loaded.DevicePlatform);
	}

	[TestMethod]
	public void RoundTripThroughHttpHeaders()
	{
		var details = NewDetails();
		using var request = new HttpRequestMessage();
		request.Headers.AddOrUpdateSyncClientDetails(details);
		IsTrue(request.Headers.Contains(SyncClientDetailsExtensions.ApplicationNameValueKey));
		IsTrue(request.Headers.Contains(SyncClientDetailsExtensions.DeviceIdValueKey));
	}

	[TestMethod]
	public void RoundTripThroughSettingsValues()
	{
		var details = NewDetails();
		var settings = new SyncSettings();
		settings.AddOrUpdateSyncClientDetails(details);

		var loaded = new SyncClientDetails();
		loaded.Load(settings);
		AreEqual(details.ApplicationName, loaded.ApplicationName);
		AreEqual(details.ApplicationVersion, loaded.ApplicationVersion);
		AreEqual(details.DeviceId, loaded.DeviceId);
		AreEqual(details.DeviceName, loaded.DeviceName);
		AreEqual(details.DevicePlatform, loaded.DevicePlatform);
		AreEqual(details.DevicePlatformVersion, loaded.DevicePlatformVersion);
		AreEqual(details.DeviceType, loaded.DeviceType);
	}

	[TestMethod]
	public void ValidateRequiresEachField()
	{
		ExpectedException<ArgumentException>(() => new SyncClientDetails().Validate(), "ApplicationName must be provided.");

		var details = NewDetails();
		details.Validate();

		details.DevicePlatform = DevicePlatform.Unknown;
		ExpectedException<ArgumentException>(() => details.Validate(), "DevicePlatform must be provided.");

		details = NewDetails();
		details.ApplicationVersion = new Version(0, 0, 0, 0);
		ExpectedException<ArgumentException>(() => details.Validate(), "ApplicationVersion must be provided.");

		details = NewDetails();
		details.DeviceId = " ";
		ExpectedException<ArgumentException>(() => details.Validate(), "DeviceId must be provided.");

		details = NewDetails();
		details.DeviceName = "";
		ExpectedException<ArgumentException>(() => details.Validate(), "DeviceName must be provided.");

		details = NewDetails();
		details.DeviceType = DeviceType.Unknown;
		ExpectedException<ArgumentException>(() => details.Validate(), "DeviceType must be provided.");

		details = NewDetails();
		details.DevicePlatformVersion = null;
		ExpectedException<ArgumentException>(() => details.Validate(), "DevicePlatform must be provided.");
	}

	private static SyncClientDetails NewDetails()
	{
		return new SyncClientDetails
		{
			ApplicationName = "App",
			ApplicationVersion = new Version(1, 2, 3, 4),
			DeviceId = "id",
			DeviceName = "name",
			DevicePlatform = DevicePlatform.Windows,
			DevicePlatformVersion = new Version(10, 0),
			DeviceType = DeviceType.Desktop
		};
	}

	#endregion
}