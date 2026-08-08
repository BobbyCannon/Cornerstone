namespace Cornerstone.Location;

/// <summary>
/// Defines the reference datum for altitude / elevation values.
/// </summary>
public enum AltitudeReferenceType
{
	/// <summary>
	/// Unknown or unspecified reference.
	/// </summary>
	Unspecified = 0,

	/// <summary>
	/// Height above ground / terrain surface (Above Ground Level - AGL) in meters.
	/// </summary>
	/// <remarks>
	/// This is relative to the actual terrain at that location.
	/// Use with caution in areas with rapidly changing elevation.
	/// </remarks>
	Terrain = 1,

	/// <summary>
	/// Height above the WGS84 ellipsoid (pure GNSS/GPS raw output) in meters.
	/// </summary>
	Ellipsoid = 2,

	/// <summary>
	/// Height above the geoid (approximates Mean Sea Level) in meters.
	/// </summary>
	/// <remarks>
	/// This is the most common "sea level" reference used in professional mapping.
	/// </remarks>
	Geoid = 3,

	/// <summary>
	/// Orthometric height (height above the geoid, corrected for local gravity) in meters.
	/// </summary>
	/// <remarks>
	/// This is the standard used in surveying, GIS systems (ArcGIS, etc.),
	/// and official elevation data. Strongly recommended for 3D mapping.
	/// </remarks>
	Orthometric = 4
}