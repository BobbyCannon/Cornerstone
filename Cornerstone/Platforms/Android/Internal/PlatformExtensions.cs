#region References

using System;
using Android.OS;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Location;

#endregion

namespace Cornerstone.Platforms.Android.Internal;

/// <summary>
/// Extensions for the Android platform.
/// </summary>
public static class PlatformExtensions
{
	#region Fields

	private static readonly int _twoMinutes;

	#endregion

	#region Constructors

	static PlatformExtensions()
	{
		_twoMinutes = 120000;
	}

	#endregion

	#region Methods

	internal static DateTimeOffset GetTimestamp(this global::Android.Locations.Location location)
	{
		try
		{
			return new DateTimeOffset(DateTimeExtensions.EpochDateTime.AddMilliseconds(location.Time));
		}
		catch (Exception)
		{
			return new DateTimeOffset(DateTimeExtensions.EpochDateTime);
		}
	}

	internal static bool IsBetterLocation(this global::Android.Locations.Location location, global::Android.Locations.Location bestLocation)
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

		var isFromSameProvider = location.Provider.IsSameProvider(bestLocation.Provider);

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

	internal static T ToPosition<T, THorizontal, TVertical>(this global::Android.Locations.Location location, string providerName)
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