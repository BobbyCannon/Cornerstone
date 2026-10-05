#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Platforms.Browser;

/// <summary>
/// https://developer.mozilla.org/en-US/docs/Web/API/Permissions_API
/// </summary>
public class BrowserPermissions : Permissions
{
	#region Fields

	private readonly IBrowserInterop _browserInterop;

	#endregion

	#region Constructors

	public BrowserPermissions(IBrowserInterop browserInterop, IDispatcher dispatcher) : base(dispatcher)
	{
		_browserInterop = browserInterop;
	}

	#endregion

	#region Methods

	public override async Task<PermissionStatus> CheckPermissionAsync(PermissionType type)
	{
		if (!TryGetPermissionName(type, out var permission))
		{
			return PermissionStatus.Unknown;
		}

		try
		{
			var status = await _browserInterop.CheckPermission(permission);
			return MapPermissionStatus(status);
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
			return type switch
			{
				PermissionType.Camera => await RequestMediaPermissionAsync("video"),
				PermissionType.Location => await RequestLocationPermissionAsync(),
				PermissionType.Microphone => await RequestMediaPermissionAsync("audio"),
				PermissionType.Video => await RequestMediaPermissionAsync("video"),
				_ => PermissionStatus.Unknown
			};
		}
		catch (Exception)
		{
			return PermissionStatus.Unknown;
		}
	}

	private PermissionStatus MapPermissionStatus(string status)
	{
		return status switch
		{
			"granted" => PermissionStatus.Granted,
			"denied" => PermissionStatus.Denied,
			"prompt" => PermissionStatus.Prompt,
			_ => PermissionStatus.Unknown
		};
	}

	private Task<PermissionStatus> RequestLocationPermissionAsync()
	{
		try
		{
			// Attempt to get location; browser will prompt if permission isn’t granted
			_browserInterop.GetWindowLocation();
			return Task.FromResult(PermissionStatus.Granted);
		}
		catch (Exception ex)
		{
			return Task.FromResult(
				ex.Message.Contains("denied")
					? PermissionStatus.Denied
					: PermissionStatus.Unknown
			);
		}
	}

	private async Task<PermissionStatus> RequestMediaPermissionAsync(string mediaType)
	{
		try
		{
			// Request media access; browser will prompt if permission isn’t granted
			var response = await _browserInterop.RequestMediaPermission(mediaType);
			return MapPermissionStatus(response);
		}
		catch (Exception ex)
		{
			return ex.Message.Contains("denied")
				? PermissionStatus.Denied
				: PermissionStatus.Unknown;
		}
	}

	private bool TryGetPermissionName(PermissionType type, out string name)
	{
		name = type switch
		{
			PermissionType.Accelerometer => "accelerometer",
			PermissionType.Camera => "camera",
			PermissionType.ClipboardRead => "clipboard-read",
			PermissionType.ClipboardWrite => "clipboard-write",
			PermissionType.Gyroscope => "gyroscope",
			PermissionType.Location => "geolocation",
			PermissionType.Magnetometer => "magnetometer",
			PermissionType.Microphone => "microphone",
			PermissionType.Notifications => "notifications",
			PermissionType.Storage => "persistent-storage",
			PermissionType.Video => "camera",
			_ => null
		};

		return name != null;
	}

	#endregion
}