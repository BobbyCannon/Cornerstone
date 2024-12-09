#region References

using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

#endregion

namespace Avalonia.Sample.Android;

[Service(
	Name = "Cornerstone.Sample.Service",
	ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSpecialUse
)]
public class ForegroundService : Service
{
	#region Fields

	private readonly string _notificationChannelId = "1000";
	private readonly string _notificationChannelName = "notification";
	private readonly int _notificationId = 1;

	#endregion

	#region Methods

	public override IBinder OnBind(Intent intent)
	{
		return null;
	}

	public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
	{
		StartForegroundService();
		return StartCommandResult.NotSticky;
	}

	private void CreateNotificationChannel(NotificationManager notificationMnaManager)
	{
		var channel = new NotificationChannel(_notificationChannelId, _notificationChannelName, NotificationImportance.Low);
		notificationMnaManager.CreateNotificationChannel(channel);
	}

	private void StartForegroundService()
	{
		var notificationManager = GetSystemService(NotificationService) as NotificationManager;

		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			CreateNotificationChannel(notificationManager);
		}

		var notification = new NotificationCompat.Builder(this, _notificationChannelId);
		notification.SetAutoCancel(false);
		notification.SetOngoing(true);
		notification.SetSmallIcon(ResourceConstant.Drawable.Icon);
		notification.SetContentTitle("Cornerstone Sample");
		notification.SetContentText("Foreground Service is running");
		
		if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
		{
			StartForeground(_notificationId, notification.Build(),
				global::Android.Content.PM.ForegroundService.TypeLocation
				| global::Android.Content.PM.ForegroundService.TypeMicrophone
			);
		}
		else
		{
			StartForeground(_notificationId, notification.Build());
		}
	}

	#endregion
}