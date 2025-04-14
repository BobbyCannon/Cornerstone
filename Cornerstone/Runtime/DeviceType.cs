#region References

using System;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Represents the type of device
/// </summary>
[Flags]
public enum DeviceType
{
	/// <summary>
	/// Unknown
	/// </summary>
	Unknown = 0b0000,

	/// <summary>
	/// Desktop
	/// </summary>
	Desktop = 0b0001,

	/// <summary>
	/// Phone
	/// </summary>
	Phone = 0b0010,

	/// <summary>
	/// Watch
	/// </summary>
	Watch = 0b0100,

	/// <summary>
	/// Tablet
	/// </summary>
	Tablet = 0b1000,

	/// <summary>
	/// TV
	/// </summary>
	TV = 0b0001_0000,

	/// <summary>
	/// Browser
	/// </summary>
	Browser = 0b0010_0000
}