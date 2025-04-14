#region References

using System;
using System.Threading.Tasks;
using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Permission = Android.Content.PM.Permission;

#endregion

namespace Cornerstone.Platforms.Android;

public class AndroidPermissions : Permissions
{
	#region Constants

	private const int RequestCode = 100;

	#endregion

	#region Fields

	private TaskCompletionSource<PermissionStatus> _completionSource;

	#endregion

	#region Constructors

	public AndroidPermissions(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override Task<PermissionStatus> CheckPermissionAsync(PermissionType type)
	{
		try
		{
			if (!TryGetPermissionName(type, out var permission))
			{
				return Task.FromResult(AddOrUpdateCache(type, PermissionStatus.Unknown));
			}

			var result = ContextCompat.CheckSelfPermission(AndroidPlatform.Activity, permission);
			return Task.FromResult(
				AddOrUpdateCache(type,
					result == Permission.Granted
						? PermissionStatus.Granted
						: PermissionStatus.Denied)
			);
		}
		catch (Exception)
		{
			return Task.FromResult(PermissionStatus.Unknown);
		}
	}

	public void OnRequestPermissionResult(int requestCode, string[] permissions, Permission[] grantResults)
	{
		if ((requestCode != RequestCode) || (_completionSource == null))
		{
			return;
		}

		// Check the result of the permission request
		var status = (grantResults.Length > 0) && (grantResults[0] == Permission.Granted)
			? PermissionStatus.Granted
			: PermissionStatus.Denied;

		// Set the result to complete the Task
		_completionSource.TrySetResult(status);
		_completionSource = null; // Reset to avoid reuse
	}

	public override async Task<PermissionStatus> RequestPermissionAsync(PermissionType type)
	{
		try
		{
			if (!TryGetPermissionName(type, out var permission))
			{
				return AddOrUpdateCache(type, PermissionStatus.Unknown);
			}

			// Check current permission status
			if (ContextCompat.CheckSelfPermission(AndroidPlatform.Activity, permission) == Permission.Granted)
			{
				return AddOrUpdateCache(type, PermissionStatus.Granted);
			}

			// If permission is denied, request it
			_completionSource = new TaskCompletionSource<PermissionStatus>();
			ActivityCompat.RequestPermissions(AndroidPlatform.Activity, [permission], RequestCode);

			// Wait for the result from OnRequestPermissionsResult
			var result = await _completionSource.Task;
			return AddOrUpdateCache(type, result);
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	private bool TryGetPermissionName(PermissionType type, out string name)
	{
		name = type switch
		{
			PermissionType.Camera => Manifest.Permission.Camera,
			PermissionType.Notifications => Manifest.Permission.PostNotifications,
			PermissionType.Location => Manifest.Permission.AccessFineLocation,
			PermissionType.Microphone => Manifest.Permission.RecordAudio,
			_ => null
		};

		return name != null;
	}

	#endregion
}