#region References

using Microsoft.Win32;

#endregion

namespace Cornerstone.Platforms.Windows.Internal;

internal class DeviceManufacturerRegistryComponent : RegistryComponent
{
	#region Constructors

	public DeviceManufacturerRegistryComponent()
		: base(RegistryView.Registry64, RegistryHive.LocalMachine,
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation", "Manufacturer")
	{
		// HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation
	}

	#endregion
}