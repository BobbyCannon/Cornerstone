#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Android.Locations;
using Android.OS;
using Cornerstone.Maui.Abstractions;
using Microsoft.Maui.Devices.Sensors;
using AndroidLocation = Android.Locations.Location;
using Object = Java.Lang.Object;

#endregion

namespace Cornerstone.Maui.Platforms.Android;

internal class GeolocationContinuousListener : Object, ILocationListener
{
	#region Fields

	private string _activeProvider;
	private readonly HashSet<string> _activeProviders;
	private AndroidLocation _lastLocation;
	private readonly LocationManager _manager;
	private IList<string> _providers;
	private readonly TimeSpan _timePeriod;

	#endregion

	#region Constructors

	public GeolocationContinuousListener(LocationManager manager, TimeSpan timePeriod, IList<string> providers)
	{
		_activeProviders = new();
		_manager = manager;
		_timePeriod = timePeriod;
		_providers = providers;

		foreach (var p in providers)
		{
			if (manager.IsProviderEnabled(p))
			{
				_activeProviders.Add(p);
			}
		}
	}

	#endregion

	#region Methods

	public void OnLocationChanged(AndroidLocation location)
	{
		if (location.Provider != _activeProvider)
		{
			if ((_activeProvider != null)
				&& _manager.IsProviderEnabled(_activeProvider))
			{
				var elapsed = GetTimeSpan(location.Time) - GetTimeSpan(_lastLocation.Time);
				bool currentIsMoreAccurate;

				if (OperatingSystem.IsAndroidVersionAtLeast(31))
				{
					#pragma warning disable CA1422
					var currentProviderAccuracy = _manager.GetProvider(location.Provider)?.Accuracy;
					var activeProviderAccuracy = _manager.GetProvider(_activeProvider)?.Accuracy;
					currentIsMoreAccurate = currentProviderAccuracy > activeProviderAccuracy;
					#pragma warning restore CA1422
				}
				else
				{
					var currentProviderAccuracy = _manager.GetProviderProperties(location.Provider)?.Accuracy;
					var activeProviderAccuracy = _manager.GetProviderProperties(_activeProvider)?.Accuracy;
					currentIsMoreAccurate = currentProviderAccuracy > activeProviderAccuracy;
				}

				if (currentIsMoreAccurate && (elapsed < _timePeriod.Add(_timePeriod)))
				{
					location.Dispose();
					return;
				}
			}

			_activeProvider = location.Provider;
		}

		var previous = Interlocked.Exchange(ref _lastLocation, location);
		if (previous != null)
		{
			previous.Dispose();
		}

		PositionChanged?.Invoke(this, new PositionEventArgs(location.ToPosition()));
	}

	public void OnProviderDisabled(string provider)
	{
		if (provider == LocationManager.PassiveProvider)
		{
			return;
		}

		lock (_activeProviders)
		{
			if (_activeProviders.Remove(provider) && (_activeProviders.Count == 0))
			{
				OnPositionError(new PositionErrorEventArgs(GeolocationError.PositionUnavailable));
			}
		}
	}

	public void OnProviderEnabled(string provider)
	{
		if (provider == LocationManager.PassiveProvider)
		{
			return;
		}

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

	private TimeSpan GetTimeSpan(long time)
	{
		return new(TimeSpan.TicksPerMillisecond * time);
	}

	private void OnPositionError(PositionErrorEventArgs e)
	{
		PositionError?.Invoke(this, e);
	}

	#endregion

	#region Events

	public event EventHandler<PositionEventArgs> PositionChanged;

	public event EventHandler<PositionErrorEventArgs> PositionError;

	#endregion
}