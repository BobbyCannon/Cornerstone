﻿#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Location;

/// <inheritdoc cref="ILocationProvider" />
public class LocationProvider : LocationProvider<Location, IHorizontalLocation, IVerticalLocation, LocationProviderSettings>, ILocationProvider
{
	#region Constructors

	/// <inheritdoc />
	public LocationProvider() : base(null)
	{
	}

	/// <inheritdoc />
	public LocationProvider(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override string ProviderName => "Sample.Provider";

	#endregion

	#region Methods

	/// <inheritdoc />
	public override Task<Location> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null)
	{
		return Task.FromResult(new Location
		{
			HorizontalLocation = new HorizontalLocation(),
			VerticalLocation = new VerticalLocation()
		});
	}

	/// <inheritdoc />
	public override Task StartMonitoringAsync()
	{
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public override Task StopMonitoringAsync()
	{
		return Task.CompletedTask;
	}

	#endregion
}

/// <summary>
/// Represents a location provider.
/// </summary>
public abstract class LocationProvider<T, THorizontalLocation, TVerticalLocation, T2>
	: InformationProvider<T>, ILocationProvider<T, THorizontalLocation, TVerticalLocation, T2>
	where T : class, ILocation<THorizontalLocation, TVerticalLocation>, IUpdateable, new()
	where THorizontalLocation : class, IHorizontalLocation, IUpdateable
	where TVerticalLocation : class, IVerticalLocation, IUpdateable
	where T2 : ILocationProviderSettings, new()
{
	#region Constructors

	/// <summary>
	/// Creates an instance of a location provider.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected LocationProvider(IDispatcher dispatcher) : base(dispatcher)
	{
		ComparerForHorizontal = new LocationInformationComparer<THorizontalLocation>(dispatcher);
		ComparerForVertical = new LocationInformationComparer<TVerticalLocation>(dispatcher);

		LocationProviderSettings = new T2();
		LocationProviderSettings.UpdateDispatcher(dispatcher);
		IsLocationAvailable = true;
		IsLocationEnabled = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True if the location provider has permission to be accessed.
	/// </summary>
	public bool HasPermission { get; protected set; }

	/// <summary>
	/// Determines if location is available.
	/// </summary>
	public virtual bool IsLocationAvailable { get; }

	/// <summary>
	/// Determines if location is enabled.
	/// </summary>
	public virtual bool IsLocationEnabled { get; }

	/// <summary>
	/// The settings for the location provider.
	/// </summary>
	public T2 LocationProviderSettings { get; }

	/// <summary>
	/// The status of the provider.
	/// </summary>
	public string Status { get; protected set; }

	/// <summary>
	/// Comparer for the horizontal location.
	/// </summary>
	protected LocationInformationComparer<THorizontalLocation> ComparerForHorizontal { get; }

	/// <summary>
	/// Comparer for the vertical location.
	/// </summary>
	protected LocationInformationComparer<TVerticalLocation> ComparerForVertical { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets position async with specified parameters
	/// </summary>
	/// <param name="timeout"> Timeout to wait, Default Infinite </param>
	/// <param name="cancelToken"> Cancellation token </param>
	/// <returns> ProviderLocation </returns>
	public abstract Task<T> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null);

	/// <inheritdoc />
	public override bool ShouldUpdate(T update, IncludeExcludeSettings settings)
	{
		return ComparerForHorizontal.ShouldUpdate(CurrentValue.HorizontalLocation, update.HorizontalLocation)
			|| ComparerForVertical.ShouldUpdate(CurrentValue.VerticalLocation, update.VerticalLocation);
	}

	/// <inheritdoc />
	public override bool UpdateWith(T update, IncludeExcludeSettings settings)
	{
		return CurrentValue.HorizontalLocation.UpdateWith(update.HorizontalLocation, settings)
			& CurrentValue.VerticalLocation.UpdateWith(update.VerticalLocation, settings);
	}

	/// <summary>
	/// Triggers event handler.
	/// </summary>
	/// <param name="e"> The value for the event handler. </param>
	protected virtual void OnLocationProviderError(LocationProviderError e)
	{
		ErrorReceived?.Invoke(this, e);
	}

	/// <summary>
	/// Triggers event handler
	/// </summary>
	/// <param name="e"> The value for the event handler. </param>
	protected virtual void OnLogEventWritten(LogEventArgs e)
	{
		LogEventWritten?.Invoke(this, e);
	}

	/// <inheritdoc />
	protected override void OnPropertyChangedInDispatcher(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(IsMonitoring):
			{
				StartMonitoringCommand.Refresh();
				StopMonitoringCommand.Refresh();
				break;
			}
		}

		base.OnPropertyChangedInDispatcher(propertyName);
	}

	#endregion

	#region Events

	/// <summary>
	/// ProviderLocation error event handler
	/// </summary>
	public event EventHandler<LocationProviderError> ErrorReceived;

	/// <summary>
	/// Provider has written a log event.
	/// </summary>
	public event EventHandler<LogEventArgs> LogEventWritten;

	#endregion
}

/// <summary>
/// Represents a location provider using all Cornerstone data models.
/// </summary>
public interface ILocationProvider : ILocationProvider<Location, IHorizontalLocation, IVerticalLocation, LocationProviderSettings>
{
}

/// <summary>
/// Represents a location provider.
/// </summary>
public interface ILocationProvider<T, THorizontalLocation, TVerticalLocation, out T2>
	: IInformationProvider<T>
	where T : class, ILocation<THorizontalLocation, TVerticalLocation>, IUpdateable, new()
	where THorizontalLocation : class, IHorizontalLocation, IUpdateable
	where TVerticalLocation : class, IVerticalLocation, IUpdateable
	where T2 : ILocationProviderSettings, new()
{
	#region Properties

	/// <summary>
	/// The settings for the location provider.
	/// </summary>
	public T2 LocationProviderSettings { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets position async with specified parameters
	/// </summary>
	/// <param name="timeout"> Timeout to wait, Default Infinite </param>
	/// <param name="cancelToken"> Cancellation token </param>
	/// <returns> ProviderLocation </returns>
	Task<T> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null);

	#endregion
}