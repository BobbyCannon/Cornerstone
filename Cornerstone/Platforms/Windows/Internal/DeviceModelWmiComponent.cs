namespace Cornerstone.Platforms.Windows.Internal;

internal class DeviceModelWmiComponent : WmiComponent
{
	#region Constructors

	public DeviceModelWmiComponent()
		: base("Win32_ComputerSystem", "Model")
	{
	}

	#endregion
}