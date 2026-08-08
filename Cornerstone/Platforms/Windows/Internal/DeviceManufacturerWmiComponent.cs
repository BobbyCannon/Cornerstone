namespace Cornerstone.Platforms.Windows.Internal;

internal class DeviceManufacturerWmiComponent : WmiComponent
{
	#region Constructors

	public DeviceManufacturerWmiComponent()
		: base("Win32_ComputerSystem", "Manufacturer")
	{
	}

	#endregion
}