#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Represents a minimal location (lat, long, alt, alt ref).
/// </summary>
public class BasicLocation
	: Bindable,
		IBasicLocation, IComparable, IComparable<BasicLocation>,
		IEquatable<BasicLocation>
{
	#region Constructors

	/// <summary>
	/// This constructor is only for serialization, do not actually use.
	/// </summary>
	public BasicLocation() : this(null)
	{
	}

	/// <summary>
	/// Initialize an instance of the BasicLocation.
	/// </summary>
	public BasicLocation(IDispatcher dispatcher) : this(0, 0, 0, AltitudeReferenceType.Unspecified, dispatcher)
	{
	}

	/// <summary>
	/// Initialize an instance of the BasicLocation.
	/// </summary>
	public BasicLocation(IBasicLocation location, IDispatcher dispatcher = null)
		: this(location.Latitude, location.Longitude, location.Altitude, location.AltitudeReference, dispatcher)
	{
	}

	/// <summary>
	/// Initialize an instance of the BasicLocation.
	/// </summary>
	/// <param name="latitude"> The default value. </param>
	/// <param name="longitude"> The default value. </param>
	/// <param name="altitude"> The default value. </param>
	/// <param name="altitudeReference"> The default value. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public BasicLocation(double latitude = 0, double longitude = 0, double altitude = 0, AltitudeReferenceType altitudeReference = AltitudeReferenceType.Unspecified, IDispatcher dispatcher = null) : base(dispatcher)
	{
		Latitude = latitude;
		Longitude = longitude;
		Altitude = altitude;
		AltitudeReference = altitudeReference;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public double Altitude { get; set; }

	/// <inheritdoc />
	public AltitudeReferenceType AltitudeReference { get; set; }

	/// <summary>
	/// Check a location to determine if <see cref="IMinimalVerticalLocation.Altitude" /> is available.
	/// </summary>
	public bool HasAltitude => this.HasSupportedAltitude();

	/// <inheritdoc />
	public double Latitude { get; set; }

	/// <inheritdoc />
	public double Longitude { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public int CompareTo(BasicLocation other)
	{
		var altitude = Altitude.CompareTo(other.Altitude);
		var altitudeReference = AltitudeReference.CompareTo(other.AltitudeReference);
		var latitude = Latitude.CompareTo(other.Latitude);
		var longitude = Longitude.CompareTo(other.Longitude);

		return (altitude == 0) && (altitudeReference == 0) && (latitude == 0) && (longitude == 0) ? 0 : 1;
	}

	/// <inheritdoc />
	public int CompareTo(object obj)
	{
		return CompareTo(obj as BasicLocation);
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		return Equals(obj as BasicLocation);
	}

	/// <inheritdoc />
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

	/// <summary>
	/// Get a IBasicLocation from a Location.
	/// </summary>
	/// <param name="location"> The location. </param>
	/// <returns> The equivalent value as a basic location. </returns>
	public static implicit operator BasicLocation(Location location)
	{
		var response = new BasicLocation(location.GetDispatcher());
		var horizontalLocation = location.HorizontalLocation;
		var verticalLocation = location.VerticalLocation;

		if (horizontalLocation != null)
		{
			response.Latitude = horizontalLocation.Latitude;
			response.Longitude = horizontalLocation.Longitude;
		}

		if (verticalLocation != null)
		{
			response.AltitudeReference = verticalLocation.AltitudeReference;
			response.Altitude = verticalLocation.Altitude;
		}

		return response;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Latitude:F7}, {Longitude:F7}, {Altitude:F3} / {AltitudeReference.GetDisplayName()}";
	}

	/// <summary>
	/// Update the BasicLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(IBasicLocation update)
	{
		return UpdateWith(update, UpdateableOptions.Empty);
	}

	/// <summary>
	/// Update the BasicLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(IBasicLocation update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			Altitude = update.Altitude;
			AltitudeReference = update.AltitudeReference;
			Latitude = update.Latitude;
			Longitude = update.Longitude;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Altitude)), x => x.Altitude = update.Altitude);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(AltitudeReference)), x => x.AltitudeReference = update.AltitudeReference);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Latitude)), x => x.Latitude = update.Latitude);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Longitude)), x => x.Longitude = update.Longitude);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			BasicLocation location => UpdateWith(location, options),
			IBasicLocation location => UpdateWith(location, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}

/// <summary>
/// Represents a minimal location (lat, long, alt, alt ref).
/// </summary>
public interface IBasicLocation : IMinimalHorizontalLocation, IMinimalVerticalLocation, IUpdateable
{
}