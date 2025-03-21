#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Represents a horizontal location.
/// </summary>
public class HorizontalLocation : LocationInformation<IHorizontalLocation>,
	IHorizontalLocation, IEquatable<HorizontalLocation>
{
	#region Constants

	/// <summary>
	/// The information ID for horizontal location.
	/// </summary>
	public const string HorizontalLocationInformationId = "A5AB1AC5-9F6D-4B19-A179-5366BEBD1F1D";

	#endregion

	#region Constructors

	/// <inheritdoc />
	public HorizontalLocation() : this(null)
	{
	}

	/// <inheritdoc />
	public HorizontalLocation(IDispatcher dispatcher) : this(0, 0, dispatcher)
	{
	}

	/// <inheritdoc />
	public HorizontalLocation(IMinimalHorizontalLocation location, IDispatcher dispatcher = null)
		: this(location.Latitude, location.Longitude, dispatcher)
	{
	}

	/// <inheritdoc />
	public HorizontalLocation(double latitude = 0, double longitude = 0, IDispatcher dispatcher = null) : base(dispatcher)
	{
		Latitude = latitude;
		Longitude = longitude;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override Guid InformationId => Guid.Parse(HorizontalLocationInformationId);

	/// <inheritdoc />
	public double Latitude { get; set; }

	/// <inheritdoc />
	public double Longitude { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override IHorizontalLocation DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		var response = new HorizontalLocation(GetDispatcher());
		response.UpdateWith(this);
		return response;
	}

	public bool Equals(HorizontalLocation other)
	{
		if (other is null)
		{
			return false;
		}
		if (ReferenceEquals(this, other))
		{
			return true;
		}
		return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);
	}

	public override bool Equals(object obj)
	{
		if (obj is null)
		{
			return false;
		}
		if (ReferenceEquals(this, obj))
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((HorizontalLocation) obj);
	}

	public override int GetHashCode()
	{
		return HashCodeCalculator.Combine(Latitude, Longitude);
	}

	public static bool operator ==(HorizontalLocation left, HorizontalLocation right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(HorizontalLocation left, HorizontalLocation right)
	{
		return !Equals(left, right);
	}

	/// <inheritdoc />
	public override bool ShouldUpdate(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			HorizontalLocation location => location.StatusTime > StatusTime,
			IHorizontalLocation locationUpdate => locationUpdate.StatusTime > StatusTime,
			ILocation<IHorizontalLocation, IVerticalLocation> location => location.HorizontalLocation.StatusTime > StatusTime,
			_ => false
		};
	}

	/// <summary>
	/// Update the HorizontalLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(IHorizontalLocation update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Latitude = update.Latitude;
			Longitude = update.Longitude;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Latitude)), x => x.Latitude = update.Latitude);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Longitude)), x => x.Longitude = update.Longitude);
		}

		return base.UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			HorizontalLocation value => UpdateWith(value, settings),
			IHorizontalLocation value => UpdateWith(value, settings),
			ILocation<IHorizontalLocation, IVerticalLocation> value => UpdateWith(value.HorizontalLocation, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}

/// <summary>
/// Represents a horizontal location (lat, long).
/// </summary>
public interface IHorizontalLocation
	: ILocationInformation,
		ICloneable<IHorizontalLocation>,
		IMinimalHorizontalLocation
{
}

/// <summary>
/// Represents a horizontal location (lat, long).
/// </summary>
public interface IMinimalHorizontalLocation
{
	#region Properties

	/// <summary>
	/// Ranges between -90 to 90 from North to South
	/// </summary>
	double Latitude { get; set; }

	/// <summary>
	/// Ranges between -180 to 180 from West to East
	/// </summary>
	double Longitude { get; set; }

	#endregion
}