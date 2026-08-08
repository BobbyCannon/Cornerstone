#region References

using Microsoft.Win32;

#endregion

namespace Cornerstone.Platforms.Windows.Internal;

internal class DeviceModelRegistryComponent : RegistryComponent
{
	#region Constructors

	public DeviceModelRegistryComponent()
		: base(RegistryView.Registry64, RegistryHive.LocalMachine,
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation", "Model")
	{
		// HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation
	}

	#endregion
}