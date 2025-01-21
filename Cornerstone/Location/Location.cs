#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Represents a full location from a LocationProvider. Contains horizontal and vertical location.
/// </summary>
public class Location : Bindable<Location>,
	ICloneable<ILocation<IHorizontalLocation, IVerticalLocation>>,
	ILocation<IHorizontalLocation, IVerticalLocation>
{
	#region Constructors

	/// <summary>
	/// Initializes a location for a LocationProvider.
	/// </summary>
	public Location() : this(null)
	{
	}

	/// <summary>
	/// Initializes a location for a LocationProvider.
	/// </summary>
	public Location(IDispatcher dispatcher) : base(dispatcher)
	{
		HorizontalLocation = new HorizontalLocation(dispatcher);
		VerticalLocation = new VerticalLocation(dispatcher);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public IHorizontalLocation HorizontalLocation { get; set; }

	/// <inheritdoc />
	public IVerticalLocation VerticalLocation { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override Location DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		var response = new Location
		{
			HorizontalLocation = HorizontalLocation.DeepClone(maxDepth, settings),
			VerticalLocation = VerticalLocation.DeepClone(maxDepth, settings)
		};

		if (response is ITrackPropertyChanges changeable)
		{
			changeable.ResetHasChanges();
		}

		return response;
	}

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public bool ShouldUpdate(ILocation<IHorizontalLocation, IVerticalLocation> update, IncludeExcludeSettings settings)
	{
		return HorizontalLocation.ShouldUpdate(update.HorizontalLocation, settings)
			|| VerticalLocation.ShouldUpdate(update.VerticalLocation, settings);
	}

	/// <inheritdoc />
	public override bool ShouldUpdate(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			Location location => ShouldUpdate(location, settings),
			ILocation<IHorizontalLocation, IVerticalLocation> location => ShouldUpdate(location, settings),
			HorizontalLocation location => HorizontalLocation.ShouldUpdate(location, settings),
			IHorizontalLocation location => HorizontalLocation.ShouldUpdate(location, settings),
			VerticalLocation location => VerticalLocation.ShouldUpdate(location, settings),
			IVerticalLocation location => VerticalLocation.ShouldUpdate(location, settings),
			_ => base.ShouldUpdate(update, settings)
		};
	}

	/// <inheritdoc />
	public override void UpdateDispatcher(IDispatcher dispatcher)
	{
		HorizontalLocation.UpdateDispatcher(dispatcher);
		VerticalLocation.UpdateDispatcher(dispatcher);
		base.UpdateDispatcher(dispatcher);
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool UpdateWith(ILocation<IHorizontalLocation, IVerticalLocation> update, IncludeExcludeSettings settings)
	{
		UpdateProperty(HorizontalLocation, update.HorizontalLocation,
			settings.ShouldProcessProperty(nameof(HorizontalLocation)),
			x => HorizontalLocation = x
		);
		UpdateProperty(VerticalLocation, update.VerticalLocation,
			settings.ShouldProcessProperty(nameof(VerticalLocation)),
			x => VerticalLocation = x
		);
		return true;
	}

	/// <summary>
	/// Update the Location with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public override bool UpdateWith(Location update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(HorizontalLocation, update.HorizontalLocation, settings.ShouldProcessProperty(nameof(HorizontalLocation)), x => HorizontalLocation = x);
		UpdateProperty(VerticalLocation, update.VerticalLocation, settings.ShouldProcessProperty(nameof(VerticalLocation)), x => VerticalLocation = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			Location location => UpdateWith(location, settings),
			ILocation<IHorizontalLocation, IVerticalLocation> location => UpdateWith(location, settings),
			HorizontalLocation location => HorizontalLocation.UpdateWith(location, settings),
			IHorizontalLocation location => HorizontalLocation.UpdateWith(location, settings),
			VerticalLocation location => VerticalLocation.UpdateWith(location, settings),
			IVerticalLocation location => VerticalLocation.UpdateWith(location, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <inheritdoc />
	ILocation<IHorizontalLocation, IVerticalLocation> ICloneable<ILocation<IHorizontalLocation, IVerticalLocation>>.DeepClone(int? maxDepth, IncludeExcludeSettings settings)
	{
		return DeepClone(maxDepth, settings);
	}

	/// <inheritdoc />
	ILocation<IHorizontalLocation, IVerticalLocation> ICloneable<ILocation<IHorizontalLocation, IVerticalLocation>>.ShallowClone(IncludeExcludeSettings settings)
	{
		return DeepClone(0, settings);
	}

	#endregion
}

/// <summary>
/// Represents a provider location.
/// </summary>
public interface ILocation<THorizontalLocation, TVerticalLocation>
	: IBindable
	where THorizontalLocation : class, IHorizontalLocation
	where TVerticalLocation : class, IVerticalLocation
{
	#region Properties

	/// <summary>
	/// The horizontal location.
	/// </summary>
	THorizontalLocation HorizontalLocation { get; set; }

	/// <summary>
	/// The vertical location.
	/// </summary>
	TVerticalLocation VerticalLocation { get; set; }

	#endregion
}