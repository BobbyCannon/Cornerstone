#region References

using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Security.Authorization.AppCapabilityAccess;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

public class WindowsPermissions : Permissions
{
	#region Constructors

	public WindowsPermissions(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override Task<PermissionStatus> CheckPermissionAsync(PermissionType type)
	{
		// The permission name is the capability name found the manifest file
		var response = type switch
		{
			PermissionType.Camera => CheckCapability("webcam"),
			PermissionType.Location => CheckCapability("location"),
			PermissionType.Microphone => CheckCapability("microphone"),
			PermissionType.Video => CheckCapability("webcam"),
			_ => PermissionStatus.Unknown
		};

		return Task.FromResult(response);
	}

	public async Task<PermissionStatus> RequestLocationPermissionAsync()
	{
		try
		{
			var status = await Geolocator.RequestAccessAsync();
			return status switch
			{
				GeolocationAccessStatus.Allowed => PermissionStatus.Granted,
				GeolocationAccessStatus.Denied => PermissionStatus.Denied,
				_ => PermissionStatus.Unknown
			};
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	public override async Task<PermissionStatus> RequestPermissionAsync(PermissionType type)
	{
		return type switch
		{
			PermissionType.Camera => await RequestCapability("webcam"),
			PermissionType.Location => await RequestLocationPermissionAsync(),
			PermissionType.Microphone => await RequestCapability("microphone"),
			PermissionType.Video => await RequestCapability("webcam"),
			_ => PermissionStatus.Unknown
		};
	}

	private PermissionStatus CheckCapability(string capabilityName)
	{
		try
		{
			var capability = AppCapability.Create(capabilityName);
			var result = capability.CheckAccess();
			return result switch
			{
				AppCapabilityAccessStatus.Allowed => PermissionStatus.Granted,
				AppCapabilityAccessStatus.DeniedByUser => PermissionStatus.Denied,
				AppCapabilityAccessStatus.DeniedBySystem => PermissionStatus.Denied,
				AppCapabilityAccessStatus.NotDeclaredByApp => PermissionStatus.Unknown,
				AppCapabilityAccessStatus.UserPromptRequired => PermissionStatus.Prompt,
				_ => PermissionStatus.Unknown
			};
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	private async Task<PermissionStatus> RequestCapability(string capabilityName)
	{
		try
		{
			var results = await AppCapability.RequestAccessForCapabilitiesAsync([capabilityName]);
			if (!results.TryGetValue(capabilityName, out var result))
			{
				return PermissionStatus.Unknown;
			}

			return result switch
			{
				AppCapabilityAccessStatus.Allowed => PermissionStatus.Granted,
				AppCapabilityAccessStatus.DeniedByUser => PermissionStatus.Denied,
				AppCapabilityAccessStatus.DeniedBySystem => PermissionStatus.Denied,
				AppCapabilityAccessStatus.NotDeclaredByApp => PermissionStatus.Unknown,
				AppCapabilityAccessStatus.UserPromptRequired => PermissionStatus.Prompt,
				_ => PermissionStatus.Unknown
			};
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	#endregion
}