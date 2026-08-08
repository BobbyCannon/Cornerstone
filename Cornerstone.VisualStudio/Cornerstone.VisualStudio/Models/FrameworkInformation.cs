#region References

using System;

#endregion

namespace Cornerstone.VisualStudio.Models;

internal static class FrameworkInformation
{
	#region Methods

	public static bool IsNetCoreApp(string targetFrameworkIdentifier)
	{
		return string.Equals(targetFrameworkIdentifier, ".NETCoreApp", StringComparison.Ordinal);
	}

	public static bool IsNetFramework(string targetFrameworkIdentifier)
	{
		return string.Equals(targetFrameworkIdentifier, ".NETFramework", StringComparison.Ordinal);
	}

	public static bool IsNetStandard(string targetFrameworkIdentifier)
	{
		return string.Equals(targetFrameworkIdentifier, ".NETStandard", StringComparison.Ordinal);
	}

	#endregion
}