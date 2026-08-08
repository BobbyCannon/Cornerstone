#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for assembly.
/// </summary>
public static class AssemblyExtensions
{
	#region Methods

	/// <summary>
	/// Gets the directory the assembly file exists in.
	/// </summary>
	/// <param name="assembly"> The assembly to be tested. </param>
	/// <returns> The directory info for the assembly. </returns>
	public static DirectoryInfo GetAssemblyDirectory(this Assembly assembly)
	{
		return new DirectoryInfo(GetAssemblyPath(assembly));
	}

	/// <summary>
	/// Gets the directory the assembly file exists in.
	/// </summary>
	/// <param name="assembly"> The assembly to be tested. </param>
	/// <returns> The directory path for the assembly. </returns>
	public static string GetAssemblyPath(this Assembly assembly)
	{
		var response = AppContext.BaseDirectory;

		if (response.StartsWith("file:\\"))
		{
			response = response.Substring(6);
		}

		return response;
	}

	/// <summary>
	/// Returns true if the assembly is a debug build.
	/// </summary>
	/// <param name="assembly"> The assembly to check. </param>
	/// <returns> True if the assembly is a debug build otherwise false. </returns>
	public static bool IsAssemblyDebugBuild(this Assembly assembly)
	{
		return assembly
			.GetCustomAttributes(false)
			.OfType<DebuggableAttribute>()
			.Any(x => x.IsJITTrackingEnabled
				|| x.IsJITOptimizerDisabled
				|| x.DebuggingFlags.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations)
			);
	}

	/// <summary>
	/// Read the embedded binary file from the assembly.
	/// </summary>
	/// <param name="assembly"> The assembly to read from. </param>
	/// <param name="path"> The path of the resource to read. </param>
	/// <returns> The value that was read. </returns>
	public static byte[] ReadEmbeddedBinary(this Assembly assembly, string path)
	{
		using var stream = assembly.GetManifestResourceStream(path);

		if (stream == null)
		{
			throw new Exception("Embedded file not found.");
		}

		return stream.ReadByteArray();
	}

	/// <summary>
	/// Read the embedded text file from the assembly.
	/// </summary>
	/// <param name="assembly"> The assembly to read from. </param>
	/// <param name="path"> The path of the resource to read. </param>
	/// <returns> The value that was read. </returns>
	public static string ReadEmbeddedText(this Assembly assembly, string path)
	{
		using var stream = assembly.GetManifestResourceStream(path);

		if (stream == null)
		{
			throw new Exception("Embedded file not found.");
		}

		return stream.ReadString();
	}

	#endregion
}