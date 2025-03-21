#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Presentation;

#endregion

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

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

	/// <summary>
	/// Update the LocationProvider with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(LocationProvider update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith - LocationProvider

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(CurrentValue, update.CurrentValue, settings.ShouldProcessProperty(nameof(CurrentValue)));
		UpdateProperty(HasPermission, update.HasPermission, settings.ShouldProcessProperty(nameof(HasPermission)), x => HasPermission = x);
		UpdateProperty(IsEnabled, update.IsEnabled, settings.ShouldProcessProperty(nameof(IsEnabled)), x => IsEnabled = x);
		UpdateProperty(IsMonitoring, update.IsMonitoring, settings.ShouldProcessProperty(nameof(IsMonitoring)), x => IsMonitoring = x);
		UpdateProperty(LocationProviderSettings, update.LocationProviderSettings, settings.ShouldProcessProperty(nameof(LocationProviderSettings)));
		UpdateProperty(Status, update.Status, settings.ShouldProcessProperty(nameof(Status)), x => Status = x);

		// Code Generated - /UpdateWith - LocationProvider

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			LocationProvider value => UpdateWith(value, settings),
			Location value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}

/// <summary>
/// Represents a location provider.
/// </summary>
public abstract class LocationProvider<T, THorizontalLocation, TVerticalLocation, T2>
	: InformationProvider<T>, ILocationProvider<T, THorizontalLocation, TVerticalLocation, T2>, IEquatable<T>
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

	public bool Equals(T other)
	{
		return CurrentValue?.Equals(other) ?? false;
	}

	public override bool Equals(object obj)
	{
		if (obj is T location)
		{
			return Equals(location);
		}

		// ReSharper disable once BaseObjectEqualsIsObjectEquals
		return base.Equals(obj);
	}

	/// <summary>
	/// Gets position async with specified parameters
	/// </summary>
	/// <param name="timeout"> Timeout to wait, Default Infinite </param>
	/// <param name="cancelToken"> Cancellation token </param>
	/// <returns> ProviderLocation </returns>
	public abstract Task<T> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null);

	/// <inheritdoc />
	public override bool ShouldUpdate(object update, IncludeExcludeSettings settings)
	{
		return (update is T value)
			&& (ComparerForHorizontal.ShouldUpdate(CurrentValue.HorizontalLocation, value.HorizontalLocation)
				|| ComparerForVertical.ShouldUpdate(CurrentValue.VerticalLocation, value.VerticalLocation));
	}

	/// <summary>
	/// Update the LocationProvider with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(LocationProvider update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		UpdateProperty(CurrentValue.HorizontalLocation, update.CurrentValue.HorizontalLocation, settings.ShouldProcessProperty(nameof(HorizontalLocation)), x => CurrentValue.HorizontalLocation = (THorizontalLocation)x);
		UpdateProperty(CurrentValue.VerticalLocation, update.CurrentValue.VerticalLocation, settings.ShouldProcessProperty(nameof(VerticalLocation)), x => CurrentValue.VerticalLocation = (TVerticalLocation)x);

		return true;
	}

	/// <summary>
	/// Update the LocationProvider with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(Location update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		UpdateProperty(CurrentValue.HorizontalLocation, update.HorizontalLocation, settings.ShouldProcessProperty(nameof(HorizontalLocation)), x => CurrentValue.HorizontalLocation = (THorizontalLocation) x);
		UpdateProperty(CurrentValue.VerticalLocation, update.VerticalLocation, settings.ShouldProcessProperty(nameof(VerticalLocation)), x => CurrentValue.VerticalLocation = (TVerticalLocation) x);

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(T update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		UpdateProperty(CurrentValue.HorizontalLocation, update.HorizontalLocation, settings.ShouldProcessProperty(nameof(HorizontalLocation)), x => CurrentValue.HorizontalLocation = x);
		UpdateProperty(CurrentValue.VerticalLocation, update.VerticalLocation, settings.ShouldProcessProperty(nameof(VerticalLocation)), x => CurrentValue.VerticalLocation = x);

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			LocationProvider value => UpdateWith(value, settings),
			Location value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
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