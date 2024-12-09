#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The sync client details.
/// </summary>
public class SyncClientDetails : Bindable<SyncClientDetails>, ISyncClientDetails
{
	#region Properties

	/// <inheritdoc />
	public string ApplicationName { get; set; }

	/// <inheritdoc />
	public Version ApplicationVersion { get; set; }

	/// <inheritdoc />
	public string DeviceId { get; set; }

	/// <inheritdoc />
	public string DeviceName { get; set; }

	/// <inheritdoc />
	public DevicePlatform DevicePlatform { get; set; }

	/// <inheritdoc />
	public Version DevicePlatformVersion { get; set; }

	/// <inheritdoc />
	public DeviceType DeviceType { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the SyncClientDetails with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(ISyncClientDetails update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			ApplicationName = update.ApplicationName;
			ApplicationVersion = update.ApplicationVersion;
			DeviceId = update.DeviceId;
			DeviceName = update.DeviceName;
			DevicePlatform = update.DevicePlatform;
			DevicePlatformVersion = update.DevicePlatformVersion;
			DeviceType = update.DeviceType;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(ApplicationName)), x => x.ApplicationName = update.ApplicationName);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(ApplicationVersion)), x => x.ApplicationVersion = update.ApplicationVersion);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DeviceId)), x => x.DeviceId = update.DeviceId);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DeviceName)), x => x.DeviceName = update.DeviceName);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DevicePlatform)), x => x.DevicePlatform = update.DevicePlatform);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DevicePlatformVersion)), x => x.DevicePlatformVersion = update.DevicePlatformVersion);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DeviceType)), x => x.DeviceType = update.DeviceType);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(SyncClientDetails update, IncludeExcludeSettings settings)
	{
		return UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			SyncClientDetails value => UpdateWith(value, settings),
			ISyncClientDetails value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}

/// <summary>
/// The details for a sync client.
/// </summary>
public interface ISyncClientDetails : ISupportedSyncClient
{
	#region Properties

	/// <summary>
	/// The DeviceId value for Sync Client Details.
	/// </summary>
	public string DeviceId { get; }

	/// <summary>
	/// The name of the device.
	/// </summary>
	public string DeviceName { get; }

	/// <summary>
	/// The DeviceVersion value for Sync Client Details.
	/// </summary>
	public Version DevicePlatformVersion { get; }

	#endregion
}