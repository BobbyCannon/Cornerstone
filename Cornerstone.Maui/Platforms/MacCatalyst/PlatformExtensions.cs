#region References

using System;
using System.Collections.Generic;
using System.Linq;
using CoreLocation;
using Cornerstone.Maui.Abstractions;
using Foundation;

#endregion

namespace Cornerstone.Maui.Platforms.MacCatalyst;

public static class PlatformExtensions
{
	#region Methods

	public static DateTime ToDateTime(this NSDate date)
	{
		return (DateTime) date;
	}

	internal static IEnumerable<Address> ToAddresses(this IEnumerable<CLPlacemark> addresses)
	{
		return addresses.Select(address => new Address
		{
			Longitude = address.Location?.Coordinate.Longitude ?? 0.0,
			Latitude = address.Location?.Coordinate.Latitude ?? 0.0,
			FeatureName = address.Name,
			PostalCode = address.PostalCode,
			SubLocality = address.SubLocality,
			CountryCode = address.IsoCountryCode,
			CountryName = address.Country,
			Thoroughfare = address.Thoroughfare,
			SubThoroughfare = address.SubThoroughfare,
			Locality = address.Locality,
			AdminArea = address.AdministrativeArea,
			SubAdminArea = address.SubAdministrativeArea
		});
	}

	#endregion
}