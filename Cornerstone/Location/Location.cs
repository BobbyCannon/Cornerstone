﻿#region References

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
	public override Location DeepClone(int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		var response = new Location
		{
			HorizontalLocation = HorizontalLocation.DeepClone(maxDepth, options),
			VerticalLocation = VerticalLocation.DeepClone(maxDepth, options)
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
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public bool ShouldUpdate(ILocation<IHorizontalLocation, IVerticalLocation> update, IncludeExcludeOptions options)
	{
		return HorizontalLocation.ShouldUpdate(update.HorizontalLocation, options)
			|| VerticalLocation.ShouldUpdate(update.VerticalLocation, options);
	}

	/// <inheritdoc />
	public override bool ShouldUpdate(object update, IncludeExcludeOptions options)
	{
		return update switch
		{
			Location location => ShouldUpdate(location, options),
			ILocation<IHorizontalLocation, IVerticalLocation> location => ShouldUpdate(location, options),
			HorizontalLocation location => HorizontalLocation.ShouldUpdate(location, options),
			IHorizontalLocation location => HorizontalLocation.ShouldUpdate(location, options),
			VerticalLocation location => VerticalLocation.ShouldUpdate(location, options),
			IVerticalLocation location => VerticalLocation.ShouldUpdate(location, options),
			_ => base.ShouldUpdate(update, options)
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
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool UpdateWith(ILocation<IHorizontalLocation, IVerticalLocation> update, IncludeExcludeOptions options)
	{
		return HorizontalLocation.TryUpdateWith(update.HorizontalLocation, options)
			| VerticalLocation.TryUpdateWith(update.VerticalLocation, options);
	}

	/// <summary>
	/// Update the Location with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the entity. </param>
	public override bool UpdateWith(Location update, IncludeExcludeOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			HorizontalLocation.UpdateWith(update.HorizontalLocation);
			VerticalLocation.UpdateWith(update.VerticalLocation);
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(HorizontalLocation)), x => x.HorizontalLocation.UpdateWith(update.HorizontalLocation));
			this.IfThen(_ => options.ShouldProcessProperty(nameof(VerticalLocation)), x => x.VerticalLocation.UpdateWith(update.VerticalLocation));
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeOptions options)
	{
		return update switch
		{
			Location location => UpdateWith(location, options),
			ILocation<IHorizontalLocation, IVerticalLocation> location => UpdateWith(location, options),
			HorizontalLocation location => HorizontalLocation.UpdateWith(location, options),
			IHorizontalLocation location => HorizontalLocation.UpdateWith(location, options),
			VerticalLocation location => VerticalLocation.UpdateWith(location, options),
			IVerticalLocation location => VerticalLocation.UpdateWith(location, options),
			_ => base.UpdateWith(update, options)
		};
	}

	/// <inheritdoc />
	ILocation<IHorizontalLocation, IVerticalLocation> ICloneable<ILocation<IHorizontalLocation, IVerticalLocation>>.DeepClone(int? maxDepth, IncludeExcludeOptions options)
	{
		return DeepClone(maxDepth, options);
	}

	/// <inheritdoc />
	ILocation<IHorizontalLocation, IVerticalLocation> ICloneable<ILocation<IHorizontalLocation, IVerticalLocation>>.ShallowClone(IncludeExcludeOptions options)
	{
		return DeepClone(0, options);
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