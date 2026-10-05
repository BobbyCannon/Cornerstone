#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Represents the bitness of a platform, application, etc
/// </summary>
public enum Bitness
{
	/// <summary>
	/// Unknown
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Intel/AMD 32-bit (x86).
	/// </summary>
	[Display(Name = "32 bit", ShortName = "x86")]
	X86 = 1,

	/// <summary>
	/// Intel/AMD 64-bit (x64).
	/// </summary>
	[Display(Name = "64 bit", ShortName = "x64")]
	X64 = 2,

	/// <summary>
	/// ARM 32-bit.
	/// </summary>
	[Display(Name = "ARM 32 bit", ShortName = "arm")]
	Arm32 = 3,

	/// <summary>
	/// ARM 64-bit.
	/// </summary>
	[Display(Name = "ARM 64 bit", ShortName = "arm64")]
	Arm64 = 4,

	/// <summary>
	/// WebAssembly 32-bit.
	/// </summary>
	[Display(Name = "Wasm 32 bit", ShortName = "wasm32")]
	Wasm32 = 5,

	/// <summary>
	/// WebAssembly 64-bit.
	/// </summary>
	[Display(Name = "Wasm 64 bit", ShortName = "wasm64")]
	Wasm64 = 6
}