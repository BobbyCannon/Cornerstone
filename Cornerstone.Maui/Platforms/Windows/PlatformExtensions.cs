#region References

using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Cornerstone.Location;
using Cornerstone.Maui.Abstractions;

#endregion

namespace Cornerstone.Maui.Platforms.Windows;

/// <summary>
/// Extension for the Windows platform.
/// </summary>
public static class PlatformExtensions
{
	#region Methods

	/// <summary>
	/// Convert altitude reference type to system.
	/// </summary>
	public static AltitudeReferenceSystem ToAltitudeReferenceSystem(this AltitudeReferenceType altitudeReference)
	{
		return altitudeReference switch
		{
			AltitudeReferenceType.Terrain => AltitudeReferenceSystem.Terrain,
			AltitudeReferenceType.Ellipsoid => AltitudeReferenceSystem.Ellipsoid,
			AltitudeReferenceType.Geoid => AltitudeReferenceSystem.Geoid,
			AltitudeReferenceType.Unspecified => AltitudeReferenceSystem.Unspecified,
			_ => AltitudeReferenceSystem.Unspecified
		};
	}

	/// <summary>
	/// Convert altitude reference system to type.
	/// </summary>
	public static AltitudeReferenceType ToAltitudeReferenceType(this AltitudeReferenceSystem altitudeReference)
	{
		return altitudeReference switch
		{
			AltitudeReferenceSystem.Terrain => AltitudeReferenceType.Terrain,
			AltitudeReferenceSystem.Ellipsoid => AltitudeReferenceType.Ellipsoid,
			AltitudeReferenceSystem.Geoid => AltitudeReferenceType.Geoid,
			AltitudeReferenceSystem.Unspecified => AltitudeReferenceType.Unspecified,
			_ => AltitudeReferenceType.Unspecified
		};
	}

	internal static IEnumerable<Address> ToAddresses(this IEnumerable<MapLocation> addresses)
	{
		return addresses.Select(address => new Address
		{
			Longitude = address.Point.Position.Longitude,
			Latitude = address.Point.Position.Latitude,
			FeatureName = address.DisplayName,
			PostalCode = address.Address.PostCode,
			CountryCode = address.Address.CountryCode,
			CountryName = address.Address.Country,
			Thoroughfare = address.Address.Street,
			SubThoroughfare = address.Address.StreetNumber,
			Locality = address.Address.Town,
			AdminArea = address.Address.Region,
			SubAdminArea = address.Address.District
		});
	}

	#endregion
}