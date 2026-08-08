#region References

using System;
using System.Drawing;
using Cornerstone.Location;
using Cornerstone.Runtime;
using Cornerstone.Security;
using UIKit;

#endregion

namespace Cornerstone.Platforms.iOS;

public class IOSPlatform : CornerstoneObject, IPlatform
{
	#region Constructors

	public IOSPlatform(DependencyProvider dependencyProvider, RuntimeInformation runtimeInformation)
	{
		DependencyProvider = dependencyProvider;
		RuntimeInformation = runtimeInformation;
	}

	#endregion

	#region Properties

	public DependencyProvider DependencyProvider { get; }

	public RuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		if (!IsLifecycleInitialized())
		{
			DeviceId.VendorId = UIDevice.CurrentDevice.IdentifierForVendor?.AsString();
			AddPlatformImplementations();
			RestrictPlatformLinkingRemoval();
		}

		base.InitializeLifecycle();
	}

	public static bool IsVersionOrHigher(int majorVersion, int minorVersion = 0)
	{
		var systemVersion = UIDevice.CurrentDevice.SystemVersion;
		if (Version.TryParse(systemVersion, out var version))
		{
			return (version.Major > majorVersion)
				|| ((version.Major == majorVersion)
					&& (version.Minor >= minorVersion));
		}

		return false;
	}

	public override void LoadLifecycle()
	{
		UpdateDeviceDisplay();
		base.LoadLifecycle();
	}

	private void AddPlatformImplementations()
	{
		DependencyProvider.AddSingleton<ILocationProvider, IOSLocationProvider>();

		//DependencyProvider.AddSingleton<SecurityCardReader, IOSSecurityCardReader>();
		//DependencyProvider.AddSingleton<IPermissions, IOSPermissions>();
		DependencyProvider.AddSingleton<PlatformCredentialVault, IOSPlatformCredentialVault>();
	}

	private static void RestrictPlatformLinkingRemoval()
	{
		//_platformLinkingHack = new PlatformLinkingHack();
	}

	private void UpdateDeviceDisplay()
	{
		var screen = UIScreen.MainScreen;
		if (screen == null)
		{
			return;
		}

		var bounds = screen.Bounds;
		var scale = (double) screen.NativeScale;

		RuntimeInformation.SetPlatformOverride(
			nameof(RuntimeInformation.DeviceDisplaySize),
			new Size(
				(int) Math.Round(bounds.Width * scale),
				(int) Math.Round(bounds.Height * scale)));

		// Maximum preferred rate for the panel (e.g. 60 or 120 on ProMotion).
		RuntimeInformation.SetPlatformOverride(
			nameof(RuntimeInformation.DeviceDisplayRefreshRate),
			(int) screen.MaximumFramesPerSecond);
	}

	#endregion
}