#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Platforms.Browser;

/// <summary>
/// Implementation for LocationProvider
/// </summary>
public class BrowserLocationProvider : BrowserLocationProvider<Location.Location, IHorizontalLocation, IVerticalLocation, LocationProviderSettings>, ILocationProvider
{
	#region Constructors

	/// <summary>
	/// Constructor for Implementation
	/// </summary>
	public BrowserLocationProvider(BrowserInteropProxy browserInterop, IDispatcher dispatcher)
		: base(browserInterop, dispatcher)
	{
	}

	#endregion
}

/// <summary>
/// Implementation for LocationProvider
/// </summary>
public class BrowserLocationProvider<TLocation, THorizontal, TVertical, TLocationProviderSettings>
	: LocationProvider<TLocation, THorizontal, TVertical, TLocationProviderSettings>
	where TLocation : class, ILocation<THorizontal, TVertical>, IUpdateable, new()
	where THorizontal : class, IHorizontalLocation, IUpdateable
	where TVertical : class, IVerticalLocation, IUpdateable
	where TLocationProviderSettings : ILocationProviderSettings, IBindable, new()
{
	#region Fields

	private readonly BrowserInteropProxy _browserInterop;

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor for Implementation
	/// </summary>
	public BrowserLocationProvider(BrowserInteropProxy browserInterop, IDispatcher dispatcher) : base(dispatcher)
	{
		_browserInterop = browserInterop;

		CurrentValue.HorizontalLocation.ProviderName = ProviderName;
		CurrentValue.VerticalLocation.ProviderName = ProviderName;
		HasPermission = true;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override bool IsLocationAvailable => false;

	/// <inheritdoc />
	public override bool IsLocationEnabled => false;

	/// <inheritdoc />
	public sealed override string ProviderName => "Cornerstone.Browser";

	#endregion

	#region Methods

	/// <summary>
	/// Gets position async with specified parameters
	/// </summary>
	/// <param name="timeout"> Timeout to wait, Default Infinite </param>
	/// <param name="cancelToken"> Cancellation token </param>
	/// <returns> ProviderLocation </returns>
	public override async Task<TLocation> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null)
	{
		var position = await GetCurrentPosition();
		CurrentValue.UpdateWith(position);
		return CurrentValue;
	}

	/// <inheritdoc />
	public override Task StartMonitoringAsync()
	{
		if (IsMonitoring)
		{
			return Task.CompletedTask;
		}

		IsMonitoring = true;

		return Task.CompletedTask;
	}

	/// <summary>
	/// Stop listening
	/// </summary>
	public override Task StopMonitoringAsync()
	{
		if (!IsMonitoring)
		{
			return Task.CompletedTask;
		}

		IsMonitoring = false;

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
	}

	private Task<TLocation> GetCurrentPosition()
	{
		//var location = await _jsRuntime.InvokeAsync<TLocation>(
		//	"navigator.geolocation.getCurrentPosition",
		//	[new { enableHighAccuracy = true, timeout = 5000, maximumAge = 0 }]
		//);
		//return location;
		return Task.FromResult<TLocation>(null);
	}

	#endregion
}