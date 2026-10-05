#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Permissions = Cornerstone.Runtime.Permissions;
using PermissionStatus = Cornerstone.Runtime.PermissionStatus;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

#endregion

namespace Cornerstone.Platforms.iOS;

public class IOSPermissions : Permissions
{
	#region Constructors

	[DependencyInjectionConstructor]
	public IOSPermissions(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override async Task<PermissionStatus> CheckPermissionAsync(PermissionType type)
	{
		try
		{
			switch (type)
			{
				case PermissionType.Camera:
				case PermissionType.Video:
				{
					var cameraStatus = await MauiPermissions.CheckStatusAsync<MauiPermissions.Camera>();
					return ToPermissionStatus(cameraStatus);
				}
				case PermissionType.Microphone:
				{
					var micStatus = await MauiPermissions.CheckStatusAsync<MauiPermissions.Microphone>();
					return ToPermissionStatus(micStatus);
				}
				case PermissionType.Location:
				{
					var locationStatus = await MauiPermissions.CheckStatusAsync<MauiPermissions.LocationWhenInUse>();
					return ToPermissionStatus(locationStatus);
				}
				case PermissionType.Notifications:
				{
					var notificationStatus = await MauiPermissions.CheckStatusAsync<MauiPermissions.PostNotifications>();
					return ToPermissionStatus(notificationStatus);
				}
				case PermissionType.Storage:
				{
					var storageStatus = await MauiPermissions.CheckStatusAsync<MauiPermissions.Photos>();
					return ToPermissionStatus(storageStatus);
				}
				case PermissionType.ClipboardRead:
				case PermissionType.ClipboardWrite:
				{
					// Clipboard access doesn't require permission on iOS
					return PermissionStatus.Granted;
				}
				case PermissionType.Accelerometer:
				case PermissionType.Gyroscope:
				case PermissionType.Magnetometer:
				case PermissionType.NearFieldCommunications:

				{
					return PermissionStatus.Granted;
				}
				case PermissionType.Unknown:
				default:
				{
					return PermissionStatus.Unknown;
				}
			}
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	public override async Task<PermissionStatus> RequestPermissionAsync(PermissionType type)
	{
		try
		{
			switch (type)
			{
				case PermissionType.Camera:
				case PermissionType.Video:
				{
					var cameraStatus = await MauiPermissions.RequestAsync<MauiPermissions.Camera>();
					return ToPermissionStatus(cameraStatus);
				}
				case PermissionType.Microphone:
				{
					var micStatus = await MauiPermissions.RequestAsync<MauiPermissions.Microphone>();
					return ToPermissionStatus(micStatus);
				}
				case PermissionType.Location:
				{
					var locationStatus = await MauiPermissions.RequestAsync<MauiPermissions.LocationWhenInUse>();
					return ToPermissionStatus(locationStatus);
				}
				case PermissionType.Notifications:
				{
					var notificationStatus = await MauiPermissions.RequestAsync<MauiPermissions.PostNotifications>();
					return ToPermissionStatus(notificationStatus);
				}
				case PermissionType.Storage:
				{
					var storageStatus = await MauiPermissions.RequestAsync<MauiPermissions.Photos>();
					return ToPermissionStatus(storageStatus);
				}
				case PermissionType.ClipboardRead:
				case PermissionType.ClipboardWrite:
				{
					// Clipboard access doesn't require permission
					return PermissionStatus.Granted;
				}
				case PermissionType.Accelerometer:
				case PermissionType.Gyroscope:
				case PermissionType.Magnetometer:
				case PermissionType.NearFieldCommunications:
				{
					return PermissionStatus.Granted;
				}
				case PermissionType.Unknown:
				default:
				{
					return PermissionStatus.Unknown;
				}
			}
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	private PermissionStatus ToPermissionStatus(Microsoft.Maui.ApplicationModel.PermissionStatus status)
	{
		return status switch
		{
			Microsoft.Maui.ApplicationModel.PermissionStatus.Granted => PermissionStatus.Granted,
			Microsoft.Maui.ApplicationModel.PermissionStatus.Denied => PermissionStatus.Denied,
			Microsoft.Maui.ApplicationModel.PermissionStatus.Disabled => PermissionStatus.Restricted,
			Microsoft.Maui.ApplicationModel.PermissionStatus.Restricted => PermissionStatus.Restricted,
			_ => PermissionStatus.Unknown
		};
	}

	#endregion
}