#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Location;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Maui.Platforms.iOS;

/// <inheritdoc />
public abstract class LocationProviderImplementation<T, THorizontalLocation, TVerticalLocation, T2>
	: LocationProvider<T, THorizontalLocation, TVerticalLocation, T2>
	where T : class, ILocation<THorizontalLocation, TVerticalLocation>, IUpdateable, new()
	where THorizontalLocation : class, IHorizontalLocation
	where TVerticalLocation : class, IVerticalLocation
	where T2 : ILocationProviderSettings, new()
{
	#region Constructors

	/// <inheritdoc />
	protected LocationProviderImplementation(IDispatcher dispatcher) : base(dispatcher)
	{
		HasPermission = true;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override string ProviderName => "Maui iOS";

	#endregion

	#region Methods

	/// <inheritdoc />
	public override Task<T> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? token = null)
	{
		return Task.FromResult(CurrentValue);
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