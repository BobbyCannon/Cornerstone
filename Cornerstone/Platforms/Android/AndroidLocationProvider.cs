﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using AndroidX.AppCompat.App;
using Cornerstone.Platforms.Android.Internal;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Location;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Microsoft.Maui.ApplicationModel;
using Debug = System.Diagnostics.Debug;
using LocationRequest = Android.Gms.Location.LocationRequest;

#endregion

namespace Cornerstone.Platforms.Android;

/// <summary>
/// Implementation for LocationProvider
/// </summary>
public class AndroidLocationProvider : AndroidLocationProvider<Location.Location, IHorizontalLocation, IVerticalLocation, LocationProviderSettings>, ILocationProvider
{
	#region Constructors

	/// <summary>
	/// Constructor for Implementation
	/// </summary>
	public AndroidLocationProvider(AppCompatActivity activity) : base(activity)
	{
	}

	#endregion
}

/// <summary>
/// Implementation for LocationProvider
/// </summary>
public class AndroidLocationProvider<TLocation, THorizontal, TVertical, TLocationProviderSettings>
	: LocationProvider<TLocation, THorizontal, TVertical, TLocationProviderSettings>
	where TLocation : class, ILocation<THorizontal, TVertical>, IUpdateable, new()
	where THorizontal : class, IHorizontalLocation, IUpdateable
	where TVertical : class, IVerticalLocation, IUpdateable
	where TLocationProviderSettings : ILocationProviderSettings, IBindable, new()
{
	#region Fields

	private readonly AppCompatActivity _activity;
	private FusedLocationProviderCallback _fusedCallback;
	private IFusedLocationProviderClient _fusedListener;
	private GeolocationContinuousListener<TLocation, THorizontal, TVertical> _listener;
	private LocationManager _locationManager;
	private readonly object _positionSync;
	private IDictionary<string, SourceInformationProvider> _sourceProviders;
	private bool _usingGooglePlayFused;

	#endregion

	#region Constructors

	/// <summary>
	/// Default constructor
	/// </summary>
	protected AndroidLocationProvider(AppCompatActivity activity) : base(null)
	{
		_activity = activity;
		_positionSync = new object();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override bool IsLocationAvailable => SubProviders.Any();

	/// <inheritdoc />
	public override bool IsLocationEnabled => SubProviders.Any(x => x.IsEnabled && Manager.IsProviderEnabled(x.ProviderName));

	/// <inheritdoc />
	public override string ProviderName => "Cornerstone.Android";

	/// <inheritdoc />
	public override IEnumerable<IInformationProvider> SubProviders
	{
		get
		{
			if (_sourceProviders == null)
			{
				var defaultEnabled = new[] { LocationManager.GpsProvider };
				_sourceProviders = Manager
					.GetProviders(false)
					.Where(x =>
						(x != LocationManager.PassiveProvider)
						&& (x != LocationManager.FusedProvider)
					)
					.Select(x => new SourceInformationProvider
					{
						ProviderName = x,
						IsEnabled = defaultEnabled.Contains(x)
					})
					.ToDictionary(x => x.ProviderName, x => x);

				if (_sourceProviders.All(x => x.Key != LocationManager.FusedProvider))
				{
					var fusedSource = new SourceInformationProvider
					{
						IsEnabled = true,
						ProviderName = LocationManager.FusedProvider
					};
					_sourceProviders.Add(fusedSource.ProviderName, fusedSource);
					_usingGooglePlayFused = true;
				}

				OnPropertyChanged(nameof(HasSubProviders));
			}

			return _sourceProviders.Values;
		}
	}

	/// <summary>
	/// The android location manager.
	/// </summary>
	private LocationManager Manager => _locationManager ??= (LocationManager) Application.Context.GetSystemService(Context.LocationService);

	#endregion

	#region Methods

	/// <summary>
	/// Gets position async with specified parameters
	/// </summary>
	/// <param name="timeout"> Timeout to wait, Default Infinite </param>
	/// <param name="cancelToken"> Cancellation token </param>
	/// <returns> ProviderLocation </returns>
	/// <remarks>
	/// bug: we must work on thread safety of this method.
	/// Touching "Manager", which is global, can be very dangerous.
	/// </remarks>
	public override Task<TLocation> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null)
	{
		global::Android.Locations.Location bestLocation = null;

		foreach (var source in SubProviders)
		{
			var location = Manager.GetLastKnownLocation(source.ProviderName);

			if ((location != null) && location.IsBetterLocation(bestLocation))
			{
				bestLocation = location;
			}
		}

		var response = bestLocation?.ToPosition<TLocation, THorizontal, TVertical>(bestLocation.Provider);

		if (response != null)
		{
			CurrentValue.UpdateWith(response);
		}

		return Task.FromResult(response);
	}

	/// <inheritdoc />
	public override Task StartMonitoringAsync()
	{
		if (IsMonitoring)
		{
			return Task.CompletedTask;
		}

		if (!IsGooglePlayServicesInstalled())
		{
			ListenerPositionError(this, LocationProviderError.MissingDependency);
			return Task.CompletedTask;
		}

		LocationProviderSettings.Cleanup();

		HasPermission = LocationProviderSettings.RequireLocationAlwaysPermission
			? CheckAlwaysPermissions()
			: CheckWhenInUsePermission();

		if (!HasPermission)
		{
			ListenerPositionError(this, LocationProviderError.Unauthorized);
			return Task.CompletedTask;
		}

		var sources = SubProviders.Cast<SourceInformationProvider>().ToArray();
		var looper = Looper.MyLooper() ?? Looper.MainLooper;

		if ((_activity != null)
			&& _usingGooglePlayFused
			&& _sourceProviders[LocationManager.FusedProvider].IsEnabled)
		{
			_fusedListener = LocationServices.GetFusedLocationProviderClient(_activity);
			var builder = new LocationRequest.Builder(Priority.PriorityHighAccuracy, (long) LocationProviderSettings.MinimumTime.TotalMilliseconds);
			builder.SetMinUpdateDistanceMeters(LocationProviderSettings.MinimumDistance);
			builder.SetMinUpdateIntervalMillis(LocationProviderSettings.MinimumTime.Milliseconds);
			_fusedCallback = new FusedLocationProviderCallback(FusedLocationProviderLocationChanged);
			_fusedListener.RequestLocationUpdates(builder.Build(), _fusedCallback, looper);
			_sourceProviders[LocationManager.FusedProvider].IsMonitoring = true;
		}

		_listener = new GeolocationContinuousListener<TLocation, THorizontal, TVertical>(GetDispatcher(), ProviderName, Manager, sources);
		_listener.LogEventWritten += ListenerOnLogEventWritten;
		_listener.PositionChanged += ListenerPositionChanged;
		_listener.PositionError += ListenerPositionError;

		for (var i = 0; i < sources.Length; ++i)
		{
			var source = sources[i];

			if ((source.ProviderName == LocationManager.FusedProvider) && _usingGooglePlayFused)
			{
				// This provider is handled above, differently
				continue;
			}

			if (!source.IsEnabled)
			{
				source.IsEnabled = false;
				continue;
			}

			Manager.RequestLocationUpdates(source.ProviderName,
				(long) LocationProviderSettings.MinimumTime.TotalMilliseconds,
				LocationProviderSettings.MinimumDistance,
				_listener,
				looper);

			source.IsMonitoring = true;
		}

		Status = "Is Monitoring";
		IsMonitoring = true;

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public override Task StopMonitoringAsync()
	{
		if (_listener == null)
		{
			return Task.CompletedTask;
		}

		_listener.LogEventWritten -= ListenerOnLogEventWritten;
		_listener.PositionChanged -= ListenerPositionChanged;
		_listener.PositionError -= ListenerPositionError;

		if (_fusedListener != null)
		{
			_sourceProviders[LocationManager.FusedProvider].IsMonitoring = false;
			_fusedListener.RemoveLocationUpdates(_fusedCallback);

			_fusedListener.Dispose();
			_fusedCallback.Dispose();

			_fusedListener = null;
			_fusedCallback = null;
		}

		try
		{
			Manager.RemoveUpdates(_listener);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Unable to remove updates: {ex}");
		}

		SubProviders.Cast<SourceInformationProvider>().ForEach(x => x.IsMonitoring = false);

		_listener = null;

		Status = string.Empty;
		IsMonitoring = false;

		return Task.CompletedTask;
	}

	private bool CheckAlwaysPermissions()
	{
		var permissionStatus = Permissions
			.CheckStatusAsync<Permissions.LocationWhenInUse>()
			.AwaitResults(TimeSpan.FromSeconds(1));

		if (permissionStatus == PermissionStatus.Granted)
		{
			return true;
		}

		Status = "Currently does not have Location permissions, requesting permissions";

		permissionStatus = Permissions
			.RequestAsync<Permissions.LocationAlways>()
			.AwaitResults(new TimeSpan(0, 0, 1));

		if (permissionStatus == PermissionStatus.Granted)
		{
			return true;
		}

		Status = "Location permission denied.";
		return false;
	}

	private bool CheckWhenInUsePermission()
	{
		var permissionStatus = Permissions
			.CheckStatusAsync<Permissions.LocationWhenInUse>()
			.AwaitResults(TimeSpan.FromSeconds(1));

		if (permissionStatus == PermissionStatus.Granted)
		{
			return true;
		}

		Status = "Currently does not have Location permissions, requesting permissions";

		permissionStatus = Permissions
			.RequestAsync<Permissions.LocationWhenInUse>()
			.AwaitResults(TimeSpan.FromSeconds(1));

		if (permissionStatus == PermissionStatus.Granted)
		{
			return true;
		}

		Status = "Location permission denied.";
		return false;
	}

	private void FusedLocationProviderLocationChanged(global::Android.Locations.Location obj)
	{
		var location = obj.ToPosition<TLocation, THorizontal, TVertical>(ProviderName);
		UpdateCurrentValue(location);
	}

	private bool IsGooglePlayServicesInstalled()
	{
		var queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(_activity);
		if (queryResult == ConnectionResult.Success)
		{
			Status = "Google Play Services is installed on this device.";
			return true;
		}

		if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
		{
			// Check if there is a way the user can resolve the issue
			var errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
			Status = $"There is a problem with Google Play Services on this device: {queryResult} - {errorString}";
		}

		return false;
	}

	private void ListenerOnLogEventWritten(object sender, LogEventArgs e)
	{
		OnLogEventWritten(e);
	}

	private void ListenerPositionChanged(object sender, TLocation e)
	{
		// Ignore anything that might come in after stop listening
		if (!IsMonitoring || e is null)
		{
			return;
		}

		lock (_positionSync)
		{
			UpdateCurrentValue(e);
		}
	}

	private async void ListenerPositionError(object sender, LocationProviderError e)
	{
		await StopMonitoringAsync();
		OnLocationProviderError(e);
	}

	#endregion

	#region Classes

	private class FusedLocationProviderCallback : LocationCallback
	{
		#region Fields

		private readonly Action<global::Android.Locations.Location> _callback;

		#endregion

		#region Constructors

		public FusedLocationProviderCallback(Action<global::Android.Locations.Location> callback)
		{
			_callback = callback;
		}

		#endregion

		#region Methods

		public override void OnLocationAvailability(LocationAvailability locationAvailability)
		{
			//Log.Debug("FusedLocationProviderSample", "IsLocationAvailable: {0}",locationAvailability.IsLocationAvailable);
		}

		public override void OnLocationResult(LocationResult result)
		{
			if (result.Locations.Any())
			{
				var location = result.Locations.First();
				_callback.Invoke(location);
				//Log.Debug("Sample", "The latitude is :" + location.Latitude);
			}
			// No locations to work with.
		}

		#endregion
	}

	#endregion
}