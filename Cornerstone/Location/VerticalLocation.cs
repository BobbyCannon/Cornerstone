#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Represents a vertical location.
/// </summary>
public class VerticalLocation : LocationInformation<IVerticalLocation>, IVerticalLocation
{
	#region Constants

	/// <summary>
	/// The information ID for vertical location.
	/// </summary>
	public const string VerticalLocationInformationId = "2AB3B4A0-A387-409A-BBA3-B74F75972463";

	#endregion

	#region Constructors

	/// <inheritdoc />
	public VerticalLocation() : this(null)
	{
	}

	/// <inheritdoc />
	public VerticalLocation(IDispatcher dispatcher) : this(0, AltitudeReferenceType.Unspecified, dispatcher)
	{
	}

	/// <inheritdoc />
	public VerticalLocation(IMinimalVerticalLocation location, IDispatcher dispatcher = null)
		: this(location.Altitude, location.AltitudeReference, dispatcher)
	{
	}

	/// <inheritdoc />
	public VerticalLocation(double altitude = 0, AltitudeReferenceType altitudeReference = AltitudeReferenceType.Unspecified, IDispatcher dispatcher = null) : base(dispatcher)
	{
		Altitude = altitude;
		AltitudeReference = altitudeReference;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public double Altitude { get; set; }

	/// <inheritdoc />
	public AltitudeReferenceType AltitudeReference { get; set; }

	/// <inheritdoc />
	public override Guid InformationId => Guid.Parse(VerticalLocationInformationId);

	#endregion

	#region Methods

	/// <inheritdoc />
	public override IVerticalLocation DeepClone(int? maxDepth = null)
	{
		var response = new VerticalLocation(GetDispatcher());
		response.UpdateWith(this);
		return response;
	}

	/// <inheritdoc />
	public override bool ShouldUpdate(object update, UpdateableOptions options)
	{
		return update switch
		{
			VerticalLocation location => location.StatusTime > StatusTime,
			IVerticalLocation locationUpdate => locationUpdate.StatusTime > StatusTime,
			ILocation<IHorizontalLocation, IVerticalLocation> location => location.VerticalLocation.StatusTime > StatusTime,
			_ => false
		};
	}

	/// <summary>
	/// Update the VerticalLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(IVerticalLocation update, UpdateableOptions options)
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
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Altitude)), x => x.Altitude = update.Altitude);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(AltitudeReference)), x => x.AltitudeReference = update.AltitudeReference);
		}

		return base.UpdateWith(update, options);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			VerticalLocation value => UpdateWith(value, options),
			IVerticalLocation value => UpdateWith(value, options),
			ILocation<IHorizontalLocation, IVerticalLocation> value => UpdateWith(value.VerticalLocation, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}

/// <summary>
/// Represents a vertical location (alt, alt ref, acc, acc ref).
/// </summary>
public interface IVerticalLocation
	: ILocationInformation,
		ICloneable<IVerticalLocation>,
		IMinimalVerticalLocation
{
}

/// <summary>
/// Represents a vertical location (alt, alt ref).
/// </summary>
public interface IMinimalVerticalLocation
{
	#region Properties

	/// <summary>
	/// The altitude of the location
	/// </summary>
	double Altitude { get; set; }

	/// <summary>
	/// The reference type for the altitude value.
	/// </summary>
	AltitudeReferenceType AltitudeReference { get; set; }

	#endregion
}