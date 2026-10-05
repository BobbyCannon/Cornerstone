#region References

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

#endregion

namespace Cornerstone.Platforms.Android;

[Service(
	Name = "com.bobbycannon.cornerstone.KeepAliveForegroundService",
	Exported = false,
	ForegroundServiceType = ForegroundService.TypeSpecialUse)]
[MetaData("android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE", Value = "Keep the sample clock running while the screen is off.")]
public class KeepAliveForegroundService : Service
{
	#region Constants

	public const string ChannelId = "cornerstone.keep-alive";
	public const int NotificationId = 0x4B41;

	#endregion

	#region Fields

	private PowerManager.WakeLock _wakeLock;

	#endregion

	#region Constructors

	public KeepAliveForegroundService()
	{
		_wakeLock = null;
	}

	#endregion

	#region Methods

	public override IBinder OnBind(Intent intent)
	{
		return null;
	}

	public override void OnDestroy()
	{
		ReleaseWakeLock();
		base.OnDestroy();
	}

	public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
	{
		EnsureChannel();
		var notification = new Notification.Builder(this, ChannelId)
			.SetContentTitle("Sample")
			.SetContentText("Keep running is on")
			.SetSmallIcon(global::Android.Resource.Drawable.StatNotifySync)
			.SetOngoing(true)
			.Build();

		if (OperatingSystem.IsAndroidVersionAtLeast(34))
		{
			StartForeground(NotificationId, notification, ForegroundService.TypeSpecialUse);
		}
		else
		{
			StartForeground(NotificationId, notification);
		}

		AcquireWakeLock();
		return StartCommandResult.Sticky;
	}

	private void AcquireWakeLock()
	{
		if (_wakeLock != null)
		{
			return;
		}

		var power = (PowerManager) GetSystemService(PowerService);
		_wakeLock = power.NewWakeLock(WakeLockFlags.Partial, "Cornerstone:KeepAlive");
		_wakeLock.Acquire();
	}

	private void EnsureChannel()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			return;
		}

		var manager = NotificationManager.FromContext(this);
		if (manager.GetNotificationChannel(ChannelId) != null)
		{
			return;
		}

		manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "Keep running", NotificationImportance.Low));
	}

	private void ReleaseWakeLock()
	{
		if (_wakeLock == null)
		{
			return;
		}

		if (_wakeLock.IsHeld)
		{
			_wakeLock.Release();
		}

		_wakeLock = null;
	}

	#endregion
}
