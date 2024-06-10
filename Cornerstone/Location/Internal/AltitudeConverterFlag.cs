

namespace Cornerstone.Location.Internal;

/// <summary>
/// Flags indicating conversions between heights above the geoid and heights above the ellipsoid.
/// </summary>
internal enum AltitudeConverterFlag
{
	/// <summary>
	/// The multiplier for converting from heights above the geoid to heights above the ellipsoid.
	/// </summary>
	EllipsoidToGeoid = -1,

	/// <summary>
	/// No conversion.
	/// </summary>
	None = 0,

	/// <summary>
	/// The multiplier for converting from heights above the ellipsoid to heights above the geoid.
	/// </summary>
	GeoidToEllipsoid = 1
}