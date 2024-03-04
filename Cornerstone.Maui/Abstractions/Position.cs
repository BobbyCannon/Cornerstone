#region References

using System;
using Microsoft.Maui.Devices.Sensors;

#endregion

namespace Cornerstone.Maui.Abstractions;

public class Position
{
	#region Constructors

	public Position()
	{
	}

	public Position(double latitude, double longitude)
	{
		Timestamp = DateTimeOffset.UtcNow;
		Latitude = latitude;
		Longitude = longitude;
	}

	public Position(Position position)
	{
		if (position == null)
		{
			throw new ArgumentNullException("position");
		}

		Timestamp = position.Timestamp;
		HasLatitudeLongitude = position.HasLatitudeLongitude;
		Latitude = position.Latitude;
		Longitude = position.Longitude;
		HasAltitude = position.HasAltitude;
		Altitude = position.Altitude;
		AltitudeAccuracy = position.AltitudeAccuracy;
		HasAccuracy = position.HasAccuracy;
		Accuracy = position.Accuracy;
		HasHeading = position.HasAccuracy;
		Heading = position.Heading;
		HasSpeed = position.HasSpeed;
		Speed = position.Speed;
		IsFromMockProvider = position.IsFromMockProvider;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the potential position error radius in meters.
	/// </summary>
	public double Accuracy { get; set; }

	/// <summary>
	/// Gets or sets the altitude in meters relative to sea level.
	/// </summary>
	public double Altitude { get; set; }

	/// <summary>
	/// Gets or sets the potential altitude error range in meters.
	/// </summary>
	/// <remarks>
	/// Not supported on Android, will always read 0.
	/// </remarks>
	public double AltitudeAccuracy { get; set; }

	/// <summary>
	/// Specifies if the Accuracy value is valid
	/// </summary>
	public bool HasAccuracy { get; set; }

	/// <summary>
	/// Specifies if the Altitude value is valid
	/// </summary>
	public bool HasAltitude { get; set; }

	/// <summary>
	/// Specifies if the Heading value is valid
	/// </summary>
	public bool HasHeading { get; set; }

	/// <summary>
	/// Specifies if the Latitude and Longitude values are valid
	/// </summary>
	public bool HasLatitudeLongitude { get; set; }

	/// <summary>
	/// Specifies if the Speed value is valid
	/// </summary>
	public bool HasSpeed { get; set; }

	/// <summary>
	/// Gets or sets the heading in degrees relative to true North.
	/// </summary>
	public double Heading { get; set; }

	/// <summary>
	/// Gets or sets if from mock provider
	/// </summary>
	public bool IsFromMockProvider { get; set; }

	/// <summary>
	/// Gets or sets the latitude.
	/// </summary>
	public double Latitude { get; set; }

	/// <summary>
	/// Gets or sets the longitude.
	/// </summary>
	public double Longitude { get; set; }

	/// <summary>
	/// Gets or sets the speed in meters per second.
	/// </summary>
	public double Speed { get; set; }

	/// <summary>
	/// Gets or sets the timestamp of the position
	/// </summary>
	public DateTimeOffset Timestamp { get; set; }

	#endregion
}

/// <summary>
/// Position args
/// </summary>
public class PositionEventArgs
	: EventArgs
{
	#region Constructors

	/// <summary>
	/// Position args
	/// </summary>
	/// <param name="position"> </param>
	public PositionEventArgs(Position position)
	{
		if (position == null)
		{
			throw new ArgumentNullException("position");
		}

		Position = position;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The Position
	/// </summary>
	public Position Position { get; private set; }

	#endregion
}

/// <summary>
/// Location exception
/// </summary>
public class GeolocationException
	: Exception
{
	#region Constructors

	/// <summary>
	/// Location exception
	/// </summary>
	/// <param name="error"> </param>
	public GeolocationException(GeolocationError error)
		: base("A geolocation error occured: " + error)
	{
		if (!Enum.IsDefined(typeof(GeolocationError), error))
		{
			throw new ArgumentException("error is not a valid GelocationError member", "error");
		}

		Error = error;
	}

	/// <summary>
	/// Geolocation error
	/// </summary>
	/// <param name="error"> </param>
	/// <param name="innerException"> </param>
	public GeolocationException(GeolocationError error, Exception innerException)
		: base("A geolocation error occured: " + error, innerException)
	{
		if (!Enum.IsDefined(typeof(GeolocationError), error))
		{
			throw new ArgumentException("error is not a valid GelocationError member", "error");
		}

		Error = error;
	}

	#endregion

	#region Properties

	//The error
	public GeolocationError Error { get; private set; }

	#endregion
}

/// <summary>
/// Error ARgs
/// </summary>
public class PositionErrorEventArgs
	: EventArgs
{
	#region Constructors

	/// <summary>
	/// Constructor for event error args
	/// </summary>
	/// <param name="error"> </param>
	public PositionErrorEventArgs(GeolocationError error)
	{
		Error = error;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The Error
	/// </summary>
	public GeolocationError Error { get; private set; }

	#endregion
}