#region References

using System;
using Android.App;
using Android.Content;
using Android.Hardware.Display;
using Android.OS;
using Android.Views;
using Cornerstone.Data.Bytes;
using Cornerstone.Location;
using Cornerstone.Runtime;
using Cornerstone.Security;
using SecureSettings = Android.Provider.Settings.Secure;
using DrawingSize = System.Drawing.Size;

#endregion

namespace Cornerstone.Platforms.Android;

public class AndroidPlatform : CornerstoneObject, IPlatform
{
	#region Constructors

	public AndroidPlatform(DependencyProvider dependencyProvider, RuntimeInformation runtimeInformation)
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
			AddPlatformImplementations();
		}

		base.InitializeLifecycle();
	}

	public override void LoadLifecycle()
	{
		UpdateDeviceDetails();
		UpdateDeviceDisplay();
		UpdateDeviceMemory();
		base.LoadLifecycle();
	}

	private void AddPlatformImplementations()
	{
		//DependencyProvider.AddTransient<AudioPlayer, AndroidAudioPlayer>();
		//DependencyProvider.AddTransient<AudioPlayer, AudioPlayerStub>();
		//DependencyProvider.AddSingleton<FileService, AndroidFileService>();
		DependencyProvider.AddSingleton<ILocationProvider>(() => new AndroidLocationProvider());

		//DependencyProvider.AddSingleton<SecurityCardReader, AndroidSecurityCardReader>();
		//DependencyProvider.AddSingleton<IPermissions, AndroidPermissions>();
		DependencyProvider.AddSingleton<PlatformCredentialVault, AndroidPlatformCredentialVault>();
	}

	private static string GetSystemSetting(string name, bool isGlobal = false)
	{
		if (isGlobal && OperatingSystem.IsAndroidVersionAtLeast(25))
		{
			return global::Android.Provider.Settings.Global.GetString(Application.Context.ContentResolver, name);
		}

		return global::Android.Provider.Settings.System.GetString(Application.Context.ContentResolver, name);
	}

	private void UpdateDeviceDetails()
	{
		var deviceName = GetSystemSetting("device_name", true);
		if (!string.IsNullOrWhiteSpace(deviceName))
		{
			RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.DeviceName), deviceName);
		}

		var deviceId = GetSystemSetting(SecureSettings.AndroidId);
		if (!string.IsNullOrWhiteSpace(deviceId))
		{
			RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.DeviceId), deviceId);
		}
	}

	private void UpdateDeviceDisplay()
	{
		// Application.Context is not a visual Context, so Context.Display / WindowManager
		// window metrics can warn or return arbitrary data. DisplayManager works from the
		// application Context during Application construction (before any Activity exists).
		var displayManager = Application.Context.GetSystemService(Context.DisplayService) as DisplayManager;
		var display = displayManager?.GetDisplay(Display.DefaultDisplay);
		if (display == null)
		{
			return;
		}

		// Nested type Display.Mode shadows the Mode property; use GetMode().
		var mode = display.GetMode();
		if (mode != null)
		{
			RuntimeInformation.SetPlatformOverride(
				nameof(RuntimeInformation.DeviceDisplaySize),
				new DrawingSize(mode.PhysicalWidth, mode.PhysicalHeight));

			RuntimeInformation.SetPlatformOverride(
				nameof(RuntimeInformation.DeviceDisplayRefreshRate),
				(int) Math.Round(mode.RefreshRate));
			return;
		}

		RuntimeInformation.SetPlatformOverride(
			nameof(RuntimeInformation.DeviceDisplayRefreshRate),
			(int) Math.Round(display.RefreshRate));
	}

	private void UpdateDeviceMemory()
	{
		var activityManager = (ActivityManager) Application.Context.GetSystemService(Context.ActivityService);
		if (activityManager != null)
		{
			var memoryInfo = new ActivityManager.MemoryInfo();
			activityManager.GetMemoryInfo(memoryInfo);
			var memory = memoryInfo.TotalMem;

			if ((int) Build.VERSION.SdkInt >= 34)
			{
				#pragma warning disable CA1416
				memory = memoryInfo.AdvertisedMem;
				#pragma warning restore CA1416
			}

			RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.DeviceMemory), ByteSize.FromBytes(memory));
		}
	}

	#endregion
}