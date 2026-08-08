#region References

using Cornerstone.Data;
using Cornerstone.Location;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using System;


#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync device (client + location).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Packable(1, ["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncDevice
	: SyncModel, ISyncDevice,
		IUpdateable<SyncDevice>,
		IUpdateable<ISyncDevice>
{
	#region Properties

	public partial double Altitude { get; set; }
	public partial AltitudeReferenceType AltitudeReference { get; set; }
	public partial string ApplicationName { get; set; }
	public partial Version ApplicationVersion { get; set; }
	public partial string DeviceId { get; set; }
	public partial string DeviceName { get; set; }
	public partial DevicePlatform DevicePlatform { get; set; }
	public partial Version DevicePlatformVersion { get; set; }
	public partial DeviceType DeviceType { get; set; }
	public partial double Latitude { get; set; }
	public partial string LocationSource { get; set; }
	public partial DateTime LocationUpdatedOn { get; set; }
	public partial double Longitude { get; set; }

	#endregion
}

/// <summary>
/// Represents a sync device (client + location).
/// </summary>
public interface ISyncDevice : ISyncSession, ISyncEntity
{
	#region Properties

	string LocationSource { get; set; }

	#endregion
}