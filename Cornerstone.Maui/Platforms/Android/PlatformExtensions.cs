#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Cornerstone.Data;
using Cornerstone.Location;
using Cornerstone.Maui.Abstractions;
using AndroidLocation = Android.Locations.Location;
using AndroidAddress = Android.Locations.Address;

#endregion

namespace Cornerstone.Maui.Platforms.Android;

/// <summary>
/// Extensions for the Android platform.
/// </summary>
public static class PlatformExtensions
{
	#region Fields

	private static readonly DateTime _epoch;
	private static readonly int _twoMinutes;

	#endregion

	#region Constructors

	static PlatformExtensions()
	{
		_epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		_twoMinutes = 120000;
	}

	#endregion

	#region Methods

	internal static DateTimeOffset GetTimestamp(this AndroidLocation location)
	{
		try
		{
			return new DateTimeOffset(_epoch.AddMilliseconds(location.Time));
		}
		catch (Exception)
		{
			return new DateTimeOffset(_epoch);
		}
	}

	internal static bool IsBetterLocation(this AndroidLocation location, AndroidLocation bestLocation)
	{
		if (bestLocation == null)
		{
			return true;
		}

		var timeDelta = location.Time - bestLocation.Time;
		var isSignificantlyNewer = timeDelta > _twoMinutes;
		var isSignificantlyOlder = timeDelta < -_twoMinutes;
		var isNewer = timeDelta > 0;

		if (isSignificantlyNewer)
		{
			return true;
		}

		if (isSignificantlyOlder)
		{
			return false;
		}

		var accuracyDelta = (int) (location.Accuracy - bestLocation.Accuracy);
		var isLessAccurate = accuracyDelta > 0;
		var isMoreAccurate = accuracyDelta < 0;
		var isSignificantlyLessAccurate = accuracyDelta > 200;

		var isFromSameProvider = IsSameProvider(location.Provider, bestLocation.Provider);

		if (isMoreAccurate)
		{
			return true;
		}

		if (isNewer && !isLessAccurate)
		{
			return true;
		}

		if (isNewer && !isSignificantlyLessAccurate && isFromSameProvider)
		{
			return true;
		}

		return false;
	}

	internal static bool IsSameProvider(this string provider1, string provider2)
	{
		if (provider1 == null)
		{
			return provider2 == null;
		}

		return provider1.Equals(provider2);
	}

	internal static IEnumerable<Address> ToAddresses(this IEnumerable<AndroidAddress> addresses)
	{
		return addresses.Select(address => new Address
		{
			Longitude = address.Longitude,
			Latitude = address.Latitude,
			FeatureName = address.FeatureName,
			PostalCode = address.PostalCode,
			SubLocality = address.SubLocality,
			CountryCode = address.CountryCode,
			CountryName = address.CountryName,
			Thoroughfare = address.Thoroughfare,
			SubThoroughfare = address.SubThoroughfare,
			Locality = address.Locality,
			AdminArea = address.AdminArea,
			SubAdminArea = address.SubAdminArea
		});
	}

	internal static Position ToPosition(this AndroidLocation location)
	{
		var p = new Position();

		p.HasAccuracy = location.HasAccuracy;
		if (location.HasAccuracy)
		{
			p.Accuracy = location.Accuracy;
		}

		p.HasAltitude = location.HasAltitude;
		if (location.HasAltitude)
		{
			p.Altitude = location.Altitude;
		}

		p.HasHeading = location.HasBearing;
		if (location.HasBearing)
		{
			p.Heading = location.Bearing;
		}

		p.HasSpeed = location.HasSpeed;
		if (location.HasSpeed)
		{
			p.Speed = location.Speed;
		}

		p.HasLatitudeLongitude = true;
		p.Longitude = location.Longitude;
		p.Latitude = location.Latitude;
		p.Timestamp = location.GetTimestamp();

		switch ((int) Build.VERSION.SdkInt)
		{
			case >= 31:
			{
				p.IsFromMockProvider = location.Mock;
				break;
			}
			case >= 18:
			{
				#pragma warning disable CA1422
				p.IsFromMockProvider = location.IsFromMockProvider;
				#pragma warning restore CA1422
				break;
			}
			default:
			{
				p.IsFromMockProvider = false;
				break;
			}
		}

		return p;
	}

	internal static T ToPosition<T, THorizontal, TVertical>(this AndroidLocation location, string providerName)
		where T : class, ILocation<THorizontal, TVertical>, new()
		where THorizontal : class, IHorizontalLocation, IUpdateable
		where TVertical : class, IVerticalLocation, IUpdateable
	{
		var sourceName = location.Provider ?? "unknown";
		var sourceTime = location.GetTimestamp().UtcDateTime;

		var response = new T
		{
			HorizontalLocation =
			{
				HasHeading = location.HasBearing,
				HasSpeed = location.HasSpeed,
				HasValue = true,
				ProviderName = providerName,
				SourceName = sourceName,
				StatusTime = sourceTime,
				Longitude = location.Longitude,
				Latitude = location.Latitude
			},
			VerticalLocation =
			{
				ProviderName = providerName,
				SourceName = sourceName,
				StatusTime = sourceTime
			}
		};

		if (location.HasAccuracy)
		{
			response.HorizontalLocation.Accuracy = location.Accuracy;
			response.HorizontalLocation.AccuracyReference = AccuracyReferenceType.Meters;
		}

		if (location.HasAltitude)
		{
			response.VerticalLocation.Altitude = location.Altitude;
			response.VerticalLocation.AltitudeReference = AltitudeReferenceType.Ellipsoid;
			response.VerticalLocation.HasValue = true;
		}
		else
		{
			response.VerticalLocation.AltitudeReference = AltitudeReferenceType.Unspecified;
			response.VerticalLocation.HasValue = false;
		}

		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			#pragma warning disable CA1416 // Validate platform compatibility
			if (location.HasVerticalAccuracy)
			{
				response.VerticalLocation.Accuracy = location.VerticalAccuracyMeters;
				response.VerticalLocation.AccuracyReference = AccuracyReferenceType.Meters;
			}
			else
			{
				response.VerticalLocation.AccuracyReference = AccuracyReferenceType.Unspecified;
			}
			#pragma warning restore CA1416 // Validate platform compatibility
		}
		else
		{
			response.VerticalLocation.AccuracyReference = AccuracyReferenceType.Unspecified;
		}

		if (response.HorizontalLocation.HasHeading)
		{
			response.HorizontalLocation.Heading = location.Bearing;
		}

		if (response.HorizontalLocation.HasSpeed)
		{
			response.HorizontalLocation.Speed = location.Speed;
		}

		//response.IsFromMockProvider = (int) Build.VERSION.SdkInt >= 18 && location.IsFromMockProvider;

		return response;
	}

	#endregion
}