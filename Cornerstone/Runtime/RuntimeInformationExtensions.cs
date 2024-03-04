#region References

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Extensions for RuntimeInformation.
/// </summary>
public static class RuntimeInformationExtensions
{
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

	#endregion
}