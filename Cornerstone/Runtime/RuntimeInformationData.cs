#region References

using System;

#endregion

namespace Cornerstone.Runtime;

/// <inheritdoc cref="IRuntimeInformation" />
public struct RuntimeInformationData : IRuntimeInformation
{
	#region Properties

	/// <inheritdoc />
	public Bitness ApplicationBitness { get; set; }

	/// <inheritdoc />
	public string ApplicationDataLocation { get; set; }

	/// <inheritdoc />
	public string ApplicationFileName { get; set; }

	/// <inheritdoc />
	public string ApplicationFilePath { get; set; }

	/// <inheritdoc />
	public bool ApplicationIsDevelopmentBuild { get; set; }

	/// <inheritdoc />
	public bool ApplicationIsElevated { get; set; }

	/// <inheritdoc />
	public string ApplicationLocation { get; set; }

	/// <inheritdoc />
	public string ApplicationName { get; set; }

	/// <inheritdoc />
	public Version ApplicationVersion { get; set; }

	/// <inheritdoc />
	public string DeviceId { get; set; }

	/// <inheritdoc />
	public string DeviceManufacturer { get; set; }

	/// <inheritdoc />
	public string DeviceModel { get; set; }

	/// <inheritdoc />
	public string DeviceName { get; set; }

	/// <inheritdoc />
	public DevicePlatform DevicePlatform { get; set; }

	/// <inheritdoc />
	public Bitness DevicePlatformBitness { get; set; }

	/// <inheritdoc />
	public Version DevicePlatformVersion { get; set; }

	/// <inheritdoc />
	public DeviceType DeviceType { get; set; }

	/// <inheritdoc />
	public Version DotNetRuntimeVersion { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Return an IRuntimeInformation sample.
	/// </summary>
	/// <returns> The sample data. </returns>
	public static IRuntimeInformation GetSample()
	{
		return new RuntimeInformationData
		{
			ApplicationBitness = Bitness.X86,
			ApplicationDataLocation = "",
			ApplicationIsDevelopmentBuild = true,
			ApplicationIsElevated = true,
			ApplicationLocation = "",
			ApplicationName = "Cornerstone",
			ApplicationVersion = new Version(1, 2, 3, 4),
			DeviceId = "DEV-123",
			DeviceManufacturer = "",
			DeviceModel = "",
			DeviceName = "",
			DevicePlatform = DevicePlatform.Windows,
			DevicePlatformBitness = Bitness.X64,
			DevicePlatformVersion = new Version(9, 8, 7, 6),
			DeviceType = DeviceType.Desktop,
			DotNetRuntimeVersion = new Version(8, 0, 0, 0)
		};
	}

	#endregion
}