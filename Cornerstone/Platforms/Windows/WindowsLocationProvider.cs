#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Cornerstone.Location;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// Implementation for LocationProvider
/// </summary>
[SourceReflection]
public class WindowsLocationProvider : LocationProvider
{
	#region Fields

	private readonly Geolocator _locator;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsLocationProvider()
	{
		_locator = new Geolocator();
	}

	#endregion

	#region Properties

	public sealed override string ProviderName => "Cornerstone.Windows";

	#endregion

	#region Methods

	public override Task<Location.Location> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null)
	{
		return Task.FromResult(CurrentValue);
	}

	public override void InitializeLifecycle()
	{
		_locator.PositionChanged += LocatorOnPositionChanged;
		_locator.StatusChanged += LocatorOnStatusChanged;
		base.InitializeLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		_locator.PositionChanged -= LocatorOnPositionChanged;
		_locator.StatusChanged -= LocatorOnStatusChanged;
		base.UninitializeLifecycle();
	}

	private void LocatorOnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
	{
		UpdateCurrentValue(args.Position);
	}

	private void LocatorOnStatusChanged(Geolocator sender, StatusChangedEventArgs args)
	{
		Status = args.Status.ToString();
	}

	private static AltitudeReferenceType ToAltitudeReferenceType(AltitudeReferenceSystem altitudeReference)
	{
		return altitudeReference switch
		{
			AltitudeReferenceSystem.Terrain => AltitudeReferenceType.Terrain,
			AltitudeReferenceSystem.Ellipsoid => AltitudeReferenceType.Ellipsoid,
			AltitudeReferenceSystem.Geoid => AltitudeReferenceType.Geoid,
			_ => AltitudeReferenceType.Unspecified
		};
	}

	private void UpdateCurrentValue(Geoposition position)
	{
		CurrentValue.Horizontal.Latitude = position.Coordinate.Point.Position.Latitude;
		CurrentValue.Horizontal.Longitude = position.Coordinate.Point.Position.Longitude;
		CurrentValue.Horizontal.HasValue = true;

		CurrentValue.Horizontal.Accuracy = position.Coordinate.Accuracy;
		CurrentValue.Horizontal.AccuracyReference = AccuracyReferenceType.Meters;

		CurrentValue.Horizontal.SourceName = position.Coordinate.PositionSource.ToString();
		CurrentValue.Horizontal.StatusTime = position.Coordinate.Timestamp.UtcDateTime;

		CurrentValue.Vertical.Altitude = position.Coordinate.Point.Position.Altitude;
		CurrentValue.Vertical.AltitudeReference = ToAltitudeReferenceType(position.Coordinate.Point.AltitudeReferenceSystem);
		CurrentValue.Vertical.HasValue = CurrentValue.Vertical.AltitudeReference != AltitudeReferenceType.Unspecified;

		if (position.Coordinate.AltitudeAccuracy.HasValue)
		{
			CurrentValue.Vertical.Accuracy = position.Coordinate.AltitudeAccuracy.Value;
			CurrentValue.Vertical.AccuracyReference = AccuracyReferenceType.Meters;
		}
		else
		{
			CurrentValue.Vertical.AccuracyReference = AccuracyReferenceType.Unspecified;
			CurrentValue.Vertical.Accuracy = 0;
		}

		CurrentValue.Vertical.SourceName = position.Coordinate.PositionSource.ToString();
		CurrentValue.Vertical.StatusTime = position.Coordinate.Timestamp.UtcDateTime;

		if (position.Coordinate.Heading != null)
		{
			CurrentValue.Horizontal.HasHeading = true;
			CurrentValue.Horizontal.Heading = position.Coordinate.Heading.Value;
		}
		else
		{
			CurrentValue.Horizontal.HasHeading = false;
			CurrentValue.Horizontal.Heading = 0;
		}

		if (position.Coordinate.Speed != null)
		{
			CurrentValue.Horizontal.HasSpeed = true;
			CurrentValue.Horizontal.Speed = position.Coordinate.Speed.Value;
		}
		else
		{
			CurrentValue.Horizontal.HasSpeed = false;
			CurrentValue.Horizontal.Speed = 0;
		}
	}

	#endregion
}