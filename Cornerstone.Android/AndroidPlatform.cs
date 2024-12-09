#region References

using System.Drawing;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;
using Cornerstone.Data.Bytes;
using Cornerstone.Location;
using Cornerstone.Media;
using Cornerstone.Runtime;
using Point = Android.Graphics.Point;
using SettingSecure = Android.Provider.Settings.Secure;

#endregion

namespace Cornerstone.Android;

public static class AndroidPlatform
{
	#region Properties

	public static AppCompatActivity Activity { get; private set; }

	public static Context ApplicationContext { get; private set; }

	public static DependencyProvider DependencyProvider { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(AppCompatActivity activity, DependencyProvider dependencyProvider)
	{
		Activity = activity;
		ApplicationContext = activity.ApplicationContext;
		DependencyProvider = dependencyProvider;

		DeviceId.VendorId = SettingSecure.GetString(ApplicationContext?.ContentResolver, SettingSecure.AndroidId);

		UpdateDeviceDisplaySize(activity);
		UpdateDeviceMemory();

		AddPlatformImplementations();
	}

	private static void AddPlatformImplementations()
	{
		DependencyProvider.AddSingleton<ILocationProvider>(() => new AndroidLocationProvider(Activity));
		DependencyProvider.AddSingleton<AudioPlayer, AndroidAudioPlayer>();
	}

	private static void UpdateDeviceDisplaySize(Activity activity)
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
		{
			var size = new Point();

			#pragma warning disable CA1422
			activity.WindowManager?.DefaultDisplay?.GetRealSize(size);
			#pragma warning restore CA1422

			if (size.X > 0)
			{
				RuntimeInformation.SetPlatformOverride(x => x.DeviceDisplaySize, new Size(size.X, size.Y));
			}
		}
	}

	private static void UpdateDeviceMemory()
	{
		var activityManager = (ActivityManager) ApplicationContext?.GetSystemService(Context.ActivityService);
		if (activityManager != null)
		{
			var memoryInfo = new ActivityManager.MemoryInfo();
			activityManager.GetMemoryInfo(memoryInfo);
			var memory = memoryInfo.TotalMem;

			try
			{
				memory = memoryInfo.AdvertisedMem;
			}
			catch
			{
				// Just ignore
			}

			RuntimeInformation.SetPlatformOverride(x => x.DeviceMemory, ByteSize.FromBytes(memory));
		}
	}

	#endregion
}