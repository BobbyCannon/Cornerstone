#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Text;
using Microsoft.Extensions.Primitives;

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

	#endregion

	#region Methods

	/// <summary>
	/// Update the sync client details into the provided sync options.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="syncSettings"> The options to load. </param>
	public static void AddOrUpdateSyncClientDetails(this ISyncClientDetails device, SyncSettings syncSettings)
	{
		AddOrUpdateSyncClientDetails(device, syncSettings.Values);
	}

	/// <summary>
	/// Update the sync client details from the provided dictionary.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="syncClient"> The values to load. </param>
	public static void AddOrUpdateSyncClientDetails(this ISyncClientDetails device, IDictionary<string, string> syncClient)
	{
		SetProperty(device, x => x.ApplicationName, ApplicationNameValueKey, syncClient);
		SetProperty(device, x => x.ApplicationVersion, ApplicationVersionValueKey, syncClient);
		SetProperty(device, x => x.DeviceId, DeviceIdValueKey, syncClient);
		SetProperty(device, x => x.DeviceName, DeviceNameValueKey, syncClient);
		SetProperty(device, x => x.DevicePlatform, DevicePlatformValueKey, syncClient);
		SetProperty(device, x => x.DevicePlatformVersion, DevicePlatformVersionValueKey, syncClient);
		SetProperty(device, x => x.DeviceType, DeviceTypeValueKey, syncClient);
	}

	/// <summary>
	/// Update the sync options with the provided sync client details.
	/// </summary>
	/// <param name="dictionary"> The dictionary to update. </param>
	/// <param name="syncClient"> The client details to use. </param>
	public static void AddOrUpdateSyncClientDetails(this IDictionary<string, string> dictionary, ISyncClientDetails syncClient)
	{
		dictionary.AddOrUpdate(ApplicationNameValueKey, syncClient.ApplicationName);
		dictionary.AddOrUpdate(ApplicationVersionValueKey, syncClient.ApplicationVersion.ToString());
		dictionary.AddOrUpdate(DeviceIdValueKey, syncClient.DeviceId);
		dictionary.AddOrUpdate(DeviceNameValueKey, syncClient.DeviceName);
		dictionary.AddOrUpdate(DevicePlatformValueKey, ((int) syncClient.DevicePlatform).ToString());
		dictionary.AddOrUpdate(DevicePlatformVersionValueKey, syncClient.DevicePlatformVersion.ToString());
		dictionary.AddOrUpdate(DeviceTypeValueKey, ((int) syncClient.DeviceType).ToString());
	}

	/// <summary>
	/// Update the sync options with the provided sync client details.
	/// </summary>
	/// <param name="headers"> The headers to update. </param>
	/// <param name="syncClient"> The client details to use. </param>
	public static void AddOrUpdateSyncClientDetails(this HttpHeaders headers, ISyncClientDetails syncClient)
	{
		headers.AddOrUpdate(ApplicationNameValueKey, syncClient.ApplicationName);
		headers.AddOrUpdate(ApplicationVersionValueKey, syncClient.ApplicationVersion.ToString());
		headers.AddOrUpdate(DeviceIdValueKey, syncClient.DeviceId);
		headers.AddOrUpdate(DeviceNameValueKey, syncClient.DeviceName);
		headers.AddOrUpdate(DevicePlatformValueKey, ((int) syncClient.DevicePlatform).ToString());
		headers.AddOrUpdate(DevicePlatformVersionValueKey, syncClient.DevicePlatformVersion.ToString());
		headers.AddOrUpdate(DeviceTypeValueKey, ((int) syncClient.DeviceType).ToString());
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
	/// Get short description from the sync device.
	/// </summary>
	/// <param name="device"> The device to process. </param>
	/// <returns> The details from the sync device. </returns>
	public static string GetDetails(this ISyncDevice device)
	{
		var builder = new TextBuilder { NewLineToken = ", " };

		if (!string.IsNullOrWhiteSpace(device.DeviceName))
		{
			builder.Append(device.DeviceName);
		}

		if (!string.IsNullOrWhiteSpace(device.ApplicationName))
		{
			builder.NewLine();
			builder.Append(device.ApplicationName);
		}

		if (!device.ApplicationVersion.IsDefault())
		{
			builder.NewLine();
			builder.Append(device.ApplicationVersion.ToString());
		}

		return builder.ToString();
	}

	/// <summary>
	/// Load the sync client details into the provided sync options.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="values"> The values to load. </param>
	public static void Load(this SyncClientDetails device, IDictionary<string, StringValues> values)
	{
		device.Load(values.ToDictionary(x => x.Key, x => string.Join("", x.Value.ToArray())));
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

	private static void SetProperty<T, T2>(T device, Expression<Func<T, T2>> property, string name, IDictionary<string, string> values)
	{
		if (values.TryGetValue<T2>(name, out var value))
		{
			device.TrySetProperty(property, value);
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