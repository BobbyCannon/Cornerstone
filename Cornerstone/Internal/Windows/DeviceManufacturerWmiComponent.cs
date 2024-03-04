﻿#region References

using System.Runtime.Versioning;

#endregion

namespace Cornerstone.Internal.Windows;

#if (!NETSTANDARD)
[SupportedOSPlatform("windows")]
#endif
internal class DeviceManufacturerWmiComponent : WmiComponent
{
	#region Constructors

	public DeviceManufacturerWmiComponent()
		: base("Win32_ComputerSystem", "Manufacturer")
	{
	}

	#endregion
}