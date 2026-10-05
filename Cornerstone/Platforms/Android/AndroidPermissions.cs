#region References

using System;
using System.Threading.Tasks;
using Android;
using Android.App;
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

			var context = AndroidPlatform.Activity ?? Application.Context;
			var result = ContextCompat.CheckSelfPermission(context, permission);
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
			var activity = AndroidPlatform.Activity;
			if ((activity != null) && (ContextCompat.CheckSelfPermission(activity, permission) == Permission.Granted))
			{
				return AddOrUpdateCache(type, PermissionStatus.Granted);
			}

			if (activity == null)
			{
				return AddOrUpdateCache(type, PermissionStatus.Unknown);
			}

			_completionSource = new TaskCompletionSource<PermissionStatus>();
			ActivityCompat.RequestPermissions(activity, [permission], RequestCode);

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
			PermissionType.NearFieldCommunications => Manifest.Permission.Nfc,
			_ => null
		};

		return name != null;
	}

	#endregion
}