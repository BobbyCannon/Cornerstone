#region References

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Extensions for RuntimeInformation.
/// </summary>
public static class RuntimeInformationExtensions
{
	#region Methods

	public static string AddOrUpdateEmbeddedFile(this IRuntimeInformation runtimeInformation,
		Assembly assembly, string namespacePath, string fileName)
	{
		var filePath = Path.Combine(runtimeInformation.ApplicationDataLocation, fileName);
		var data = assembly.ReadEmbeddedBinary($"{namespacePath}.{fileName}");
		UpdateFile(filePath, data);
		return filePath;
	}

	public static string AddOrUpdateEmbeddedText(this IRuntimeInformation runtimeInformation,
		Assembly assembly, string namespacePath, string fileName)
	{
		return AddOrUpdateEmbeddedText(runtimeInformation, assembly, namespacePath, fileName, x => x);
	}

	public static string AddOrUpdateEmbeddedText(this IRuntimeInformation runtimeInformation,
		Assembly assembly, string namespacePath, string fileName, Func<string, string> update)
	{
		var filePath = Path.Combine(runtimeInformation.ApplicationDataLocation, fileName);
		var data = assembly.ReadEmbeddedText($"{namespacePath}.{fileName}");
		UpdateFile(filePath, update.Invoke(data));
		return filePath;
	}

	/// <summary>
	/// Get the information as a data structure.
	/// </summary>
	/// <returns> The information. </returns>
	public static IRuntimeInformation Copy(this IRuntimeInformation runtimeInformation)
	{
		var response = new RuntimeInformationData
		{
			ApplicationBitness = runtimeInformation.ApplicationBitness,
			ApplicationDataLocation = runtimeInformation.ApplicationDataLocation,
			ApplicationFileName = runtimeInformation.ApplicationFileName,
			ApplicationFilePath = runtimeInformation.ApplicationFilePath,
			ApplicationLocation = runtimeInformation.ApplicationLocation,
			ApplicationIsDevelopmentBuild = runtimeInformation.ApplicationIsDevelopmentBuild,
			ApplicationIsElevated = runtimeInformation.ApplicationIsElevated,
			ApplicationName = runtimeInformation.ApplicationName,
			ApplicationVersion = runtimeInformation.ApplicationVersion,
			DeviceId = runtimeInformation.DeviceId,
			DeviceManufacturer = runtimeInformation.DeviceManufacturer,
			DeviceModel = runtimeInformation.DeviceModel,
			DeviceName = runtimeInformation.DeviceName,
			DevicePlatform = runtimeInformation.DevicePlatform,
			DevicePlatformBitness = runtimeInformation.DevicePlatformBitness,
			DevicePlatformVersion = runtimeInformation.DevicePlatformVersion,
			DeviceType = runtimeInformation.DeviceType,
			DotNetRuntimeVersion = runtimeInformation.DotNetRuntimeVersion
		};

		return response;
	}

	private static void UpdateFile(string filePath, byte[] data)
	{
		var info = new FileInfo(filePath);
		if (info.Exists && (info.Length == data.Length))
		{
			return;
		}

		File.WriteAllBytes(filePath, data);
	}

	private static void UpdateFile(string filePath, string data)
	{
		var info = new FileInfo(filePath);
		if (info.Exists && (info.Length == data.Length))
		{
			return;
		}

		File.WriteAllText(filePath, data, Encoding.UTF8);
	}

	#endregion
}