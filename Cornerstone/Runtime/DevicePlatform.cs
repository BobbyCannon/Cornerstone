#region References

using System;
using System.ComponentModel.DataAnnotations;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Represents the platform of device
/// </summary>
[Flags]
[SourceReflection]
public enum DevicePlatform
{
	/// <summary>
	/// Unknown
	/// </summary>
	Unknown = 0b0,

	/// <summary>
	/// Windows
	/// </summary>
	Windows = 0b0001,

	/// <summary>
	/// Android
	/// </summary>
	Android = 0b0010,

	/// <summary>
	/// iOS
	/// </summary>
	[Display(Name = "iOS")]
	IOS = 0b0100,

	/// <summary>
	/// Mac OS
	/// </summary>
	MacOS = 0b1000,

	/// <summary>
	/// Linux
	/// </summary>
	Linux = 0b0001_0000,

	/// <summary>
	/// Browser
	/// </summary>
	Browser = 0b0010_0000,

	/// <summary>
	/// All platforms
	/// </summary>
	All = 0b0011_1111
}