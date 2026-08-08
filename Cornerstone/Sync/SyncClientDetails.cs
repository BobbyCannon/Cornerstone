#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The sync client details.
/// </summary>
public partial class SyncClientDetails
	: CornerstoneObject<SyncClientDetails>,
		ISyncClientDetails,
		IUpdateable<ISyncClientDetails>
{
	#region Properties

	[UpdateableAction(UpdateableAction.All)]
	public string ApplicationName { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public Version ApplicationVersion { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public string DeviceId { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public string DeviceName { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public DevicePlatform DevicePlatform { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public Version DevicePlatformVersion { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public DeviceType DeviceType { get; set; }

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