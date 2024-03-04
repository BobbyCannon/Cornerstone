#region References

using System.Collections.Generic;
using System.Linq;
using CoreLocation;
using Cornerstone.Maui.Abstractions;

#endregion

namespace Cornerstone.Maui.Platforms.iOS;

public static class GeolocationUtilities
{
	#region Methods

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