#region References

using System;
using Cornerstone.Location;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync device (client + location).
/// </summary>
public class SyncDevice : SyncModel, ISyncDevice
{
	#region Properties

	/// <inheritdoc />
	public double Altitude { get; set; }

	/// <inheritdoc />
	public AltitudeReferenceType AltitudeReference { get; set; }

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

	/// <inheritdoc />
	public double Latitude { get; set; }

	/// <inheritdoc />
	public DateTime LocationUpdatedOn { get; set; }

	/// <inheritdoc />
	public double Longitude { get; set; }

	#endregion
}

/// <summary>
/// Represents a sync device (client + location).
/// </summary>
public interface ISyncDevice : IBasicLocation, ISyncClientDetails
{
	#region Properties

	/// <summary>
	/// The last date and time the location was updated.
	/// </summary>
	DateTime LocationUpdatedOn { get; set; }

	#endregion
}