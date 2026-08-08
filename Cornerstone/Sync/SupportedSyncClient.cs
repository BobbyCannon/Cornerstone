#region References

using System;
using Cornerstone.Data;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <inheritdoc cref="ISupportedSyncClient" />
public partial class SupportedSyncClient
	: CornerstoneObject, ISupportedSyncClient,
		IUpdateable<SupportedSyncClient>,
		IUpdateable<ISupportedSyncClient>
{
	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string ApplicationName { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Version ApplicationVersion { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial DevicePlatform DevicePlatform { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial DeviceType DeviceType { get; set; }

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