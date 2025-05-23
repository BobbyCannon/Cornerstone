﻿#region References

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
	#region Constructors

	static RuntimeInformationExtensions()
	{
		Sample = RuntimeInformationData.GetSample();
	}

	#endregion

	#region Properties

	public static IRuntimeInformation Sample { get; }

	#endregion

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

	/// <summary>
	/// Determine if the platform is a desktop platform.
	/// </summary>
	/// <returns> True if the platform is a desktop otherwise false. </returns>
	public static bool IsBrowser(this IRuntimeInformation runtimeInformation)
	{
		return runtimeInformation.DevicePlatform is DevicePlatform.Browser;
	}

	/// <summary>
	/// Determine if the platform is a desktop platform.
	/// </summary>
	/// <returns> True if the platform is a desktop otherwise false. </returns>
	public static bool IsDesktop(this IRuntimeInformation runtimeInformation)
	{
		return runtimeInformation.DevicePlatform
				is DevicePlatform.Windows
				or DevicePlatform.Linux
				or DevicePlatform.MacOS
			&& runtimeInformation.DeviceType is DeviceType.Desktop;
	}

	/// <summary>
	/// Determine if the platform is a mobile platform.
	/// </summary>
	/// <returns> True if the platform is a mobile otherwise false. </returns>
	public static bool IsMobile(this IRuntimeInformation runtimeInformation)
	{
		return runtimeInformation.DevicePlatform
				is DevicePlatform.Android
				or DevicePlatform.IOS
			&& runtimeInformation.DeviceType is DeviceType.Phone;
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