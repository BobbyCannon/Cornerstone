#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Data.Bytes;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Runtime;

/// <inheritdoc cref="IRuntimeInformation" />
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class RuntimeInformationData
	: CornerstoneObject<RuntimeInformationData>,
		IUpdateable<IRuntimeInformation>,
		IRuntimeInformation
{
	#region Properties

	public partial Bitness ApplicationBitness { get; set; }
	public partial string ApplicationDataLocation { get; set; }
	public partial string ApplicationFileName { get; set; }
	public partial string ApplicationFilePath { get; set; }
	public partial bool ApplicationIsDevelopmentBuild { get; set; }
	public partial bool ApplicationIsElevated { get; set; }
	public partial bool ApplicationIsLoaded { get; set; }
	public partial bool ApplicationIsNativeBuild { get; set; }
	public partial bool ApplicationIsReadyToRunBuild { get; set; }
	public partial bool ApplicationIsShuttingDown { get; set; }
	public partial string ApplicationLocation { get; set; }
	public partial string ApplicationName { get; set; }
	public partial TimeSpan ApplicationStartup { get; set; }
	public partial Version ApplicationVersion { get; set; }
	public partial Version CornerstoneRuntimeVersion { get; set; }
	public int Count => Keys.Count();
	public partial int DeviceDisplayRefreshRate { get; set; }
	public partial Size DeviceDisplaySize { get; set; }
	public partial string DeviceId { get; set; }
	public partial string DeviceManufacturer { get; set; }
	public partial ByteSize DeviceMemory { get; set; }
	public partial string DeviceModel { get; set; }
	public partial string DeviceName { get; set; }
	public partial DevicePlatform DevicePlatform { get; set; }
	public partial Bitness DevicePlatformBitness { get; set; }
	public partial Version DevicePlatformVersion { get; set; }
	public partial DeviceType DeviceType { get; set; }
	public partial Version DotNetRuntimeVersion { get; set; }

	public object this[string key]
	{
		get => SourceReflector.GetSourceType<RuntimeInformationData>()!.GetProperty(key).GetValue(this);
		set => throw new NotSupportedException();
	}

	public IEnumerable<string> Keys => this.Select(x => x.Key);
	public IEnumerable<object> Values => this.Select(x => x.Value);

	#endregion

	#region Methods

	public void CompleteStartup()
	{
	}

	public bool ContainsKey(string key)
	{
		return SourceReflector.GetSourceType<RuntimeInformationData>().GetProperty(key) != null;
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		var ignore = new List<string> { nameof(Count), nameof(Keys), nameof(Values) };
		var properties = SourceReflector
			.GetSourceType<RuntimeInformationData>()
			.GetProperties()
			.Where(x =>
				!ignore.Contains(x.Name)
				&& !x.IsIndexer
			)
			.ToList();

		foreach (var property in properties)
		{
			yield return new KeyValuePair<string, object>(property.Name, property.GetValue(this));
		}
	}

	/// <summary>
	/// Return an IRuntimeInformation sample.
	/// </summary>
	/// <returns> The sample data. </returns>
	public static RuntimeInformationData GetSample()
	{
		return new RuntimeInformationData
		{
			ApplicationBitness = Bitness.X86,
			ApplicationDataLocation = "C:\\Users\\Public\\Documents",
			ApplicationFileName = "Sample.exe",
			ApplicationFilePath = "C:\\Users\\Public\\Documents\\Sample.exe",
			ApplicationIsDevelopmentBuild = false,
			ApplicationIsNativeBuild = false,
			ApplicationIsReadyToRunBuild = false,
			ApplicationIsElevated = true,
			ApplicationLocation = "C:\\Users\\Public\\Documents\\",
			ApplicationName = "Sample",
			ApplicationVersion = new Version(2, 16, 1, 109),
			CornerstoneRuntimeVersion = new Version(12, 0, 999),
			DeviceDisplayRefreshRate = 60,
			DeviceDisplaySize = new Size(1920, 1280),
			DeviceId = "WPGR602V4CZBT6BM82BPNYXMM9N8T0FK1K3G4KR3BXGB97AKYR23",
			DeviceManufacturer = "Dell",
			DeviceMemory = ByteSize.FromGigabytes(64),
			DeviceModel = "X-Model-Y",
			DeviceName = "Sample-RIG",
			DevicePlatform = DevicePlatform.Windows,
			DevicePlatformBitness = Bitness.X64,
			DevicePlatformVersion = new Version(10, 0, 26100, 0),
			DeviceType = DeviceType.Desktop,
			DotNetRuntimeVersion = new Version(9, 8, 7)
		};
	}

	public void Shutdown()
	{
		ApplicationIsShuttingDown = true;
	}

	public void StartShutdown()
	{
	}

	public bool TryGetValue(string key, out object value)
	{
		var property = SourceReflector.GetSourceType<RuntimeInformationData>().GetProperty(key);
		if (property == null)
		{
			value = null;
			return false;
		}

		value = property.GetValue(this);
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}