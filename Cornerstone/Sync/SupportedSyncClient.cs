#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <inheritdoc cref="ISupportedSyncClient" />
public class SupportedSyncClient : Bindable, ISupportedSyncClient
{
	#region Constructors

	/// <inheritdoc />
	public SupportedSyncClient() : this(null)
	{
	}

	/// <inheritdoc />
	public SupportedSyncClient(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public string ApplicationName { get; set; }

	/// <inheritdoc />
	public Version ApplicationVersion { get; set; }

	/// <inheritdoc />
	public DevicePlatform DevicePlatform { get; set; }

	/// <inheritdoc />
	public DeviceType DeviceType { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the SupportedSyncClient with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(SupportedSyncClient update)
	{
		return UpdateWith(update, UpdateableOptions.Empty);
	}

	/// <summary>
	/// Update the SupportedSyncClient with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(SupportedSyncClient update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			ApplicationName = update.ApplicationName;
			ApplicationVersion = update.ApplicationVersion;
			DevicePlatform = update.DevicePlatform;
			DeviceType = update.DeviceType;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ApplicationName)), x => x.ApplicationName = update.ApplicationName);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ApplicationVersion)), x => x.ApplicationVersion = update.ApplicationVersion);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(DevicePlatform)), x => x.DevicePlatform = update.DevicePlatform);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(DeviceType)), x => x.DeviceType = update.DeviceType);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			SupportedSyncClient value => UpdateWith(value, options),
			ISupportedSyncClient value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}

/// <summary>
/// Represents a supported sync client.
/// </summary>
public interface ISupportedSyncClient
{
	#region Properties

	/// <summary>
	/// The ApplicationName value for Sync Client Details.
	/// </summary>
	public string ApplicationName { get; }

	/// <summary>
	/// The DevicePlatform value for Sync Client Details.
	/// </summary>
	public Version ApplicationVersion { get; }

	/// <summary>
	/// The DevicePlatform value for Sync Client Details.
	/// </summary>
	public DevicePlatform DevicePlatform { get; }

	/// <summary>
	/// The DeviceType value for Sync Client Details.
	/// </summary>
	public DeviceType DeviceType { get; }

	#endregion
}