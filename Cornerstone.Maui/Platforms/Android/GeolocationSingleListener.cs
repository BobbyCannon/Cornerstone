#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Cornerstone.Maui.Abstractions;
using Microsoft.Maui.Devices.Sensors;
using AndroidLocation = Android.Locations.Location;
using Object = Java.Lang.Object;

#endregion

namespace Cornerstone.Maui.Platforms.Android;

internal class GeolocationSingleListener : Object, ILocationListener
{
	#region Fields

	private readonly HashSet<string> _activeProviders;
	private AndroidLocation _bestLocation;
	private readonly TaskCompletionSource<Position> _completionSource;
	private readonly float _desiredAccuracy;
	private readonly Action _finishedCallback;
	private readonly object _locationSync;
	private readonly Timer _timer;

	#endregion

	#region Constructors

	public GeolocationSingleListener(LocationManager manager, float desiredAccuracy, int timeout, IEnumerable<string> activeProviders, Action finishedCallback)
	{
		_locationSync = new();
		_desiredAccuracy = desiredAccuracy;
		_finishedCallback = finishedCallback;

		_activeProviders = [];
		_completionSource = new();
		_activeProviders = [..activeProviders];

		foreach (var provider in _activeProviders)
		{
			var location = manager.GetLastKnownLocation(provider);
			if ((location != null) && location.IsBetterLocation(_bestLocation))
			{
				_bestLocation = location;
			}
		}

		if (timeout != Timeout.Infinite)
		{
			_timer = new Timer(TimesUp, null, timeout, 0);
		}
	}

	#endregion

	#region Properties

	public Task<Position> Task => _completionSource.Task;

	#endregion

	#region Methods

	public void Cancel()
	{
		_completionSource.TrySetCanceled();
	}

	public void OnLocationChanged(AndroidLocation location)
	{
		if (location.Accuracy <= _desiredAccuracy)
		{
			Finish(location);
			return;
		}

		lock (_locationSync)
		{
			if (location.IsBetterLocation(_bestLocation))
			{
				_bestLocation = location;
			}
		}
	}

	public void OnProviderDisabled(string provider)
	{
		lock (_activeProviders)
		{
			if (_activeProviders.Remove(provider) && (_activeProviders.Count == 0))
			{
				_completionSource.TrySetException(new GeolocationException(GeolocationError.PositionUnavailable));
			}
		}
	}

	public void OnProviderEnabled(string provider)
	{
		lock (_activeProviders)
		{
			_activeProviders.Add(provider);
		}
	}

	public void OnStatusChanged(string provider, Availability status, Bundle extras)
	{
		switch (status)
		{
			case Availability.Available:
				OnProviderEnabled(provider);
				break;

			case Availability.OutOfService:
				OnProviderDisabled(provider);
				break;
		}
	}

	private void Finish(AndroidLocation location)
	{
		_finishedCallback?.Invoke();
		_completionSource.TrySetResult(location.ToPosition());
	}

	private void TimesUp(object state)
	{
		lock (_locationSync)
		{
			if (_bestLocation == null)
			{
				if (_completionSource.TrySetCanceled())
				{
					_finishedCallback?.Invoke();
				}
			}
			else
			{
				Finish(_bestLocation);
			}
		}
	}

	#endregion
}