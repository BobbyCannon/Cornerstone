#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Cornerstone.Convert;
using Cornerstone.Convert.Converters;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for Sync Device
/// </summary>
public static class SyncDeviceExtensions
{
	#region Constants

	/// <summary>
	/// The key for the ApplicationName value for Sync Client Details.
	/// </summary>
	public const string ApplicationNameValueKey = "ApplicationName";

	/// <summary>
	/// The key for the ApplicationVersion value for Sync Client Details.
	/// </summary>
	public const string ApplicationVersionValueKey = "ApplicationVersion";

	/// <summary>
	/// The key for the DeviceId value for Sync Client Details.
	/// </summary>
	public const string DeviceIdValueKey = "DeviceId";

	/// <summary>
	/// The key for the DeviceName value for Sync Client Details.
	/// </summary>
	public const string DeviceNameValueKey = "DeviceName";

	/// <summary>
	/// The key for the DevicePlatform value for Sync Client Details.
	/// </summary>
	public const string DevicePlatformValueKey = "DevicePlatform";

	/// <summary>
	/// The key for the DeviceType value for Sync Client Details.
	/// </summary>
	public const string DeviceTypeValueKey = "DeviceType";

	#endregion

	#region Methods

	/// <summary>
	/// Update the sync options with the provided sync client details.
	/// </summary>
	/// <param name="dictionary"> The dictionary to update. </param>
	/// <param name="clientDetails"> The client details to use. </param>
	public static void AddOrUpdateSyncClientDetails(this IDictionary<string, string> dictionary, ISyncClientDetails clientDetails)
	{
		dictionary.AddOrUpdate(ApplicationNameValueKey, clientDetails.ApplicationName);
		dictionary.AddOrUpdate(ApplicationVersionValueKey, clientDetails.ApplicationVersion.ToString());
		dictionary.AddOrUpdate(DeviceIdValueKey, clientDetails.DeviceId);
		dictionary.AddOrUpdate(DeviceNameValueKey, clientDetails.DeviceName);
		dictionary.AddOrUpdate(DevicePlatformValueKey, ((int) clientDetails.DevicePlatform).ToString());
		dictionary.AddOrUpdate(DeviceTypeValueKey, ((int) clientDetails.DeviceType).ToString());
	}

	/// <summary>
	/// Update the sync options with the provided sync client details.
	/// </summary>
	/// <param name="headers"> The headers to update. </param>
	/// <param name="clientDetails"> The client details to use. </param>
	public static void AddOrUpdateSyncClientDetails(this HttpHeaders headers, ISyncClientDetails clientDetails)
	{
		headers.AddOrUpdate(ApplicationNameValueKey, clientDetails.ApplicationName);
		headers.AddOrUpdate(ApplicationVersionValueKey, clientDetails.ApplicationVersion.ToString());
		headers.AddOrUpdate(DeviceIdValueKey, clientDetails.DeviceId);
		headers.AddOrUpdate(DeviceNameValueKey, clientDetails.DeviceName);
		headers.AddOrUpdate(DevicePlatformValueKey, ((int) clientDetails.DevicePlatform).ToString());
		headers.AddOrUpdate(DeviceTypeValueKey, ((int) clientDetails.DeviceType).ToString());
	}

	/// <summary>
	/// Update the sync options with the provided sync client details.
	/// </summary>
	/// <param name="syncOptions"> The options to update. </param>
	/// <param name="clientDetails"> The client details to use. </param>
	public static void AddOrUpdateSyncClientDetails(this SyncSettings syncOptions, ISyncClientDetails clientDetails)
	{
		syncOptions.Values.AddOrUpdateSyncClientDetails(clientDetails);
	}

	/// <summary>
	/// Load the sync client details into the provided sync options.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="syncOptions"> The options to load. </param>
	public static void Load(this SyncClientDetails device, SyncSettings syncOptions)
	{
		device.Load(syncOptions.Values);
	}

	/// <summary>
	/// Load the sync client details from the provided dictionary.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="values"> The values to load. </param>
	public static void Load(this SyncClientDetails device, IDictionary<string, string> values)
	{
		device.ApplicationName = TryGetValue(values, ApplicationNameValueKey, string.Empty);
		device.ApplicationVersion = TryGetValue(values, ApplicationVersionValueKey, new Version(0, 0, 0, 0));
		device.DeviceId = TryGetValue(values, DeviceIdValueKey, string.Empty);
		device.DeviceName = TryGetValue(values, DeviceNameValueKey, string.Empty);
		device.DevicePlatform = TryGetValue(values, DevicePlatformValueKey, DevicePlatform.Unknown);
		device.DeviceType = TryGetValue(values, DeviceTypeValueKey, DeviceType.Unknown);
	}

	/// <summary>
	/// Validate that all the sync client details are available.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	public static void Validate(this ISyncClientDetails device)
	{
		if (string.IsNullOrWhiteSpace(device.ApplicationName))
		{
			throw new ArgumentException($"{nameof(device.ApplicationName)} must be provided.");
		}

		if (device.ApplicationVersion.IsDefault())
		{
			throw new ArgumentException($"{nameof(device.ApplicationVersion)} must be provided.");
		}

		if (string.IsNullOrWhiteSpace(device.DeviceId))
		{
			throw new ArgumentException($"{nameof(device.DeviceId)} must be provided.");
		}

		if (string.IsNullOrWhiteSpace(device.DeviceName))
		{
			throw new ArgumentException($"{nameof(device.DeviceName)} must be provided.");
		}

		if (device.DevicePlatform == DevicePlatform.Unknown)
		{
			throw new ArgumentException($"{nameof(device.DevicePlatform)} must be provided.");
		}

		if (device.DeviceType == DeviceType.Unknown)
		{
			throw new ArgumentException($"{nameof(device.DeviceType)} must be provided.");
		}
	}

	private static string TryGetValue(IDictionary<string, string> dictionary, string name, string defaultValue)
	{
		return dictionary.TryGetValue(name, out var value) ? value : defaultValue;
	}

	private static T TryGetValue<T>(IDictionary<string, string> dictionary, string name, T defaultValue)
	{
		try
		{
			return dictionary.TryGetValue(name, out var value)
				? value.ConvertTo<T>()
				: defaultValue;
		}
		catch
		{
			return defaultValue;
		}
	}

	#endregion
}