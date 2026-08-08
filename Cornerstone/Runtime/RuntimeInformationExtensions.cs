#region References

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
			ApplicationIsDevelopmentBuild = runtimeInformation.ApplicationIsDevelopmentBuild,
			ApplicationIsElevated = runtimeInformation.ApplicationIsElevated,
			ApplicationIsLoaded = runtimeInformation.ApplicationIsLoaded,
			ApplicationIsNativeBuild = runtimeInformation.ApplicationIsNativeBuild,
			ApplicationIsShuttingDown = runtimeInformation.ApplicationIsShuttingDown,
			ApplicationLocation = runtimeInformation.ApplicationLocation,
			ApplicationName = runtimeInformation.ApplicationName,
			ApplicationStartup = runtimeInformation.ApplicationStartup,
			ApplicationVersion = runtimeInformation.ApplicationVersion,
			AvaloniaRuntimeVersion = runtimeInformation.AvaloniaRuntimeVersion,
			DeviceDisplayRefreshRate = runtimeInformation.DeviceDisplayRefreshRate,
			DeviceDisplaySize = runtimeInformation.DeviceDisplaySize,
			DeviceId = runtimeInformation.DeviceId,
			DeviceManufacturer = runtimeInformation.DeviceManufacturer,
			DeviceModel = runtimeInformation.DeviceModel,
			DeviceMemory = runtimeInformation.DeviceMemory,
			DeviceName = runtimeInformation.DeviceName,
			DevicePlatform = runtimeInformation.DevicePlatform,
			DevicePlatformBitness = runtimeInformation.DevicePlatformBitness,
			DevicePlatformVersion = runtimeInformation.DevicePlatformVersion,
			DotNetRuntimeVersion = runtimeInformation.DotNetRuntimeVersion,
			DeviceType = runtimeInformation.DeviceType
		};

		return response;
	}

	#endregion
}