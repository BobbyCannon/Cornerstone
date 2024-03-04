﻿
using Cornerstone.Runtime;

#region References

#if ANDROID
using Android.Provider;
#elif IOS || __MACCATALYST__
using UIKit;

#elif WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.System.Profile;
#endif

#endregion

namespace Cornerstone.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="DeviceId" />.
/// </summary>
public static class DeviceIdExtensions
{
	#region Methods

	/// <summary>
	/// Add an ID from the Vendor (platform).
	/// </summary>
	/// <param name="builder"> The device ID builder. </param>
	/// <returns> The device ID updated with the vendor ID if available. </returns>
	public static DeviceId AddVendorId(this DeviceId builder)
	{
		#if ANDROID
		var context = Android.App.Application.Context;
		var id = Android.Provider.Settings.Secure.GetString(context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
		builder.AddComponent("VendorId", new DeviceIdComponent(id));
		#elif IOS || __MACCATALYST__
		builder.AddComponent("VendorId", new DeviceIdComponent(UIDevice.CurrentDevice.IdentifierForVendor.AsString()));
		#elif WINDOWS
		var systemId = SystemIdentification.GetSystemIdForPublisher();
		if (systemId == null)
		{
			builder.AddComponent("VendorId", new DeviceIdComponent(string.Empty));
		}
		else
		{
			var systemIdBytes = systemId.Id.ToArray();
			var encoder = new Base32ByteArrayEncoder(Base32ByteArrayEncoder.CrockfordAlphabet);
			var id = encoder.Encode(systemIdBytes);
			builder.AddComponent("VendorId", new DeviceIdComponent(id));
		}
		#endif

		return builder;
	}

	#endregion
}