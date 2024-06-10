#region References

using System;
using Cornerstone.Attributes;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Represents location information for a device.
/// </summary>
public abstract class LocationInformation<T>
	: Bindable<T>, ILocationInformation
	where T : ILocationInformation
{
	#region Constructors

	/// <summary>
	/// Initializes location information for a device.
	/// </summary>
	protected LocationInformation(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public double Accuracy { get; set; }

	/// <inheritdoc />
	public AccuracyReferenceType AccuracyReference { get; set; }

	/// <inheritdoc />
	public LocationFlags Flags { get; set; }

	/// <inheritdoc />
	[ComputedProperty]
	public bool HasAccuracy => this.HasAccuracy();

	/// <inheritdoc />
	[ComputedProperty]
	public bool HasHeading
	{
		get => this.HasHeading();
		set => this.UpdateHasHeading(value);
	}

	/// <inheritdoc />
	[ComputedProperty]
	public bool HasSpeed
	{
		get => this.HasSpeed();
		set => this.UpdateHasSpeed(value);
	}

	/// <inheritdoc cref="IInformation.HasValue" />
	[ComputedProperty]
	public virtual bool HasValue
	{
		get => this.HasLocation();
		set => this.UpdateHasLocation(value);
	}

	/// <inheritdoc />
	public double Heading { get; set; }

	/// <summary>
	/// Represents a global unique ID to identify an information type.
	/// </summary>
	public abstract Guid InformationId { get; }

	/// <inheritdoc />
	public string ProviderName { get; set; }

	/// <inheritdoc />
	public string SourceName { get; set; }

	/// <inheritdoc />
	public double Speed { get; set; }

	/// <inheritdoc />
	public DateTime StatusTime { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the LocationInformation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(T update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			Accuracy = update.Accuracy;
			AccuracyReference = update.AccuracyReference;
			Flags = update.Flags;
			Heading = update.Heading;
			ProviderName = update.ProviderName;
			SourceName = update.SourceName;
			Speed = update.Speed;
			StatusTime = update.StatusTime;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Accuracy)), x => x.Accuracy = update.Accuracy);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(AccuracyReference)), x => x.AccuracyReference = update.AccuracyReference);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Flags)), x => x.Flags = update.Flags);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Heading)), x => x.Heading = update.Heading);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ProviderName)), x => x.ProviderName = update.ProviderName);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SourceName)), x => x.SourceName = update.SourceName);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Speed)), x => x.Speed = update.Speed);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(StatusTime)), x => x.StatusTime = update.StatusTime);
		}

		return true;
	}

	/// <summary>
	/// Update the LocationInformation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(ILocationInformation update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			Accuracy = update.Accuracy;
			AccuracyReference = update.AccuracyReference;
			Flags = update.Flags;
			Heading = update.Heading;
			ProviderName = update.ProviderName;
			SourceName = update.SourceName;
			Speed = update.Speed;
			StatusTime = update.StatusTime;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Accuracy)), x => x.Accuracy = update.Accuracy);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(AccuracyReference)), x => x.AccuracyReference = update.AccuracyReference);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Flags)), x => x.Flags = update.Flags);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Heading)), x => x.Heading = update.Heading);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ProviderName)), x => x.ProviderName = update.ProviderName);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SourceName)), x => x.SourceName = update.SourceName);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Speed)), x => x.Speed = update.Speed);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(StatusTime)), x => x.StatusTime = update.StatusTime);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			LocationInformation<T> value => UpdateWith(value, options),
			ILocationInformation value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	/// <inheritdoc />
	protected override void OnPropertyChangedInDispatcher(string propertyName)
	{
		this.HandleFlagsChanged(propertyName);
		base.OnPropertyChangedInDispatcher(propertyName);
	}

	#endregion
}

/// <summary>
/// Represents location information for a device.
/// </summary>
public interface ILocationInformation : IAccurateInformation, IUpdateable
{
	#region Properties

	/// <summary>
	/// Flags for the location of the provider.
	/// </summary>
	LocationFlags Flags { get; set; }

	/// <summary>
	/// Specifies if the Heading value is valid.
	/// </summary>
	bool HasHeading { get; set; }

	/// <summary>
	/// Specifies if the Speed value is valid.
	/// </summary>
	bool HasSpeed { get; set; }

	/// <summary>
	/// The heading of a device.
	/// </summary>
	double Heading { get; set; }

	/// <summary>
	/// The speed of the device in meters per second.
	/// </summary>
	double Speed { get; set; }

	#endregion
}