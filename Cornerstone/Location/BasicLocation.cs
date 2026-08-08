#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Represents a minimal location (lat, long, alt, alt ref).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class BasicLocation : CornerstoneObject,
	IBasicLocation, IComparable, IComparable<BasicLocation>,
	IEquatable<BasicLocation>
{
	#region Constructors

	/// <summary>
	/// This constructor is only for serialization, do not actually use.
	/// </summary>
	public BasicLocation()
	{
	}

	/// <summary>
	/// Initialize an instance of the BasicLocation.
	/// </summary>
	public BasicLocation(IBasicLocation location)
		: this(location.Latitude, location.Longitude, location.Altitude, location.AltitudeReference)
	{
	}

	/// <summary>
	/// Initialize an instance of the BasicLocation.
	/// </summary>
	/// <param name="latitude"> The default value. </param>
	/// <param name="longitude"> The default value. </param>
	/// <param name="altitude"> The default value. </param>
	/// <param name="altitudeReference"> The default value. </param>
	public BasicLocation(double latitude = 0, double longitude = 0, double altitude = 0, AltitudeReferenceType altitudeReference = AltitudeReferenceType.Unspecified)
	{
		Latitude = latitude;
		Longitude = longitude;
		Altitude = altitude;
		AltitudeReference = altitudeReference;
	}

	#endregion

	#region Properties

	public double Altitude { get; set; }
	public AltitudeReferenceType AltitudeReference { get; set; }
	public double Latitude { get; set; }
	public double Longitude { get; set; }

	#endregion

	#region Methods

	public int CompareTo(BasicLocation other)
	{
		var altitude = Altitude.CompareTo(other.Altitude);
		var altitudeReference = AltitudeReference.CompareTo(other.AltitudeReference);
		var latitude = Latitude.CompareTo(other.Latitude);
		var longitude = Longitude.CompareTo(other.Longitude);

		return (altitude == 0) && (altitudeReference == 0) && (latitude == 0) && (longitude == 0) ? 0 : 1;
	}

	public int CompareTo(object obj)
	{
		return CompareTo(obj as BasicLocation);
	}

	public bool Equals(BasicLocation other)
	{
		if (ReferenceEquals(null, other))
		{
			return false;
		}
		if (ReferenceEquals(this, other))
		{
			return true;
		}
		return Altitude.Equals(other.Altitude)
			&& AltitudeReference.Equals(other.AltitudeReference)
			&& Latitude.Equals(other.Latitude)
			&& Longitude.Equals(other.Longitude);
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as BasicLocation);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Altitude.GetHashCode();
			hashCode = (hashCode * 397) ^ AltitudeReference.GetHashCode();
			hashCode = (hashCode * 397) ^ Latitude.GetHashCode();
			hashCode = (hashCode * 397) ^ Longitude.GetHashCode();
			return hashCode;
		}
	}

	public override string ToString()
	{
		return AltitudeReference != AltitudeReferenceType.Unspecified
			? $"{Latitude:F7}, {Longitude:F7}, {Altitude:F3} / {AltitudeReference}"
			: $"{Latitude:F7}, {Longitude:F7}";
	}

	#endregion
}

/// <summary>
/// Represents a minimal location (lat, long, alt, alt ref).
/// </summary>
public interface IBasicLocation : IMinimalHorizontalLocation, IMinimalVerticalLocation, IUpdateable
{
}