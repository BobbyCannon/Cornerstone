#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <inheritdoc cref="ISupportedSyncClient" />
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SupportedSyncClient
	: CornerstoneObject, ISupportedSyncClient,
		IUpdateable<SupportedSyncClient>,
		IUpdateable<ISupportedSyncClient>
{
	#region Properties

	public partial string ApplicationName { get; set; }

	public partial Version ApplicationVersion { get; set; }

	public partial DevicePlatform DevicePlatform { get; set; }

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