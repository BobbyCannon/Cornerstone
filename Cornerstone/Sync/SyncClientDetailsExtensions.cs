#region References

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Extensions for Sync Device
/// </summary>
public static class SyncClientDetailsExtensions
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
	/// The key for the DevicePlatformVersion value for Sync Client Details.
	/// </summary>
	public const string DevicePlatformVersionValueKey = "DevicePlatformVersion";

	/// <summary>
	/// The key for the DeviceType value for Sync Client Details.
	/// </summary>
	public const string DeviceTypeValueKey = "DeviceType";

	/// <summary>
	/// The key for the altitude for the location.
	/// </summary>
	public const string LocationAltitudeKey = "Altitude";

	/// <summary>
	/// The key for the altitude reference for the location.
	/// </summary>
	public const string LocationAltitudeReferenceKey = "AltitudeReference";

	/// <summary>
	/// The key for the latitude for the location.
	/// </summary>
	public const string LocationLatitudeKey = "Latitude";

	/// <summary>
	/// The key for the longitude for the location.
	/// </summary>
	public const string LocationLongitudeKey = "Longitude";

	/// <summary>
	/// The key for the location source for the location.
	/// </summary>
	public const string LocationSourceKey = "LocationSource";

	/// <summary>
	/// The key for the location updated on for the location.
	/// </summary>
	public const string LocationUpdatedOnKey = "LocationUpdatedOn";

	#endregion

	#region Methods

	/// <summary>
	/// Update the sync options with the provided sync client details.
	/// </summary>
	/// <param name="syncSettings"> The options to update. </param>
	/// <param name="clientDetails"> The client details to use. </param>
	public static void AddOrUpdateSyncClientDetails(this SyncSettings syncSettings, ISyncClientDetails clientDetails)
	{
		syncSettings.Values.AddOrUpdateSyncClientDetails(clientDetails);
	}

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
		dictionary.AddOrUpdate(DevicePlatformVersionValueKey, clientDetails.DevicePlatformVersion.ToString());
		dictionary.AddOrUpdate(DeviceTypeValueKey, ((int) clientDetails.DeviceType).ToString());
	}

	/// <summary>
	/// Update the HTTP headers with the provided sync client details.
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
		headers.AddOrUpdate(DevicePlatformVersionValueKey, clientDetails.DevicePlatformVersion.ToString());
		headers.AddOrUpdate(DeviceTypeValueKey, ((int) clientDetails.DeviceType).ToString());
	}

	/// <summary>
	/// Load the sync client details into the provided sync options.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="syncSettings"> The options to load. </param>
	public static void Load(this SyncClientDetails device, SyncSettings syncSettings)
	{
		device.Load(syncSettings.Values);
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
		device.DevicePlatformVersion = TryGetValue(values, DevicePlatformVersionValueKey, new Version(0, 0, 0, 0));
		device.DeviceType = TryGetValue(values, DeviceTypeValueKey, DeviceType.Unknown);
	}

	/// <summary>
	/// Validate that all the sync client details are available.
	/// </summary>
	/// <param name="syncClient"> The device to load options into. </param>
	public static void Validate(this ISyncClientDetails syncClient)
	{
		if (string.IsNullOrWhiteSpace(syncClient.ApplicationName))
		{
			throw new ArgumentException($"{nameof(syncClient.ApplicationName)} must be provided.");
		}

		if (syncClient.ApplicationVersion.IsDefault())
		{
			throw new ArgumentException($"{nameof(syncClient.ApplicationVersion)} must be provided.");
		}

		if (string.IsNullOrWhiteSpace(syncClient.DeviceId))
		{
			throw new ArgumentException($"{nameof(syncClient.DeviceId)} must be provided.");
		}

		if (string.IsNullOrWhiteSpace(syncClient.DeviceName))
		{
			throw new ArgumentException($"{nameof(syncClient.DeviceName)} must be provided.");
		}

		if (syncClient.DevicePlatform == DevicePlatform.Unknown)
		{
			throw new ArgumentException($"{nameof(syncClient.DevicePlatform)} must be provided.");
		}

		if (syncClient.DevicePlatformVersion == null)
		{
			throw new ArgumentException($"{nameof(syncClient.DevicePlatform)} must be provided.");
		}

		if (syncClient.DeviceType == DeviceType.Unknown)
		{
			throw new ArgumentException($"{nameof(syncClient.DeviceType)} must be provided.");
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

	private static bool TryGetValue<T>(this IDictionary<string, string> dictionary, string name, out T value)
	{
		try
		{
			if (!dictionary.TryGetValue(name, out var sValue))
			{
				value = default;
				return false;
			}

			value = sValue.ConvertTo<T>();
			return true;
		}
		catch
		{
			value = default;
			return false;
		}
	}

	#endregion
}