#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Cornerstone.Data.Bytes;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Runtime;

/// <inheritdoc cref="IRuntimeInformation" />
[SourceReflection]
public struct RuntimeInformationData : IRuntimeInformation
{
	#region Properties

	public Bitness ApplicationBitness { get; set; }
	public string ApplicationDataLocation { get; set; }
	public string ApplicationFileName { get; set; }
	public string ApplicationFilePath { get; set; }
	public bool ApplicationIsDevelopmentBuild { get; set; }
	public bool ApplicationIsElevated { get; set; }
	public bool ApplicationIsLoaded { get; set; }
	public bool ApplicationIsNativeBuild { get; set; }
	public bool ApplicationIsShuttingDown { get; set; }
	public string ApplicationLocation { get; set; }
	public string ApplicationName { get; set; }
	public TimeSpan ApplicationStartup { get; set; }
	public Version ApplicationVersion { get; set; }
	public Version AvaloniaRuntimeVersion { get; set; }
	public int Count => Keys.Count();
	public int DeviceDisplayRefreshRate { get; set; }
	public Size DeviceDisplaySize { get; set; }
	public string DeviceId { get; set; }
	public string DeviceManufacturer { get; set; }
	public ByteSize DeviceMemory { get; set; }
	public string DeviceModel { get; set; }
	public string DeviceName { get; set; }
	public DevicePlatform DevicePlatform { get; set; }
	public Bitness DevicePlatformBitness { get; set; }
	public Version DevicePlatformVersion { get; set; }
	public DeviceType DeviceType { get; set; }
	public Version DotNetRuntimeVersion { get; set; }

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
			ApplicationIsElevated = true,
			ApplicationLocation = "C:\\Users\\Public\\Documents\\",
			ApplicationName = "Sample",
			ApplicationVersion = new Version(2, 16, 1, 109),
			AvaloniaRuntimeVersion = new Version(12, 0, 999),
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