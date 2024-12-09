#region References

using System;
using Cornerstone.Parsers.VisualStudio;

#endregion



namespace Cornerstone.Attributes;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class)]
public class TargetFrameworkAttribute : CornerstoneAttribute
{
	#region Properties

	public bool IsNetFramework { get; set; }

	public bool IsNetStandard { get; set; }

	public bool IsUniversal { get; set; }

	public string Moniker { get; set; }

	public TargetFrameworkType NetStandard { get; set; }

	/// <summary>
	/// The operating system portion of the moniker.
	/// Ex. net9.0-windows -> windows, net9.0-ios -> ios, net9.0 -> ""
	/// </summary>
	/// <remarks>
	/// We don't track all OperatingSystemVersions because they'd be too many.
	/// </remarks>
	public string OperatingSystem { get; set; }

	public TargetFrameworkType Parent { get; set; }

	public TargetFrameworkType Platform { get; set; }

	public string Version { get; set; }

	#endregion
}