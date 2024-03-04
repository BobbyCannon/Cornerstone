﻿#region References

using System.Runtime.Versioning;

#endregion

namespace Cornerstone.Internal.Windows;

#if (!NETSTANDARD)
[SupportedOSPlatform("windows")]
#endif
internal class DeviceModelWmiComponent : WmiComponent
{
	#region Constructors

	public DeviceModelWmiComponent()
		: base("Win32_ComputerSystem", "Model")
	{
	}

	#endregion
}