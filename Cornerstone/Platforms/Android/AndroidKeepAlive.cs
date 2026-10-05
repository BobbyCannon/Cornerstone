#region References

using System;
using Android.App;
using Android.Content;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Android;

/// <summary>
/// Starts a foreground service and a partial wakelock so Stopwatch can keep ticking with the screen off.
/// </summary>
public class AndroidKeepAlive : IKeepAlive
{
	#region Fields

	private bool _isActive;

	#endregion

	#region Constructors

	public AndroidKeepAlive()
	{
		_isActive = false;
	}

	#endregion

	#region Properties

	public bool IsActive => _isActive;

	public bool IsSupported => true;

	#endregion

	#region Methods

	public void Start()
	{
		var context = Application.Context;
		var intent = new Intent(context, typeof(KeepAliveForegroundService));

		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			context.StartForegroundService(intent);
		}
		else
		{
			context.StartService(intent);
		}

		_isActive = true;
	}

	public void Stop()
	{
		var context = Application.Context;
		context.StopService(new Intent(context, typeof(KeepAliveForegroundService)));
		_isActive = false;
	}

	#endregion
}
