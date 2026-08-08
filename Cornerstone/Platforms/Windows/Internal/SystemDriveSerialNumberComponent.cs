#region References

using System;
using System.Management;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows.Internal;

/// <summary>
/// An implementation of <see cref="IDeviceIdComponent" /> that uses the system drive's serial number.
/// </summary>
internal class SystemDriveSerialNumberComponent : DeviceIdComponent
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="SystemDriveSerialNumberComponent" /> class.
	/// </summary>
	public SystemDriveSerialNumberComponent()
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override string GetComponentValue()
	{
		try
		{
			var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
			if (string.IsNullOrEmpty(systemDirectory) || (systemDirectory.Length < 2))
			{
				return null;
			}

			var deviceId = systemDirectory.Substring(0, 2);
			var queryString = $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition RESULTCLASS=Win32_DiskPartition";

			using var searcher = new ManagementObjectSearcher(queryString);
			foreach (ManagementObject partition in searcher.Get())
			{
				using var driveSearcher = new ManagementObjectSearcher(
					$"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition RESULTCLASS=Win32_DiskDrive");

				foreach (ManagementObject drive in driveSearcher.Get())
				{
					if (drive.Properties["SerialNumber"]?.Value is string serialNumber && !string.IsNullOrEmpty(serialNumber))
					{
						return serialNumber;
					}
				}
			}

			return null;
		}
		catch
		{
			return null;
		}
	}

	#endregion
}