#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The sync client details.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncClientDetails
	: CornerstoneObject<SyncClientDetails>,
		ISyncClientDetails,
		IUpdateable<ISyncClientDetails>
{
	#region Properties

	public partial string ApplicationName { get; set; }

	public partial Version ApplicationVersion { get; set; }

	public partial string DeviceId { get; set; }

	public partial string DeviceName { get; set; }

	public partial DevicePlatform DevicePlatform { get; set; }

	public partial Version DevicePlatformVersion { get; set; }

	public partial DeviceType DeviceType { get; set; }

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